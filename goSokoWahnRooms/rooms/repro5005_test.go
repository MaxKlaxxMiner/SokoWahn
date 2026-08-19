package rooms

import (
	"fmt"
	"sort"
	"testing"

	"goSokoWahnRooms/maps"
)

// Max' Repro (2026-08-19) am unteren linken Bereich von 5005:
// Fall 1: alles unten OHNE das Kisten-Feld (2,6) mergen - die Kiste liegt
//   dann draußen vor dem Portal und kommt später von oben rein (Einschub).
//   Nach Optimize fehlte genau dieser Einschub am Start-Zustand -> Level tot.
// Fall 2: alles unten MIT dem Kisten-Feld - der Eingang ist durch die Kiste
//   blockiert, kein Einschub am Start nötig; Ergebnis war korrekt (Effort 6).

func mergeLower5005(t *testing.T, includeBoxField, optimizeBetween bool) (*Network, *Room) {
	t.Helper()
	n := buildNetwork(t, maps.Map5005)
	width := n.Field.Width()

	// Max' Region: alles unterhalb des Säulen-Felds (2,12) ("room 123");
	// Fall 2 nimmt drei Felder mehr bis inkl. Kiste (2,10)
	forbidden := 2 + 12*width
	if includeBoxField {
		forbidden = 2 + 9*width
	}
	roomAt := map[int]*Room{}
	for _, room := range n.Rooms {
		roomAt[n.Field.FieldPos(room.Fields[0])] = room
	}
	startPos := 2 + 13*width
	posSet := map[int]bool{startPos: true}
	var selection []uint32
	for queue := []int{startPos}; len(queue) > 0; queue = queue[1:] {
		selection = append(selection, roomAt[queue[0]].Index)
		for _, p := range roomAt[queue[0]].Outgoing {
			next := n.Field.FieldPos(p.To)
			if next != forbidden && !posSet[next] {
				posSet[next] = true
				queue = append(queue, next)
			}
		}
	}
	// Max' Strategie "von hinten heraus": immer einen Ein-Portal-Raum der
	// Auswahl mit seinem Nachbarn mergen und sofort optimieren - so bleiben
	// die Zwischen-Räume klein (alles-auf-einmal explodiert in Step3)
	inSelection := map[*Room]bool{}
	for _, idx := range selection {
		inSelection[n.Rooms[idx]] = true
	}
	for len(inSelection) > 1 {
		var a, b *Room
		bestPortals := 1 << 30
		for r := range inSelection {
			for _, op := range r.Outgoing {
				if inSelection[op.ToRoom] && len(r.Incoming) < bestPortals {
					bestPortals, a, b = len(r.Incoming), r, op.ToRoom
				}
			}
		}
		if a == nil {
			break
		}
		delete(inSelection, a)
		delete(inSelection, b)
		merged, err := n.MergeRooms(a, b, nil)
		if err != nil {
			t.Fatal("merge:", err)
		}
		if optimizeBetween {
			if _, err := n.OptimizeRooms([]uint32{merged.Index}, nil); err != nil {
				t.Fatal("optimize:", err)
			}
		}
		inSelection[merged] = true
	}
	for _, r := range n.Rooms {
		if len(r.Fields) > 10 {
			return n, r
		}
	}
	t.Fatal("merged room not found")
	return nil, nil
}

func dumpRoomStatus(t *testing.T, label string, room *Room) {
	t.Helper()
	ip := room.Incoming[0]
	var swaps []string
	for from, to := range ip.BoxSwap {
		swaps = append(swaps, fmt.Sprintf("%d->%d", from, to))
	}
	sort.Strings(swaps)
	g := buildUsageGraph(room, nil)
	sigs := g.signatures(7)
	var words []string
	for sig := range sigs {
		words = append(words, sig)
	}
	sort.Strings(words)
	t.Logf("%s: states=%d varianten=%d start=%d boxSwap=%v graphstart=%d wörter(bis 7)=%v",
		label, room.States.Count(), room.Variants.Count(), room.StartState, swaps, g.start, words)
}

func TestRepro5005Case1(t *testing.T) {
	if testing.Short() {
		t.Skip("langer Lauf (5005-Regression)")
	}
	n, room := mergeLower5005(t, false, true)
	dumpRoomStatus(t, "nach merge", room)

	if _, ok := n.DeadlockScan(room, nil); !ok {
		t.Fatal("scan abgebrochen")
	}
	dumpRoomStatus(t, "nach scan", room)

	if _, ok := n.DominanceReduce(room, nil); !ok {
		t.Fatal("dominance abgebrochen")
	}
	dumpRoomStatus(t, "nach dominanz", room)

	// der Einschub am Start-Zustand muss ueberleben (die Kiste vor dem
	// Portal MUSS irgendwann rein, sonst kommt der Spieler nie nach unten)
	if _, exists := room.Incoming[0].BoxSwap[room.StartState]; !exists {
		t.Error("REPRO: BoxSwap am Start-Zustand verschwunden - Level tot")
	}
}

func TestRepro5005Case2(t *testing.T) {
	if testing.Short() {
		t.Skip("langer Lauf (5005-Regression)")
	}
	n, room := mergeLower5005(t, true, true)
	dumpRoomStatus(t, "nach merge", room)
	if _, err := n.OptimizeRooms([]uint32{room.Index}, nil); err != nil {
		t.Fatal("optimize:", err)
	}
	dumpRoomStatus(t, "nach optimize", room)
}

// Trennung der Verdächtigen: Fall 1 OHNE Zwischen-Optimize gemergt - liefert
// der Merger den Einschub am Start-Zustand korrekt, und killt ihn erst die
// Dominanzsuche?
func TestRepro5005Case1NoInterOptimize(t *testing.T) {
	n, room := mergeLower5005(t, false, false)
	dumpRoomStatus(t, "nach merge (ohne zwischen-optimize)", room)
	hadSwap := false
	if _, exists := room.Incoming[0].BoxSwap[room.StartState]; exists {
		hadSwap = true
	}
	t.Logf("BoxSwap am StartState nach Merge: %v", hadSwap)

	if _, ok := n.DominanceReduce(room, nil); !ok {
		t.Fatal("dominance abgebrochen")
	}
	dumpRoomStatus(t, "nach dominanz", room)
	if _, exists := room.Incoming[0].BoxSwap[room.StartState]; hadSwap && !exists {
		t.Error("TÄTER GEFUNDEN: Dominanz + Kaskade entfernen den Start-Einschub")
	}
}
