package solver

import "goSokoWahnBrute/crc64"

// kompakte Hashtabelle mit offener Adressierung und linearem Sondieren:
// 10 Byte pro Slot (voller 64-Bit-Schlüssel + 16-Bit-Tiefe, verlustfrei),
// Kapazität 2^k mit Verdopplung bei 75% Füllstand.
// Der Schlüssel 0 markiert freie Slots - das Sondieren berührt dadurch nur das
// crcs-Array (8 Slots je Cacheline); der (praktisch nie vorkommende) echte
// Schlüssel 0 wird als Sonderfall separat geführt.
type CompactTable struct {
	crcs      []crc64.Value // volle Schlüssel je Slot, 0 = Slot frei
	depths    []uint16      // Tiefen je Slot
	count     int64         // Anzahl der Einträge (inklusive Sonderfall 0)
	mask      uint64        // Kapazität-1 (Kapazität ist immer eine Zweierpotenz)
	zeroDepth uint16        // Tiefe für den Sonderfall Schlüssel 0, DepthUnknown = nicht vorhanden
}

// Max-Memory-Modus (TUI-Taste m): CompactTables verdoppeln erst bei 93,75%
// Füllstand statt 75% (Anzeige: 125%) - quetscht ein Viertel mehr Einträge aus
// jeder Verdopplungsstufe, auf Kosten deutlich längerer Sondierketten kurz vor
// dem Resize. Zur Laufzeit umschaltbar, wirkt ab der nächsten Einfügung; beim
// Zurückschalten mit über 75% Füllstand wächst die Tabelle bei der nächsten
// Einfügung sofort. (Die SegmentTable hat ihr eigenes SegmentGrowPercent.)
var CompactMaxMemory = false

func NewCompactTable() PosTable {
	return newCompactTable(1 << 12)
}

// große Start-Kapazität für echte Läufe (App setzt die TableFactory in main um):
// 2^28 Slots = 2,68 GB je Tabelle, hält 201M (bzw. 251M mit MaxMem) Einträge ohne
// eine einzige Verdopplung - spart die Rehash-Pausen der Aufwärm-Leiter und hält
// den Füllstand lange niedrig (Messung von Max: +10% Durchsatz im Minutentest)
func NewCompactTableLarge() PosTable {
	return newCompactTable(1 << 28)
}

func newCompactTable(capacity int) *CompactTable {
	return &CompactTable{
		crcs:      make([]crc64.Value, capacity),
		depths:    make([]uint16, capacity),
		mask:      uint64(capacity - 1),
		zeroDepth: DepthUnknown,
	}
}

func (t *CompactTable) Get(crc crc64.Value) uint16 {
	if crc == 0 {
		return t.zeroDepth
	}
	i := uint64(crc) & t.mask
	for {
		c := t.crcs[i]
		if c == crc {
			return t.depths[i]
		}
		if c == 0 {
			return DepthUnknown // freier Slot -> Schlüssel nicht vorhanden
		}
		i = (i + 1) & t.mask
	}
}

func (t *CompactTable) Add(crc crc64.Value, depth uint16) {
	if crc == 0 {
		if t.zeroDepth == DepthUnknown {
			t.count++
		}
		t.zeroDepth = depth
		return
	}

	limit := int64(len(t.crcs)) / 4 * 3
	if CompactMaxMemory {
		limit = int64(len(t.crcs)) / 16 * 15
	}
	if t.count >= limit {
		t.grow()
	}

	i := uint64(crc) & t.mask
	for {
		c := t.crcs[i]
		if c == 0 {
			break
		}
		if c == crc {
			t.depths[i] = depth // Schlüssel existiert bereits -> nur Tiefe setzen (map-Semantik)
			return
		}
		i = (i + 1) & t.mask
	}

	t.crcs[i] = crc
	t.depths[i] = depth
	t.count++
}

func (t *CompactTable) Update(crc crc64.Value, depth uint16) {
	t.Add(crc, depth)
}

func (t *CompactTable) Len() int64 {
	return t.count
}

func (t *CompactTable) Bytes() int64 {
	return int64(len(t.crcs))*8 + int64(len(t.depths))*2
}

// Füllstand relativ zur Standard-Wachstums-Schwelle (75% der Kapazität):
// bei 1.0 löst die nächste Einfügung die Verdopplung aus; im Max-Memory-Modus
// läuft die Anzeige bis 1.25 (Resize erst bei 93,75% der Kapazität)
func (t *CompactTable) Fill() float64 {
	return float64(t.count) / (float64(len(t.crcs)) * 0.75)
}

// verdoppelt die Kapazität und sortiert alle Einträge neu ein
func (t *CompactTable) grow() {
	oldCrcs, oldDepths := t.crcs, t.depths

	capacity := len(oldCrcs) * 2
	t.crcs = make([]crc64.Value, capacity)
	t.depths = make([]uint16, capacity)
	t.mask = uint64(capacity - 1)

	for i, crc := range oldCrcs {
		if crc == 0 {
			continue
		}
		j := uint64(crc) & t.mask
		for t.crcs[j] != 0 {
			j = (j + 1) & t.mask
		}
		t.crcs[j] = crc
		t.depths[j] = oldDepths[i]
	}
}
