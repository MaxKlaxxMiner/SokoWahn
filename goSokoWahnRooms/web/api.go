package web

import (
	"encoding/json"
	"errors"
	"fmt"
	"net/http"
	"strconv"
	"strings"

	"goSokoWahnRooms/rooms"
	"goSokoWahnRooms/soko"
	"goSokoWahnRooms/tools"
)

// ---------- JSON-Datentypen der API ----------
// IDs bleiben rohe Zahlen: Feldpositionen als Wpos (kompakter Lauf-Index),
// die Zuordnung Wpos -> x/y liefert /api/field einmalig über das walk-Array.

type summaryJSON struct {
	Title        string `json:"title"`
	Width        int    `json:"width"`
	Height       int    `json:"height"`
	BoxCount     int    `json:"boxCount"`
	WalkCount    int    `json:"walkCount"`
	RoomCount    int    `json:"roomCount"`
	StateCount   uint64 `json:"stateCount"`
	VariantCount uint64 `json:"variantCount"`
	MinMoves     uint64 `json:"minMoves"` // Summe der bewiesenen Pflicht-Minima aller Räume
	Effort       string `json:"effort"`
}

type fieldJSON struct {
	Width  int        `json:"width"`
	Height int        `json:"height"`
	Rows   []string   `json:"rows"` // Wände/Ziele/Boden, ohne Kisten und Spieler
	Walk   []walkJSON `json:"walk"` // Index = Wpos
	Player uint32     `json:"player"`
	Boxes  []uint32   `json:"boxes"` // Kisten am Spielstart
}

type walkJSON struct {
	X       int  `json:"x"`
	Y       int  `json:"y"`
	Goal    bool `json:"goal"`
	Corner  bool `json:"corner"`
	BoxPath bool `json:"boxPath"` // Feld kann laut Single-Box-Scan eine Kiste tragen
}

type roomJSON struct {
	Index             uint32   `json:"index"`
	Fields            []uint32 `json:"fields"`
	Goals             []uint32 `json:"goals"`
	StartBoxes        []uint32 `json:"startBoxes"`
	MaxBoxes          uint32   `json:"maxBoxes"`
	Portals           int      `json:"portals"`
	States            uint64   `json:"states"`
	Variants          uint64   `json:"variants"`
	StartState        uint64   `json:"startState"`
	StartVariantCount uint64   `json:"startVariantCount"`
	MinMoves          uint64   `json:"minMoves"` // bewiesene Untergrenze der Pflicht-Moves
}

type roomDetailJSON struct {
	roomJSON
	PortalList []portalJSON `json:"portalList"` // eingehende Portale des Raums
}

type portalJSON struct {
	Index         uint32              `json:"index"`
	From          uint32              `json:"from"`
	To            uint32              `json:"to"`
	Dir           string              `json:"dir"`
	FromRoom      uint32              `json:"fromRoom"`
	OppositeIndex uint32              `json:"oppositeIndex"` // Position in FromRoom.Incoming
	BlockedBox    bool                `json:"blockedBox"`
	BoxSwap       map[uint64]uint64   `json:"boxSwap"`      // JSON-Schlüssel werden Strings
	VariantSpans  map[uint64]spanJSON `json:"variantSpans"` // Zustand -> Varianten-Bereich
}

type spanJSON struct {
	Start uint64 `json:"start"`
	Count uint64 `json:"count"`
}

type stateJSON struct {
	ID    uint64   `json:"id"`
	Boxes []uint32 `json:"boxes"`
}

type variantJSON struct {
	ID           uint64   `json:"id"`
	OldState     uint64   `json:"oldState"`
	NewState     uint64   `json:"newState"`
	Moves        uint32   `json:"moves"`
	Pushes       uint32   `json:"pushes"`
	BoxPortals   []uint32 `json:"boxPortals"`
	PlayerPortal int64    `json:"playerPortal"` // -1 = Spieler bleibt drin = Spielende
	Path         string   `json:"path"`
	Start        bool     `json:"start"` // Startvariante (Spieler startet im Raum)
}

type pageJSON struct {
	Total  uint64 `json:"total"`
	Offset uint64 `json:"offset"`
	Items  any    `json:"items"`
}

// ---------- Hilfen ----------

func writeJSON(w http.ResponseWriter, v any) {
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	if err := json.NewEncoder(w).Encode(v); err != nil {
		// Verbindung weg o.ä. - nichts mehr zu retten
		return
	}
}

func writeError(w http.ResponseWriter, code int, msg string) {
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	w.WriteHeader(code)
	_ = json.NewEncoder(w).Encode(map[string]string{"error": msg})
}

// liest offset/limit aus der Anfrage und klemmt beide auf den gültigen Bereich
func parsePage(r *http.Request, total uint64) (offset, limit uint64, err error) {
	const defaultLimit, maxLimit = 100, 10000
	limit = defaultLimit
	if v := r.FormValue("limit"); v != "" {
		n, e := strconv.ParseUint(v, 10, 64)
		if e != nil || n == 0 {
			return 0, 0, errors.New("ungültiges limit")
		}
		limit = min(n, maxLimit)
	}
	if v := r.FormValue("offset"); v != "" {
		n, e := strconv.ParseUint(v, 10, 64)
		if e != nil {
			return 0, 0, errors.New("ungültiges offset")
		}
		offset = n
	}
	offset = min(offset, total)
	limit = min(limit, total-offset)
	return offset, limit, nil
}

// holt den Raum aus dem {index}-Pfadsegment (nil = Fehler bereits gemeldet)
func roomFromPath(w http.ResponseWriter, r *http.Request, n *rooms.Network) *rooms.Room {
	idx, err := strconv.ParseUint(r.PathValue("index"), 10, 32)
	if err != nil || idx >= uint64(len(n.Rooms)) {
		writeError(w, http.StatusNotFound, "unbekannter Raum-Index")
		return nil
	}
	return n.Rooms[idx]
}

func wposList(list []soko.Wpos) []uint32 {
	result := make([]uint32, len(list))
	for i, p := range list {
		result[i] = uint32(p)
	}
	return result
}

func roomToJSON(room *rooms.Room) roomJSON {
	return roomJSON{
		Index:             room.Index,
		Fields:            wposList(room.Fields),
		Goals:             wposList(room.Goals),
		StartBoxes:        wposList(room.StartBoxes),
		MaxBoxes:          room.MaxBoxes,
		Portals:           len(room.Incoming),
		States:            room.States.Count(),
		Variants:          room.Variants.Count(),
		StartState:        room.StartState,
		StartVariantCount: room.StartVariantCount,
		MinMoves:          room.MinMoves(),
	}
}

// prüft Raum-Indizes synchron unter der Lesesperre (die Jobs selbst laufen
// asynchron, aber offensichtliche Eingabefehler sollen sofort ein 400 geben)
func (s *Server) validRooms(indices []uint32) bool {
	s.mu.RLock()
	defer s.mu.RUnlock()
	for _, idx := range indices {
		if int(idx) >= len(s.network.Rooms) {
			return false
		}
	}
	return true
}

// ---------- Handler ----------

func (s *Server) handleSummary(w http.ResponseWriter, r *http.Request) {
	n, title := s.snapshot()
	states, variants, minMoves := uint64(0), uint64(0), uint64(0)
	for _, room := range n.Rooms {
		states += room.States.Count()
		variants += room.Variants.Count()
		minMoves += room.MinMoves()
	}
	writeJSON(w, summaryJSON{
		Title:        title,
		Width:        n.Field.Width(),
		Height:       n.Field.Height(),
		BoxCount:     n.Field.BoxCount(),
		WalkCount:    n.Field.WalkCount(),
		RoomCount:    len(n.Rooms),
		StateCount:   states,
		VariantCount: variants,
		MinMoves:     minMoves,
		Effort:       n.EffortString(),
	})
}

func (s *Server) handleField(w http.ResponseWriter, r *http.Request) {
	n, _ := s.snapshot()
	f := n.Field

	// Grundriss ohne Kisten/Spieler (die kommen als Wpos-Listen, damit das
	// Frontend sie später zustandsabhängig setzen kann)
	replacer := strings.NewReplacer("$", " ", "@", " ", "*", ".", "+", ".")
	rows := strings.Split(strings.TrimRight(replacer.Replace(f.String()), "\n"), "\n")

	walk := make([]walkJSON, f.WalkEof())
	for p := soko.Wpos(0); p < f.WalkEof(); p++ {
		pos := f.FieldPos(p)
		walk[p] = walkJSON{
			X:       pos % f.Width(),
			Y:       pos / f.Width(),
			Goal:    f.IsGoal(p),
			Corner:  f.IsCorner(p),
			BoxPath: n.Scan.OnBoxPath(p),
		}
	}

	writeJSON(w, fieldJSON{
		Width:  f.Width(),
		Height: f.Height(),
		Rows:   rows,
		Walk:   walk,
		Player: uint32(f.InitPlayer()),
		Boxes:  wposList(f.InitBoxes()),
	})
}

// Zuordnung Feld -> Raum fürs Einfärben des Canvas: Array über alle Wpos,
// Wert = Raum-Index (bewusst ungepagt - schrumpft beim Mergen, nie größer als WalkEof)
func (s *Server) handleMap(w http.ResponseWriter, r *http.Request) {
	n, _ := s.snapshot()
	roomOf := make([]uint32, n.Field.WalkEof())
	for _, room := range n.Rooms {
		for _, p := range room.Fields {
			roomOf[p] = room.Index
		}
	}
	writeJSON(w, map[string]any{"rooms": roomOf})
}

func (s *Server) handleRooms(w http.ResponseWriter, r *http.Request) {
	n, _ := s.snapshot()
	total := uint64(len(n.Rooms))
	offset, limit, err := parsePage(r, total)
	if err != nil {
		writeError(w, http.StatusBadRequest, err.Error())
		return
	}
	items := make([]roomJSON, 0, limit)
	for id := offset; id < offset+limit; id++ {
		items = append(items, roomToJSON(n.Rooms[id]))
	}
	writeJSON(w, pageJSON{Total: total, Offset: offset, Items: items})
}

func (s *Server) handleRoom(w http.ResponseWriter, r *http.Request) {
	n, _ := s.snapshot()
	room := roomFromPath(w, r, n)
	if room == nil {
		return
	}
	detail := roomDetailJSON{roomJSON: roomToJSON(room)}
	detail.PortalList = make([]portalJSON, len(room.Incoming))
	for i, p := range room.Incoming {
		spans := make(map[uint64]spanJSON, len(p.VariantSpans))
		for state, span := range p.VariantSpans {
			spans[state] = spanJSON{Start: span.Start, Count: span.Count}
		}
		detail.PortalList[i] = portalJSON{
			Index:         p.Index,
			From:          uint32(p.From),
			To:            uint32(p.To),
			Dir:           string(rune(p.Dir)),
			FromRoom:      p.FromRoom.Index,
			OppositeIndex: p.Opposite.Index,
			BlockedBox:    p.BlockedBox,
			BoxSwap:       p.BoxSwap,
			VariantSpans:  spans,
		}
	}
	writeJSON(w, detail)
}

func (s *Server) handleStates(w http.ResponseWriter, r *http.Request) {
	n, _ := s.snapshot()
	room := roomFromPath(w, r, n)
	if room == nil {
		return
	}
	total := room.States.Count()
	offset, limit, err := parsePage(r, total)
	if err != nil {
		writeError(w, http.StatusBadRequest, err.Error())
		return
	}
	items := make([]stateJSON, 0, limit)
	for id := offset; id < offset+limit; id++ {
		items = append(items, stateJSON{ID: id, Boxes: wposList(room.States.Get(id))})
	}
	writeJSON(w, pageJSON{Total: total, Offset: offset, Items: items})
}

// verschmilzt die übergebene Raum-Auswahl (M3) als Hintergrund-Job: solange
// zwei ausgewählte Räume direkt verbunden sind, wird paarweise gemergt (mit
// Validate nach jedem Merge); Fortschritt und Ergebnis kommen über /api/progress
func (s *Server) handleMerge(w http.ResponseWriter, r *http.Request) {
	var req struct {
		Rooms    []uint32 `json:"rooms"`
		MaxMoves uint64   `json:"maxMoves"` // 0 = kein Limit (siehe handleOptimize)
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "ungültige Anfrage: "+err.Error())
		return
	}
	if len(req.Rooms) < 2 {
		writeError(w, http.StatusBadRequest, "mindestens zwei Räume auswählen")
		return
	}
	if !s.validRooms(req.Rooms) {
		writeError(w, http.StatusBadRequest, "unbekannter Raum-Index")
		return
	}
	started := s.runJob("merge...", func(info rooms.ProgressFunc) (string, error) {
		n, _ := s.snapshot()
		merges, err := n.MergeSelection(req.Rooms, req.MaxMoves, info)
		if err != nil {
			// Merge- oder Validate-Fehler: Zustand des Netzwerks ist verdächtig,
			// der Fehler muss sichtbar werden (Konzept: Validate nach jedem Schritt)
			return "", err
		}
		return fmt.Sprintf("Merge: %d Merges, %s Räume übrig", merges, tools.FormatInt(len(n.Rooms))), nil
	})
	if !started {
		writeError(w, http.StatusConflict, "es läuft bereits eine Rechnung")
		return
	}
	writeJSON(w, map[string]any{"started": true})
}

// setzt gemergte Räume der Auswahl auf ihre Ein-Feld-Start-Räume zurück
// (Entf-Taste in der GUI): wie frisch initialisiert, Nachbarn samt ihrer
// Optimierungen bleiben unberührt - für Fehlgriffe beim Mergen, ohne das
// ganze Level neu laden zu müssen
func (s *Server) handleReset(w http.ResponseWriter, r *http.Request) {
	var req struct {
		Rooms []uint32 `json:"rooms"`
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "ungültige Anfrage: "+err.Error())
		return
	}
	if len(req.Rooms) == 0 {
		writeError(w, http.StatusBadRequest, "mindestens einen Raum auswählen")
		return
	}
	if !s.validRooms(req.Rooms) {
		writeError(w, http.StatusBadRequest, "unbekannter Raum-Index")
		return
	}
	started := s.runJob("reset...", func(info rooms.ProgressFunc) (string, error) {
		n, _ := s.snapshot()
		count, err := n.ResetRooms(req.Rooms, info)
		if err != nil {
			return "", err
		}
		if count == 0 {
			return "Reset: keine gemergten Räume in der Auswahl", nil
		}
		return fmt.Sprintf("Reset: %d Räume zurückgesetzt, jetzt %s Räume", count, tools.FormatInt(len(n.Rooms))), nil
	})
	if !started {
		writeError(w, http.StatusConflict, "es läuft bereits eine Rechnung")
		return
	}
	writeJSON(w, map[string]any{"started": true})
}

// führt Deadlock-Scan (M4) und Dominanzsuche (M4b) auf der übergebenen
// Raum-Auswahl als Hintergrund-Job aus; Fortschritt über /api/progress,
// Abbruch über /api/stop (bereits Bewiesenes bleibt angewandt)
func (s *Server) handleOptimize(w http.ResponseWriter, r *http.Request) {
	var req struct {
		Rooms    []uint32 `json:"rooms"`
		MaxMoves uint64   `json:"maxMoves"` // 0 = kein Limit; sonst VERIFIZIERTE obere Schranke der Gesamtlösung
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "ungültige Anfrage: "+err.Error())
		return
	}
	if len(req.Rooms) == 0 {
		writeError(w, http.StatusBadRequest, "mindestens einen Raum auswählen")
		return
	}
	if !s.validRooms(req.Rooms) {
		writeError(w, http.StatusBadRequest, "unbekannter Raum-Index")
		return
	}
	started := s.runJob("optimize...", func(info rooms.ProgressFunc) (string, error) {
		n, _ := s.snapshot()
		removed, err := n.OptimizeRooms(req.Rooms, req.MaxMoves, info)
		if err != nil {
			return "", err
		}
		return fmt.Sprintf("Optimize: %s Varianten entfernt", tools.FormatInt(removed)), nil
	})
	if !started {
		writeError(w, http.StatusConflict, "es läuft bereits eine Rechnung")
		return
	}
	writeJSON(w, map[string]any{"started": true})
}

// prüft die Konsistenz des Netzwerks auf Anforderung (Validate-Button);
// mutiert nichts und läuft daher unter der Lesesperre
func (s *Server) handleValidate(w http.ResponseWriter, r *http.Request) {
	n, _ := s.snapshot()
	if err := n.Validate(true); err != nil {
		writeError(w, http.StatusInternalServerError, err.Error())
		return
	}
	writeJSON(w, map[string]any{"ok": true})
}

// Varianten eines Raums; optional gefiltert nach eingehendem Portal + Zustand
// (beide nur gemeinsam - der Filter läuft über die Span-Verzeichnisse der Portale)
func (s *Server) handleVariants(w http.ResponseWriter, r *http.Request) {
	n, _ := s.snapshot()
	room := roomFromPath(w, r, n)
	if room == nil {
		return
	}

	base, total := uint64(0), room.Variants.Count()
	portalStr, stateStr := r.FormValue("portal"), r.FormValue("state")
	if (portalStr == "") != (stateStr == "") {
		writeError(w, http.StatusBadRequest, "portal und state nur gemeinsam angeben")
		return
	}
	if portalStr != "" {
		portal, err := strconv.ParseUint(portalStr, 10, 32)
		if err != nil || portal >= uint64(len(room.Incoming)) {
			writeError(w, http.StatusBadRequest, "ungültiges portal")
			return
		}
		state, err := strconv.ParseUint(stateStr, 10, 64)
		if err != nil || state >= room.States.Count() {
			writeError(w, http.StatusBadRequest, "ungültiger state")
			return
		}
		span := room.Incoming[portal].GetVariantSpan(state)
		base, total = span.Start, span.Count
	}

	offset, limit, err := parsePage(r, total)
	if err != nil {
		writeError(w, http.StatusBadRequest, err.Error())
		return
	}
	items := make([]variantJSON, 0, limit)
	for i := offset; i < offset+limit; i++ {
		id := base + i
		d := room.Variants.Get(id)
		playerPortal := int64(-1)
		if d.PlayerPortal != rooms.NoPortal {
			playerPortal = int64(d.PlayerPortal)
		}
		boxPortals := d.BoxPortals
		if boxPortals == nil {
			boxPortals = []uint32{} // im JSON [] statt null
		}
		items = append(items, variantJSON{
			ID:           id,
			OldState:     d.OldState,
			NewState:     d.NewState,
			Moves:        d.Moves,
			Pushes:       d.Pushes,
			BoxPortals:   boxPortals,
			PlayerPortal: playerPortal,
			Path:         d.Path,
			Start:        id < room.StartVariantCount,
		})
	}
	writeJSON(w, pageJSON{Total: total, Offset: offset, Items: items})
}
