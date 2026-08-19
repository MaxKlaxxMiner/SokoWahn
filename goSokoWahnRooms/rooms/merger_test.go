package rooms

import (
	"testing"

	"goSokoWahnRooms/maps"
)

// Zwei-Kisten-Level: optimale Lösung 9 Züge (dRRRlluRR), verifiziert mit
// goSokoWahnBrute als Orakel (siehe Konzept Kap. 8)
const mapTwoBox = `
#######
#@ $ .#
# $  .#
#######
`

// verschmilzt alle Räume des Netzwerks über MergeSelection zu einem einzigen Raum
func fullMerge(t *testing.T, n *Network) *Room {
	t.Helper()
	indices := make([]uint32, len(n.Rooms))
	for i := range indices {
		indices[i] = uint32(i)
	}
	merges, err := n.MergeSelection(indices, 0, nil)
	if err != nil {
		t.Fatal("merge:", err)
	}
	if merges != len(indices)-1 || len(n.Rooms) != 1 {
		t.Fatalf("full merge incomplete: %d merges, %d rooms left", merges, len(n.Rooms))
	}
	return n.Rooms[0]
}

// verschmilzt alle Räume in umgekehrter Reihenfolge (höchste Indizes zuerst)
func fullMergeBackward(t *testing.T, n *Network) *Room {
	t.Helper()
	for len(n.Rooms) > 1 {
		var a, b *Room
		for i := len(n.Rooms) - 1; i >= 0 && a == nil; i-- {
			room := n.Rooms[i]
			for j := len(room.Outgoing) - 1; j >= 0; j-- {
				if op := room.Outgoing[j]; op.ToRoom != room {
					a, b = room, op.ToRoom
					break
				}
			}
		}
		if a == nil {
			t.Fatal("no connected rooms left")
		}
		if _, err := n.MergeRooms(a, b, 0, nil); err != nil {
			t.Fatal("merge:", err)
		}
	}
	return n.Rooms[0]
}

// prüft das Ergebnis eines Voll-Merges: keine Portale mehr, nur Startvarianten,
// alle enden im gelösten Zustand; liefert die minimale Move-Zahl der End-Varianten
func checkFullMerge(t *testing.T, room *Room) (minMoves uint32, minPath string) {
	t.Helper()
	if len(room.Incoming) != 0 {
		t.Fatalf("full merge: %d portals left", len(room.Incoming))
	}
	count := room.Variants.Count()
	if count == 0 || room.StartVariantCount != count {
		t.Fatalf("full merge: %d variants, %d start variants", count, room.StartVariantCount)
	}
	minMoves = ^uint32(0)
	for id := uint64(0); id < count; id++ {
		v := room.Variants.Get(id)
		if v.PlayerPortal != NoPortal || v.NewState != 0 {
			t.Fatalf("variant %d is no end variant: portal %d, new state %d", id, v.PlayerPortal, v.NewState)
		}
		if v.Moves < minMoves {
			minMoves, minPath = v.Moves, v.Path
		}
	}
	return minMoves, minPath
}

// Einzel-Merge der beiden Nicht-Start-Räume des Mini-Levels: eingefrorene
// Kennzahlen (Kiste + Sackgassen-Ziel ergeben einen 2-Felder-Raum)
func TestMergePairMini(t *testing.T) {
	n := buildNetwork(t, mapMini)
	merged, err := n.MergeRooms(n.Rooms[1], n.Rooms[2], 0, nil)
	if err != nil {
		t.Fatal("merge:", err)
	}
	if len(n.Rooms) != 2 {
		t.Fatalf("rooms: got %d, want 2", len(n.Rooms))
	}
	if merged != n.Rooms[1] {
		t.Error("merged room not at index 1")
	}
	if got := len(merged.Fields); got != 2 {
		t.Errorf("fields: got %d, want 2", got)
	}
	// handverifiziert: nur Endzustand (leer+Ziel belegt) und Startzustand
	// (Kiste+Ziel frei) überleben; "leer+leer" stirbt, weil das Portal vom
	// Eck-Startfeld keinen BoxSwap tragen kann. Varianten: Kiste aufs Ziel
	// schieben und zurücklaufen bzw. schieben und drinbleiben (Spielende).
	if got := merged.States.Count(); got != 2 {
		t.Errorf("states: got %d, want 2", got)
	}
	if got := merged.Variants.Count(); got != 2 {
		t.Errorf("variants: got %d, want 2", got)
	}
}

// Voll-Merge Mini-Level: es bleibt das perfekte Spiel übrig (1 Zug)
func TestMergeFullMini(t *testing.T) {
	n := buildNetwork(t, mapMini)
	room := fullMerge(t, n)
	moves, path := checkFullMerge(t, room)
	if moves != 1 || path != "r" {
		t.Errorf("solution: got %d moves (%q), want 1 (\"r\")", moves, path)
	}
	if got := room.States.Count(); got != 2 {
		t.Errorf("states: got %d, want 2", got)
	}
	if got := room.Variants.Count(); got != 1 {
		t.Errorf("variants: got %d, want 1", got)
	}
}

// Voll-Merge Zwei-Schub-Level: perfektes Spiel mit 2 Zügen
func TestMergeFullTwoPush(t *testing.T) {
	n := buildNetwork(t, mapTwoPush)
	room := fullMerge(t, n)
	moves, path := checkFullMerge(t, room)
	if moves != 2 || path != "rr" {
		t.Errorf("solution: got %d moves (%q), want 2 (\"rr\")", moves, path)
	}
	if got := room.Variants.Count(); got != 1 {
		t.Errorf("variants: got %d, want 1", got)
	}
}

// Voll-Merge Zwei-Kisten-Level: minimale Move-Zahl muss dem Brute-Orakel
// entsprechen (9 Züge)
func TestMergeFullTwoBox(t *testing.T) {
	n := buildNetwork(t, mapTwoBox)
	room := fullMerge(t, n)
	moves, path := checkFullMerge(t, room)
	if moves != 9 {
		t.Errorf("solution: got %d moves (%q), want 9", moves, path)
	}
	t.Logf("twobox: %d end variants, best %d moves (%s)", room.Variants.Count(), moves, path)
}

// Merge-Reihenfolge-Test (Konzept Kap. 8): verschiedene Reihenfolgen müssen
// auf dieselben Lösungs-Kennzahlen führen
func TestMergeOrderIndependence(t *testing.T) {
	for _, sokoMap := range []string{mapMini, mapTwoPush, mapTwoBox} {
		forward := fullMerge(t, buildNetwork(t, sokoMap))
		backward := fullMergeBackward(t, buildNetwork(t, sokoMap))

		fMoves, _ := checkFullMerge(t, forward)
		bMoves, _ := checkFullMerge(t, backward)
		if fMoves != bMoves {
			t.Errorf("min moves differ: forward %d, backward %d", fMoves, bMoves)
		}
		if forward.Variants.Count() != backward.Variants.Count() {
			t.Errorf("variant count differs: forward %d, backward %d",
				forward.Variants.Count(), backward.Variants.Count())
		}
		if forward.States.Count() != backward.States.Count() {
			t.Errorf("state count differs: forward %d, backward %d",
				forward.States.Count(), backward.States.Count())
		}
	}
}

// Abbruch über den Status-Callback: das Netzwerk bleibt unverändert
func TestMergeAbort(t *testing.T) {
	n := buildNetwork(t, mapTwoBox)
	roomsBefore := len(n.Rooms)
	merged, err := n.MergeRooms(n.Rooms[0], n.Rooms[1], 0, func(string, []*Room) bool { return false })
	if err != nil {
		t.Fatal("merge:", err)
	}
	if merged != nil {
		t.Error("merge not aborted")
	}
	if len(n.Rooms) != roomsBefore {
		t.Errorf("rooms changed after abort: got %d, want %d", len(n.Rooms), roomsBefore)
	}
	if err := n.Validate(true); err != nil {
		t.Error("validate after abort:", err)
	}
}

// MergeSelection mit unverbundenen Räumen: es passiert nichts, kein Fehler
func TestMergeSelectionUnconnected(t *testing.T) {
	n := buildNetwork(t, mapTwoBox)
	// Raum 0 und der letzte Raum liegen nicht nebeneinander
	merges, err := n.MergeSelection([]uint32{0, uint32(len(n.Rooms) - 1)}, 0, nil)
	if err != nil {
		t.Fatal("merge:", err)
	}
	if merges != 0 {
		t.Errorf("merges: got %d, want 0", merges)
	}
}

// Moves-Budget beim Mergen: Verbund-Varianten über min1+min2+Slack werden
// gar nicht erst erzeugt. Mit Limit = exaktem Optimum (TwoBox: 9) muss der
// Voll-Merge trotzdem die Optimallösung liefern; mit großzügigem Budget
// (202: Optimum 83) muss die Kammer identisch zum ungebremsten Merge sein
func TestMergeWithMoveLimit(t *testing.T) {
	n := buildNetwork(t, mapTwoBox)
	indices := make([]uint32, len(n.Rooms))
	for i := range indices {
		indices[i] = uint32(i)
	}
	if _, err := n.MergeSelection(indices, 9, nil); err != nil {
		t.Fatal("merge:", err)
	}
	if len(n.Rooms) != 1 {
		t.Fatalf("full merge incomplete: %d rooms left", len(n.Rooms))
	}
	minMoves, minPath := checkFullMerge(t, n.Rooms[0])
	if minMoves != 9 {
		t.Errorf("optimum mit merge-budget: %d moves (%q), want 9", minMoves, minPath)
	}

	n2 := buildNetwork(t, maps.Map202)
	if _, err := n2.MergeSelection([]uint32{11, 18, 24, 25, 26, 33, 34, 35, 36, 46, 47, 48}, 83, nil); err != nil {
		t.Fatal("merge 202:", err)
	}
	for _, room := range n2.Rooms {
		if len(room.Fields) == 12 {
			if room.States.Count() != 10 || room.Variants.Count() != 25 {
				t.Errorf("kammer mit budget 83: %d states / %d varianten, want 10 / 25",
					room.States.Count(), room.Variants.Count())
			}
			return
		}
	}
	t.Fatal("merged chamber not found")
}
