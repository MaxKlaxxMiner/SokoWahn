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

// gemeinsames Lastprofil: Einfügen + Treffer-Lookups + Fehlschlag-Lookups
func benchTable(b *testing.B, create func() PosTable) {
	const entries = 1 << 20 // 1 Mio Einträge

	for i := 0; i < b.N; i++ {
		table := create()

		seed := uint64(12345)
		for n := 0; n < entries; n++ {
			table.Add(nextCrc(&seed), uint16(n&30000))
		}

		// Treffer-Lookups (gleiche Sequenz erneut)
		seed = 12345
		var sum uint32
		for n := 0; n < entries; n++ {
			sum += uint32(table.Get(nextCrc(&seed)))
		}

		// Fehlschlag-Lookups (andere Sequenz)
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

func BenchmarkMapTable(b *testing.B)     { benchTable(b, NewMapTable) }
func BenchmarkCompactTable(b *testing.B) { benchTable(b, NewCompactTable) }

// Konsistenz: beide Implementierungen müssen sich identisch verhalten
func TestCompactTableMatchesMap(t *testing.T) {
	mapT := NewMapTable()
	compact := NewCompactTable()

	seed := uint64(42)
	keys := make([]crc64.Value, 0, 50000)
	for n := 0; n < 50000; n++ {
		crc := nextCrc(&seed)
		keys = append(keys, crc)
		depth := uint16(n % 60001)
		mapT.Add(crc, depth)
		compact.Add(crc, depth)
	}

	// Updates auf einem Teil der Schlüssel
	for n := 0; n < len(keys); n += 7 {
		mapT.Update(keys[n], uint16(n%777))
		compact.Update(keys[n], uint16(n%777))
	}

	if mapT.Len() != compact.Len() {
		t.Fatalf("Längen weichen ab: map=%d compact=%d", mapT.Len(), compact.Len())
	}
	for _, crc := range keys {
		if m, c := mapT.Get(crc), compact.Get(crc); m != c {
			t.Fatalf("Wert weicht ab für %x: map=%d compact=%d", uint64(crc), m, c)
		}
	}

	// unbekannte Schlüssel
	seed = uint64(4711)
	for n := 0; n < 10000; n++ {
		crc := nextCrc(&seed)
		if m, c := mapT.Get(crc), compact.Get(crc); m != c {
			t.Fatalf("Fehlschlag-Lookup weicht ab für %x: map=%d compact=%d", uint64(crc), m, c)
		}
	}
}
