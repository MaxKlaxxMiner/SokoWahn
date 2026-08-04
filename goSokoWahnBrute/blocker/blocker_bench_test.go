package blocker

import (
	"testing"

	"goSokoWahnBrute/soko"
	"goSokoWahnBrute/solver"
)

// Referenz-Workload für Blocker-Performance: Level 349 von game-sokoban.com bis 4-Steiner
// (Referenzwerte: [1] 36/196, [2] 24/8.910, [3] 150/248.306, [4] 346/4.528.779)
const mapLid349 = `
       #####
      ##   ##
    ### $ $ ###
   ##   # #   ##
  ##           ##
  # $#  ... #$  #
  #     .@.     #
  #  $# ...  #$ #
  ##           ##
   ##   # #   ##
    ### $ $ ###
      ##   ##
       #####
`

// Aufruf z.B.: go test -bench BenchmarkBlockerLid349 -benchtime 1x ./blocker/
func benchmarkLid349(b *testing.B, workers int) {
	field, err := soko.Parse(mapLid349)
	if err != nil {
		b.Fatal(err)
	}

	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		blk := New(field, "")
		if workers > 0 {
			blk.SetWorkers(workers)
		}
		for blk.Next(1000000000) {
			if len(blk.GetStats().Stages) >= 4 {
				blk.Abort()
				break
			}
		}

		// Referenzwerte absichern, damit der Benchmark nie ein anderes Ergebnis misst
		stats := blk.GetStats()
		if len(stats.Stages) != 4 || stats.Stages[3].PatternCount != 346 || stats.Stages[3].CheckedStates != 4528779 {
			b.Fatalf("unerwartete Stufenwerte: %+v", stats.Stages)
		}
	}
}

func BenchmarkBlockerLid349Stages4(b *testing.B)        { benchmarkLid349(b, 0) } // Standard (8x NumCPU)
func BenchmarkBlockerLid349Stages4Serial(b *testing.B)  { benchmarkLid349(b, 1) }
func BenchmarkBlockerLid349Stages4Worker4(b *testing.B) { benchmarkLid349(b, 4) }

// --- Serial-Merge-Referenz (CompactTable, Direct-Write abgeschaltet) ---

func benchmarkLid349Serial(b *testing.B, workers int) {
	field, err := soko.Parse(mapLid349)
	if err != nil {
		b.Fatal(err)
	}

	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		blk := New(field, "")
		blk.SetWorkers(workers)
		blk.SetTableFactory(solver.NewCompactTable)
		blk.SetDirectTableFactory(nil) // alten Pfad messen: Vorfiltern + serieller Merge
		for blk.Next(1000000000) {
			if len(blk.GetStats().Stages) >= 4 {
				blk.Abort()
				break
			}
		}

		stats := blk.GetStats()
		if len(stats.Stages) != 4 || stats.Stages[3].PatternCount != 346 || stats.Stages[3].CheckedStates != 4528779 {
			b.Fatalf("unerwartete Stufenwerte: %+v", stats.Stages)
		}
	}
}

func BenchmarkSerialMerge_Compact4(b *testing.B)  { benchmarkLid349Serial(b, 4) }
func BenchmarkSerialMerge_Compact8(b *testing.B)  { benchmarkLid349Serial(b, 8) }
func BenchmarkSerialMerge_Compact14(b *testing.B) { benchmarkLid349Serial(b, 14) }

// --- Direct-Write-Experiment: Worker schreiben atomar selbst, kein serieller Merge ---

func benchmarkLid349Direct(b *testing.B, factory func() DirectTable, workers int) {
	field, err := soko.Parse(mapLid349)
	if err != nil {
		b.Fatal(err)
	}

	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		blk := New(field, "")
		blk.SetWorkers(workers)
		blk.SetDirectTableFactory(factory)
		for blk.Next(1000000000) {
			if len(blk.GetStats().Stages) >= 4 {
				blk.Abort()
				break
			}
		}

		stats := blk.GetStats()
		if len(stats.Stages) != 4 || stats.Stages[3].PatternCount != 346 || stats.Stages[3].CheckedStates != 4528779 {
			b.Fatalf("unerwartete Stufenwerte: %+v", stats.Stages)
		}
	}
}

func BenchmarkDW_Xsync4(b *testing.B)  { benchmarkLid349Direct(b, NewXsyncDirect, 4) }
func BenchmarkDW_Xsync8(b *testing.B)  { benchmarkLid349Direct(b, NewXsyncDirect, 8) }
func BenchmarkDW_Xsync14(b *testing.B) { benchmarkLid349Direct(b, NewXsyncDirect, 14) }
func BenchmarkDW_Shard4(b *testing.B)  { benchmarkLid349Direct(b, NewShardDirect, 4) }
func BenchmarkDW_Shard8(b *testing.B)  { benchmarkLid349Direct(b, NewShardDirect, 8) }
func BenchmarkDW_Shard14(b *testing.B) { benchmarkLid349Direct(b, NewShardDirect, 14) }

func BenchmarkDW_Shard128(b *testing.B) { benchmarkLid349Direct(b, NewShardDirect, 128) }
func BenchmarkDW_Xsync128(b *testing.B) { benchmarkLid349Direct(b, NewXsyncDirect, 128) }
