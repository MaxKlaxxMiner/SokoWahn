package soko

import (
	"testing"
	"time"
)

// Steps mit einem unmöglichen Stellungs-Paar, bei dem die Schub-Position eine
// Wand wäre: muss einen Fehler liefern statt endlos zu laufen. Solche Paare
// baut die Direkt-Kanten-Sonde der Push-Optimierung (bestToStart prüft
// beliebige Stellungen gegen die Startstellung): der Wand-Sentinel der
// Laufweg-Suche markiert walkEof als besucht, die Erreichbarkeits-Prüfung
// griff deshalb nicht und die Rückverfolgung kreiste endlos in parent[0]
// (Hänger bei Level 25327, per pprof-CPU-Profil eingegrenzt).
func TestStepsStandPosWall(t *testing.T) {
	fieldA, err := Parse(`
#####
#@$.#
#   #
#####
`)
	if err != nil {
		t.Fatal(err)
	}
	// gleiche Wand-Geometrie, aber Spieler über der Kiste: die angeblich nach
	// unten geschobene Kiste verlangt eine Schub-Position in der Wand darüber
	fieldB, err := Parse(`
#####
# @.#
# $ #
#####
`)
	if err != nil {
		t.Fatal(err)
	}

	stateA, stateB := State{}, State{}
	fieldA.GetState(&stateA)
	fieldB.GetState(&stateB)

	done := make(chan struct{})
	var moves string
	var stepsErr error
	go func() {
		moves, stepsErr = fieldA.Steps(&stateA, &stateB)
		close(done)
	}()
	select {
	case <-done:
	case <-time.After(5 * time.Second):
		t.Fatal("Steps hängt (Endlosschleife in der Rückverfolgung)")
	}
	if stepsErr == nil {
		t.Fatalf("unmögliches Stellungs-Paar muss einen Fehler liefern, erhalten: %q", moves)
	}
}

// Steps mit einem Stellungs-Paar, bei dem sich MEHRERE Kisten unterscheiden:
// muss einen Fehler liefern. Die alte Erkennung nahm nur die erste abweichende
// Kiste und lieferte einen gültig aussehenden Ein-Schub-Weg, obwohl b mehrere
// Schübe von a entfernt ist. Über die Direkt-Kanten-Sonde der Push-Optimierung
// (Steps mit beliebigen Stellungspaaren gegen die Startstellung) wurde daraus
// eine Schein-1-Push-Kante: die rekonstruierte Lösung begann mit einem
// unsinnigen Segment und die Schub-Zahl stimmte nicht (Level 25523,
// angezeigt 300 Schübe, real nachspielbar wären 306 gewesen).
func TestStepsRejectsMultipleMovedBoxes(t *testing.T) {
	fieldA, err := Parse(`
########
#@$ $..#
#      #
########
`)
	if err != nil {
		t.Fatal(err)
	}
	// beide Kisten je einen Schritt nach rechts geschoben: die erste abweichende
	// Kiste liegt direkt neben b.Player, der alte Code fand deshalb "R" (1 Zug)
	fieldB, err := Parse(`
########
# @$ *.#
#      #
########
`)
	if err != nil {
		t.Fatal(err)
	}

	stateA, stateB := State{}, State{}
	fieldA.GetState(&stateA)
	fieldB.GetState(&stateB)

	if moves, err := fieldA.Steps(&stateA, &stateB); err == nil {
		t.Fatalf("Paar mit zwei verschobenen Kisten muss einen Fehler liefern, erhalten: %q", moves)
	}
}

// Steps mit einem Stellungs-Paar, bei dem dieselbe Kiste ZWEI Schübe (mit
// Richtungswechsel) entfernt ist: genau eine Kisten-Abweichung, aber die in a
// verschwundene Kiste steht nicht auf b.Player. Der alte Code lieferte einen
// Laufweg mit Geister-Schub einer Kiste, die dort in a gar nicht steht.
func TestStepsRejectsBoxNotOnPlayerField(t *testing.T) {
	fieldA, err := Parse(`
######
# @  #
# $  #
#  . #
#    #
######
`)
	if err != nil {
		t.Fatal(err)
	}
	// Kiste erst nach unten, dann nach rechts geschoben (2 Schübe):
	// der alte Code fand den scheinbar gültigen Ein-Schub-Weg "lddR"
	// (das Goal steht frei, damit Parse die Kiste nicht als eingefroren
	// zur Wand macht, siehe freezeGoalBoxesToWalls)
	fieldB, err := Parse(`
######
#    #
#    #
# @* #
#    #
######
`)
	if err != nil {
		t.Fatal(err)
	}

	stateA, stateB := State{}, State{}
	fieldA.GetState(&stateA)
	fieldB.GetState(&stateB)

	if moves, err := fieldA.Steps(&stateA, &stateB); err == nil {
		t.Fatalf("Paar zwei Schübe entfernt muss einen Fehler liefern, erhalten: %q", moves)
	}
}
