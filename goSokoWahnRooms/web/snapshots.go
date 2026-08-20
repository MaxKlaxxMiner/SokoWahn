// Snapshot-Verwaltung: kompletter Netzwerk-Stand als Datei in
// temp/room-snapshots (wie die Blocker-Caches von brute im temp-Ordner,
// Dateiname mit level-abhängigem FieldCrc-Code; gzip bewusst nicht nötig).
// Die Liste zeigt je Snapshot den Effort zum Speicherzeitpunkt (aus dem
// Datei-Header, ohne Voll-Laden) und die Dateigröße.
package web

import (
	"encoding/json"
	"fmt"
	"net/http"
	"os"
	"path/filepath"
	"regexp"
	"sort"
	"strings"
	"time"

	"goSokoWahnRooms/rooms"
)

const snapshotDir = "temp/room-snapshots"

// Dateinamens-Muster: rooms_x<FieldCrc>_<Zeitstempel>.snap - der Crc bindet
// den Snapshot ans Level, der Zeitstempel sortiert die Liste chronologisch
func snapshotPrefix(n *rooms.Network) string {
	return fmt.Sprintf("rooms_x%016x_", n.Field.FieldCrc())
}

// erlaubte Snapshot-Namen (keine Pfad-Zeichen - schützt vor Ausbrüchen aus dem Ordner)
var snapshotNameRe = regexp.MustCompile(`^[A-Za-z0-9_.-]+\.snap$`)

type snapshotJSON struct {
	Name   string `json:"name"`
	Effort string `json:"effort"` // Effort-String zum Speicherzeitpunkt
	Size   int64  `json:"size"`   // Dateigröße in Bytes
}

// GET /api/snapshots: alle Snapshots des aktuellen Levels, neueste zuerst
// (wie ein Mail-Postfach; die Zeitstempel-Namen sortieren chronologisch)
func (s *Server) handleSnapshots(w http.ResponseWriter, r *http.Request) {
	n, _ := s.snapshot()
	prefix := snapshotPrefix(n)
	entries, err := os.ReadDir(snapshotDir)
	if err != nil && !os.IsNotExist(err) {
		writeError(w, http.StatusInternalServerError, err.Error())
		return
	}
	items := []snapshotJSON{}
	for _, entry := range entries {
		name := entry.Name()
		if entry.IsDir() || !strings.HasPrefix(name, prefix) || !strings.HasSuffix(name, ".snap") {
			continue
		}
		fi, err := entry.Info()
		if err != nil {
			continue
		}
		// Effort aus dem Datei-Header; kaputte Dateien bleiben gelistet
		// (mit "?"), damit sie über den Löschen-Knopf entfernbar sind
		effort := "?"
		if file, err := os.Open(filepath.Join(snapshotDir, name)); err == nil {
			if _, e, err := rooms.ReadSnapshotHeader(file); err == nil {
				effort = e
			}
			file.Close()
		}
		items = append(items, snapshotJSON{Name: name, Effort: effort, Size: fi.Size()})
	}
	sort.Slice(items, func(a, b int) bool { return items[a].Name > items[b].Name })
	writeJSON(w, map[string]any{"items": items})
}

// POST /api/snapshots: aktuellen Netzwerk-Stand als neuen Snapshot speichern
// (Hintergrund-Job wie Merge - der Stand bleibt während des Schreibens stabil)
func (s *Server) handleSnapshotSave(w http.ResponseWriter, r *http.Request) {
	started := s.runJob("snapshot", "save...", func(info rooms.ProgressFunc) (string, error) {
		n, _ := s.snapshot()
		if err := os.MkdirAll(snapshotDir, 0o755); err != nil {
			return "", err
		}
		// freier Dateiname: Zeitstempel, bei Kollision (mehrere Saves in
		// derselben Sekunde) mit laufender Nummer
		base := snapshotPrefix(n) + time.Now().Format("20060102-150405")
		path := filepath.Join(snapshotDir, base+".snap")
		for i := 2; ; i++ {
			if _, err := os.Stat(path); os.IsNotExist(err) {
				break
			}
			path = filepath.Join(snapshotDir, fmt.Sprintf("%s_%d.snap", base, i))
		}
		file, err := os.Create(path)
		if err != nil {
			return "", err
		}
		if err := n.WriteSnapshot(file, info); err != nil {
			file.Close()
			os.Remove(path) // halbe Datei nicht stehen lassen
			return "", err
		}
		if err := file.Close(); err != nil {
			os.Remove(path)
			return "", err
		}
		fi, err := os.Stat(path)
		if err != nil {
			return "", err
		}
		return fmt.Sprintf("Snapshot gespeichert (%s)", formatMB(fi.Size())), nil
	})
	if !started {
		writeError(w, http.StatusConflict, "es läuft bereits eine Rechnung")
		return
	}
	writeJSON(w, map[string]any{"started": true})
}

// POST /api/snapshots/load: gewählten Snapshot laden - ersetzt das aktuelle
// Netzwerk (Hintergrund-Job; bei jedem Fehler bleibt das alte Netzwerk stehen)
func (s *Server) handleSnapshotLoad(w http.ResponseWriter, r *http.Request) {
	name, ok := snapshotNameFromRequest(w, r)
	if !ok {
		return
	}
	started := s.runJob("snapshot", "load...", func(info rooms.ProgressFunc) (string, error) {
		n, _ := s.snapshot()
		file, err := os.Open(filepath.Join(snapshotDir, name))
		if err != nil {
			return "", err
		}
		defer file.Close()
		loaded, err := rooms.ReadSnapshot(n.Field, n.Scan, file, info)
		if err != nil {
			return "", err
		}
		s.network = loaded // der Job läuft unter der Schreibsperre
		return fmt.Sprintf("Snapshot geladen: %d Räume, Effort %s", len(loaded.Rooms), loaded.EffortString()), nil
	})
	if !started {
		writeError(w, http.StatusConflict, "es läuft bereits eine Rechnung")
		return
	}
	writeJSON(w, map[string]any{"started": true})
}

// POST /api/snapshots/delete: gewählten Snapshot löschen (reine Datei-Operation)
func (s *Server) handleSnapshotDelete(w http.ResponseWriter, r *http.Request) {
	name, ok := snapshotNameFromRequest(w, r)
	if !ok {
		return
	}
	if err := os.Remove(filepath.Join(snapshotDir, name)); err != nil {
		writeError(w, http.StatusInternalServerError, err.Error())
		return
	}
	writeJSON(w, map[string]any{"ok": true})
}

// liest und prüft den Snapshot-Namen aus dem Request-Body (false = Fehler gemeldet)
func snapshotNameFromRequest(w http.ResponseWriter, r *http.Request) (string, bool) {
	var req struct {
		Name string `json:"name"`
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "ungültige Anfrage: "+err.Error())
		return "", false
	}
	if !snapshotNameRe.MatchString(req.Name) {
		writeError(w, http.StatusBadRequest, "ungültiger Snapshot-Name")
		return "", false
	}
	return req.Name, true
}

// formatiert eine Dateigröße in MByte mit einer Nachkommastelle
func formatMB(bytes int64) string {
	return fmt.Sprintf("%.1f MB", float64(bytes)/(1024*1024))
}
