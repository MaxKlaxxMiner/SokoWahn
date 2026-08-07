package blocker

import (
	"path/filepath"
	"testing"

	"goSokoWahnBrute/maps"
	"goSokoWahnBrute/soko"
	"goSokoWahnBrute/solver"
)

// kleines Mehrkisten-Level (Referenz: 16 Züge optimal)
const mapSmall = `
#######
#.@ # #
#$* $ #
#   $ #
# ..  #
#  *  #
#######
`

// berechnet alle Blocker-Stufen eines Feldes
func buildBlocker(t *testing.T, field *soko.Field, cachePath string) *Blocker {
	t.Helper()
	b := New(field, cachePath)
	for b.Next(1000000000) {
	}
	if b.Creating() {
		t.Fatal("Blocker-Erstellung wurde nicht abgeschlossen")
	}
	return b
}

// die Lösung mit Blocker muss identisch lang sein wie ohne, aber weniger Stellungen prüfen
func TestBlockerKeepsOptimalSolution(t *testing.T) {
	fieldPlain, err := soko.Parse(mapSmall)
	if err != nil {
		t.Fatal(err)
	}
	sPlain := solver.New(fieldPlain)
	for sPlain.Step(1000000000) {
	}

	fieldBlocked, err := soko.Parse(mapSmall)
	if err != nil {
		t.Fatal(err)
	}
	fieldBlocked.SetBlocker(buildBlocker(t, fieldBlocked, ""))
	sBlocked := solver.New(fieldBlocked)
	for sBlocked.Step(1000000000) {
	}

	if plain, blocked := sPlain.GetStats().FoundMoves, sBlocked.GetStats().FoundMoves; plain != blocked {
		t.Errorf("Lösungslänge weicht ab: ohne Blocker %d, mit Blocker %d", plain, blocked)
	}
	if sBlocked.NodeCount() > sPlain.NodeCount() {
		t.Errorf("Blocker müsste Stellungen sparen: ohne %d, mit %d", sPlain.NodeCount(), sBlocked.NodeCount())
	}

	// die Lösung selbst muss weiterhin rekonstruierbar und gültig sein
	solution, err := sBlocked.GetSolution()
	if err != nil {
		t.Fatal(err)
	}
	if len(solution.Moves) != 16 {
		t.Errorf("erwartete 16 Züge, erhalten: %d", len(solution.Moves))
	}
}

// Cache: nach dem Speichern muss ein zweiter Blocker alle Stufen laden und sofort fertig sein
func TestBlockerCacheResume(t *testing.T) {
	field, err := soko.Parse(mapSmall)
	if err != nil {
		t.Fatal(err)
	}
	cachePath := filepath.Join(t.TempDir(), CacheName(field))

	first := buildBlocker(t, field, cachePath)
	firstStats := first.GetStats()
	if len(firstStats.Stages) == 0 {
		t.Fatal("keine Blocker-Stufen berechnet")
	}

	// zweiter Blocker lädt den Cache und muss ohne Rechnen sofort abschließen
	second := New(field, cachePath)
	steps := 0
	for second.Next(1000000000) {
		steps++
	}
	if steps != 0 {
		t.Errorf("Cache-Wiederaufnahme hat %d Rechenschritte gebraucht (erwartet: 0)", steps)
	}

	secondStats := second.GetStats()
	if len(secondStats.Stages) != len(firstStats.Stages) {
		t.Fatalf("Stufenanzahl weicht ab: %d statt %d", len(secondStats.Stages), len(firstStats.Stages))
	}
	for i := range firstStats.Stages {
		if firstStats.Stages[i] != secondStats.Stages[i] {
			t.Errorf("Stufe %d weicht ab: %+v statt %+v", i+1, secondStats.Stages[i], firstStats.Stages[i])
		}
	}
}

// Cache: Abbruch nach zwei Stufen, Wiederaufnahme muss bei Stufe 3 weitermachen
// und am Ende dieselben Stufen liefern wie ein kompletter Durchlauf
func TestBlockerCachePartialResume(t *testing.T) {
	field, err := soko.Parse(mapSmall)
	if err != nil {
		t.Fatal(err)
	}
	cachePath := filepath.Join(t.TempDir(), CacheName(field))

	// nur die ersten beiden Stufen berechnen, dann abbrechen
	partial := New(field, cachePath)
	for len(partial.GetStats().Stages) < 2 && partial.Next(1000000000) {
	}
	partial.Abort()

	// Wiederaufnahme: lädt zwei Stufen aus dem Cache und rechnet den Rest
	resumed := buildBlocker(t, field, cachePath)

	// Vergleich gegen einen kompletten Durchlauf ohne Cache
	full := buildBlocker(t, field, "")

	resumedStats, fullStats := resumed.GetStats(), full.GetStats()
	if len(resumedStats.Stages) != len(fullStats.Stages) {
		t.Fatalf("Stufenanzahl weicht ab: %d statt %d", len(resumedStats.Stages), len(fullStats.Stages))
	}
	for i := range fullStats.Stages {
		if resumedStats.Stages[i] != fullStats.Stages[i] {
			t.Errorf("Stufe %d weicht ab: %+v statt %+v", i+1, resumedStats.Stages[i], fullStats.Stages[i])
		}
	}
}

// Orakel-Vergleich: die Vanilla-Blocker-Stufen müssen exakt den Werten des GEFIXTEN
// C#-SokowahnBlockerBx entsprechen (refcli: "vanilla.txt blockerbx 5", Cache-Version 108;
// der Bx-Hinterland-Fix wurde in beide Richtungen verifiziert, siehe CheckAllowed und
// docs/architektur.md). Die alte unbedingte Bx-Semantik lieferte: 17/92, 216/2251,
// 239/26848, 1024/208306, 2835/1056514 - nur Stufe 1 ist unverändert.
func TestBlockerVanillaOracle(t *testing.T) {
	if testing.Short() {
		t.Skip("Vanilla-Blocker dauert ca. 1 Sekunde plus Lösungszeit (übersprungen mit -short)")
	}

	field, err := soko.Parse(maps.MapVanilla)
	if err != nil {
		t.Fatal(err)
	}

	b := buildBlocker(t, field, "")
	expected := []StageStats{
		{BoxCount: 1, PatternCount: 17, CheckedStates: 92},
		{BoxCount: 2, PatternCount: 218, CheckedStates: 2257},
		{BoxCount: 3, PatternCount: 496, CheckedStates: 27219},
		{BoxCount: 4, PatternCount: 1173, CheckedStates: 210093},
		{BoxCount: 5, PatternCount: 2652, CheckedStates: 1071408},
	}

	stats := b.GetStats()
	if len(stats.Stages) != len(expected) {
		t.Fatalf("erwartet %d Stufen, erhalten: %d", len(expected), len(stats.Stages))
	}
	for i, want := range expected {
		if stats.Stages[i] != want {
			t.Errorf("Stufe %d: erwartet %+v, erhalten %+v", i+1, want, stats.Stages[i])
		}
	}

	// Lösung mit Blocker (vorwärts + rückwärts gefiltert): weiterhin 230 Züge,
	// 1.595.042 Knoten (Regressionswert; die alte unbedingte Regel kam auf 1.568.540,
	// die bedingte Kill-Regel kostet also nur ca. 1,7% Pruning-Leistung)
	field.SetBlocker(b)
	s := solver.New(field)
	for s.Step(1000000000) {
	}
	if moves := s.GetStats().FoundMoves; moves != 230 {
		t.Errorf("erwartete 230 Züge, erhalten: %d", moves)
	}
	if nodes := s.NodeCount(); nodes != 1595042 {
		t.Errorf("erwartete 1595042 Knoten (Regressionswert), erhalten: %d", nodes)
	}
}

// Level 201 von game-sokoban.com (8 Kisten): hier zeigte sich der Unterschied
// zwischen SokowahnBlocker (plain) und SokowahnBlockerBx zuerst
const mapLid201 = `
  ###########
 ##         ##
 #  $     $  #
 # $# #.# #$ #
 #    #*#    #####
 #  ###.###  #   #
 #  .*.@.*.      #
 #  ###.###  #   #
 #    #*#    #####
 # $# #.# #$ #
 #  $     $  #
 ##         ##
  ###########
`

// Orakel-Vergleich: die ersten drei Blocker-Stufen von Level 201 müssen exakt den
// Werten des GEFIXTEN C#-SokowahnBlockerBx entsprechen (refcli: "lid201.txt blockerbx 3").
// Die alte unbedingte Bx-Semantik lieferte 80/214, 35/8019, 781/232082 - vor allem
// Stufe 2 registriert jetzt deutlich mehr Hinterland-Muster, weil die Rückwärtswellen
// nicht mehr von der fehlerhaften unbedingten Regel beschnitten werden.
func TestBlockerLid201Oracle(t *testing.T) {
	if testing.Short() {
		t.Skip("Level-201-Blocker dauert ein paar Sekunden (übersprungen mit -short)")
	}

	field, err := soko.Parse(mapLid201)
	if err != nil {
		t.Fatal(err)
	}

	// nur bis Stufe 3 rechnen (die höheren Stufen wären deutlich teurer)
	b := New(field, "")
	for b.Next(1000000000) {
		if len(b.GetStats().Stages) >= 3 {
			b.Abort()
			break
		}
	}

	expected := []StageStats{
		{BoxCount: 1, PatternCount: 80, CheckedStates: 214},
		{BoxCount: 2, PatternCount: 2288, CheckedStates: 10272},
		{BoxCount: 3, PatternCount: 1819, CheckedStates: 233120},
	}

	stats := b.GetStats()
	if len(stats.Stages) != len(expected) {
		t.Fatalf("erwartet %d Stufen, erhalten: %d", len(expected), len(stats.Stages))
	}
	for i, want := range expected {
		if stats.Stages[i] != want {
			t.Errorf("Stufe %d: erwartet %+v, erhalten %+v", i+1, want, stats.Stages[i])
		}
	}
}
