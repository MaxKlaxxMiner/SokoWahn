package rooms

import (
	"strings"
	"testing"

	"goSokoWahnRooms/maps"
)

// Regressionstest für Merge + Deadlock-Scan zusammen: die ersten 20 Räume des
// Vanilla-Levels werden gemergt, der Scan im MergeRooms-Gating läuft mit.
// Kennzahlen eingefroren am 2026-08-17 (5 Scans entfernen zusammen 1164
// Varianten, Effort sinkt von 6,5e40 auf 7,8e30).
func TestMergeWithDeadlockScanVanilla(t *testing.T) {
	n := buildNetwork(t, maps.MapVanilla)

	removedMsgs := 0
	info := func(msg string) bool {
		if strings.Contains(msg, "remove") {
			removedMsgs++
		}
		return true
	}
	indices := make([]uint32, 20)
	for i := range indices {
		indices[i] = uint32(i)
	}
	merges, err := n.MergeSelection(indices, info)
	if err != nil {
		t.Fatal("merge:", err)
	}
	if merges != 18 || len(n.Rooms) != 38 {
		t.Errorf("merges: got %d (%d rooms), want 18 (38 rooms)", merges, len(n.Rooms))
	}
	if removedMsgs != 5 {
		t.Errorf("scan removals: got %d messages, want 5", removedMsgs)
	}

	states, variants := uint64(0), uint64(0)
	for _, room := range n.Rooms {
		states += room.States.Count()
		variants += room.Variants.Count()
	}
	if states != 211 || variants != 7879 {
		t.Errorf("totals: got %d states / %d variants, want 211 / 7879", states, variants)
	}
	if got := n.EffortString(); !strings.HasPrefix(got, "7,815e30") {
		t.Errorf("effort: got %s, want 7,815e30 (...)", got)
	}
}

// OptimizeRooms als eigenständige Aktion: ein zweiter Scan direkt nach den
// Merges darf nichts mehr finden (die Merge-Scans haben schon aufgeräumt)
// und muss das Netzwerk gültig hinterlassen
func TestOptimizeRoomsIdempotent(t *testing.T) {
	n := buildNetwork(t, maps.MapVanilla)
	indices := make([]uint32, 10)
	for i := range indices {
		indices[i] = uint32(i)
	}
	if _, err := n.MergeSelection(indices, nil); err != nil {
		t.Fatal("merge:", err)
	}

	all := make([]uint32, len(n.Rooms))
	for i := range all {
		all[i] = uint32(i)
	}
	removed, err := n.OptimizeRooms(all, nil)
	if err != nil {
		t.Fatal("optimize:", err)
	}
	if removed != 0 {
		t.Errorf("second scan removed %d variants, want 0", removed)
	}
}

// Abbruch über den Status-Callback: der Raum bleibt unverändert
func TestDeadlockScanAbort(t *testing.T) {
	n := buildNetwork(t, mapTwoBox)
	room := n.Rooms[1]
	statesBefore, variantsBefore := room.States.Count(), room.Variants.Count()
	if _, ok := n.DeadlockScan(room, func(string) bool { return false }); ok {
		t.Error("scan not aborted")
	}
	if room.States.Count() != statesBefore || room.Variants.Count() != variantsBefore {
		t.Error("room changed after abort")
	}
	if err := n.Validate(true); err != nil {
		t.Error("validate after abort:", err)
	}
}
