package solver

import (
	"testing"

	"goSokoWahnBrute/maps"
	"goSokoWahnBrute/soko"
)

// löst ein Level komplett und prüft die Lösung auf Konsistenz
func solveLevel(t *testing.T, level string, expectedMoves int) (*Solver, *Solution) {
	t.Helper()

	field, err := soko.Parse(level)
	if err != nil {
		t.Fatal(err)
	}

	s := New(field)
	for s.Step(1000000000) {
	}

	stats := s.GetStats()
	if !stats.Done {
		t.Fatal("Suche wurde nicht abgeschlossen")
	}
	if stats.FoundMoves != expectedMoves {
		t.Fatalf("erwartete Lösungslänge %d, erhalten: %d", expectedMoves, stats.FoundMoves)
	}

	solution, err := s.GetSolution()
	if err != nil {
		t.Fatal(err)
	}
	if len(solution.Moves) != expectedMoves {
		t.Fatalf("erwartete %d Züge in der LURD-Notation, erhalten: %d (%s)", expectedMoves, len(solution.Moves), solution.Moves)
	}

	// erste Stellung muss die Startstellung sein
	start := soko.State{}
	field.GetState(&start)
	if solution.States[0].Crc != start.Crc {
		t.Error("Lösungsweg beginnt nicht bei der Startstellung")
	}

	// letzte Stellung muss gelöst sein
	work := field.Clone()
	work.SetState(&solution.States[len(solution.States)-1])
	if !work.IsSolved() {
		t.Errorf("letzte Stellung des Lösungswegs ist nicht gelöst:\n%s", work)
	}

	return s, solution
}

// 1-Schub-Level: läuft über den forwardOnly-Sonderfall (keine Zielstellungen)
func TestSolveMini(t *testing.T) {
	_, solution := solveLevel(t, `
#####
#@$.#
#####
`, 1)
	if solution.Moves != "R" {
		t.Errorf("erwartete Zugfolge 'R', erhalten: %q", solution.Moves)
	}
}

// 2-Schub-Level: kleinster Fall der normalen bidirektionalen Suche
func TestSolveTwoPush(t *testing.T) {
	_, solution := solveLevel(t, `
######
#@$ .#
######
`, 2)
	if solution.Moves != "RR" {
		t.Errorf("erwartete Zugfolge 'RR', erhalten: %q", solution.Moves)
	}
}

// kleines Mehrkisten-Level (Referenzlösung: refcli = 16 Züge)
func TestSolveSmall(t *testing.T) {
	solveLevel(t, `
#######
#.@ # #
#$* $ #
#   $ #
# ..  #
#  *  #
#######
`, 16)
}

// unlösbares Level: Kiste klemmt in der Ecke
func TestSolveUnsolvable(t *testing.T) {
	field, err := soko.Parse(`
#####
#@ ##
##$.#
#####
`)
	if err != nil {
		t.Fatal(err)
	}

	s := New(field)
	for s.Step(1000000000) {
	}

	stats := s.GetStats()
	if !stats.Done {
		t.Fatal("Suche wurde nicht abgeschlossen")
	}
	if stats.FoundMoves >= 0 {
		t.Fatalf("Level ist unlösbar, aber Lösung mit %d Zügen gemeldet", stats.FoundMoves)
	}
}

// Vanilla-Level: bitgenauer Vergleich mit dem C#-Orakel (refcli):
// 230 Züge optimal, 8.710.434 bekannte Stellungen am Ende
func TestSolveVanillaOracle(t *testing.T) {
	if testing.Short() {
		t.Skip("Vanilla-Level dauert ca. 10 Sekunden (übersprungen mit -short)")
	}

	s, _ := solveLevel(t, maps.MapVanilla, 230)
	if s.NodeCount() != 8710434 {
		t.Errorf("erwartete 8710434 Knoten (Orakel-Wert), erhalten: %d", s.NodeCount())
	}
}
