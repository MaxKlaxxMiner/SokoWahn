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
