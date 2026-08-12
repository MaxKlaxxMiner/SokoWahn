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

// Referenzwerte der Vanilla-Blocker-Stufen (seit Cache-Version 5 Hybrid: Stufen
// unterhalb RulesMinBoxCount=4 bauen klassisch und bleiben bitgenau gleich dem
// C#-Orakel refcli blockerbx; ab Stufe 4 filtern die Vorwärts-Phasen mit den
// Stufe-1-Regeln und verlieren genau die regel-erkennbaren Muster). Die
// refcli-Werte für Stufe 4 wären 1173/210093; Stufe 5 ist trotz Filterung
// unverändert orakel-gleich - die Regeln killen dort exakt die Zustände, welche
// sonst die Muster der Stufe 4 filtern.
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
		{BoxCount: 4, PatternCount: 1069, CheckedStates: 209989},
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

	// Lösung NUR mit dem Blocker, ohne Live-Regeln: weiterhin 230 Züge und dank der
	// vollen 1-3-Stufen fast die alte Vollblocker-Stärke (1.595.820 statt 1.595.042;
	// die wenigen fehlenden 4er-Muster trägt im Normalbetrieb die Live-Regel).
	// Der Lauf beweist, dass der Hybrid-Blocker auch allein korrekt bleibt.
	field.SetBlocker(b)
	s := solver.New(field)
	for s.Step(1000000000) {
	}
	if moves := s.GetStats().FoundMoves; moves != 230 {
		t.Errorf("erwartete 230 Züge, erhalten: %d", moves)
	}
	if nodes := s.NodeCount(); nodes != 1595820 {
		t.Errorf("erwartete 1595820 Knoten (Regressionswert Blocker solo), erhalten: %d", nodes)
	}
	s.Close()

	// Standard-Kombination des TUI (Blocker + Live-Regeln beidseitig): 230 Züge mit
	// 1.488.952 Knoten - besser als die alte Vollblocker-Referenz (1.595.042), die
	// Regeln ersetzen die entfallenen Muster und legen über die Pull-Seite noch drauf
	rules := soko.NewRules(field)
	field.SetRules(rules)
	field.SetRulesBackward(rules)
	s2 := solver.New(field)
	for s2.Step(1000000000) {
	}
	if moves := s2.GetStats().FoundMoves; moves != 230 {
		t.Errorf("erwartete 230 Züge (Blocker+Regeln), erhalten: %d", moves)
	}
	if nodes := s2.NodeCount(); nodes != 1488952 {
		t.Errorf("erwartete 1488952 Knoten (Regressionswert Blocker+Regeln), erhalten: %d", nodes)
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

// Orakel-Vergleich: die ersten drei Blocker-Stufen von Level 201 liegen unterhalb
// von RulesMinBoxCount und bauen klassisch - sie müssen exakt den Werten des
// GEFIXTEN C#-SokowahnBlockerBx entsprechen (refcli: "lid201.txt blockerbx 3").
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
