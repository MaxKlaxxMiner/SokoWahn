package rooms

import (
	"fmt"
	"sort"
	"testing"

	"goSokoWahnRooms/maps"
	"goSokoWahnRooms/soko"
)

// Vergleichs-Framework gegen das C#-Original: die linke Kammer von Level 202
// (GUI-Räume 12,19,25-27,34-37,47-49) wird wie beim Start gemergt und gegen den
// Dump von SokoWahn/roomscli/roomscli.exe gehalten (dort dieselben Merges auf
// der SokoWahnLib). Kein bitgenauer Vergleich - verglichen werden die
// Zustands-MENGEN und Kennzahlen, nicht die IDs.
//
// Weitere Vergleichsfälle nach diesem Muster ergänzen: roomscli.cs erweitern,
// Dump ablesen, hier verankern.

// merged die linke Kammer von Level 202 und liefert Netzwerk + Kammer-Raum
func merge202Chamber(t *testing.T) (*Network, *Room) {
	t.Helper()
	n := buildNetwork(t, maps.Map202)
	if _, err := n.MergeSelection([]uint32{11, 18, 24, 25, 26, 33, 34, 35, 36, 46, 47, 48}, 0, nil); err != nil {
		t.Fatal("merge:", err)
	}
	for _, room := range n.Rooms {
		if len(room.Fields) == 12 {
			return n, room
		}
	}
	t.Fatal("merged chamber not found")
	return nil, nil
}

// Zustands-Menge eines Raums als sortierte, kanonische Strings ("11+36", "leer")
func stateSet(room *Room) []string {
	result := make([]string, 0, room.States.Count())
	for id := uint64(0); id < room.States.Count(); id++ {
		key := ""
		for i, pos := range room.States.Get(id) {
			if i > 0 {
				key += "+"
			}
			key += fmt.Sprint(uint32(pos))
		}
		if key == "" {
			key = "leer"
		}
		result = append(result, key)
	}
	sort.Strings(result)
	return result
}

// Orakel-Vergleich (C#-Dump vom 2026-08-17): 10 Zustände, 25 Varianten,
// Startzustand leer; Kisten können nur auf der Ziel-Säule (11,18,26,25) und
// dem Portalfeld (36) stehen, maximal zwei zugleich (zweite immer auf 36).
// Der Zustand "Ziel belegt + Zweitkiste" (11+36) hat genau eine Variante:
// Zweitkiste wieder rausschieben (9 Moves, 5 Pushes) - alles andere ist per
// Selbes-Portal-Regel dominiert und wegoptimiert.
func TestOracle202Chamber(t *testing.T) {
	_, room := merge202Chamber(t)

	if room.States.Count() != 10 || room.Variants.Count() != 25 {
		t.Errorf("chamber: got %d states / %d variants, want 10 / 25 (wie C#)",
			room.States.Count(), room.Variants.Count())
	}
	if len(room.Incoming) != 1 {
		t.Fatalf("chamber: got %d portals, want 1", len(room.Incoming))
	}
	if room.States.BoxCount(room.StartState) != 0 {
		t.Error("start state not empty")
	}

	// Zustands-Mengen aus dem C#-Dump (x/y -> Wpos: 3/4=11, 3/5=18, 3/6=26, 2/6=25, 4/7=36)
	want := []string{"11", "11+36", "18", "18+36", "25", "25+36", "26", "26+36", "36", "leer"}
	got := stateSet(room)
	if fmt.Sprint(got) != fmt.Sprint(want) {
		t.Errorf("states:\n got %v\nwant %v", got, want)
	}

	// Zustand {11,36}: genau eine Variante, Kiste raus (wie C#: 9 Moves, 5 Pushes)
	ip := room.Incoming[0]
	for id := uint64(0); id < room.States.Count(); id++ {
		if fmt.Sprint(room.States.Get(id)) != fmt.Sprint([]soko.Wpos{11, 36}) {
			continue
		}
		span := ip.GetVariantSpan(id)
		if span.Count != 1 {
			t.Fatalf("state {11,36}: got %d variants, want 1", span.Count)
		}
		v := room.Variants.Get(span.Start)
		if v.Moves != 9 || v.Pushes != 5 || len(v.BoxPortals) != 1 || room.States.BoxCount(v.NewState) != 1 {
			t.Errorf("state {11,36} variant: got moves=%d pushes=%d boxPortals=%v, want 9/5/[0]",
				v.Moves, v.Pushes, v.BoxPortals)
		}
	}
}

// Dump der Kammer für die Sichtprüfung (nur mit -v sichtbar):
// alle Zustände als Feld-Skizze plus Varianten und BoxSwaps des Portals
func TestDump202Chamber(t *testing.T) {
	n, room := merge202Chamber(t)
	f := n.Field

	t.Logf("chamber: index=%d states=%d variants=%d startState=%d",
		room.Index, room.States.Count(), room.Variants.Count(), room.StartState)

	w := f.Width()
	inRoom := map[soko.Wpos]bool{}
	for _, p := range room.Fields {
		inRoom[p] = true
	}
	for id := uint64(0); id < room.States.Count(); id++ {
		boxes := map[soko.Wpos]bool{}
		for _, b := range room.States.Get(id) {
			boxes[b] = true
		}
		t.Logf("state %d: boxes=%v", id, room.States.Get(id))
		grid := make([][]byte, f.Height())
		for y := range grid {
			grid[y] = make([]byte, w)
			for x := range grid[y] {
				grid[y][x] = ' '
			}
		}
		for p := soko.Wpos(0); p < f.WalkEof(); p++ {
			pos := f.FieldPos(p)
			c := byte(' ')
			switch {
			case boxes[p] && f.IsGoal(p):
				c = '*'
			case boxes[p]:
				c = '$'
			case inRoom[p] && f.IsGoal(p):
				c = '.'
			case inRoom[p]:
				c = '_'
			}
			grid[pos/w][pos%w] = c
		}
		for y := 3; y < 10; y++ {
			t.Logf("  %s", string(grid[y][:8]))
		}
	}

	for _, ip := range room.Incoming {
		t.Logf("portal %d (%c %d->%d):", ip.Index, ip.Dir, ip.From, ip.To)
		for state := uint64(0); state < room.States.Count(); state++ {
			span := ip.GetVariantSpan(state)
			for id := span.Start; id < span.Start+span.Count; id++ {
				v := room.Variants.Get(id)
				t.Logf("  state %d variant %d: new=%d moves=%d pushes=%d boxPortals=%v exit=%d path=%q",
					state, id, v.NewState, v.Moves, v.Pushes, v.BoxPortals, int32(v.PlayerPortal), v.Path.LURD())
			}
			if to, exists := ip.BoxSwap[state]; exists {
				t.Logf("  boxswap %d -> %d", state, to)
			}
		}
	}
}
