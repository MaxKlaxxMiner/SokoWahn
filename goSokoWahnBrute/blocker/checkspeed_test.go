package blocker

import (
	"os"
	"testing"
	"time"

	"goSokoWahnBrute/soko"
	"goSokoWahnBrute/solver"
)

// Speedcheck fuer den Blocker-Check unter Realbedingungen: Level 46084 mit dem
// 5-Steiner-Blocker (190.708 Muster) aus dem Cache im Hauptordner. Gemessen wird
// die Hauptsuche bis Suchtiefe 266; die Knotenzahl dient als Determinismus-Anker.
// Wird uebersprungen, wenn Level oder Cache fehlen (der Cache liegt nur auf Max' Maschine).
func TestSpeedCheckAllowedLid46084(t *testing.T) {
	if testing.Short() {
		t.Skip("Speedcheck nur im Langlauf (-short gesetzt)")
	}

	levelData, err := os.ReadFile("testdata/lid46084.txt")
	if err != nil {
		t.Skip("Level-Testdatei fehlt:", err)
	}
	field, err := soko.Parse(string(levelData))
	if err != nil {
		t.Fatal(err)
	}

	cache := "../../temp/" + CacheName(field)
	if _, err := os.Stat(cache); err != nil {
		t.Skip("Blocker-Cache im Hauptordner fehlt:", err)
	}

	blk := New(field, cache)
	stats := blk.GetStats()
	patterns := 0
	for _, st := range stats.Stages {
		patterns += st.PatternCount
	}
	if len(stats.Stages) < 5 {
		t.Skipf("erwartet 5 Stufen im Cache, vorhanden: %d", len(stats.Stages))
	}
	blk.Abort()
	field.SetBlocker(blk)

	s := solver.New(field)
	start := time.Now()
	for s.SearchDepth() < 266 && s.Step(1000000000) {
	}
	elapsed := time.Since(start)

	t.Logf("Muster: %d | Tiefe %d nach %s | Knoten: %d", patterns, s.SearchDepth(), elapsed.Round(time.Millisecond), s.NodeCount())
	if s.NodeCount() != 232619 {
		t.Errorf("Determinismus-Anker verletzt: erwartet 232.619 Knoten bei Tiefe 266, erhalten: %d", s.NodeCount())
	}
}
