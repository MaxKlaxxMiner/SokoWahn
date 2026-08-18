package solver

import (
	"testing"

	"goSokoWahnBrute/maps"
	"goSokoWahnBrute/soko"
)

// löst ein Level komplett und prüft die Lösung auf Konsistenz
// (mit der Default-Richtungswahl, dem Effizienz-Verhältnis)
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

// kleines Mehrkisten-Level (verankerte Referenz: 16 Züge)
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

// Level startet bereits gelöst: das zählt NICHT als 0-Züge-Lösung (das Spiel prüft
// den Zielzustand erst nach einem Zug) - die kürzeste Lösung schiebt die Kiste
// heraus und wieder auf das Ziel zurück (uuRdrruL = 8 Züge)
func TestSolveSolvedStart(t *testing.T) {
	_, solution := solveLevel(t, `
######
#    #
# *  #
#    #
#@   #
######
`, 8)
	if len(solution.Moves) == 0 {
		t.Fatal("gelöster Start darf keine 0-Züge-Lösung liefern")
	}
}

// manuelle Richtungsvorgabe: die erzwungene Seite muss allein suchen (die andere Front
// bleibt bei ihren Startstellungen) und trotzdem die optimale Lösung finden;
// erst nach gefundener Lösung darf die vorgegebene Endphase beide Seiten nutzen
func TestSolveDirMode(t *testing.T) {
	level := `
#######
#.@ # #
#$* $ #
#   $ #
# ..  #
#  *  #
#######
`
	for _, tc := range []struct {
		name string
		mode DirMode
	}{
		{"forward", DirForward},
		{"backward", DirBackward},
	} {
		t.Run(tc.name, func(t *testing.T) {
			field, err := soko.Parse(level)
			if err != nil {
				t.Fatal(err)
			}

			s := New(field)
			s.SetDirMode(tc.mode)
			frozenOpen := s.GetStats().BackwardOpen[0]

			for s.Step(1000) {
				stats := s.GetStats()
				if stats.FoundMoves >= 0 {
					continue // Endphase: der Rest der Beweisführung ist vorgegeben
				}
				if tc.mode == DirForward && (stats.BackwardDepth != 0 || stats.BackwardOpen[0] != frozenOpen) {
					t.Fatal("Rückwärtssuche lief trotz DirForward an")
				}
				// DirBackward arbeitet die Vorwärts-Tiefe 0 (nur die Startstellung) zuerst ab
				// (siehe Step) - danach darf die Vorwärtsfront nicht weiterlaufen
				if tc.mode == DirBackward && stats.ForwardDepth > 1 {
					t.Fatal("Vorwärtssuche lief trotz DirBackward über Tiefe 1 hinaus")
				}
			}

			if stats := s.GetStats(); stats.FoundMoves != 16 {
				t.Fatalf("erwartete Lösungslänge 16, erhalten: %d", stats.FoundMoves)
			}
			solution, err := s.GetSolution()
			if err != nil {
				t.Fatal(err)
			}
			if len(solution.Moves) != 16 {
				t.Fatalf("erwartete 16 Züge, erhalten: %d (%s)", len(solution.Moves), solution.Moves)
			}
		})
	}
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

// Disk-Auslagerung: dasselbe Level einmal im RAM und einmal mit winziger
// Auslagerungs-Schwelle lösen - Zugfolge und Knotenzahl müssen exakt übereinstimmen
// (die FIFO-Reihenfolge der Listen bleibt beim Auslagern bitgenau erhalten)
func TestSolveSpillDeterminism(t *testing.T) {
	level := `
#######
#.@ # #
#$* $ #
#   $ #
# ..  #
#  *  #
#######
`
	sRam, solRam := solveLevel(t, level, 16)

	dir := setupSpill(t, 128) // 128 Bytes: bei 6er-Sätzen lagern Listen ab ca. 11 Sätzen aus
	sDisk, solDisk := solveLevel(t, level, 16)

	if solRam.Moves != solDisk.Moves {
		t.Errorf("Zugfolgen weichen ab: RAM %q, Disk %q", solRam.Moves, solDisk.Moves)
	}
	if sRam.NodeCount() != sDisk.NodeCount() {
		t.Errorf("Knotenzahlen weichen ab: RAM %d, Disk %d", sRam.NodeCount(), sDisk.NodeCount())
	}

	sDisk.Close()
	if n := countSpillFiles(t, dir); n != 0 {
		t.Errorf("nach Close liegen noch %d Auslagerungsdateien im Temp-Ordner", n)
	}

	// Gegenprobe, dass die Auslagerung überhaupt griff: denselben Lauf in kleinen
	// Schritten wiederholen und zwischendurch nach Auslagerungsdateien schauen
	field, err := soko.Parse(level)
	if err != nil {
		t.Fatal(err)
	}
	spilled := false
	s := New(field)
	for s.Step(50) {
		if countSpillFiles(t, dir) > 0 {
			spilled = true
		}
	}
	s.Close()
	if !spilled {
		t.Error("die Suche hat nie ausgelagert - der Determinismus-Vergleich testet nichts")
	}
}

// Vanilla mit aktiver Auslagerung (256-KB-Puffer): muss bitgenau dieselben
// Anker-Werte liefern wie die RAM-Variante (230 Züge, vanillaNodes Knoten)
func TestSolveVanillaSpill(t *testing.T) {
	if testing.Short() {
		t.Skip("Vanilla-Level dauert ca. 10 Sekunden (übersprungen mit -short)")
	}

	setupSpill(t, 256<<10)
	s, _ := solveLevel(t, maps.MapVanilla, 230)
	if s.NodeCount() != vanillaNodes {
		t.Errorf("erwartete %d Knoten (Vanilla-Anker), erhalten: %d", vanillaNodes, s.NodeCount())
	}
	if s.SpillBytes() == 0 {
		t.Error("die Vanilla-Suche hätte bei 256-KB-Puffern auslagern müssen")
	}
	s.Close()
}

// Vanilla-Level ohne Filter: 230 Züge optimal, vanillaNodes bekannte Stellungen
// am Ende - der zentrale Anker des Suchverhaltens (bei Abweichung: Bug oder
// bewusste, dokumentierte Änderung)
func TestSolveVanilla(t *testing.T) {
	if testing.Short() {
		t.Skip("Vanilla-Level dauert ca. 10 Sekunden (übersprungen mit -short)")
	}

	s, _ := solveLevel(t, maps.MapVanilla, 230)
	if s.NodeCount() != vanillaNodes {
		t.Errorf("erwartete %d Knoten (Vanilla-Anker), erhalten: %d", vanillaNodes, s.NodeCount())
	}
}

// Anker-Knotenzahl des Suchverhaltens auf dem Vanilla-Level: Effizienz-
// Richtungswahl plus Behalten der Gleichstands-Stellungen nach dem ersten Fund
// (Futter der Push-Optimierung - Level 361); Herkunft der Zahl und die
// historischen Vorgänger-Werte in docs/history.md
const vanillaNodes = 8747345
