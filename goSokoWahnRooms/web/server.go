// Paket web: HTTP-Server mit JSON-API für die Debug-GUI (M2, siehe docs/konzept.md).
// Alle Listen-Endpunkte liefern grundsätzlich seitenweise (offset/limit), nie
// Komplettlisten - Zustands-/Variantenlisten großer Räume können Millionen
// Einträge haben. Das Frontend (webui) wird als statisches Bundle eingebettet.
package web

import (
	"embed"
	"io/fs"
	"net/http"
	"sync"

	"goSokoWahnRooms/rooms"
)

//go:embed static
var staticFS embed.FS

// Server hält das aktuelle Netzwerk und beantwortet die API-Anfragen.
// Das Netzwerk ist austauschbar (SetNetwork), damit die GUI später neue
// Levels laden kann, ohne den Server neu zu starten.
type Server struct {
	mu        sync.RWMutex
	network   *rooms.Network
	title     string
	bestMoves int    // bekannter Züge-Rekord des Levels (0 = unbekannt), füllt das max-moves-Feld
	levelSeq  uint64 // wächst bei jedem Level-Wechsel - die GUI erkennt daran den Feld-Austausch
	solution  *solutionJSON // Lösung des letzten Solver-Laufs (überlebt Merges, nicht den Level-Wechsel)
	mux       *http.ServeMux
	progress  progressState

	// Steuer-Kanal der laufenden Solver-Sitzung (nil = keine); eigener Mutex,
	// damit Kommandos nicht hinter den Netzwerk-Sperren warten
	solveMu  sync.Mutex
	solveCmd chan solveCommand
}

func New(network *rooms.Network, title string) *Server {
	s := &Server{network: network, title: title}

	s.mux = http.NewServeMux()
	s.mux.HandleFunc("GET /api/summary", s.read(s.handleSummary))
	s.mux.HandleFunc("GET /api/field", s.read(s.handleField))
	s.mux.HandleFunc("GET /api/map", s.read(s.handleMap))
	s.mux.HandleFunc("GET /api/rooms", s.read(s.handleRooms))
	s.mux.HandleFunc("GET /api/rooms/{index}", s.read(s.handleRoom))
	s.mux.HandleFunc("GET /api/rooms/{index}/states", s.read(s.handleStates))
	s.mux.HandleFunc("GET /api/rooms/{index}/variants", s.read(s.handleVariants))
	// Merge/Optimize starten Hintergrund-Jobs (die Goroutine nimmt selbst die
	// Schreibsperre); Progress/Stop laufen bewusst OHNE die Netzwerk-Sperren,
	// damit sie während der Rechnung erreichbar bleiben
	s.mux.HandleFunc("POST /api/merge", s.handleMerge)
	s.mux.HandleFunc("POST /api/optimize", s.handleOptimize)
	s.mux.HandleFunc("POST /api/reset", s.handleReset)
	// universelles Strg+V (Level-URL/-Nummer, Levelnotation oder LURD-Lösung);
	// nimmt die Sperren selbst (Level-Laden läuft als Hintergrund-Job)
	s.mux.HandleFunc("POST /api/paste", s.handlePaste)
	// Snapshots: Liste liest nur Datei-Header (Lesesperre reicht), Save/Load
	// laufen als Hintergrund-Jobs, Delete ist eine reine Datei-Operation
	s.mux.HandleFunc("GET /api/snapshots", s.read(s.handleSnapshots))
	s.mux.HandleFunc("POST /api/snapshots", s.handleSnapshotSave)
	s.mux.HandleFunc("POST /api/snapshots/load", s.handleSnapshotLoad)
	s.mux.HandleFunc("POST /api/snapshots/delete", s.handleSnapshotDelete)
	// Solver-Sitzung (M7): Start als Hintergrund-Job unter der Lesesperre,
	// Steuerung (Bulk/Auto) und Lösungs-Abfrage wie in brute
	s.mux.HandleFunc("POST /api/solve", s.handleSolve)
	s.mux.HandleFunc("POST /api/solve/cmd", s.handleSolveCmd)
	s.mux.HandleFunc("GET /api/solution", s.read(s.handleSolution))
	s.mux.HandleFunc("GET /api/progress", s.handleProgress)
	s.mux.HandleFunc("POST /api/stop", s.handleStop)
	s.mux.HandleFunc("POST /api/validate", s.read(s.handleValidate))

	static, err := fs.Sub(staticFS, "static")
	if err != nil {
		panic(err) // kann nur bei kaputtem embed passieren
	}
	s.mux.Handle("GET /", http.FileServerFS(static))

	return s
}

func (s *Server) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	s.mux.ServeHTTP(w, r)
}

// tauscht das Netzwerk aus (für das Level-Laden aus der GUI)
func (s *Server) SetNetwork(network *rooms.Network, title string) {
	s.mu.Lock()
	defer s.mu.Unlock()
	s.swapNetwork(network, title, 0)
}

// SetBestMoves setzt den bekannten Züge-Rekord des aktuellen Levels
// (z.B. beim Start mit einem Web-Level, siehe main.go)
func (s *Server) SetBestMoves(moves int) {
	s.mu.Lock()
	defer s.mu.Unlock()
	s.bestMoves = moves
}

// ersetzt das Netzwerk samt Metadaten; der Aufrufer hält die Schreibsperre
func (s *Server) swapNetwork(network *rooms.Network, title string, bestMoves int) {
	s.network = network
	s.title = title
	s.bestMoves = bestMoves
	s.solution = nil // die Lösung gehört zum alten Level
	s.levelSeq++
}

// kapselt einen Handler unter der Lesesperre: das Netzwerk bleibt für die ganze
// Anfrage stabil, während mutierende Aktionen (Merge & Co.) exklusiv laufen
func (s *Server) read(h http.HandlerFunc) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		s.mu.RLock()
		defer s.mu.RUnlock()
		h(w, r)
	}
}

// kapselt einen mutierenden Handler unter der Schreibsperre
func (s *Server) write(h http.HandlerFunc) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		s.mu.Lock()
		defer s.mu.Unlock()
		h(w, r)
	}
}

// liefert Netzwerk und Titel; die Aufrufer laufen bereits unter der
// read-/write-Sperre der Anfrage
func (s *Server) snapshot() (*rooms.Network, string) {
	return s.network, s.title
}
