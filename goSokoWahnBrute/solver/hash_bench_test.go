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
