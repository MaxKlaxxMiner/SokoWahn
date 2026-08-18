package solver

import (
	"testing"

	"goSokoWahnBrute/crc64"
	"goSokoWahnBrute/maps"
)

// Micro-Benchmark der SegmentTable, identischer Ablauf wie BenchmarkCompactTable
func BenchmarkSegmentTable(b *testing.B) {
	const entries = 1 << 20

	for i := 0; i < b.N; i++ {
		table := NewSegmentTable()

		seed := uint64(12345)
		for n := 0; n < entries; n++ {
			table.Add(nextCrc(&seed), uint16(n&30000))
		}

		seed = 12345
		var sum uint32
		for n := 0; n < entries; n++ {
			sum += uint32(table.Get(nextCrc(&seed)))
		}

		seed = 98765
		for n := 0; n < entries; n++ {
			sum += uint32(table.Get(nextCrc(&seed)))
		}

		if table.Len() != entries {
			b.Fatalf("unerwartete Länge: %d", table.Len())
		}
		_ = sum
	}
}

// Konsistenz: die SegmentTable muss sich exakt wie eine Referenz-map verhalten
func TestSegmentTableMatchesMap(t *testing.T) {
	reference := make(map[crc64.Value]uint16)
	table := NewSegmentTable()

	seed := uint64(42)
	keys := make([]crc64.Value, 0, 50000)
	for n := 0; n < 50000; n++ {
		crc := nextCrc(&seed)
		keys = append(keys, crc)
		depth := uint16(n % 60001)
		reference[crc] = depth
		table.Add(crc, depth)
	}

	// Updates auf einem Teil der Schlüssel
	for n := 0; n < len(keys); n += 7 {
		reference[keys[n]] = uint16(n % 777)
		table.Update(keys[n], uint16(n%777))
	}

	if int64(len(reference)) != table.Len() {
		t.Fatalf("Längen weichen ab: map=%d segment=%d", len(reference), table.Len())
	}
	for _, crc := range keys {
		if table.Get(crc) != reference[crc] {
			t.Fatalf("Wert weicht ab für %x: map=%d segment=%d", uint64(crc), reference[crc], table.Get(crc))
		}
	}

	// unbekannte Schlüssel
	seed = uint64(4711)
	for n := 0; n < 10000; n++ {
		crc := nextCrc(&seed)
		if _, exists := reference[crc]; exists {
			continue
		}
		if got := table.Get(crc); got != DepthUnknown {
			t.Fatalf("Fehlschlag-Lookup liefert %d statt DepthUnknown für %x", got, uint64(crc))
		}
	}
}

// Grenzfälle des Slot-Layouts: Schlüssel 0, Rest-48-Bit 0 (Slot-Wert = nur ^Tiefe)
// und Tiefe 0 (Slot-Wert = rest<<16 | 0xffff) dürfen sich nicht mit dem
// Frei-Marker (Slot == 0) in die Quere kommen
func TestSegmentTableEdgeKeys(t *testing.T) {
	table := NewSegmentTable()

	// Schlüssel mit Rest 0 in verschiedenen Segmenten (inklusive Schlüssel 0)
	restZero := []crc64.Value{0, 1 << 48, 42 << 48, 65535 << 48}
	for _, crc := range restZero {
		if got := table.Get(crc); got != DepthUnknown {
			t.Fatalf("leere Tabelle liefert %d statt DepthUnknown für %x", got, uint64(crc))
		}
	}
	for i, crc := range restZero {
		table.Add(crc, uint16(i)) // Tiefe 0 ist dabei (Slot-Wert = 0xffff)
	}
	for i, crc := range restZero {
		if got := table.Get(crc); got != uint16(i) {
			t.Fatalf("Rest-0-Schlüssel %x: erwartete %d, erhalten %d", uint64(crc), i, got)
		}
	}
	if table.Len() != int64(len(restZero)) {
		t.Fatalf("erwartete %d Einträge, erhalten %d", len(restZero), table.Len())
	}

	// Update auf Rest-0-Schlüssel darf nicht als Neueintrag zählen
	table.Update(0, 30000)
	if got := table.Get(0); got != 30000 {
		t.Fatalf("Update auf Schlüssel 0: erwartete 30000, erhalten %d", got)
	}
	if table.Len() != int64(len(restZero)) {
		t.Fatalf("Update hat die Länge verändert: %d", table.Len())
	}

	// maximale reguläre Tiefe (65534 -> Slot-Tiefenbits = 1)
	table.Add(crc64.Value(0xdeadbeef), 65534)
	if got := table.Get(crc64.Value(0xdeadbeef)); got != 65534 {
		t.Fatalf("Tiefe 65534: erhalten %d", got)
	}
}

// Wachstum über mehrere Verdopplungen (inklusive des parallelen Rehash-Pfads
// ab 32 MB): alle Einträge müssen erhalten bleiben
func TestSegmentTableGrow(t *testing.T) {
	const entries = 3_500_000 // treibt die Tabelle von 2^19 auf 2^23 Slots (64 MB)
	table := NewSegmentTable()

	seed := uint64(777)
	for n := 0; n < entries; n++ {
		table.Add(nextCrc(&seed), uint16(n%60000))
	}
	if table.Len() != entries {
		t.Fatalf("erwartete %d Einträge, erhalten %d", entries, table.Len())
	}

	seed = uint64(777)
	for n := 0; n < entries; n++ {
		if got := table.Get(nextCrc(&seed)); got != uint16(n%60000) {
			t.Fatalf("Eintrag %d nach Grow verloren oder falsch: %d", n, got)
		}
	}
}

// Vanilla-Level mit der SegmentTable statt der CompactTable: die Tabelle ist ein
// reiner Key-Value-Store ohne Iteration, das Suchverhalten muss also bitgenau
// dem Vanilla-Anker entsprechen (230 Züge, vanillaNodes Knoten)
func TestSolveVanillaSegmentTable(t *testing.T) {
	if testing.Short() {
		t.Skip("Vanilla-Level dauert ca. 10 Sekunden (übersprungen mit -short)")
	}

	oldFactory := TableFactory
	TableFactory = NewSegmentTable
	defer func() { TableFactory = oldFactory }()

	s, _ := solveLevel(t, maps.MapVanilla, 230)
	if s.NodeCount() != vanillaNodes {
		t.Errorf("erwartete %d Knoten (Vanilla-Anker), erhalten: %d", vanillaNodes, s.NodeCount())
	}
}
