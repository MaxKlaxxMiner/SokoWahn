package solver

import (
	"fmt"
	"strings"
	"testing"

	"goSokoWahnBrute/soko"
)

// löst ein Level mit gegebener Worker-Zahl und Schritt-Limit und protokolliert die
// komplette Tiefen-Entwicklung (Knoten/Rest je erreichter Suchtiefe) plus Endergebnis -
// zwei Läufe sind genau dann bitgenau gleich, wenn diese Protokolle übereinstimmen
func runTrace(t *testing.T, level string, workers, stepLimit int) string {
	t.Helper()
	field, err := soko.Parse(level)
	if err != nil {
		t.Fatal(err)
	}
	s := New(field)
	defer s.Close()
	s.SetWorkers(workers)

	var sb strings.Builder
	lastDepth := -1
	for s.Step(stepLimit) {
		if depth := s.SearchDepth(); depth != lastDepth {
			lastDepth = depth
			fmt.Fprintf(&sb, "Tiefe %d: Knoten=%d Rest=%d\n", depth, s.NodeCount(), s.OpenCount())
		}
	}

	stats := s.GetStats()
	fmt.Fprintf(&sb, "Züge=%d\n", stats.FoundMoves)
	if stats.FoundMoves >= 0 {
		solution, err := s.GetSolution()
		if err != nil {
			t.Fatal(err)
		}
		sb.WriteString(solution.Moves)
	}
	return sb.String()
}

// Parallel-Suche: muss bitgenau dieselbe Tiefen-Entwicklung, Knotenzahl und Zugfolge
// liefern wie die serielle Suche - unabhängig von Worker-Zahl und Batch-Größe
// (parallelMinRecords wird auf 1 gesenkt, damit auch Mini-Batches parallel laufen)
func TestSolveParallelDeterminism(t *testing.T) {
	level := `
#######
#.@ # #
#$* $ #
#   $ #
# ..  #
#  *  #
#######
`
	oldMin := parallelMinRecords
	parallelMinRecords = 1
	defer func() { parallelMinRecords = oldMin }()

	reference := runTrace(t, level, 1, 1000000000) // seriell, ganze Tiefen pro Schritt
	if !strings.Contains(reference, "Züge=16") {
		t.Fatalf("serielle Referenz löst das Level nicht wie erwartet:\n%s", reference)
	}

	for _, cfg := range []struct{ workers, stepLimit int }{
		{2, 1000000000},  // wenige Worker, ganze Tiefen
		{7, 1000000000},  // ungerade Worker-Zahl -> ungleiche Bereichs-Größen
		{4, 50},          // kleine Batches -> viele Batch-Grenzen
		{16, 3},          // mehr Worker als Sätze -> leere Bereiche
	} {
		if got := runTrace(t, level, cfg.workers, cfg.stepLimit); got != reference {
			t.Errorf("Abweichung bei %d Workern (Schritt-Limit %d):\nseriell:\n%s\nparallel:\n%s",
				cfg.workers, cfg.stepLimit, reference, got)
		}
	}
}

// Parallel-Suche kombiniert mit Disk-Auslagerung: auch mit winziger Auslagerungs-Schwelle
// (Batch-Grenzen an den Leseblöcken) muss die Tiefen-Entwicklung bitgenau stimmen
func TestSolveParallelSpillDeterminism(t *testing.T) {
	level := `
#######
#.@ # #
#$* $ #
#   $ #
# ..  #
#  *  #
#######
`
	oldMin := parallelMinRecords
	parallelMinRecords = 1
	defer func() { parallelMinRecords = oldMin }()

	reference := runTrace(t, level, 1, 1000000000) // seriell im RAM

	setupSpill(t, 128)
	if got := runTrace(t, level, 4, 1000000000); got != reference {
		t.Errorf("Abweichung bei 4 Workern mit Disk-Auslagerung:\nseriell:\n%s\nparallel:\n%s",
			reference, got)
	}
}
