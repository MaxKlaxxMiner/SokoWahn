package solver

import (
	"testing"

	"goSokoWahnBrute/crc64"
)

// einfacher deterministischer Pseudozufalls-Generator (SplitMix64)
func nextCrc(state *uint64) crc64.Value {
	*state += 0x9e3779b97f4a7c15
	z := *state
	z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9
	z = (z ^ (z >> 27)) * 0x94d049bb133111eb
	return crc64.Value(z ^ (z >> 31))
}

// Micro-Benchmark der CompactTable: Einfügen + Treffer- + Fehlschlag-Lookups
func BenchmarkCompactTable(b *testing.B) {
	const entries = 1 << 20

	for i := 0; i < b.N; i++ {
		table := NewCompactTable()

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

// Max-Memory-Modus: Verdopplung erst bei 93,75% statt 75% der Kapazität,
// die Inhalte bleiben auch bei hohem Füllstand korrekt
func TestCompactMaxMemory(t *testing.T) {
	defer func() { CompactMaxMemory = false }()

	// Referenz Normal-Modus: Kapazität 4096 verdoppelt bei der 3073. Einfügung
	normal := newCompactTable(1 << 12)
	seed := uint64(7)
	for n := 0; n < 3073; n++ {
		normal.Add(nextCrc(&seed), uint16(n))
	}
	if got := normal.Bytes(); got != (1<<13)*10 {
		t.Fatalf("Normal-Modus: erwartet Verdopplung auf 8192 Slots, Bytes = %d", got)
	}

	// Max-Memory-Modus: dieselbe Tabelle bleibt bis 3840 Einträge (93,75%) klein
	CompactMaxMemory = true
	table := newCompactTable(1 << 12)
	seed = uint64(7)
	keys := make([]crc64.Value, 0, 3840)
	for n := 0; n < 3840; n++ {
		crc := nextCrc(&seed)
		keys = append(keys, crc)
		table.Add(crc, uint16(n))
	}
	if got := table.Bytes(); got != (1<<12)*10 {
		t.Fatalf("Max-Memory: Tabelle ist vorzeitig gewachsen (Bytes = %d bei %d Einträgen)", got, table.Len())
	}
	if fill := table.Fill(); fill < 1.24 || fill > 1.26 {
		t.Fatalf("Füllstand-Anzeige an der MaxMem-Schwelle: erwartet 1.25, erhalten %f", fill)
	}
	for i, crc := range keys {
		if got := table.Get(crc); got != uint16(i) {
			t.Fatalf("Wert weicht bei 93,75%% Füllstand ab für %x: erwartet %d, erhalten %d", uint64(crc), i, got)
		}
	}

	// die nächste Einfügung überschreitet die Schwelle -> Verdopplung
	table.Add(nextCrc(&seed), 4711)
	if got := table.Bytes(); got != (1<<13)*10 {
		t.Fatalf("Max-Memory: erwartet Verdopplung auf 8192 Slots, Bytes = %d", got)
	}

	// Zurückschalten mit Füllstand über 75%: die nächste Einfügung wächst sofort
	over := newCompactTable(1 << 12)
	seed = uint64(99)
	for n := 0; n < 3200; n++ { // 78% von 4096, nur mit MaxMem erreichbar
		over.Add(nextCrc(&seed), uint16(n))
	}
	CompactMaxMemory = false
	over.Add(nextCrc(&seed), 1)
	if got := over.Bytes(); got != (1<<13)*10 {
		t.Fatalf("Rückschalt-Verhalten: erwartet sofortige Verdopplung, Bytes = %d", got)
	}
}

// Vergleich Normal- gegen Max-Memory-Modus kurz vor der Resize-Schwelle:
// 15,6M Einträge sind 93% von 2^24 - der Normal-Modus ist da längst auf 2^25
// gewachsen (Füllstand 47%, 320 MB), MaxMem bleibt bei 2^24 (93%, 160 MB).
// Misst den Lookup-Preis des halbierten Speichers (je Iteration ein Treffer-
// und ein Fehlschlag-Lookup; Treffer nur solange b.N <= 15,6M)
func BenchmarkCompactTableMaxMemory(b *testing.B) {
	const entries = 15_600_000

	for _, maxMem := range []bool{false, true} {
		name := "normal75"
		if maxMem {
			name = "maxmem93"
		}
		b.Run(name, func(b *testing.B) {
			CompactMaxMemory = maxMem
			defer func() { CompactMaxMemory = false }()

			table := NewCompactTable()
			seed := uint64(12345)
			for n := 0; n < entries; n++ {
				table.Add(nextCrc(&seed), uint16(n&30000))
			}
			b.Logf("RAM: %d MB, Füllstand-Anzeige: %.1f %%", table.Bytes()>>20, table.(*CompactTable).Fill()*100)

			hitSeed, missSeed := uint64(12345), uint64(98765)
			var sum uint32
			b.ResetTimer()
			for n := 0; n < b.N; n++ {
				sum += uint32(table.Get(nextCrc(&hitSeed)))
				sum += uint32(table.Get(nextCrc(&missSeed)))
			}
			_ = sum
		})
	}
}

// Konsistenz: die CompactTable muss sich exakt wie eine Referenz-map verhalten
func TestCompactTableMatchesMap(t *testing.T) {
	reference := make(map[crc64.Value]uint16)
	compact := NewCompactTable()

	seed := uint64(42)
	keys := make([]crc64.Value, 0, 50000)
	for n := 0; n < 50000; n++ {
		crc := nextCrc(&seed)
		keys = append(keys, crc)
		depth := uint16(n % 60001)
		reference[crc] = depth
		compact.Add(crc, depth)
	}

	// Updates auf einem Teil der Schlüssel
	for n := 0; n < len(keys); n += 7 {
		reference[keys[n]] = uint16(n % 777)
		compact.Update(keys[n], uint16(n%777))
	}

	if int64(len(reference)) != compact.Len() {
		t.Fatalf("Längen weichen ab: map=%d compact=%d", len(reference), compact.Len())
	}
	for _, crc := range keys {
		if compact.Get(crc) != reference[crc] {
			t.Fatalf("Wert weicht ab für %x: map=%d compact=%d", uint64(crc), reference[crc], compact.Get(crc))
		}
	}

	// unbekannte Schlüssel
	seed = uint64(4711)
	for n := 0; n < 10000; n++ {
		crc := nextCrc(&seed)
		if _, exists := reference[crc]; exists {
			continue
		}
		if got := compact.Get(crc); got != DepthUnknown {
			t.Fatalf("Fehlschlag-Lookup liefert %d statt DepthUnknown für %x", got, uint64(crc))
		}
	}
}
