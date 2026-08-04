package blocker

import (
	"testing"

	"goSokoWahnBrute/soko"
)

// beide Direct-Write-Varianten müssen exakt dieselben Stufen liefern wie der Standard-Pfad
func TestBlockerDirectWriteMatches(t *testing.T) {
	build := func(directFactory func() DirectTable) []StageStats {
		field, err := soko.Parse(mapSmall)
		if err != nil {
			t.Fatal(err)
		}
		b := New(field, "")
		b.SetWorkers(8)
		b.SetChunkSize(1) // winzige Chunks, damit auch beim kleinen Level wirklich parallel gearbeitet wird
		if directFactory != nil {
			b.SetDirectTableFactory(directFactory)
		}
		for b.Next(1000000000) {
		}
		return b.GetStats().Stages
	}

	expected := build(nil)
	if len(expected) == 0 {
		t.Fatal("keine Blocker-Stufen berechnet")
	}

	for name, factory := range map[string]func() DirectTable{
		"xsync": NewXsyncDirect,
		"shard": NewShardDirect,
	} {
		got := build(factory)
		if len(got) != len(expected) {
			t.Fatalf("%s: Stufenanzahl weicht ab: %d statt %d", name, len(got), len(expected))
		}
		for i := range expected {
			if got[i] != expected[i] {
				t.Errorf("%s: Stufe %d weicht ab: %+v statt %+v", name, i+1, got[i], expected[i])
			}
		}
	}
}
