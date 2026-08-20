package web

import (
	"encoding/json"
	"fmt"
	"net/http"
	"time"

	"goSokoWahnRooms/rooms"
	"goSokoWahnRooms/tools"
)

// Solver-Sitzung: die Suche läuft als Hintergrund-Job unter der LESE-Sperre
// (der Solver liest das Netzwerk nur; Merge & Co. sind über den Busy-Status
// gesperrt, die GUI bleibt derweil voll bedienbar). Gesteuert wird wie in
// brute: die Sitzung startet PAUSIERT, Kommandos (Bulk-Schritte, Auto an/aus)
// kommen über /api/solve/cmd, der Stop-Button beendet die Sitzung.

// Aufgaben je Auto-Häppchen: klein genug, dass Kommandos und Status-Updates
// zwischendurch drankommen, groß genug, dass der Schleifen-Overhead untergeht
const solveAutoChunk = 4096

// ein Steuer-Kommando der GUI ({bulk: n}, {auto: true/false} oder {stop: true})
type solveCommand struct {
	Bulk int   `json:"bulk"`
	Auto *bool `json:"auto"`
	Stop bool  `json:"stop"` // Sitzung beenden (Esc in der GUI; nicht der Stop-Button)
}

// die gemerkte Lösung des letzten erfolgreichen Solver-Laufs
// (überlebt Merges - die Lösung gehört zum Level, nicht zum Netzwerk-Stand;
// ein Level-Wechsel verwirft sie, siehe swapNetwork)
type solutionJSON struct {
	Moves    uint32 `json:"moves"`
	Pushes   uint32 `json:"pushes"`
	Path     string `json:"path"`
	Complete bool   `json:"complete"` // true = Optimum bewiesen (Suche lief bis zum Ende)
}

// startet eine Solver-Sitzung (pausiert); maxMoves > 0 = hartes Budget
func (s *Server) handleSolve(w http.ResponseWriter, r *http.Request) {
	var req struct {
		MaxMoves int `json:"maxMoves"`
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "ungültige Anfrage: "+err.Error())
		return
	}
	if req.MaxMoves < 0 {
		req.MaxMoves = 0
	}
	if !s.progress.begin("solve", "solve: initialisiere...") {
		writeError(w, http.StatusConflict, "es läuft bereits eine Rechnung")
		return
	}
	cmd := make(chan solveCommand, 16)
	s.solveMu.Lock()
	s.solveCmd = cmd
	s.solveMu.Unlock()
	go s.solveJob(uint32(req.MaxMoves), cmd)
	writeJSON(w, map[string]any{"started": true})
}

// nimmt ein Steuer-Kommando für die laufende Sitzung entgegen
func (s *Server) handleSolveCmd(w http.ResponseWriter, r *http.Request) {
	var req solveCommand
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "ungültige Anfrage: "+err.Error())
		return
	}
	s.solveMu.Lock()
	cmd := s.solveCmd
	s.solveMu.Unlock()
	if cmd == nil {
		writeError(w, http.StatusConflict, "kein solver aktiv")
		return
	}
	select {
	case cmd <- req:
	default: // Kanal voll (GUI hämmert schneller als die Sitzung liest) - verwerfen
	}
	writeJSON(w, map[string]any{"ok": true})
}

// liefert die gemerkte Lösung des Levels (null = keine vorhanden)
func (s *Server) handleSolution(w http.ResponseWriter, r *http.Request) {
	writeJSON(w, map[string]any{"solution": s.solution})
}

// die Hintergrund-Goroutine der Solver-Sitzung
func (s *Server) solveJob(maxMoves uint32, cmd chan solveCommand) {
	defer func() {
		s.solveMu.Lock()
		s.solveCmd = nil
		s.solveMu.Unlock()
	}()

	s.mu.RLock()
	solver, err := rooms.NewSolver(s.network, maxMoves)
	if err != nil {
		s.mu.RUnlock()
		s.progress.finish("", err)
		return
	}

	auto := false
	var lastReport time.Time
	report := func(force bool) {
		now := time.Now()
		if !force && now.Sub(lastReport) < 100*time.Millisecond {
			return
		}
		lastReport = now
		mode := "pause"
		if auto {
			mode = "auto"
		}
		// keine Raum-Markierung (nil): der Raum der zuletzt verarbeiteten
		// Aufgabe ist praktisch zufällig - gelbes Geflacker ohne Aussage
		s.progress.report(fmt.Sprintf("[%s]\n%s", mode, solver.Status()), nil)
	}

	aborted := false
	report(true)
loop:
	for !solver.Done() {
		// /api/stop als Notbremse (der Stop-Button der GUI ist beim Solven
		// deaktiviert - die Sitzung endet regulär per Esc = Stop-Kommando)
		if s.progress.stopRequested() {
			aborted = true
			break
		}
		if auto {
			select {
			case c := <-cmd:
				if c.Stop {
					aborted = true
					break loop
				}
				if c.Auto != nil {
					auto = *c.Auto
				}
				if !auto {
					report(true)
					continue
				}
			default:
			}
			solver.Step(solveAutoChunk)
			report(false)
		} else {
			// pausiert: auf Kommandos warten, zwischendurch den Stop-Wunsch prüfen
			select {
			case c := <-cmd:
				if c.Stop {
					aborted = true
					break loop
				}
				if c.Auto != nil {
					auto = *c.Auto
				}
				if c.Bulk > 0 {
					solver.Step(c.Bulk)
				}
				report(true)
			case <-time.After(200 * time.Millisecond):
			}
		}
	}
	if solver.Done() {
		report(true)
	}
	s.mu.RUnlock()

	// Ergebnis übernehmen: kurz die Schreibsperre (Busy-Status hält derweil
	// alle anderen mutierenden Jobs fern, die Lücke ist also unkritisch)
	sol := solver.Solution()
	if sol != nil {
		s.mu.Lock()
		s.solution = &solutionJSON{Moves: sol.Moves, Pushes: sol.Pushes, Path: sol.Path, Complete: solver.Done() && solver.Err() == nil}
		if s.bestMoves == 0 || int(sol.Moves) < s.bestMoves {
			s.bestMoves = int(sol.Moves) // neue verifizierte obere Schranke des Levels
		}
		s.mu.Unlock()
	}

	switch {
	case solver.Err() != nil:
		s.progress.finish("", solver.Err())
	case aborted && sol != nil:
		s.progress.finish(fmt.Sprintf("Solve beendet - beste Lösung bisher: %s Züge / %s Pushes",
			tools.FormatInt(sol.Moves), tools.FormatInt(sol.Pushes)), nil)
	case aborted:
		s.progress.finish("Solve beendet (keine Lösung gefunden)", nil)
	default:
		s.progress.finish("Solve: "+solver.ResultText(), nil)
	}
}
