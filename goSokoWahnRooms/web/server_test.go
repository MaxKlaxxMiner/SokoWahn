package web

import (
	"encoding/json"
	"net/http/httptest"
	"strings"
	"testing"

	"goSokoWahnRooms/rooms"
	"goSokoWahnRooms/soko"
)

// Mini-Level als API-Testfeld: 4 begehbare Felder in einer Reihe
// (wpos 0=@, 1=$, 2=frei, 3=Ziel), von Hand nachrechenbar
const miniLevel = `
######
#@$ .#
######
`

func testServer(t *testing.T) *Server {
	t.Helper()
	field, err := soko.Parse(miniLevel)
	if err != nil {
		t.Fatal(err)
	}
	network, err := rooms.NewNetwork(field)
	if err != nil {
		t.Fatal(err)
	}
	return New(network, "test")
}

// führt eine GET-Anfrage aus und dekodiert die JSON-Antwort nach out (nil = nur Status prüfen)
func get(t *testing.T, s *Server, path string, wantStatus int, out any) {
	t.Helper()
	rec := httptest.NewRecorder()
	s.ServeHTTP(rec, httptest.NewRequest("GET", path, nil))
	if rec.Code != wantStatus {
		t.Fatalf("GET %s: Status %d, erwartet %d (Body: %s)", path, rec.Code, wantStatus, rec.Body.String())
	}
	if out != nil {
		if err := json.Unmarshal(rec.Body.Bytes(), out); err != nil {
			t.Fatalf("GET %s: JSON-Fehler %v (Body: %s)", path, err, rec.Body.String())
		}
	}
}

func TestSummary(t *testing.T) {
	s := testServer(t)
	var sum summaryJSON
	get(t, s, "/api/summary", 200, &sum)

	if sum.Title != "test" || sum.Width != 6 || sum.Height != 3 {
		t.Errorf("unerwarteter Kopf: %+v", sum)
	}
	if sum.RoomCount != 4 || sum.BoxCount != 1 || sum.WalkCount != 4 {
		t.Errorf("unerwartete Zählwerte: %+v", sum)
	}
	// Zustände von Hand: @-Feld 1 (Kiste laut Scan unmöglich), $-Feld 2, freies Feld 2, Zielfeld 2
	if sum.StateCount != 7 {
		t.Errorf("erwartet 7 Zustände, erhalten: %d", sum.StateCount)
	}
	if sum.VariantCount == 0 || sum.Effort == "" {
		t.Errorf("Varianten/Effort fehlen: %+v", sum)
	}
}

func TestField(t *testing.T) {
	s := testServer(t)
	var field fieldJSON
	get(t, s, "/api/field", 200, &field)

	// Grundriss ohne Kisten und Spieler
	if field.Rows[0] != "######" || field.Rows[1] != "#   .#" || field.Rows[2] != "######" {
		t.Errorf("unerwarteter Grundriss: %q", field.Rows)
	}
	if len(field.Walk) != 4 {
		t.Fatalf("erwartet 4 begehbare Felder, erhalten: %d", len(field.Walk))
	}
	if field.Player != 0 || len(field.Boxes) != 1 || field.Boxes[0] != 1 {
		t.Errorf("Spieler/Kisten falsch: player=%d boxes=%v", field.Player, field.Boxes)
	}
	if field.Walk[0].X != 1 || field.Walk[0].Y != 1 {
		t.Errorf("Wpos 0 falsch verortet: %+v", field.Walk[0])
	}
	if !field.Walk[3].Goal || field.Walk[0].Goal {
		t.Errorf("Goal-Flags falsch: %+v", field.Walk)
	}
	if !field.Walk[0].Corner || field.Walk[1].Corner {
		t.Errorf("Corner-Flags falsch: %+v", field.Walk)
	}
	// Kiste kann nie auf dem Spieler-Startfeld stehen (Single-Box-Scan)
	if field.Walk[0].BoxPath || !field.Walk[1].BoxPath || !field.Walk[3].BoxPath {
		t.Errorf("BoxPath-Flags falsch: %+v", field.Walk)
	}
}

func TestMap(t *testing.T) {
	s := testServer(t)
	var m struct {
		Rooms []uint32 `json:"rooms"`
	}
	get(t, s, "/api/map", 200, &m)

	// bei 1-Feld-Räumen gilt: Raum-Index = Wpos
	if len(m.Rooms) != 4 {
		t.Fatalf("erwartet 4 Einträge, erhalten: %d", len(m.Rooms))
	}
	for wpos, roomIdx := range m.Rooms {
		if roomIdx != uint32(wpos) {
			t.Errorf("Feld %d gehört Raum %d, erwartet %d", wpos, roomIdx, wpos)
		}
	}
}

func TestRoomsPaging(t *testing.T) {
	s := testServer(t)
	var page struct {
		Total  uint64     `json:"total"`
		Offset uint64     `json:"offset"`
		Items  []roomJSON `json:"items"`
	}

	get(t, s, "/api/rooms?limit=2", 200, &page)
	if page.Total != 4 || page.Offset != 0 || len(page.Items) != 2 {
		t.Errorf("Seite 1 falsch: total=%d offset=%d items=%d", page.Total, page.Offset, len(page.Items))
	}
	if page.Items[1].Index != 1 {
		t.Errorf("unerwarteter Raum: %+v", page.Items[1])
	}

	get(t, s, "/api/rooms?offset=3&limit=2", 200, &page)
	if len(page.Items) != 1 || page.Items[0].Index != 3 {
		t.Errorf("Restseite falsch: %+v", page.Items)
	}

	get(t, s, "/api/rooms?offset=10", 200, &page)
	if len(page.Items) != 0 {
		t.Errorf("Seite hinter dem Ende muss leer sein: %+v", page.Items)
	}

	get(t, s, "/api/rooms?limit=0", 400, nil)
	get(t, s, "/api/rooms?limit=abc", 400, nil)
}

func TestRoomDetail(t *testing.T) {
	s := testServer(t)
	var room roomDetailJSON
	get(t, s, "/api/rooms/1", 200, &room)

	if room.Index != 1 || len(room.StartBoxes) != 1 || room.States != 2 {
		t.Errorf("Raum 1 falsch: %+v", room.roomJSON)
	}
	// Kistenfeld im Korridor: Portale von links (Raum 0, Richtung r) und rechts (Raum 2, Richtung l)
	if len(room.PortalList) != 2 {
		t.Fatalf("erwartet 2 Portale, erhalten: %d", len(room.PortalList))
	}
	p0, p1 := room.PortalList[0], room.PortalList[1]
	if p0.FromRoom != 0 || p0.Dir != "r" || p0.From != 0 || p0.To != 1 {
		t.Errorf("Portal 0 falsch: %+v", p0)
	}
	if p1.FromRoom != 2 || p1.Dir != "l" {
		t.Errorf("Portal 1 falsch: %+v", p1)
	}

	// Gegenportal-Verlinkung: Portal 0 kommt aus Raum 0, dessen Incoming[OppositeIndex]
	// muss zurück auf Raum 1 zeigen
	var room0 roomDetailJSON
	get(t, s, "/api/rooms/0", 200, &room0)
	opp := room0.PortalList[p0.OppositeIndex]
	if opp.From != 1 || opp.To != 0 {
		t.Errorf("Gegenportal falsch: %+v", opp)
	}

	get(t, s, "/api/rooms/99", 404, nil)
	get(t, s, "/api/rooms/abc", 404, nil)
}

func TestStates(t *testing.T) {
	s := testServer(t)
	var page struct {
		Total uint64      `json:"total"`
		Items []stateJSON `json:"items"`
	}
	get(t, s, "/api/rooms/1/states", 200, &page)

	// Kistenfeld: Zustand 0 = leer (Endzustand), Zustand 1 = Kiste
	if page.Total != 2 || len(page.Items) != 2 {
		t.Fatalf("erwartet 2 Zustände: %+v", page)
	}
	if len(page.Items[0].Boxes) != 0 {
		t.Errorf("Zustand 0 muss leer sein: %+v", page.Items[0])
	}
	if len(page.Items[1].Boxes) != 1 || page.Items[1].Boxes[0] != 1 {
		t.Errorf("Zustand 1 muss die Kiste tragen: %+v", page.Items[1])
	}
}

func TestVariants(t *testing.T) {
	s := testServer(t)
	var page struct {
		Total uint64        `json:"total"`
		Items []variantJSON `json:"items"`
	}

	// ungefiltert: alle Varianten von Raum 2, IDs lückenlos aufsteigend
	get(t, s, "/api/rooms/2/variants", 200, &page)
	if page.Total == 0 || uint64(len(page.Items)) != page.Total {
		t.Fatalf("Varianten von Raum 2 fehlen: %+v", page)
	}
	for i, v := range page.Items {
		if v.ID != uint64(i) {
			t.Errorf("Varianten-IDs nicht fortlaufend: %+v", v)
		}
	}

	// gefiltert über den Span eines (Portal, Zustand)-Paares: alle Treffer
	// müssen zum Zustand passen und zusammenhängende IDs haben
	var room roomDetailJSON
	get(t, s, "/api/rooms/2", 200, &room)
	span, ok := room.PortalList[0].VariantSpans[0]
	if !ok {
		t.Fatal("Portal 0 hat keinen Span für Zustand 0")
	}
	get(t, s, "/api/rooms/2/variants?portal=0&state=0", 200, &page)
	if page.Total != span.Count {
		t.Errorf("Filter-Total %d != Span-Count %d", page.Total, span.Count)
	}
	for i, v := range page.Items {
		if v.OldState != 0 {
			t.Errorf("gefilterte Variante mit falschem Zustand: %+v", v)
		}
		if v.ID != span.Start+uint64(i) {
			t.Errorf("gefilterte IDs nicht im Span: %+v (Span %+v)", v, span)
		}
	}

	// Startvarianten: nur der Spieler-Raum (0) hat welche
	get(t, s, "/api/rooms/0/variants", 200, &page)
	if len(page.Items) == 0 || !page.Items[0].Start {
		t.Errorf("Raum 0 muss Startvarianten haben: %+v", page.Items)
	}

	// Fehlerfälle
	get(t, s, "/api/rooms/2/variants?portal=0", 400, nil)
	get(t, s, "/api/rooms/2/variants?state=0", 400, nil)
	get(t, s, "/api/rooms/2/variants?portal=99&state=0", 400, nil)
	get(t, s, "/api/rooms/2/variants?portal=0&state=99", 400, nil)
}

// führt eine POST-Anfrage mit JSON-Body aus und dekodiert die Antwort nach out
func post(t *testing.T, s *Server, path, body string, wantStatus int, out any) {
	t.Helper()
	rec := httptest.NewRecorder()
	req := httptest.NewRequest("POST", path, strings.NewReader(body))
	req.Header.Set("Content-Type", "application/json")
	s.ServeHTTP(rec, req)
	if rec.Code != wantStatus {
		t.Fatalf("POST %s: Status %d, erwartet %d (Body: %s)", path, rec.Code, wantStatus, rec.Body.String())
	}
	if out != nil {
		if err := json.Unmarshal(rec.Body.Bytes(), out); err != nil {
			t.Fatalf("POST %s: JSON-Fehler %v (Body: %s)", path, err, rec.Body.String())
		}
	}
}

func TestMerge(t *testing.T) {
	s := testServer(t)
	var result struct {
		Merges int `json:"merges"`
		Rooms  int `json:"rooms"`
	}

	// Räume 1+2 verschmelzen (Kistenfeld + freies Feld)
	post(t, s, "/api/merge", `{"rooms":[1,2]}`, 200, &result)
	if result.Merges != 1 || result.Rooms != 3 {
		t.Errorf("Merge-Ergebnis falsch: %+v", result)
	}

	// die Karte muss den neuen Raum zeigen (Felder 1 und 2 im selben Raum)
	var m struct {
		Rooms []uint32 `json:"rooms"`
	}
	get(t, s, "/api/map", 200, &m)
	if m.Rooms[1] != m.Rooms[2] {
		t.Errorf("Felder 1 und 2 nach Merge in verschiedenen Räumen: %v", m.Rooms)
	}

	// nicht verbundene Auswahl: kein Merge, kein Fehler
	post(t, s, "/api/merge", `{"rooms":[0,2]}`, 200, &result)
	if result.Merges != 0 {
		t.Errorf("unverbundene Auswahl darf nicht mergen: %+v", result)
	}

	// Fehlerfälle
	post(t, s, "/api/merge", `{"rooms":[0]}`, 400, nil)
	post(t, s, "/api/merge", `{"rooms":[0,99]}`, 400, nil)
	post(t, s, "/api/merge", `kein json`, 400, nil)
}

func TestStaticIndex(t *testing.T) {
	s := testServer(t)
	rec := httptest.NewRecorder()
	s.ServeHTTP(rec, httptest.NewRequest("GET", "/", nil))
	if rec.Code != 200 || !strings.Contains(rec.Body.String(), "goSokoWahnRooms") {
		t.Errorf("Startseite fehlt: Status %d", rec.Code)
	}
}
