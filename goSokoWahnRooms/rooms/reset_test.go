package rooms

import (
	"fmt"
	"testing"
)

// vergleicht ein Netzwerk Raum für Raum mit einem frisch initialisierten
// (Felder, Zustände, Varianten, Portale samt Verzeichnissen)
func checkFreshEqual(t *testing.T, n, fresh *Network) {
	t.Helper()
	if len(n.Rooms) != len(fresh.Rooms) {
		t.Fatalf("rooms: got %d, want %d", len(n.Rooms), len(fresh.Rooms))
	}
	for i, room := range n.Rooms {
		want := fresh.Rooms[i]
		if fmt.Sprint(room.Fields) != fmt.Sprint(want.Fields) {
			t.Fatalf("room %d fields: got %v, want %v", i, room.Fields, want.Fields)
		}
		if room.States.Count() != want.States.Count() || room.StartState != want.StartState {
			t.Errorf("room %d states: got %d/start %d, want %d/start %d",
				i, room.States.Count(), room.StartState, want.States.Count(), want.StartState)
		}
		if room.Variants.Count() != want.Variants.Count() || room.StartVariantCount != want.StartVariantCount {
			t.Errorf("room %d variants: got %d/%d starts, want %d/%d starts",
				i, room.Variants.Count(), room.StartVariantCount, want.Variants.Count(), want.StartVariantCount)
		}
		for id := uint64(0); id < min(room.Variants.Count(), want.Variants.Count()); id++ {
			if fmt.Sprint(*room.Variants.Get(id)) != fmt.Sprint(*want.Variants.Get(id)) {
				t.Errorf("room %d variant %d: got %+v, want %+v", i, id, room.Variants.Get(id), want.Variants.Get(id))
			}
		}
		if len(room.Incoming) != len(want.Incoming) {
			t.Fatalf("room %d portals: got %d, want %d", i, len(room.Incoming), len(want.Incoming))
		}
		for pi, p := range room.Incoming {
			wp := want.Incoming[pi]
			if p.From != wp.From || p.To != wp.To || p.Dir != wp.Dir || p.BlockedBox != wp.BlockedBox {
				t.Errorf("room %d portal %d: got %v, want %v", i, pi, p, wp)
			}
			if fmt.Sprint(p.BoxSwap) != fmt.Sprint(wp.BoxSwap) || fmt.Sprint(p.VariantSpans) != fmt.Sprint(wp.VariantSpans) {
				t.Errorf("room %d portal %d verzeichnisse: got %v/%v, want %v/%v",
					i, pi, p.BoxSwap, p.VariantSpans, wp.BoxSwap, wp.VariantSpans)
			}
		}
	}
}

// Voll-Merge und Reset: das Netzwerk muss wieder exakt dem frisch
// initialisierten entsprechen, und ein erneuter Voll-Merge muss das
// bekannte Optimum liefern (Anker: TwoBox 9 Züge)
func TestResetAfterFullMerge(t *testing.T) {
	n := buildNetwork(t, mapTwoBox)
	fullMerge(t, n)

	count, err := n.ResetRooms([]uint32{0}, nil)
	if err != nil {
		t.Fatal("reset:", err)
	}
	if count != 1 {
		t.Fatalf("reset count: got %d, want 1", count)
	}
	checkFreshEqual(t, n, buildNetwork(t, mapTwoBox))

	minMoves, _ := checkFullMerge(t, fullMerge(t, n))
	if minMoves != 9 {
		t.Errorf("min moves nach reset+merge: got %d, want 9", minMoves)
	}
}

// Teil-Reset: nur der gemergte Raum wird zurückgesetzt, die übrigen Räume
// (samt Objekt-Identität) bleiben unangetastet
func TestResetPartial(t *testing.T) {
	n := buildNetwork(t, mapMini)
	fresh := buildNetwork(t, mapMini)
	keep := n.Rooms[0] // Startraum bleibt beim Merge der Räume 1+2 außen vor

	merged, err := n.MergeRooms(n.Rooms[1], n.Rooms[2], 0, nil)
	if err != nil {
		t.Fatal("merge:", err)
	}
	count, err := n.ResetRooms([]uint32{merged.Index}, nil)
	if err != nil {
		t.Fatal("reset:", err)
	}
	if count != 1 {
		t.Fatalf("reset count: got %d, want 1", count)
	}
	if n.Rooms[0] != keep {
		t.Error("unbeteiligter Raum wurde ersetzt")
	}
	checkFreshEqual(t, n, fresh)
}

// Reset einer Auswahl aus 1-Feld-Räumen ist ein No-Op
func TestResetNoop(t *testing.T) {
	n := buildNetwork(t, mapMini)
	count, err := n.ResetRooms([]uint32{0, 1, 2}, nil)
	if err != nil {
		t.Fatal("reset:", err)
	}
	if count != 0 {
		t.Fatalf("reset count: got %d, want 0", count)
	}
	checkFreshEqual(t, n, buildNetwork(t, mapMini))
}
