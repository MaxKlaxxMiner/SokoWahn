package sokofield

type wpos uint32 // Index-Position innerhalb begehbarer Felder

type Field struct {
	// --- statische Daten (bleiben dauerhaft erhalten) ---
	fieldData   []byte // Grunddaten des gesamten Spielfeldes als Standard ASCII-Zeichen, Größe: width * height
	width       int    // Breite des gesamten Spielfeldes
	height      int    // Höhe des gesamten Spielfeldes
	walkSize    wpos   // Anzahl der (theoretisch) begehbaren Felder
	walkLeft    []wpos // Index zum linken begehbaren Feld, walkSize wenn nicht begehbar
	walkRight   []wpos // Index zum rechten begehbaren Feld, walkSize wenn nicht begehbar
	walkUp      []wpos // Index zum oberen begehbaren Feld, walkSize wenn nicht begehbar
	walkDown    []wpos // Index zum unteren begehbaren Feld, walkSize wenn nicht begehbar
	fieldToWpos []wpos // Map zu den Index-Werten, walkSize wenn nicht begehbar
	wposToField []int  // Map vom Index wieder zu den absoluten Positionen
	boxCount    uint32 // Anzahl der Kisten auf dem Spielfeld
	initPlayer  wpos   // Startposition des Spielers, Index zu begehbaren Feldern
	initBoxes   []wpos // Startpositionen aller Kisten, Index zu begehbaren Feldern
	goals       []wpos // Endpositionen aller Kisten = die Zielfelder, Index zu begehbaren Feldern

	// --- dynamische Daten (werden durch Spielzüge geändert) ---
	player      wpos     // Aktuelle Spielerposition, Index zu begehbaren Feldern
	wposToBoxes []uint32 // Index von begehbaren Feldern zu Kisten, boxCount wenn keine Kiste auf dem Feld
	boxes       []wpos   // Positionen der Kisten, walkSize wenn keine Kiste vorhanden
	moveDepth   int      // Aktuelle Zugtiefe

	// --- temporäre Buffer für die Suchfunktion ---
	tmpCheckPos   []wpos   // Temporäre Positionen zur Prüfung
	tmpCheckDepth []uint32 // Temporäre Zugtiefen zur Prüfung
	tmpCheckDone  []bool   // Felder, die bereits geprüft wurden
}

func (f *Field) String() string {
	output := make([]byte, 0, len(f.fieldData)+f.height)

	for y := 0; y < f.height; y++ {
		for x := 0; x < f.width; x++ {
			c := f.fieldData[x+y*f.width]
			switch c {
			case '$', '@':
				c = ' '
			case '*', '+':
				c = '.'
			}
			output = append(output, c)
		}
		output = append(output, '\n')
	}

	px, py := f.wposToField[f.player]%f.width, f.wposToField[f.player]/f.width
	if opos := px + py*(f.width+1); output[opos] == ' ' {
		output[opos] = '@'
	} else {
		output[opos] = '+'
	}
	for _, box := range f.boxes {
		px, py := f.wposToField[box]%f.width, f.wposToField[box]/f.width
		if opos := px + py*(f.width+1); output[opos] == ' ' {
			output[opos] = '$'
		} else {
			output[opos] = '*'
		}
	}

	return string(output)
}
