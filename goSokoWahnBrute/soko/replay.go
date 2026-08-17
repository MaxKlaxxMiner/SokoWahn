package soko

import (
	"fmt"
	"sort"
)

// ReplayLurd spielt eine LURD-Zugfolge (Kleinbuchstaben = Laufzüge, Großbuchstaben
// = Schiebezüge) ab dem aktuellen Spielstand des Feldes ab und liefert die Folge
// der Schub-Stellungen: die Ausgangsstellung plus eine Stellung je Schub, mit
// MoveDepth als kumulierter Zugzahl und aufsteigend sortierten Kisten (dieselbe
// Repräsentation wie die Varianten der Suche). Das Feld selbst bleibt unverändert.
// Ungültige Züge (nicht begehbar, Kiste fehlt oder blockiert) melden einen Fehler.
func (f *Field) ReplayLurd(lurd string) ([]State, error) {
	player := f.player
	boxes := make([]Wpos, len(f.boxes))
	copy(boxes, f.boxes)
	boxAt := make(map[Wpos]int, len(boxes)) // Kistenposition -> Index in boxes
	for i, b := range boxes {
		boxAt[b] = i
	}

	collect := func(moveDepth int) State {
		st := State{Player: player, MoveDepth: int32(moveDepth), Boxes: make([]Wpos, len(boxes))}
		copy(st.Boxes, boxes)
		sort.Slice(st.Boxes, func(i, j int) bool { return st.Boxes[i] < st.Boxes[j] })
		st.UpdateCrc()
		return st
	}

	states := []State{collect(0)}
	for i := 0; i < len(lurd); i++ {
		c := lurd[i]
		var walk []Wpos
		switch c | 0x20 { // Kleinbuchstaben-Variante entscheidet die Richtung
		case 'l':
			walk = f.walkLeft
		case 'r':
			walk = f.walkRight
		case 'u':
			walk = f.walkUp
		case 'd':
			walk = f.walkDown
		default:
			return nil, fmt.Errorf("ungültiges LURD-Zeichen %q an Zug %d", c, i+1)
		}

		next := walk[player]
		if next >= f.walkEof {
			return nil, fmt.Errorf("Zug %d (%c): Zielfeld nicht begehbar", i+1, c)
		}

		if c >= 'A' && c <= 'Z' { // Schiebezug
			box, ok := boxAt[next]
			if !ok {
				return nil, fmt.Errorf("Zug %d (%c): keine Kiste auf dem Nachbarfeld", i+1, c)
			}
			target := walk[next]
			if target >= f.walkEof {
				return nil, fmt.Errorf("Zug %d (%c): Kistenziel nicht begehbar", i+1, c)
			}
			if _, blocked := boxAt[target]; blocked {
				return nil, fmt.Errorf("Zug %d (%c): Kistenziel durch Kiste blockiert", i+1, c)
			}
			delete(boxAt, next)
			boxAt[target] = box
			boxes[box] = target
			player = next
			states = append(states, collect(i+1))
		} else { // Laufzug
			if _, blocked := boxAt[next]; blocked {
				return nil, fmt.Errorf("Zug %d (%c): Laufzug in eine Kiste", i+1, c)
			}
			player = next
		}
	}

	return states, nil
}
