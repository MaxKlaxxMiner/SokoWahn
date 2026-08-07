package soko

import "testing"

// Mini-Level: ein einzelner Schub nach rechts auf das Zielfeld löst das Level
const mapMini = `
#####
#@$.#
#####
`

// Zwei-Schub-Level: Kiste muss zweimal nach rechts geschoben werden
const mapTwoPush = `
######
#@$ .#
######
`

// kleines Level mit mehreren Kisten für Konsistenz-Tests
const mapSmall = `
#######
#.@ # #
#$* $ #
#   $ #
# ..  #
#  *  #
#######
`

func TestSearchVariantsForwardMini(t *testing.T) {
	field, err := Parse(mapMini)
	if err != nil {
		t.Fatal(err)
	}

	variants := field.SearchVariantsForward(field.MakeStateBuffer(4)[:0])
	if len(variants) != 1 {
		t.Fatalf("erwartet 1 Variante, erhalten: %d", len(variants))
	}
	if variants[0].MoveDepth != 1 {
		t.Errorf("erwartete Zugtiefe 1, erhalten: %d", variants[0].MoveDepth)
	}

	// Variante anwenden -> Level muss gelöst sein
	field.SetState(&variants[0])
	if !field.IsSolved() {
		t.Errorf("Level müsste gelöst sein:\n%s", field)
	}
}

// jede Vorwärts-Variante zweiter Stufe muss per Rückwärtssuche exakt (Crc-gleich) zu ihrer
// Vorgänger-Stellung zurückführen (Vorgänger ist selbst eine Schub-Stellung, daher immer auffindbar)
func TestSearchVariantsForwardBackwardConsistency(t *testing.T) {
	field, err := Parse(mapSmall)
	if err != nil {
		t.Fatal(err)
	}

	firstGen := field.SearchVariantsForward(field.MakeStateBuffer(64)[:0])
	if len(firstGen) == 0 {
		t.Fatal("keine Vorwärts-Varianten gefunden")
	}

	work := field.Clone()
	secondBuf := work.MakeStateBuffer(64)
	backBuf := work.MakeStateBuffer(64)

	for i := range firstGen {
		work.SetState(&firstGen[i])
		secondGen := work.SearchVariantsForward(secondBuf[:0])

		for j := range secondGen {
			work.SetState(&secondGen[j])
			backward := work.SearchVariantsBackward(backBuf[:0])

			found := false
			for k := range backward {
				if backward[k].Crc == firstGen[i].Crc {
					found = true
					break
				}
			}
			if !found {
				t.Errorf("Variante %d/%d führt rückwärts nicht zum Vorgänger zurück:\n%s", i, j, work.DebugState(&secondGen[j]))
			}

			work.SetState(&firstGen[i]) // Zustand für die nächste zweite Stufe wiederherstellen
		}
	}
}

func TestSearchVariantsForwardRestoresField(t *testing.T) {
	field, err := Parse(mapSmall)
	if err != nil {
		t.Fatal(err)
	}

	before := State{}
	field.GetState(&before)

	field.SearchVariantsForward(field.MakeStateBuffer(64)[:0])

	after := State{}
	field.GetState(&after)

	if before.Crc != after.Crc || before.Player != after.Player {
		t.Errorf("Feld wurde nach der Suche nicht wiederhergestellt: vorher %v, nachher %v", before, after)
	}
}

func TestSearchGoalStates(t *testing.T) {
	// Quirk aus dem C#-Original: Zielstellungen werden nur geliefert, wenn von ihnen aus
	// ein Rückwärtszug mit anschließendem weiteren Rückwärtszug möglich ist -
	// 1-Schub-Level haben daher keine Zielstellungen (das Original konnte sie nicht lösen)
	field, err := Parse(mapMini)
	if err != nil {
		t.Fatal(err)
	}
	if goals := field.SearchGoalStates(); len(goals) != 0 {
		t.Errorf("Mini-Level: erwartet 0 Zielstellungen (C#-Verhalten), erhalten: %d", len(goals))
	}

	// beim 2-Schub-Level muss genau eine Zielstellung gefunden werden
	field, err = Parse(mapTwoPush)
	if err != nil {
		t.Fatal(err)
	}
	goals := field.SearchGoalStates()
	if len(goals) != 1 {
		t.Fatalf("erwartet 1 Zielstellung, erhalten: %d", len(goals))
	}
	if goals[0].MoveDepth != 0 {
		t.Errorf("erwartete Zugtiefe 0, erhalten: %d", goals[0].MoveDepth)
	}

	// die Zielstellung selbst muss gelöst sein
	work := field.Clone()
	work.SetState(&goals[0])
	if !work.IsSolved() {
		t.Errorf("Zielstellung ist nicht gelöst:\n%s", work)
	}
}
