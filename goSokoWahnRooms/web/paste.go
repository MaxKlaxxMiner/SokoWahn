// Universelles Strg+V wie in brute (POST /api/paste): der Inhalt der
// Zwischenablage wird klassifiziert und entsprechend behandelt -
//   - Level-Nummer oder game-sokoban.com-URL: Level aus dem levelcache bzw.
//     dem Web laden und das Spielfeld ersetzen (max moves = bekannter Rekord)
//   - Levelnotation (erstes echtes Zeichen '#'): als neues Spielfeld laden,
//     max moves wird geleert
//   - reine LURD-Zugfolge (Groß/Klein): als Lösung gegen das aktuelle Feld
//     simulieren; passt sie, setzt die GUI max moves auf die Zuglänge
package web

import (
	"encoding/json"
	"fmt"
	"net/http"
	"strings"

	"goSokoWahnRooms/rooms"
	"goSokoWahnRooms/soko"
	"goSokoWahnRooms/tools"
	"goSokoWahnRooms/weblevel"
)

func (s *Server) handlePaste(w http.ResponseWriter, r *http.Request) {
	var req struct {
		Text string `json:"text"`
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "ungültige Anfrage: "+err.Error())
		return
	}
	trimmed := strings.TrimSpace(req.Text)
	switch {
	case trimmed == "":
		writeError(w, http.StatusBadRequest, "die Zwischenablage ist leer")
	case weblevel.IsWebInput(trimmed):
		s.pasteLevel(w, trimmed, true)
	case trimmed[0] == '#':
		s.pasteLevel(w, req.Text, false)
	case isLURD(trimmed):
		s.pasteSolution(w, trimmed)
	default:
		writeError(w, http.StatusBadRequest,
			"weder Levelnotation (#...), Level-Nummer/URL noch LURD-Lösung erkannt")
	}
}

// meldet, ob der Text nur aus LURD-Zeichen (plus Leerraum) besteht
func isLURD(text string) bool {
	for _, c := range text {
		switch c {
		case 'l', 'u', 'r', 'd', 'L', 'U', 'R', 'D', ' ', '\t', '\r', '\n':
		default:
			return false
		}
	}
	return true
}

// LURD-Lösung: synchron gegen das aktuelle Spielfeld simulieren; die GUI
// setzt bei Erfolg das max-moves-Feld auf die Zuglänge
func (s *Server) pasteSolution(w http.ResponseWriter, text string) {
	// Leerraum raus (Lösungen kommen oft mehrzeilig aus Foren/Dateien)
	lurd := strings.Map(func(c rune) rune {
		if c == ' ' || c == '\t' || c == '\r' || c == '\n' {
			return -1
		}
		return c
	}, text)

	s.mu.RLock()
	field := s.network.Field
	s.mu.RUnlock()
	if err := field.CheckSolution(lurd); err != nil {
		writeError(w, http.StatusBadRequest, "Lösung passt nicht: "+err.Error())
		return
	}
	writeJSON(w, map[string]any{"kind": "solution", "moves": len(lurd)})
}

// Level laden (Web/Nummer oder Levelnotation) als Hintergrund-Job: erst nach
// fehlerfreiem Aufbau wird das Netzwerk getauscht, sonst bleibt alles stehen
func (s *Server) pasteLevel(w http.ResponseWriter, input string, isWeb bool) {
	started := s.runJob("load level...", func(info rooms.ProgressFunc) (string, error) {
		sokoMap, title := input, "Level aus der Zwischenablage"
		bestMoves := 0
		if isWeb {
			info("lade level von game-sokoban.com...", nil)
			level, webInfo, err := weblevel.Load(input)
			if err != nil {
				return "", err
			}
			sokoMap = level
			title = fmt.Sprintf("Level %s: %s - %s (%s)", webInfo.ID, webInfo.Catalog, webInfo.Name, webInfo.Number)
			bestMoves = webInfo.BestMoves
		}
		field, err := soko.Parse(sokoMap)
		if err != nil {
			return "", err
		}
		info("baue netzwerk...", nil)
		network, err := rooms.NewNetwork(field)
		if err != nil {
			return "", err
		}
		s.swapNetwork(network, title, bestMoves) // der Job hält die Schreibsperre
		result := fmt.Sprintf("Level geladen: %s (%s Räume)", title, tools.FormatInt(len(network.Rooms)))
		if bestMoves > 0 {
			result += fmt.Sprintf(" - Rekord %s Züge", tools.FormatInt(bestMoves))
		}
		return result, nil
	})
	if !started {
		writeError(w, http.StatusConflict, "es läuft bereits eine Rechnung")
		return
	}
	writeJSON(w, map[string]any{"kind": "level", "started": true})
}
