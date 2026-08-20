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

	// Voll-Merge: die Lösung steckt komplett in den Startvarianten
	n2 := buildNetwork(t, mapTwoBox)
	fullMerge(t, n2)
	checkSolution(t, n2, runSolver(t, n2, 0), 9)
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
// (aenigma "soko 03", siehe maps.Map202) - der Anker des Solvers (~20 s)
func TestSolve202Fresh(t *testing.T) {
	if testing.Short() {
		t.Skip("volle 202er-suche im short-modus übersprungen")
	}
	n := buildNetwork(t, maps.Map202)
	sol := runSolver(t, n, 0)
	checkSolution(t, n, sol, 83)
	t.Logf("202 frisch: %d moves / %d pushes", sol.Moves, sol.Pushes)
}

// Vanilla mit Teil-Merging (Max' Klassiker-Szenario): erst zwei 16er-Gruppen
// mergen (Budget = bekanntes Optimum 230 aus goSokoWahnBrute), dann lösen -
// der Solver muss das Brute-Optimum beweisen (~50 s, brute: 8,7 Mio. Knoten)
func TestSolveVanillaMerged(t *testing.T) {
	if testing.Short() {
		t.Skip("vanilla-suche im short-modus übersprungen")
	}
	n := buildNetwork(t, maps.MapVanilla)
	var sel1 []uint32
	for i := 0; i < 16; i++ {
		sel1 = append(sel1, uint32(i))
	}
	if _, err := n.MergeSelection(sel1, 230, nil); err != nil {
		t.Fatal("merge:", err)
	}
	var sel2 []uint32
	for i := 0; i < 16; i++ {
		sel2 = append(sel2, uint32(len(n.Rooms)-1-i))
	}
	if _, err := n.MergeSelection(sel2, 230, nil); err != nil {
		t.Fatal("merge:", err)
	}
	sol := runSolver(t, n, 230)
	checkSolution(t, n, sol, 230)
	t.Logf("vanilla teil-gemergt (%d räume): %d moves / %d pushes", len(n.Rooms), sol.Moves, sol.Pushes)
}
