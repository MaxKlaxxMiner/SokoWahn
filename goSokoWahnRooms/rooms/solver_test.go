package rooms

import (
	"testing"

	"goSokoWahnRooms/maps"
)

// treibt eine Solver-Suche bis zum Ende und liefert die Lösung
func runSolver(t *testing.T, n *Network, maxMoves uint32) *Solution {
	t.Helper()
	s, err := NewSolver(n, maxMoves)
	if err != nil {
		t.Fatal("new solver:", err)
	}
	for !s.Step(100000) {
	}
	if err := s.Err(); err != nil {
		t.Fatal("solver:", err)
	}
	return s.Solution()
}

// prüft eine Lösung gegen das Spielfeld und die erwartete Zugzahl
func checkSolution(t *testing.T, n *Network, sol *Solution, wantMoves uint32) {
	t.Helper()
	if sol == nil {
		t.Fatal("keine lösung gefunden")
	}
	if sol.Moves != wantMoves {
		t.Errorf("moves: got %d (%q), want %d", sol.Moves, sol.Path, wantMoves)
	}
	if int(sol.Moves) != len(sol.Path) {
		t.Errorf("moves != len(path): %d != %d", sol.Moves, len(sol.Path))
	}
	if err := n.Field.CheckSolution(sol.Path); err != nil {
		t.Errorf("lösung ungültig: %v (%q)", err, sol.Path)
	}
}

// Levels frisch (1-Feld-Räume, ganz ohne Mergen): bekannte Optima; Level 200
// ist Max' Referenz für "muss ohne jeden Merge lösbar sein" (löst in ~10 ms)
func TestSolveFresh(t *testing.T) {
	for _, tc := range []struct {
		name  string
		m     string
		moves uint32
	}{
		{"mini", mapMini, 1},
		{"twopush", mapTwoPush, 2},
		{"twobox", mapTwoBox, 9},
		{"200", maps.Map200, 78},
	} {
		n := buildNetwork(t, tc.m)
		sol := runSolver(t, n, 0)
		checkSolution(t, n, sol, tc.moves)
		t.Logf("%s: %d moves / %d pushes (%s)", tc.name, sol.Moves, sol.Pushes, sol.Path)
	}
}

// Solver nach Merges: Teil-Merge und Voll-Merge müssen dasselbe Optimum
// liefern wie das frische Netzwerk (TwoBox: 9)
func TestSolveAfterMerge(t *testing.T) {
	// Teil-Merge: die ersten vier Räume verschmelzen
	n := buildNetwork(t, mapTwoBox)
	if _, err := n.MergeSelection([]uint32{0, 1, 2, 3}, 0, nil); err != nil {
		t.Fatal("merge:", err)
	}
	checkSolution(t, n, runSolver(t, n, 0), 9)

	// Voll-Merge: die Lösung steckt komplett in den Startvarianten - die
	// Suche ist damit schon bei der Initialisierung entschieden (die GUI
	// bietet die Lösung sofort an, ohne ersten Bulk)
	n2 := buildNetwork(t, mapTwoBox)
	fullMerge(t, n2)
	s, err := NewSolver(n2, 0)
	if err != nil {
		t.Fatal("new solver:", err)
	}
	if !s.Done() {
		t.Error("voll-merge: suche nicht sofort entschieden")
	}
	checkSolution(t, n2, s.Solution(), 9)
}

// Bulk-Grenze wie im C#-Original und in brute: ein Step arbeitet höchstens
// die aktuelle Tiefenzeile EINER Front ab, auch wenn das Budget größer ist -
// erst der nächste Step geht die nächste Zeile an
func TestSolveStepDepthBound(t *testing.T) {
	n := buildNetwork(t, mapTwoBox)
	s, err := NewSolver(n, 0)
	if err != nil {
		t.Fatal("new solver:", err)
	}
	steps := 0
	lastFwd, lastBwd := -1, -1
	for !s.Step(1000000) {
		steps++
		if s.fwd.depth == lastFwd && s.bwd.depth == lastBwd {
			t.Fatalf("step %d: tiefe %d+%d nicht weitergeschaltet (bulk >= zeilengröße)",
				steps, s.fwd.depth, s.bwd.depth)
		}
		lastFwd, lastBwd = s.fwd.depth, s.bwd.depth
		if steps > 100 {
			t.Fatal("suche endet nicht")
		}
	}
	if steps < 2 {
		t.Errorf("suche endete nach %d steps - ein bulk darf nur eine tiefenzeile abarbeiten", steps)
	}
	checkSolution(t, n, s.Solution(), 9)
}

// Richtungsvorgaben wie in brute (Tasten 1/2/3): reine Vorwärts-, reine
// Rückwärts- und automatische Suche müssen dasselbe bewiesene Optimum
// liefern; Schein-Treffen (Hash-Kollisionen) dürfen auf den kleinen
// Levels nicht auftreten
func TestSolveDirModes(t *testing.T) {
	for _, tc := range []struct {
		name  string
		m     string
		moves uint32
	}{
		{"twobox", mapTwoBox, 9},
		{"200", maps.Map200, 78},
	} {
		for mode, modeName := range map[DirMode]string{
			DirForward: "vorwärts", DirBackward: "rückwärts", DirAuto: "auto",
		} {
			n := buildNetwork(t, tc.m)
			s, err := NewSolver(n, 0)
			if err != nil {
				t.Fatal("new solver:", err)
			}
			s.SetDirMode(mode)
			for !s.Step(100000) {
			}
			if err := s.Err(); err != nil {
				t.Fatalf("%s %s: %v", tc.name, modeName, err)
			}
			checkSolution(t, n, s.Solution(), tc.moves)
			if s.collisions != 0 {
				t.Errorf("%s %s: %d hash-kollisionen", tc.name, modeName, s.collisions)
			}
			t.Logf("%s %s: %d moves, tiefe %d+%d, verarbeitet %d+%d",
				tc.name, modeName, s.Solution().Moves, s.fwd.depth, s.bwd.depth,
				s.fwd.processed, s.bwd.processed)
		}
	}
}

// Budget-Beweis auch rückwärts: ein zu kleines Budget endet in jeder
// Richtung bewiesen ohne Lösung
func TestSolveBudgetBackward(t *testing.T) {
	for _, mode := range []DirMode{DirBackward, DirAuto} {
		n := buildNetwork(t, mapTwoBox)
		s, err := NewSolver(n, 8)
		if err != nil {
			t.Fatal("new solver:", err)
		}
		s.SetDirMode(mode)
		for !s.Step(100000) {
		}
		if sol := s.Solution(); sol != nil {
			t.Errorf("mode %d, budget 8: unerwartete lösung %d moves (%q)", mode, sol.Moves, sol.Path)
		}
	}
}

// Budget-Verhalten: exaktes Optimum als Budget findet die Lösung,
// ein zu kleines Budget endet bewiesen ohne Lösung
func TestSolveBudget(t *testing.T) {
	n := buildNetwork(t, mapTwoBox)
	checkSolution(t, n, runSolver(t, n, 9), 9)

	n2 := buildNetwork(t, mapTwoBox)
	if sol := runSolver(t, n2, 8); sol != nil {
		t.Errorf("budget 8: unerwartete lösung %d moves (%q)", sol.Moves, sol.Path)
	}
}

// Level 202 frisch (1-Feld-Räume, ganz ohne Mergen): Optimum 83 Züge
// (aenigma "soko 03", siehe maps.Map202) - der Anker des Solvers
// (bidirektionale Automatik ~6 s; rein vorwärts waren es ~21 s)
func TestSolve202Fresh(t *testing.T) {
	skipAnker(t)
	n := buildNetwork(t, maps.Map202)
	sol := runSolver(t, n, 0)
	checkSolution(t, n, sol, 83)
	t.Logf("202 frisch: %d moves / %d pushes", sol.Moves, sol.Pushes)
}

// Vanilla (Optimum 230 Züge / 97 Pushes, brute: 8,7 Mio. Knoten) läuft NICHT
// im Testlauf - frisch OHNE Budget beweist die bidirektionale Automatik das
// Optimum in ~50 s (12,7 Mio. Hash-Einträge, 0 Kollisionen; rein vorwärts
// mit Budget 230 waren es ~65 s, rein rückwärts ~122 s); als Dauer-Anker
// zu teuer, live in der GUI jederzeit nachstellbar (gemessen 2026-08-20).
