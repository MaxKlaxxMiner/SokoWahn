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

	result := reduceVariants(room, nil, 100000, 0, false, nil)
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

// Der Finder über ALLE Räume eines frisch initialisierten Netzwerks (seit
// der Mehr-Portal-Signatur auch Mehr-Portal- und Startvarianten-Räume):
// zeigt, wie viel die Dominanzsuche schon ohne Merges hergibt
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
			if room.Variants.Count() == 0 {
				continue
			}
			result := reduceVariants(room, n.usageEnv(room), 100000, 0, false, nil)
			rooms++
			variants += int(room.Variants.Count())
			removed += len(result.Removed)
			if len(result.Undecided) != 0 {
				t.Errorf("%s raum %d: unentschiedene Kandidaten %v",
					tc.name, room.Index, result.Undecided)
			}
		}
		t.Logf("%s: %d Räume, %d/%d Varianten entbehrlich",
			tc.name, rooms, removed, variants)
	}
}

// End-to-End-Soundness der Mehr-Portal-Dominanz (2026-08-20): erst wird
// JEDER Raum des frischen Netzwerks dominanz-reduziert (inklusive
// Mehr-Portal- und Startvarianten-Räume - vor der Mehr-Portal-Signatur
// wurden die übersprungen), dann alles zu einem Raum gemergt. Das Optimum
// muss dem Brute-Orakel entsprechen - die Dominanz darf also keine für die
// Optimallösung nötige Variante gestrichen haben.
func TestDominanceThenFullMerge(t *testing.T) {
	for _, tc := range []struct {
		name  string
		level string
		moves uint32
	}{
		{"mini", mapMini, 1},
		{"twopush", mapTwoPush, 2},
		{"twobox", mapTwoBox, 9},
	} {
		n := buildNetwork(t, tc.level)
		removed := uint64(0)
		for _, room := range n.Rooms {
			count, ok := n.DominanceReduce(room, 0, nil)
			if !ok {
				t.Fatalf("%s: dominance raum %d abgebrochen", tc.name, room.Index)
			}
			removed += count
		}
		if err := n.Validate(true); err != nil {
			t.Fatalf("%s: validate nach dominanz: %v", tc.name, err)
		}
		room := fullMerge(t, n)
		moves, path := checkFullMerge(t, room)
		if moves != tc.moves {
			t.Errorf("%s: optimum nach dominanz %d moves (%q), want %d",
				tc.name, moves, path, tc.moves)
		}
		t.Logf("%s: %d varianten dominanz-entfernt, optimum %d moves (%q)",
			tc.name, removed, moves, path)
	}
}

// End-to-End über den Optimize-Pfad des Buttons: nach dem Merge der
// 202er-Kammer entfernt OptimizeRooms die 18 entbehrlichen Varianten
// wirklich aus dem Raum - übrig bleiben 7 Varianten auf 5 Zuständen
func TestOptimizeRoomsDominance202(t *testing.T) {
	n, room := merge202Chamber(t)

	all := make([]uint32, len(n.Rooms))
	for i := range all {
		all[i] = uint32(i)
	}
	removed, err := n.OptimizeRooms(all, 0, nil)
	if err != nil {
		t.Fatal("optimize:", err)
	}
	t.Logf("entfernt: %d varianten; kammer: %d states, %d varianten",
		removed, room.States.Count(), room.Variants.Count())

	if room.Variants.Count() != 7 {
		t.Errorf("kammer hat %d varianten, want 7", room.Variants.Count())
	}
	if room.States.Count() != 5 {
		t.Errorf("kammer hat %d states, want 5", room.States.Count())
	}

	// zweiter Lauf: nichts mehr zu holen
	removed, err = n.OptimizeRooms(all, 0, nil)
	if err != nil {
		t.Fatal("second optimize:", err)
	}
	if removed != 0 {
		t.Errorf("second optimize removed %d, want 0", removed)
	}
}

// Max-Moves-Budget (Max' Idee, 2026-08-19): eine verifizierte obere Schranke
// der Gesamtlösung kappt Nutzungen über dem Raum-Budget (Minimum + Slack).
// Korrektheit: mit Limit = exaktem Optimum muss die Optimallösung überleben
func TestOptimizeWithMoveLimit(t *testing.T) {
	// mapTwoBox: Optimum 9 Züge (Brute-verifiziert, TestMergeFullTwoBox)
	n := buildNetwork(t, mapTwoBox)

	all := make([]uint32, len(n.Rooms))
	for i := range all {
		all[i] = uint32(i)
	}
	removed, err := n.OptimizeRooms(all, 9, nil)
	if err != nil {
		t.Fatal("optimize:", err)
	}
	t.Logf("optimize mit maxMoves=9: %d entfernt", removed)

	room := fullMerge(t, n)
	minMoves, minPath := checkFullMerge(t, room)
	if minMoves != 9 {
		t.Errorf("optimum nach budget-optimize: %d moves (%q), want 9", minMoves, minPath)
	}
}

// ein Limit unter dem bewiesenen Minimum muss als Fehler gemeldet werden
// (statt still die Optimallösung wegzuwerfen); eine Schranke über der
// Minima-Summe, aber unter dem echten Optimum, wird vom Budget-Scan als
// bewiesen unerreichbar entlarvt; die Dominanz mit Slack 0 behält nur die
// billigsten Nutzungen (Kammer schrumpft unter die 7 des Normalfalls)
func TestOptimizeMoveLimitTight(t *testing.T) {
	n, room := merge202Chamber(t)

	minRoom := room.MinMoves()
	if minRoom == 0 {
		t.Fatal("Kammer ohne bewiesenes Minimum")
	}
	minTotal := uint64(0)
	for _, r := range n.Rooms {
		minTotal += r.MinMoves()
	}
	t.Logf("Minimum: Kammer %d, Netz gesamt %d", minRoom, minTotal)

	// Limit unter der Minima-Summe: sofortiger Fehler ohne Mutation
	if _, err := n.OptimizeRooms([]uint32{room.Index}, minTotal-1, nil); err == nil {
		t.Error("Limit unter dem Minimum wurde nicht als Fehler gemeldet")
	}

	// Dominanz direkt mit Slack 0 (Raum-Limit = Kammer-Minimum): nur die
	// billigsten Nutzungen überleben
	removed, ok := n.DominanceReduce(room, int64(minRoom), nil)
	if !ok {
		t.Fatal("dominance abgebrochen")
	}
	t.Logf("slack 0: %d entfernt -> %d states, %d varianten",
		removed, room.States.Count(), room.Variants.Count())
	if room.Variants.Count() >= 7 {
		t.Errorf("Kammer behält %d Varianten, mit Slack 0 erwartet < 7", room.Variants.Count())
	}
	if err := n.Validate(true); err != nil {
		t.Fatal("validate:", err)
	}
}

// eine Schranke zwischen Minima-Summe und echtem Optimum: der Budget-Scan
// muss die Unerreichbarkeit BEWEISEN (sauberer Fehler statt Validate-Crash)
func TestBudgetScanProvesUnreachable(t *testing.T) {
	n, _ := merge202Chamber(t)
	minTotal := uint64(0)
	for _, r := range n.Rooms {
		minTotal += r.MinMoves()
	}
	// 202-Optimum ist 83; minTotal (deutlich darunter) als falsche Schranke
	if _, err := n.OptimizeRooms([]uint32{0}, minTotal, nil); err == nil {
		t.Errorf("falsche Schranke %d wurde nicht als unerreichbar entlarvt", minTotal)
	} else {
		t.Logf("bewiesen: %v", err)
	}
}

// Budget-Schnellscan: streicht in JEDEM Raum (auch Mehr-Portal, wo die
// Dominanz nicht greift) Varianten über dem Distanz-Korridor-Limit; mit
// Limit = exaktem Optimum muss die Optimallösung überleben
func TestBudgetScan(t *testing.T) {
	// TwoBox frisch (Mehr-Portal-Räume!), Optimum 9 Züge
	n := buildNetwork(t, mapTwoBox)
	removed, ok, err := n.BudgetScan(9, nil)
	if err != nil || !ok {
		t.Fatalf("budget scan: removed=%d ok=%v err=%v", removed, ok, err)
	}
	if err := n.Validate(true); err != nil {
		t.Fatal("validate:", err)
	}
	t.Logf("twobox budget 9: %d varianten entfernt", removed)

	room := fullMerge(t, n)
	minMoves, minPath := checkFullMerge(t, room)
	if minMoves != 9 {
		t.Errorf("optimum nach budget scan: %d moves (%q), want 9", minMoves, minPath)
	}

	// Wirkung am ungemergten 202er-Netz (nur Mehr-Portal-Räume) mit dem
	// bekannten Optimum 83 als Schranke
	n2 := buildNetwork(t, maps.Map202)
	removed2, ok2, err2 := n2.BudgetScan(83, nil)
	if err2 != nil || !ok2 {
		t.Fatalf("budget scan 202: ok=%v err=%v", ok2, err2)
	}
	if err := n2.Validate(true); err != nil {
		t.Fatal("validate 202:", err)
	}
	t.Logf("202 budget 83: %d varianten entfernt", removed2)
}
