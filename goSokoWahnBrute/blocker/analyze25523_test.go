package blocker

// Temporäre Analyse für Level 25523: wie verteilen sich die Muster des Caches
// auf Stufen, Spielerpositionen und Anker-Buckets? (Wird nach der Diagnose
// wieder entfernt.)

import (
	"fmt"
	"os"
	"sort"
	"testing"
	"time"

	"goSokoWahnBrute/soko"
	"goSokoWahnBrute/solver"
)

func TestAnalyzeLid25523Cache(t *testing.T) {
	if os.Getenv("ANALYZE25523") == "" {
		t.Skip("Diagnose-Test, nur mit ANALYZE25523=1 (laeuft sonst bei jedem go test ./... mit)")
	}
	levelData, err := os.ReadFile("../../levelcache/25523.txt")
	if err != nil {
		t.Skip("Level fehlt:", err)
	}
	field, err := soko.Parse(string(levelData))
	if err != nil {
		t.Fatal(err)
	}

	cache := "../../temp/" + CacheName(field)
	if _, err := os.Stat(cache); err != nil {
		t.Skip("Cache fehlt:", err)
	}
	t.Log("Cache:", cache)

	blk := New(field, cache)
	blk.Abort()

	fmt.Printf("walkCount=%d maxBoxes=%d\n", blk.walkCount, blk.maxBoxes)
	total := 0
	for i := range blk.stages {
		st := &blk.stages[i]
		count := 0
		for _, pat := range st.patterns {
			count += len(pat) / st.boxCount
		}
		total += count
		fmt.Printf("Stufe k=%d: %d Muster, %d geprüft\n", st.boxCount, count, st.checkedStates)
	}
	fmt.Printf("gesamt: %d Muster\n", total)

	if blk.checkTries == nil {
		t.Fatal("keine checkTries")
	}

	// Trie-Statistik je Spielerposition: Knotenzahl gegen Musterzahl (Präfix-Sharing)
	type posInfo struct {
		player int
		nodes  int
	}
	infos := make([]posInfo, 0, blk.walkCount)
	sumNodes := 0
	for p := 0; p < blk.walkCount; p++ {
		trie := &blk.checkTries[p]
		if len(trie.fields) <= 1 {
			continue
		}
		infos = append(infos, posInfo{p, len(trie.fields) - 1})
		sumNodes += len(trie.fields) - 1
	}
	sort.Slice(infos, func(a, b int) bool { return infos[a].nodes > infos[b].nodes })
	fmt.Printf("Spielerpositionen mit Mustern: %d, Trie-Knoten gesamt: %d\n", len(infos), sumNodes)
	fmt.Println("Top 10 Spielerpositionen (Trie-Knoten):")
	for i := 0; i < 10 && i < len(infos); i++ {
		fmt.Printf("  Pos %4d: %8d Knoten\n", infos[i].player, infos[i].nodes)
	}
}

// Vergleichslauf: wie teuer ist die Suche mit unterschiedlich vielen Blocker-Stufen?
// (Bedingungen wie im TUI: Regeln an, Blocker vorwaerts+rueckwaerts)
func TestCompare25523Stages(t *testing.T) {
	if os.Getenv("ANALYZE25523") == "" {
		t.Skip("Diagnose-Test, nur mit ANALYZE25523=1 (laeuft sonst bei jedem go test ./... mit)")
	}
	levelData, err := os.ReadFile("../../levelcache/25523.txt")
	if err != nil {
		t.Skip("Level fehlt:", err)
	}

	// Zeitlimit je Lauf statt fester Tiefe: kurze, schonende Messung
	// (erreichte Tiefe im Limit = Vergleichsmass), Worker bewusst klein
	limitSecs := 20
	if v := os.Getenv("CMP_SECS"); v != "" {
		fmt.Sscan(v, &limitSecs)
	}

	for _, stageCount := range []int{4, 5, 6} {
		field, err := soko.Parse(string(levelData))
		if err != nil {
			t.Fatal(err)
		}
		rules := soko.NewRules(field)
		field.SetRules(rules)
		field.SetRulesBackward(rules)

		blk := New(field, "../../temp/"+CacheName(field))
		blk.Abort()
		if len(blk.stages) < stageCount {
			t.Fatalf("Cache hat nur %d Stufen", len(blk.stages))
		}
		blk.stages = blk.stages[:stageCount]
		blk.rebuildCheckIndex()
		field.SetBlocker(blk)

		s := solver.New(field)
		s.SetWorkers(4)
		start := time.Now()
		deadline := start.Add(time.Duration(limitSecs) * time.Second)
		targetDepth := 1 << 30
		if v := os.Getenv("CMP_TARGET"); v != "" {
			fmt.Sscan(v, &targetDepth)
		}
		for s.SearchDepth() < targetDepth && time.Now().Before(deadline) && s.Step(200000) {
		}
		elapsed := time.Since(start)
		fmt.Printf("Stufen 1-%d: Tiefe %d nach %8s | Knoten=%d offen=%d\n",
			stageCount, s.SearchDepth(), elapsed.Round(time.Millisecond), s.NodeCount(), s.OpenCount())
		s.Close()
	}
}
