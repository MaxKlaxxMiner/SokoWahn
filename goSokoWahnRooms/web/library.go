// Raum-Bibliothek (M8): einzelne Räume als Dateien in temp/room-library -
// je Geometrie und Budget ein wiederverwendbarer Baustein (Konzept in
// rooms/library.go). Die Liste zeigt nur Räume, deren Budget zum aktuellen
// max moves passt: Budget 0 = unbedingt gültig (immer sichtbar), sonst muss
// das aktuelle max moves <= dem Datei-Budget sein (strengere Suche darf
// die Streichungen erst recht nutzen) - zu optimistisch optimierte Räume
// werden so unsichtbar statt giftig (Max' Konzept 2026-08-21).
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
	"goSokoWahnRooms/tools"
)

const libraryDir = "temp/room-library"

// Dateinamens-Muster: room_x<FieldCrc>_g<GeoCode>_mm<Budget>_<Zeitstempel>.room
func libraryPrefix(n *rooms.Network) string {
	return fmt.Sprintf("room_x%016x_", n.Field.FieldCrc())
}

var libraryNameRe = regexp.MustCompile(`^[A-Za-z0-9_.-]+\.room$`)

// prüft die Budget-Gültigkeit einer Bibliotheks-Datei gegen das aktuelle
// max moves (0 = kein Budget gesetzt: nur unbedingt gültige Räume passen)
func libraryUsable(meta rooms.LibraryMeta, maxMoves uint64) bool {
	return meta.MaxMoves == 0 || (maxMoves > 0 && maxMoves <= meta.MaxMoves)
}

type libraryJSON struct {
	Name     string `json:"name"`
	Fields   uint32 `json:"fields"`
	States   uint64 `json:"states"`
	Variants uint64 `json:"variants"`
	MinMoves uint64 `json:"minMoves"`
	MaxMoves uint64 `json:"maxMoves"` // Gültigkeits-Budget (0 = unbedingt)
	Size     int64  `json:"size"`
}

// GET /api/library?maxMoves=N: Bibliotheks-Räume des Levels, die zum
// aktuellen Budget passen; neueste zuerst
func (s *Server) handleLibrary(w http.ResponseWriter, r *http.Request) {
	n, _ := s.snapshot()
	var maxMoves uint64
	fmt.Sscanf(r.URL.Query().Get("maxMoves"), "%d", &maxMoves)
	prefix := libraryPrefix(n)
	entries, err := os.ReadDir(libraryDir)
	if err != nil && !os.IsNotExist(err) {
		writeError(w, http.StatusInternalServerError, err.Error())
		return
	}
	items := []libraryJSON{}
	for _, entry := range entries {
		name := entry.Name()
		if entry.IsDir() || !strings.HasPrefix(name, prefix) || !strings.HasSuffix(name, ".room") {
			continue
		}
		fi, err := entry.Info()
		if err != nil {
			continue
		}
		file, err := os.Open(filepath.Join(libraryDir, name))
		if err != nil {
			continue
		}
		meta, err := rooms.ReadLibraryHeader(file)
		file.Close()
		if err != nil || !libraryUsable(meta, maxMoves) {
			continue // kaputt oder Budget passt nicht: unsichtbar
		}
		items = append(items, libraryJSON{
			Name: name, Fields: meta.Fields, States: meta.States, Variants: meta.Variants,
			MinMoves: meta.MinMoves, MaxMoves: meta.MaxMoves, Size: fi.Size(),
		})
	}
	sort.Slice(items, func(a, b int) bool { return items[a].Name > items[b].Name })
	writeJSON(w, map[string]any{"items": items})
}

// POST /api/library: einen Raum des aktuellen Netzwerks in die Bibliothek
// speichern; maxMoves ist das Budget, unter dem seine Streichungen bewiesen
// wurden (der Wert aus dem GUI-Feld - der Nutzer arbeitet je Level mit
// einer festen, verifizierten Schranke)
func (s *Server) handleLibrarySave(w http.ResponseWriter, r *http.Request) {
	var req struct {
		Room     uint32 `json:"room"`
		MaxMoves uint64 `json:"maxMoves"`
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "ungültige Anfrage: "+err.Error())
		return
	}
	if !s.validRooms([]uint32{req.Room}) {
		writeError(w, http.StatusBadRequest, "unbekannter Raum-Index")
		return
	}
	started := s.runJob("library", "raum speichern...", func(info rooms.ProgressFunc) (string, error) {
		n, _ := s.snapshot()
		room := n.Rooms[req.Room]
		restMin := uint64(0)
		for _, other := range n.Rooms {
			if other != room {
				restMin += other.MinMoves()
			}
		}
		meta := rooms.LibraryMeta{
			FieldCrc: n.Field.FieldCrc(),
			MaxMoves: req.MaxMoves,
			GeoCode:  rooms.RoomGeoCode(room),
			Fields:   uint32(len(room.Fields)),
			States:   room.States.Count(),
			Variants: room.Variants.Count(),
			MinMoves: room.MinMoves(),
			RestMin:  restMin,
		}
		if err := os.MkdirAll(libraryDir, 0o755); err != nil {
			return "", err
		}
		base := fmt.Sprintf("%sg%016x_mm%d_%s", libraryPrefix(n), meta.GeoCode,
			meta.MaxMoves, time.Now().Format("20060102-150405"))
		path := filepath.Join(libraryDir, base+".room")
		for i := 2; ; i++ {
			if _, err := os.Stat(path); os.IsNotExist(err) {
				break
			}
			path = filepath.Join(libraryDir, fmt.Sprintf("%s_%d.room", base, i))
		}
		file, err := os.Create(path)
		if err != nil {
			return "", err
		}
		if err := rooms.WriteLibraryRoom(file, room, meta); err != nil {
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
		return fmt.Sprintf("Raum gespeichert: %d Felder, %s Varianten (%s)",
			meta.Fields, tools.FormatInt(meta.Variants), formatMB(fi.Size())), nil
	})
	if !started {
		writeError(w, http.StatusConflict, "es läuft bereits eine Rechnung")
		return
	}
	writeJSON(w, map[string]any{"started": true})
}

// POST /api/library/load: gewählten Bibliotheks-Raum einfügen - überlappende
// gemergte Räume werden auf Einzelfelder zurückgesetzt (rooms.InsertRoom)
func (s *Server) handleLibraryLoad(w http.ResponseWriter, r *http.Request) {
	var req struct {
		Name     string `json:"name"`
		MaxMoves uint64 `json:"maxMoves"`
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "ungültige Anfrage: "+err.Error())
		return
	}
	if !libraryNameRe.MatchString(req.Name) {
		writeError(w, http.StatusBadRequest, "ungültiger Datei-Name")
		return
	}
	started := s.runJob("library", "raum einfügen...", func(info rooms.ProgressFunc) (string, error) {
		n, _ := s.snapshot()
		file, err := os.Open(filepath.Join(libraryDir, req.Name))
		if err != nil {
			return "", err
		}
		defer file.Close()
		room, meta, err := rooms.ReadLibraryRoom(n.Field, file)
		if err != nil {
			return "", err
		}
		// Budget-Wächter (die Liste filtert schon, hier zählt es endgültig):
		// ein unter kleinerem Budget optimierter Raum wäre für die aktuelle
		// Suche giftig - seine Streichungen gelten nur bis zu SEINEM Budget
		if !libraryUsable(meta, req.MaxMoves) {
			return "", fmt.Errorf("raum wurde unter budget %d optimiert - aktuelles max moves %d passt nicht (muss <= %d und > 0 sein)",
				meta.MaxMoves, req.MaxMoves, meta.MaxMoves)
		}
		if err := n.InsertRoom(room, info); err != nil {
			return "", err
		}
		return fmt.Sprintf("Raum eingefügt: %d Felder, %s Varianten - jetzt %s Räume",
			len(room.Fields), tools.FormatInt(room.Variants.Count()), tools.FormatInt(len(n.Rooms))), nil
	})
	if !started {
		writeError(w, http.StatusConflict, "es läuft bereits eine Rechnung")
		return
	}
	writeJSON(w, map[string]any{"started": true})
}

// POST /api/library/delete: gewählten Bibliotheks-Raum löschen
func (s *Server) handleLibraryDelete(w http.ResponseWriter, r *http.Request) {
	var req struct {
		Name string `json:"name"`
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "ungültige Anfrage: "+err.Error())
		return
	}
	if !libraryNameRe.MatchString(req.Name) {
		writeError(w, http.StatusBadRequest, "ungültiger Datei-Name")
		return
	}
	if err := os.Remove(filepath.Join(libraryDir, req.Name)); err != nil {
		writeError(w, http.StatusInternalServerError, err.Error())
		return
	}
	writeJSON(w, map[string]any{"ok": true})
}
