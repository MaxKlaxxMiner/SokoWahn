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
