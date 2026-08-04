package blocker

import (
	"testing"

	"goSokoWahnBrute/soko"
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

func BenchmarkBlockerLid349Stages4(b *testing.B)        { benchmarkLid349(b, 0) } // Standard (NumCPU)
func BenchmarkBlockerLid349Stages4Serial(b *testing.B)  { benchmarkLid349(b, 1) }
func BenchmarkBlockerLid349Stages4Worker4(b *testing.B) { benchmarkLid349(b, 4) }
