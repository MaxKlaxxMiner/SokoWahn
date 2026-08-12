package solver

import (
	"testing"

	"goSokoWahnBrute/maps"
	"goSokoWahnBrute/soko"
)

// löst ein Level mit aktivem Regel-Filter (Stufe 1: Freeze + Diagonale) und prüft
// die Lösung auf Konsistenz - die Lösungslänge muss identisch zur Suche ohne
// Regeln sein (die Regeln verwerfen nur beweisbar tote Stellungen)
func solveLevelWithRules(t *testing.T, level string, expectedMoves int) (*Solver, *Solution) {
	t.Helper()

	field, err := soko.Parse(level)
	if err != nil {
		t.Fatal(err)
	}
	rules := soko.NewRules(field)
	field.SetRules(rules)
	field.SetRulesBackward(rules)

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
		t.Fatalf("erwartete %d Züge, erhalten: %d (%s)", expectedMoves, len(solution.Moves), solution.Moves)
	}
	return s, solution
}

// kleine Levels: Lösungslängen mit Regeln unverändert
func TestSolveRulesSmall(t *testing.T) {
	solveLevelWithRules(t, `
#######
#.@ # #
#$* $ #
#   $ #
# ..  #
#  *  #
#######
`, 16)
}

func TestSolveRulesMini(t *testing.T) {
	solveLevelWithRules(t, `
#####
#@$.#
#####
`, 1)
}

func TestSolveRulesSolvedStart(t *testing.T) {
	solveLevelWithRules(t, `
######
#    #
# *  #
#    #
#@   #
######
`, 8)
}

// unlösbares Level bleibt unlösbar (die Regeln beschleunigen nur den Beweis)
func TestSolveRulesUnsolvable(t *testing.T) {
	field, err := soko.Parse(`
#####
#@ ##
##$.#
#####
`)
	if err != nil {
		t.Fatal(err)
	}
	rules := soko.NewRules(field)
	field.SetRules(rules)
	field.SetRulesBackward(rules)

	s := New(field)
	for s.Step(1000000000) {
	}
	if stats := s.GetStats(); !stats.Done || stats.FoundMoves >= 0 {
		t.Fatalf("Level ist unlösbar, aber Lösung mit %d Zügen gemeldet", s.GetStats().FoundMoves)
	}
}

// Gegenprobe zum Diagonal-Test im soko-Paket: das Level mit der geschlossenen
// Diagonale ist wirklich unlösbar - einmal ohne und einmal mit Regeln geprüft
// (mit Regeln muss der Unlösbarkeits-Beweis mit weniger Knoten gelingen)
func TestSolveRulesDiagonalUnsolvable(t *testing.T) {
	level := `
##########
###   .  #
## $  .  #
# $ $ .  #
#  $ #.  #
#   ##   #
#@       #
##########
`
	solve := func(withRules bool) *Solver {
		field, err := soko.Parse(level)
		if err != nil {
			t.Fatal(err)
		}
		if withRules {
			rules := soko.NewRules(field)
			field.SetRules(rules)
			field.SetRulesBackward(rules)
		}
		s := New(field)
		for s.Step(1000000000) {
		}
		if stats := s.GetStats(); !stats.Done || stats.FoundMoves >= 0 {
			t.Fatalf("Level ist unlösbar (Diagonal-Deadlock), aber Lösung mit %d Zügen gemeldet (Regeln: %v)",
				stats.FoundMoves, withRules)
		}
		return s
	}

	plain := solve(false)
	rules := solve(true)
	if rules.NodeCount() >= plain.NodeCount() {
		t.Errorf("mit Regeln sollte der Beweis weniger Knoten brauchen: %d (Regeln) vs %d (ohne)",
			rules.NodeCount(), plain.NodeCount())
	}
}

// Umschalter: Regeln und Blocker lassen sich am laufenden Solver an- und abschalten
// (ohne gesetzte Quellen bleiben die Filter aus)
func TestSolveRulesToggle(t *testing.T) {
	field, err := soko.Parse(`
######
#@$ .#
######
`)
	if err != nil {
		t.Fatal(err)
	}
	rules := soko.NewRules(field)
	field.SetRules(rules)
	field.SetRulesBackward(rules)

	s := New(field)
	if !s.RulesEnabled() {
		t.Error("Regeln müssen nach New aktiv sein (vom Feld geerbt)")
	}
	s.SetRulesEnabled(false)
	if s.RulesEnabled() {
		t.Error("SetRulesEnabled(false) muss die Regeln abschalten")
	}
	s.SetRulesEnabled(true)
	if !s.RulesEnabled() {
		t.Error("SetRulesEnabled(true) muss die Regeln wieder anschalten")
	}
	if s.BlockerEnabled() {
		t.Error("ohne Blocker-Quelle darf kein Blocker-Filter aktiv sein")
	}
	for s.Step(1000000000) {
	}
	if stats := s.GetStats(); stats.FoundMoves != 2 {
		t.Fatalf("erwartete Lösungslänge 2, erhalten: %d", stats.FoundMoves)
	}
}

// erzwungene Rückwärtssuche mit Regeln: die Pull-Regeln (Totfeld + Pull-Freeze)
// tragen die Hauptlast und dürfen die optimale Lösung nicht verlieren
func TestSolveRulesDirBackward(t *testing.T) {
	field, err := soko.Parse(`
#######
#.@ # #
#$* $ #
#   $ #
# ..  #
#  *  #
#######
`)
	if err != nil {
		t.Fatal(err)
	}
	rules := soko.NewRules(field)
	field.SetRules(rules)
	field.SetRulesBackward(rules)

	s := New(field)
	s.SetDirMode(DirBackward)
	for s.Step(1000000000) {
	}
	if stats := s.GetStats(); stats.FoundMoves != 16 {
		t.Fatalf("erwartete Lösungslänge 16, erhalten: %d", stats.FoundMoves)
	}
	if r := s.Rules(); r != nil {
		if st := r.Stats(); st.PullDeadKills+st.PullFreezeKills == 0 {
			t.Error("die Pull-Regeln haben bei reiner Rückwärtssuche nie gefeuert - Test prüft nichts")
		}
	}
}

// Ziel-Matching (Regel-Stufe 2) in der echten Suche: drei Kisten im rechten Raum,
// eine muss ZUERST quer durch den Korridor zum linken Ziel - füllt die Suche die
// beiden Korridor-Ziele vorher, friert das Paar ein und schneidet die Restkiste
// ab (Matching-Treffer). Das Level ist lösbar: die Lösungslänge muss exakt der
// ungefilterten Suche entsprechen, der Beweis darf nicht mehr Knoten brauchen.
func TestSolveRulesGoalMatch(t *testing.T) {
	level := `
##############
#.  ###      #
#    ..$ $ $ #
#   ###     @#
##############
`
	solve := func(withRules bool) *Solver {
		field, err := soko.Parse(level)
		if err != nil {
			t.Fatal(err)
		}
		if withRules {
			rules := soko.NewRules(field)
			field.SetRules(rules)
			field.SetRulesBackward(rules)
		}
		s := New(field)
		for s.Step(1000000000) {
		}
		if stats := s.GetStats(); !stats.Done || stats.FoundMoves < 0 {
			t.Fatalf("Level ist lösbar, aber keine Lösung gefunden (Regeln: %v)", withRules)
		}
		return s
	}

	plain := solve(false)
	rules := solve(true)
	if p, r := plain.GetStats().FoundMoves, rules.GetStats().FoundMoves; p != r {
		t.Errorf("Lösungslänge weicht ab: ohne Regeln %d, mit Regeln %d", p, r)
	}
	if rules.NodeCount() > plain.NodeCount() {
		t.Errorf("mit Regeln dürfen nicht mehr Knoten entstehen: %d (Regeln) vs %d (ohne)",
			rules.NodeCount(), plain.NodeCount())
	}
	if r := rules.Rules(); r != nil {
		if st := r.Stats(); st.MatchKills == 0 {
			t.Error("das Ziel-Matching hat nie gefeuert - Test prüft nichts")
		}
	}
}

// Vanilla-Level mit Regel-Filter: die optimale Lösungslänge (230 Züge) bleibt
// erhalten, die Knotenzahl sinkt gegenüber der ungefilterten Suche (8.710.434).
// Der Knotenwert ist als Regressionswert verankert (Änderungen am Regelwerk
// ändern ihn - dann bewusst neu verankern und die 230 Züge erneut prüfen).
func TestSolveRulesVanillaOracle(t *testing.T) {
	if testing.Short() {
		t.Skip("Vanilla-Level dauert einige Sekunden (übersprungen mit -short)")
	}

	s, _ := solveLevelWithRules(t, maps.MapVanilla, 230)
	if s.NodeCount() != 1825644 {
		t.Errorf("erwartete 1.825.644 Knoten (Regressionswert der Stufe-1-Regeln inkl. Pull-Freeze), erhalten: %d", s.NodeCount())
	}
	if r := s.Rules(); r != nil {
		st := r.Stats()
		want := soko.RuleStats{FreezeKills: 1104786, DiagonalKills: 71007,
			PullDeadKills: 118087, PullFreezeKills: 783}
		if st.FreezeKills != want.FreezeKills || st.DiagonalKills != want.DiagonalKills ||
			st.PullDeadKills != want.PullDeadKills || st.PullFreezeKills != want.PullFreezeKills {
			t.Errorf("Regel-Treffer weichen von den Regressionswerten ab:\nerwartet: %+v\nerhalten: Freeze=%d Diag=%d PullTot=%d PullFreeze=%d",
				want, st.FreezeKills, st.DiagonalKills, st.PullDeadKills, st.PullFreezeKills)
		}
	}
}
