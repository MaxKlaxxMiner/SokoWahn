package soko

import "testing"

// Hilfsfunktion: Wpos eines Feldes über absolute x/y-Koordinaten
func wposAt(t *testing.T, f *Field, x, y int) Wpos {
	t.Helper()
	p := f.fieldToWpos[y*f.width+x]
	if p == f.walkEof {
		t.Fatalf("Feld (%d,%d) ist nicht begehbar", x, y)
	}
	return p
}

// tote Felder: von dort erreicht keine Kiste mehr ein Ziel (Pull-BFS von den Zielen)
func TestRulesDeadSquares(t *testing.T) {
	f, err := Parse(`
#####
#. $#
#  @#
#####
`)
	if err != nil {
		t.Fatal(err)
	}
	r := NewRules(f)

	// lebendig: das Ziel selbst und das Feld rechts daneben (Schub nach links möglich)
	for _, c := range [][2]int{{1, 1}, {2, 1}} {
		if r.shared.deadAt(wposAt(t, f, c[0], c[1])) {
			t.Errorf("Feld (%d,%d) muss lebendig sein", c[0], c[1])
		}
	}
	// tot: von dort ist kein Schub Richtung Ziel mehr möglich (Wände blockieren den Spieler)
	for _, c := range [][2]int{{3, 1}, {1, 2}, {2, 2}, {3, 2}} {
		if !r.shared.deadAt(wposAt(t, f, c[0], c[1])) {
			t.Errorf("Feld (%d,%d) muss tot sein", c[0], c[1])
		}
	}
}

// Freeze-Fixpunkt: 2x2-Block abseits der Ziele = Deadlock
func TestRulesFreezeDeadlock(t *testing.T) {
	f, err := Parse(`
#######
#     #
# $$..#
# $$..#
#@    #
#######
`)
	if err != nil {
		t.Fatal(err)
	}
	r := NewRules(f)

	if r.checkFreeze(wposAt(t, f, 2, 2), f.boxBits) {
		t.Error("2x2-Block abseits der Ziele muss als Freeze-Deadlock erkannt werden")
	}
	if r.CheckPush(f.player, wposAt(t, f, 2, 2), f.boxBits) {
		t.Error("CheckPush muss den 2x2-Block verwerfen")
	}
	if st := r.Stats(); st.FreezeKills != 1 {
		t.Errorf("erwartete 1 Freeze-Treffer in der Statistik, erhalten: %d", st.FreezeKills)
	}
}

// Freeze-Fixpunkt: L-Form ist beweglich (die untere Kiste kann nach unten ausweichen)
func TestRulesFreezeMovable(t *testing.T) {
	f, err := Parse(`
#######
#     #
# $$..#
# $  .#
#@    #
#######
`)
	if err != nil {
		t.Fatal(err)
	}
	r := NewRules(f)

	if !r.checkFreeze(wposAt(t, f, 2, 2), f.boxBits) {
		t.Error("L-Form ist kein Deadlock (Fixpunkt muss alle Kisten entfernen)")
	}
}

// Freeze-Fixpunkt: eingefrorener 2x2-Block komplett auf Zielen ist KEIN Deadlock
// (Maske von Hand gebaut, weil der Parse-Filter solche Blöcke zu Wänden machen würde)
func TestRulesFreezeAllOnGoals(t *testing.T) {
	f, err := Parse(`
#######
#     #
# ..$ #
# ..$ #
#@ $$ #
#######
`)
	if err != nil {
		t.Fatal(err)
	}
	r := NewRules(f)

	mask := make([]uint64, len(f.boxBits))
	for _, c := range [][2]int{{2, 2}, {3, 2}, {2, 3}, {3, 3}} {
		p := wposAt(t, f, c[0], c[1])
		mask[p>>6] |= 1 << (p & 63)
	}
	if !r.checkFreeze(wposAt(t, f, 2, 2), mask) {
		t.Error("eingefrorener 2x2-Block auf Zielen darf nicht als Deadlock gelten")
	}
}

// geschlossene Diagonale (JSoko-Port): Kisten-Treppe, oben durch Wände und unten
// durch eine Doppelwand geschlossen, ohne Ziele im Muster = Deadlock.
// Die Unlösbarkeit des Levels ist im solver-Paket gegengeprüft (TestSolveRulesDiagonalUnsolvable).
func TestRulesDiagonalDeadlock(t *testing.T) {
	f, err := Parse(`
##########
###   .  #
## $  .  #
# $ $ .  #
#  $ #.  #
#   ##   #
#@       #
##########
`)
	if err != nil {
		t.Fatal(err)
	}
	r := NewRules(f)

	if !r.isDiagonalDeadlock(f.player, wposAt(t, f, 3, 2), f.boxBits) {
		t.Error("geschlossene Diagonale muss als Deadlock erkannt werden")
	}
	if r.CheckPush(f.player, wposAt(t, f, 3, 2), f.boxBits) {
		t.Error("CheckPush muss die geschlossene Diagonale verwerfen")
	}
}

// pull-tote Felder (Spiegel der toten Felder): von dort erreicht keine Kiste
// per Ziehen mehr ein Start-Kistenfeld
func TestRulesPullDeadSquares(t *testing.T) {
	f, err := Parse(`
#####
#$ .#
#  @#
#####
`)
	if err != nil {
		t.Fatal(err)
	}
	r := NewRules(f)

	// nur das Startfeld selbst ist pull-lebendig: jede Zug-Landung auf (1,1)
	// bräuchte dahinter Platz für den Spieler, der überall von Wänden fehlt
	if r.shared.pullDeadAt(wposAt(t, f, 1, 1)) {
		t.Error("das Start-Kistenfeld muss pull-lebendig sein")
	}
	for _, c := range [][2]int{{2, 1}, {3, 1}, {1, 2}, {2, 2}, {3, 2}} {
		if !r.shared.pullDeadAt(wposAt(t, f, c[0], c[1])) {
			t.Errorf("Feld (%d,%d) muss pull-tot sein", c[0], c[1])
		}
	}
}

// Pull-Freeze: Kisten-Paar an der oberen Wand, seitlich von Wänden flankiert und
// mit pull-toten Feldern darunter - lässt sich per Ziehen nie mehr auflösen und
// steht abseits der Startfelder -> vorwärts unerreichbar
func TestRulesPullFreeze(t *testing.T) {
	f, err := Parse(`
#####
#  ##
# @.#
#$$.#
#####
`)
	if err != nil {
		t.Fatal(err)
	}
	r := NewRules(f)

	// hypothetische Rückwärts-Konfiguration: beide Kisten oben statt auf den Startfeldern
	mask := make([]uint64, len(f.boxBits))
	for _, c := range [][2]int{{1, 1}, {2, 1}} {
		p := wposAt(t, f, c[0], c[1])
		mask[p>>6] |= 1 << (p & 63)
	}
	if r.checkPullFreeze(wposAt(t, f, 1, 1), mask) {
		t.Error("pull-eingefrorenes Paar abseits der Startfelder muss verworfen werden")
	}
	// der O(1)-Vorabcheck greift hier sogar schon vor dem Fixpunkt: (1,1) ist pull-tot
	if r.CheckPull(wposAt(t, f, 1, 1), mask) {
		t.Error("CheckPull muss die Konfiguration verwerfen")
	}
	if st := r.Stats(); st.PullDeadKills != 1 {
		t.Errorf("erwartete 1 Pull-Totfeld-Treffer, erhalten: %d", st.PullDeadKills)
	}

	// die echte Startkonfiguration ist pull-eingefroren, steht aber komplett auf
	// den Startfeldern -> erlaubt
	if !r.CheckPull(wposAt(t, f, 1, 3), f.boxBits) {
		t.Error("die Startkonfiguration darf nicht verworfen werden")
	}
}

// offene Diagonale: unten fehlt die Doppelwand, die Kette ist zu öffnen -> kein Deadlock
func TestRulesDiagonalOpen(t *testing.T) {
	f, err := Parse(`
##########
###   .  #
## $  .  #
# $ $ .  #
#  $  .  #
#        #
#@       #
##########
`)
	if err != nil {
		t.Fatal(err)
	}
	r := NewRules(f)

	if r.isDiagonalDeadlock(f.player, wposAt(t, f, 3, 2), f.boxBits) {
		t.Error("offene Diagonale darf nicht als Deadlock gelten")
	}
}
