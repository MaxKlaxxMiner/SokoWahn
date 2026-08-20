package soko

import "fmt"

// CheckSolution prüft eine LURD-Zugfolge gegen die Startaufstellung
// (Groß-/Kleinschreibung egal - Großbuchstaben markieren nur Schiebeschritte):
// jeder Zug muss spielbar sein, am Ende müssen alle Ziele belegt sein.
// Frozen-Boxes aus dem Parsen sind dabei Wände - eine echte Lösung fasst
// sie ohnehin nie an.
func (f *Field) CheckSolution(lurd string) error {
	player := f.initPlayer
	eof := f.walkEof
	boxAt := make([]bool, eof)
	for _, b := range f.InitBoxes() {
		boxAt[b] = true
	}

	for i := 0; i < len(lurd); i++ {
		dir := lurd[i] | 0x20 // zu Kleinbuchstaben
		if dir != DirLeft && dir != DirRight && dir != DirUp && dir != DirDown {
			return fmt.Errorf("ungültiges Zeichen %q an Position %d", string(lurd[i]), i+1)
		}
		next := f.Neighbor(player, dir)
		if next >= eof {
			return fmt.Errorf("Zug %d (%c) läuft in die Wand", i+1, lurd[i])
		}
		if boxAt[next] {
			behind := f.Neighbor(next, dir)
			if behind >= eof || boxAt[behind] {
				return fmt.Errorf("Zug %d (%c): Kiste lässt sich nicht schieben", i+1, lurd[i])
			}
			boxAt[next], boxAt[behind] = false, true
		}
		player = next
	}

	for _, g := range f.goals {
		if !boxAt[g] {
			return fmt.Errorf("Zugfolge spielbar, aber nicht gelöst (Ziel bleibt frei)")
		}
	}
	return nil
}
