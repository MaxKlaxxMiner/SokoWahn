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

func NewCompactTable() PosTable {
	return newCompactTable(1 << 12)
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

	if t.count >= int64(len(t.crcs))/4*3 {
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
