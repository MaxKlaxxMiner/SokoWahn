package rooms

import (
	"testing"
	"time"

	"goSokoWahnRooms/maps"
)

// Level 5018 (aenigma "soko 47"): der Ziel-Trakt links ist eine große
// Ein-Portal-Kammer (3x5 Felder, 9 Ziele, einziger Zugang der Kisten-
// Durchgang in der Mittelzeile) - der Härtetest für die Dominanzsuche.

// merged den Ziel-Trakt (Spalten 1-5 der drei Ziel-Zeilen); die Räume werden
// geometrisch über die Feld-Koordinaten eingesammelt
func merge5018GoalRoom(t *testing.T) (*Network, *Room) {
	t.Helper()
	n := buildNetwork(t, maps.Map5018)

	width := n.Field.Width()
	minX, minY := width, n.Field.Height()
	for _, goal := range n.Field.Goals() {
		pos := n.Field.FieldPos(goal)
		minX, minY = min(minX, pos%width), min(minY, pos/width)
	}

	var selection []uint32
	for i, room := range n.Rooms {
		pos := n.Field.FieldPos(room.Fields[0])
		x, y := pos%width, pos/width
		if x >= minX && x <= minX+4 && y >= minY && y <= minY+2 {
			selection = append(selection, uint32(i))
		}
	}
	if len(selection) != 15 {
		t.Fatalf("Ziel-Trakt-Selektion: %d Räume statt 15", len(selection))
	}
	if _, err := n.MergeSelection(selection, nil); err != nil {
		t.Fatal("merge:", err)
	}
	for _, room := range n.Rooms {
		if len(room.Fields) == 15 {
			return n, room
		}
	}
	t.Fatal("merged goal room not found")
	return nil, nil
}

func TestOptimizeRoomsDominance5018(t *testing.T) {
	if testing.Short() {
		t.Skip("langer Lauf (Dominanzsuche am 5018er Ziel-Trakt)")
	}
	n, room := merge5018GoalRoom(t)
	t.Logf("nach merge: %d states, %d varianten, %d portale",
		room.States.Count(), room.Variants.Count(), len(room.Incoming))

	// wie in der GUI: Optimize drücken, bis nichts mehr kommt (das
	// Zeitbudget je Druck macht die Suche inkrementell)
	start := time.Now()
	total := uint64(0)
	for round := 1; ; round++ {
		removed, err := n.OptimizeRooms([]uint32{room.Index}, nil)
		if err != nil {
			t.Fatal("optimize:", err)
		}
		total += removed
		t.Logf("runde %d: %d entfernt (gesamt %v) -> %d states, %d varianten",
			round, removed, time.Since(start).Round(time.Millisecond),
			room.States.Count(), room.Variants.Count())
		if removed == 0 {
			break
		}
		if round > 40 {
			t.Fatal("kein Fixpunkt nach 40 Runden")
		}
	}
	t.Logf("fixpunkt: %d entfernt, %d states, %d varianten",
		total, room.States.Count(), room.Variants.Count())

	// weiche Anker (der exakte Fixpunkt hängt vom Zeitbudget-Schnitt ab und
	// kann je nach Rechnertempo minimal variieren; Referenzlauf 2026-08-18:
	// 312 states, 325 varianten nach 5 Runden / 78s)
	if room.Variants.Count() > 500 {
		t.Errorf("kammer behält %d varianten, erwartet <= 500", room.Variants.Count())
	}
	if room.States.Count() > 400 {
		t.Errorf("kammer behält %d states, erwartet <= 400", room.States.Count())
	}
}
