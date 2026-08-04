package solver

import "goSokoWahnBrute/soko"

// Suchliste für noch zu prüfende Stellungen einer bestimmten Zugtiefe (FIFO)
// Satz = Spielerposition + Kistenpositionen, jeweils als uint16
// (die Zugtiefe selbst steckt im Listenindex des Solvers, nicht im Satz)
type DepthList struct {
	data       []uint16
	recordSize int // Satzgröße in uint16-Werten = Kistenanzahl + 1
	readPos    int // Leseposition in Sätzen
}

func NewDepthList(recordSize int) *DepthList {
	return &DepthList{recordSize: recordSize}
}

// trägt eine Stellung ein
func (l *DepthList) Push(state *soko.State) {
	l.data = append(l.data, uint16(state.Player))
	for _, box := range state.Boxes {
		l.data = append(l.data, uint16(box))
	}
}

// entnimmt bis zu maxRecords Sätze und gibt sie als flaches Slice zurück
// (das Slice bleibt auch gültig, wenn die Liste danach weiter wächst)
func (l *DepthList) PopBatch(maxRecords int) []uint16 {
	if remain := l.Count(); maxRecords > remain {
		maxRecords = remain
	}
	from := l.readPos * l.recordSize
	l.readPos += maxRecords
	return l.data[from : from+maxRecords*l.recordSize]
}

// Anzahl der noch nicht entnommenen Sätze
func (l *DepthList) Count() int {
	return len(l.data)/l.recordSize - l.readPos
}

// gibt den Speicher frei (sinnvoll, sobald die Liste komplett abgearbeitet ist)
func (l *DepthList) Release() {
	l.data = nil
	l.readPos = 0
}
