// Paket rooms: Kern des Rooms-Konzepts (Räume, Portale, Zustände, Varianten).
// Konzept siehe docs/konzept.md, C#-Vorbild: SokoWahn/SokoWahnLib/Rooms.
package rooms

import (
	"goSokoWahnRooms/soko"
)

// Liste aller Kisten-Zustände eines Raumes.
// Konvention: Zustand 0 ist immer der gelöste Endzustand (alle Raum-Ziele belegt bzw. leer).
// Die Kistenpositionen aller Zustände liegen hintereinander in einem flachen Puffer.
type StateList struct {
	offs []uint32    // Start-Offsets in buf, len = Count+1 (letzter Eintrag = len(buf))
	buf  []soko.Wpos // Kistenpositionen aller Zustände (je Zustand aufsteigend sortiert)
}

func NewStateList() *StateList {
	return &StateList{offs: []uint32{0}}
}

// gibt die Anzahl der gespeicherten Zustände zurück
func (s *StateList) Count() uint64 {
	return uint64(len(s.offs) - 1)
}

// fügt einen weiteren Zustand hinzu und gibt dessen ID zurück
// (boxes müssen aufsteigend sortiert sein, es findet kein Dedup statt)
func (s *StateList) Add(boxes []soko.Wpos) uint64 {
	id := uint64(len(s.offs) - 1)
	s.buf = append(s.buf, boxes...)
	s.offs = append(s.offs, uint32(len(s.buf)))
	return id
}

// gibt die Kistenpositionen eines Zustandes zurück (View in den Puffer, nur lesen!)
func (s *StateList) Get(id uint64) []soko.Wpos {
	return s.buf[s.offs[id]:s.offs[id+1]]
}

// gibt die Anzahl der Kisten eines Zustandes zurück
func (s *StateList) BoxCount(id uint64) int {
	return int(s.offs[id+1] - s.offs[id])
}
