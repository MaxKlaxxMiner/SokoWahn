package soko

// gibt die Anzahl der Kisten auf dem Spielfeld zurück
func (f *Field) BoxCount() int {
	return int(f.boxCount)
}

// gibt die Zielfelder zurück (aufsteigend sortiert, nur lesen!)
func (f *Field) Goals() []Wpos {
	return f.goals
}

// gibt die Startpositionen der Kisten zurück (aufsteigend sortiert, nur lesen!)
func (f *Field) InitBoxes() []Wpos {
	return f.initBoxes
}

// gibt die Anzahl der begehbaren Felder zurück
func (f *Field) WalkCount() int {
	return int(f.walkEof)
}

// rechnet ein begehbares Feld in seine Spielfeld-Koordinaten um
// (Spalte/Zeile passend zur Ausgabe von String(), fürs Einfärben im TUI)
func (f *Field) FieldXY(pos Wpos) (x, y int) {
	abs := f.wposToField[pos]
	return abs % f.width, abs / f.width
}

// gibt den gesetzten Deadlock-Filter zurück (nil = keiner)
func (f *Field) Blocker() BlockerCheck {
	return f.blocker
}

// ermittelt die Schub-Pose-Kandidaten einer Stellung: alle Kisten direkt neben der
// Spielerposition, die als "zuletzt geschobene Kiste" infrage kommen (Kiste auf dem
// Nachbarfeld, gegenüberliegendes Feld begehbar und kistenfrei). Jede Nach-Schub-Stellung
// hat mindestens einen Kandidaten (die tatsächlich geschobene Kiste).
// boxBits ist die Kisten-Bitmaske des abfragenden Feldes (gleiche Feldgeometrie
// vorausgesetzt); es werden nur die Nachbar-Tabellen dieses Feldes gelesen.
func (f *Field) PushPoseCandidates(player Wpos, boxBits []uint64) (candidates [4]Wpos, count int) {
	hasBox := func(pos Wpos) bool {
		return pos < f.walkEof && boxBits[pos>>6]&(1<<(pos&63)) != 0
	}
	free := func(pos Wpos) bool {
		return pos < f.walkEof && boxBits[pos>>6]&(1<<(pos&63)) == 0
	}

	if box := f.walkLeft[player]; hasBox(box) && free(f.walkRight[player]) {
		candidates[count] = box // Kiste wurde zuletzt nach links geschoben
		count++
	}
	if box := f.walkRight[player]; hasBox(box) && free(f.walkLeft[player]) {
		candidates[count] = box // Kiste wurde zuletzt nach rechts geschoben
		count++
	}
	if box := f.walkUp[player]; hasBox(box) && free(f.walkDown[player]) {
		candidates[count] = box // Kiste wurde zuletzt nach oben geschoben
		count++
	}
	if box := f.walkDown[player]; hasBox(box) && free(f.walkUp[player]) {
		candidates[count] = box // Kiste wurde zuletzt nach unten geschoben
		count++
	}
	return
}

// berechnet einen Hash über die Feldgeometrie (für Cache-Dateinamen,
// gleiche Idee wie im C#-Original: FNV über Breite, Höhe und Feldinhalt)
func (f *Field) FieldCrc() uint64 {
	crc := crc64start
	crc = (crc ^ uint64(f.width)) * crc64mul
	crc = (crc ^ uint64(f.height)) * crc64mul
	for _, c := range f.fieldData {
		crc = (crc ^ uint64(c)) * crc64mul
	}
	return crc
}

const (
	crc64start = uint64(0xcbf29ce484222325)
	crc64mul   = uint64(0x100000001b3)
)
