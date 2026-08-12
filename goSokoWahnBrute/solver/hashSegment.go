package solver

import (
	"runtime"
	"sync"

	"goSokoWahnBrute/crc64"
)

// SegmentTable: 8-Byte-Slots statt 10 wie in der CompactTable, verlustfrei.
// Die oberen 16 Bit des Schlüssels stecken in der Slot-Position (65536 Segmente),
// die dadurch freien 16 Bit nehmen die Zugtiefe auf:
//
//	slot = rest48<<16 | ^tiefe    (0 = Slot frei)
//
// Die Tiefe wird bitweise invertiert abgelegt: DepthUnknown (65535, wird nie
// gespeichert) invertiert zu 0 - dadurch ist der komplett leere Slot exakt
// "Rest-Schlüssel 0 mit unbekannter Tiefe", Zero-Init kommt gratis von den
// OS-Zero-Pages und es braucht keinen Sonderfall für den Schlüssel 0.
//
// Sondiert wird linear, aber strikt innerhalb des Segments (Wrap am Segmentende) -
// nur so bleiben die implizierten Top-16-Bits rekonstruierbar. Der Home-Slot ist
// auf 8er-Buckets ausgerichtet (64 Byte = eine Cacheline, Muster von Stockfish):
// die ersten 8 Probes lesen genau eine Cacheline.
//
// Grow (Verdopplung) hat zwei Auslöser: den globalen Füllstand (SegmentGrowPercent,
// Performance-Wächter gegen lange Sondierketten) und ein fast volles Segment
// (Korrektheits-Backstop: jedes Segment behält immer mindestens einen freien Slot,
// sonst würden Fehlschlag-Lookups endlos kreisen). Das Rehash läuft je Segment
// unabhängig und deshalb bei großen Tabellen parallel über alle Kerne - erlaubt,
// weil alle Schreibzugriffe des Solvers in der seriellen Merge-Phase passieren.
type SegmentTable struct {
	slots     []uint64 // rest48<<16 | ^Tiefe, 0 = frei
	segCounts []int32  // Einträge je Segment (für den Segment-voll-Auslöser)
	count     int64    // Anzahl der Einträge gesamt
	segShift  uint     // log2 der Slots je Segment
	segMask   uint64   // Slots je Segment - 1
}

const (
	segmentCount = 1 << 16     // Segmente = implizierte Top-16-Bits des Schlüssels
	segRestMask  = 1<<48 - 1   // untere 48 Bit des Schlüssels (gespeicherter Rest)
	segMinSlots  = 8 * segmentCount // Mindestkapazität: 8 Slots je Segment = 4 MB
)

// globaler Füllstand in Prozent, ab dem die SegmentTable verdoppelt (Performance-
// Wächter: bei 75% kostet ein Fehlschlag-Lookup im Schnitt ~8,5 Probes = 1-2
// Cachelines; zur Laufzeit erhöhbar, wenn RAM wichtiger ist als Suchtempo)
var SegmentGrowPercent = 75

func NewSegmentTable() PosTable {
	return newSegmentTable(segMinSlots)
}

func newSegmentTable(capacity int) *SegmentTable {
	t := &SegmentTable{
		slots:     make([]uint64, capacity),
		segCounts: make([]int32, segmentCount),
	}
	segSize := uint64(capacity / segmentCount)
	for 1<<(t.segShift+1) <= segSize {
		t.segShift++
	}
	t.segMask = 1<<t.segShift - 1
	return t
}

func (t *SegmentTable) Get(crc crc64.Value) uint16 {
	key := uint64(crc)
	rest := key & segRestMask
	base := (key >> 48) << t.segShift
	j := rest & t.segMask &^ 7
	for {
		v := t.slots[base|j]
		if v>>16 == rest {
			return ^uint16(v) // trifft für freie Slots (v == 0) nur bei rest == 0 zu
			// und liefert dann korrekt ^0 = DepthUnknown ("nicht vorhanden")
		}
		if v == 0 {
			return DepthUnknown // freier Slot -> Schlüssel nicht vorhanden
		}
		j = (j + 1) & t.segMask
	}
}

func (t *SegmentTable) Add(crc crc64.Value, depth uint16) {
	key := uint64(crc)
	seg := key >> 48

	// Grow vor dem Einfügen: global bei SegmentGrowPercent oder wenn das Ziel-Segment
	// nur noch einen freien Slot hat (der bleibt als Sondier-Stopper immer erhalten)
	if t.count >= int64(len(t.slots))*int64(SegmentGrowPercent)/100 ||
		int64(t.segCounts[seg]) >= int64(t.segMask) {
		t.grow()
	}

	rest := key & segRestMask
	base := seg << t.segShift
	j := rest & t.segMask &^ 7
	for {
		v := t.slots[base|j]
		if v == 0 {
			break // freier Slot gefunden (Prüfreihenfolge wichtig: sonst würde ein
			// neuer Schlüssel mit rest 0 als Überschreiben statt Einfügen zählen)
		}
		if v>>16 == rest {
			t.slots[base|j] = rest<<16 | uint64(^depth) // Schlüssel existiert -> nur Tiefe setzen
			return
		}
		j = (j + 1) & t.segMask
	}

	t.slots[base|j] = rest<<16 | uint64(^depth)
	t.segCounts[seg]++
	t.count++
}

func (t *SegmentTable) Update(crc crc64.Value, depth uint16) {
	t.Add(crc, depth)
}

func (t *SegmentTable) Len() int64 {
	return t.count
}

func (t *SegmentTable) Bytes() int64 {
	return int64(len(t.slots))*8 + int64(len(t.segCounts))*4
}

// Füllstand relativ zur globalen Wachstums-Schwelle (SegmentGrowPercent);
// der Segment-voll-Backstop kann die Verdopplung vereinzelt früher auslösen
func (t *SegmentTable) Fill() float64 {
	return float64(t.count) / (float64(len(t.slots)) * float64(SegmentGrowPercent) / 100)
}

// verdoppelt die Kapazität; jedes Segment rehasht ausschließlich in sein eigenes,
// verdoppeltes Gegenstück - bei großen Tabellen parallel über alle Kerne
func (t *SegmentTable) grow() {
	oldSlots, oldShift, oldMask := t.slots, t.segShift, t.segMask

	t.slots = make([]uint64, len(oldSlots)*2)
	t.segShift = oldShift + 1
	t.segMask = oldMask<<1 | 1

	workers := 1
	if len(t.slots) >= 1<<22 { // ab 32 MB lohnen sich Goroutinen fürs Umkopieren
		workers = runtime.NumCPU()
	}

	var wg sync.WaitGroup
	for w := 0; w < workers; w++ {
		wg.Add(1)
		go func(w int) {
			defer wg.Done()
			for seg := uint64(segmentCount * w / workers); seg < uint64(segmentCount*(w+1)/workers); seg++ {
				oldBase := seg << oldShift
				newBase := seg << t.segShift
				for i := uint64(0); i <= oldMask; i++ {
					v := oldSlots[oldBase|i]
					if v == 0 {
						continue
					}
					j := (v >> 16) & t.segMask &^ 7 // Home-Slot aus dem Rest-Schlüssel
					for t.slots[newBase|j] != 0 {
						j = (j + 1) & t.segMask
					}
					t.slots[newBase|j] = v
				}
			}
		}(w)
	}
	wg.Wait()
}
