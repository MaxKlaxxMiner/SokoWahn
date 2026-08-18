package rooms

import (
	"testing"

	"goSokoWahnRooms/maps"
)

// Der Kandidaten-Finder an der 202er-Kammer: Greedy-Elimination muss eine
// bewiesen kostengleiche, lokal minimale Menge finden - erwartet wird die
// Größenordnung der Handrechnung (Max' Minimal-These: 7 Varianten).
func TestReduceVariants202(t *testing.T) {
	_, room := merge202Chamber(t)

	result := reduceVariants(room, 100000)
	t.Logf("behalten (%d): %v", len(result.Kept), result.Kept)
	t.Logf("gestrichen (%d): %v", len(result.Removed), result.Removed)
	t.Logf("eliminierte Zustände (%d): %v", len(result.RemovedStates), result.RemovedStates)
	t.Logf("unentschieden (%d): %v", len(result.Undecided), result.Undecided)
	t.Logf("abschluss-beweis: %s", result.Detail)

	if len(result.Undecided) != 0 {
		t.Errorf("unentschiedene Kandidaten: %v", result.Undecided)
	}
	// das Zwei-Phasen-Greedy trifft exakt die Handrechnung (Max' 7er-Menge)
	if len(result.Kept) != len(lab202MinimalSet) {
		t.Errorf("Greedy behält %d Varianten, Handrechnung braucht %d",
			len(result.Kept), len(lab202MinimalSet))
	}
	for _, vid := range result.Kept {
		if !lab202MinimalSet[vid] {
			t.Errorf("Greedy behält v%d, die nicht in der Handrechnungs-Menge liegt", vid)
		}
	}
	if want := int(room.States.Count()) - 5; len(result.RemovedStates) != want {
		t.Errorf("Phase 1 eliminiert %d Zustände statt %d (5 von %d bleiben)",
			len(result.RemovedStates), want, room.States.Count())
	}
}

// Der Finder über alle geeigneten Räume eines frisch initialisierten
// Netzwerks (Ein-Portal-Räume ohne Startvarianten = Sackgassen): zeigt, wie
// viel die Dominanzsuche schon ohne Merges hergibt
func TestReduceVariantsFreshNetworks(t *testing.T) {
	for _, tc := range []struct {
		name  string
		level string
	}{
		{"202", maps.Map202},
		{"vanilla", maps.MapVanilla},
	} {
		n := buildNetwork(t, tc.level)
		rooms, variants, removed := 0, 0, 0
		for _, room := range n.Rooms {
			if !canReduceVariants(room) {
				continue
			}
			result := reduceVariants(room, 100000)
			rooms++
			variants += int(room.Variants.Count())
			removed += len(result.Removed)
			if len(result.Undecided) != 0 {
				t.Errorf("%s raum %d: unentschiedene Kandidaten %v",
					tc.name, room.Index, result.Undecided)
			}
		}
		t.Logf("%s: %d Ein-Portal-Räume, %d/%d Varianten entbehrlich",
			tc.name, rooms, removed, variants)
	}
}
