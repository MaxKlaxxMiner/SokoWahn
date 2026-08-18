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

	if _, ok := r.checkFreeze(wposAt(t, f, 2, 2), f.boxBits); ok {
		t.Error("2x2-Block abseits der Ziele muss als Freeze-Deadlock erkannt werden")
	}
	if r.CheckPush(f.player, wposAt(t, f, 2, 2), f.boxBits) {
		t.Error("CheckPush muss den 2x2-Block verwerfen")
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

	if _, ok := r.checkFreeze(wposAt(t, f, 2, 2), f.boxBits); !ok {
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
	if _, ok := r.checkFreeze(wposAt(t, f, 2, 2), mask); !ok {
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
	if !r.shared.pullDeadAt(wposAt(t, f, 1, 1)) {
		t.Error("(1,1) muss als pull-totes Feld erkannt werden")
	}
	if r.CheckPull(wposAt(t, f, 1, 1), mask) {
		t.Error("CheckPull muss die Konfiguration verwerfen")
	}

	// die echte Startkonfiguration ist pull-eingefroren, steht aber komplett auf
	// den Startfeldern -> erlaubt
	if !r.CheckPull(wposAt(t, f, 1, 3), f.boxBits) {
		t.Error("die Startkonfiguration darf nicht verworfen werden")
	}
}

// Ziel-Matching (Stufe 2), Erreichbarkeits-Teil: ein eingefrorenes Kisten-Paar
// auf den Korridor-Zielen wirkt als Wand und schneidet die Kiste im rechten Raum
// vom letzten freien Ziel (1,1) ab. Die Kiste selbst bleibt lokal beweglich -
// Stufe 1 (Freeze) und die statischen Totfelder greifen hier bewusst NICHT.
// (Masken von Hand gebaut: ein solches Paar entsteht erst während der Suche,
// beim Parsen würde der Start-Filter freeze.go es zu Wänden machen)
func TestRulesGoalMatchReach(t *testing.T) {
	f, err := Parse(`
############
#.  ###    #
# $$ .. $  #
#   ###@   #
############
`)
	if err != nil {
		t.Fatal(err)
	}
	r := NewRules(f)

	// hypothetische Stellung nach einem Schub: Paar auf den Korridor-Zielen
	// (5,2)/(6,2) eingefroren, die dritte Kiste eben nach (8,2) geschoben
	mask := make([]uint64, len(f.boxBits))
	for _, c := range [][2]int{{5, 2}, {6, 2}, {8, 2}} {
		p := wposAt(t, f, c[0], c[1])
		mask[p>>6] |= 1 << (p & 63)
	}

	if r.CheckPush(wposAt(t, f, 7, 2), wposAt(t, f, 8, 2), mask) {
		t.Error("die abgeschnittene Kiste erreicht kein freies Ziel mehr - CheckPush muss verwerfen")
	}
	// dass wirklich das Matching (und nicht Stufe 1) verwirft, zeigt der Fixpunkt direkt
	if _, ok := r.checkFreeze(wposAt(t, f, 8, 2), mask); !ok {
		t.Error("die Stellung darf nicht schon am Freeze-Fixpunkt scheitern")
	}

	// Gegenprobe: ohne Stufe 2 lässt die Stellung sich nicht widerlegen
	r.MatchEnabled = false
	if !r.CheckPush(wposAt(t, f, 7, 2), wposAt(t, f, 8, 2), mask) {
		t.Error("ohne Ziel-Matching darf die Stellung nicht verworfen werden")
	}
}

// Ziel-Matching (Stufe 2), Matching-Teil: zwei bewegliche Kisten hinter dem
// eingefrorenen Paar erreichen beide NUR das eine freie Ziel im rechten Raum -
// die Erreichbarkeits-Vorstufe ist je Kiste erfüllt, erst das bipartite
// Matching deckt den Engpass auf
func TestRulesGoalMatchAssignment(t *testing.T) {
	f, err := Parse(`
#############
#.  ###     #
# $$ .. $.$ #
#   ###  @  #
#############
`)
	if err != nil {
		t.Fatal(err)
	}
	r := NewRules(f)

	buildMask := func(coords [][2]int) []uint64 {
		mask := make([]uint64, len(f.boxBits))
		for _, c := range coords {
			p := wposAt(t, f, c[0], c[1])
			mask[p>>6] |= 1 << (p & 63)
		}
		return mask
	}

	// Paar eingefroren auf (5,2)/(6,2), zwei Kisten bei (8,2) und (10,2),
	// freie Ziele: (1,1) (unerreichbar) und (9,2) (für beide erreichbar)
	mask := buildMask([][2]int{{5, 2}, {6, 2}, {8, 2}, {10, 2}})
	if r.CheckPush(wposAt(t, f, 11, 2), wposAt(t, f, 10, 2), mask) {
		t.Error("zwei Kisten um ein erreichbares Ziel - das Matching muss verwerfen")
	}

	// Gegenprobe: mit nur EINER beweglichen Kiste geht die Zuordnung auf
	single := buildMask([][2]int{{5, 2}, {6, 2}, {8, 2}})
	if !r.CheckPush(wposAt(t, f, 7, 2), wposAt(t, f, 8, 2), single) {
		t.Error("eine Kiste, ein erreichbares freies Ziel - kein Deadlock")
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
