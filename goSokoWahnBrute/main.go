package main

import (
	"flag"
	"fmt"
	"os"
	"path/filepath"
	"runtime/debug"
	"time"

	"goSokoWahnBrute/blocker"
	"goSokoWahnBrute/maps"
	"goSokoWahnBrute/soko"
	"goSokoWahnBrute/solver"
	"goSokoWahnBrute/tools"
	"goSokoWahnBrute/tui"
)

func main() {
	// GC-Headroom drosseln: der Heap besteht fast nur aus wenigen Riesen-Slices
	// (Hashtabellen, Suchlisten-Puffer) mit kaum Pointern - der Default (100 = Ziel
	// 2x Live-Heap) verdoppelt sonst nur nutzlos den RAM-Verbrauch, 5% Reserve
	// reichen für das Kleinzeug locker
	debug.SetGCPercent(5)

	cliMode := flag.Bool("cli", false, "Kommandozeilen-Modus ohne TUI (für Skripte und Orakel-Vergleiche)")
	useBlocker := flag.Bool("blocker", false, "CLI: Deadlock-Blocker vorberechnen (alle Stufen bis Kistenanzahl-1)")
	blockerStages := flag.Int("stages", 0, "CLI: nur die Blocker-Stufen bis N berechnen und ausgeben (ohne Suche, ohne Cache)")
	ramLimitGB := flag.Int("ram", 100, "TUI: RAM-Notbremse in GB für den Auto-Modus (0 = aus)")
	workers := flag.Int("workers", 0, "Anzahl der Worker für Blocker und Suche (0 = automatisch, 1 = seriell)")
	flag.Parse()

	// Auslagerung großer Suchlisten auf die Festplatte aktivieren und dabei
	// liegengebliebene Dateien abgestürzter Läufe aufräumen (älter als eine Woche;
	// parallele Prozesse stören sich dank der Zufallsnamen nicht).
	// Existiert C:\temp\sokowahn (von Max angelegt, z.B. auf einer anderen Platte),
	// hat dieser Ordner Vorrang - sonst wie gehabt temp/ im Arbeitsverzeichnis.
	spillDir := `C:\temp\sokowahn`
	if info, err := os.Stat(spillDir); err != nil || !info.IsDir() {
		spillDir = "temp"
		if err := os.MkdirAll(spillDir, 0755); err != nil {
			spillDir = "" // kein Auslagerungs-Ordner verfügbar -> alles bleibt im RAM
		}
	}
	if spillDir != "" {
		solver.CleanupSpillFiles(spillDir, 7*24*time.Hour)
		solver.SpillDir = spillDir
	}

	// optionales Level aus Datei laden
	levelData := ""
	if flag.NArg() >= 1 {
		fileData, err := os.ReadFile(flag.Arg(0))
		if err != nil {
			panic(err)
		}
		levelData = string(fileData)
	}

	if *blockerStages > 0 {
		runBlockerOnly(levelData, *blockerStages, *workers)
		return
	}

	if !*cliMode {
		if err := tui.Run(levelData, *ramLimitGB); err != nil {
			panic(err)
		}
		return
	}

	runCli(levelData, *useBlocker, *workers)
}

// berechnet nur die Blocker-Stufen bis einschließlich maxStages und gibt sie aus
// (ohne Suche und ohne Cache-Datei, für schnelle Orakel-Vergleiche)
func runBlockerOnly(levelData string, maxStages int, workers int) {
	if levelData == "" {
		levelData = maps.MapVanilla
	}

	field, err := soko.Parse(levelData)
	if err != nil {
		panic(err)
	}

	blk := blocker.New(field, "")
	if workers > 0 {
		blk.SetWorkers(workers)
	}
	for blk.Next(1000000000) {
		if len(blk.GetStats().Stages) >= maxStages {
			blk.Abort()
			break
		}
	}
	fmt.Print(blk)
}

// Kommandozeilen-Modus: Level lösen und Fortschritt als Text ausgeben
// (deterministische Ausgaben, direkt vergleichbar mit dem C#-Orakel refcli)
func runCli(levelData string, useBlocker bool, workers int) {
	if levelData == "" {
		levelData = maps.MapVanilla
	}

	field, err := soko.Parse(levelData)
	if err != nil {
		panic(err)
	}

	fmt.Println(field)

	if useBlocker {
		if err := os.MkdirAll("temp", 0755); err != nil {
			panic(err)
		}
		blk := blocker.New(field, filepath.Join("temp", blocker.CacheName(field)))
		if workers > 0 {
			blk.SetWorkers(workers)
		}
		blockerStart := time.Now()
		for blk.Next(1000000000) {
		}
		fmt.Printf("Blocker fertig nach %s:\n%s\n", time.Since(blockerStart).Round(time.Millisecond), blk)
		field.SetBlocker(blk)
	}

	s := solver.New(field)
	defer s.Close() // Auslagerungsdateien der Suchlisten löschen
	if workers > 0 {
		s.SetWorkers(workers)
	}
	startTime := time.Now()
	lastDepth := -1

	// ganze Tiefenstufen pro Schritt (vergleichbar mit refcli-Standardaufruf)
	for s.Step(1000000000) {
		if depth := s.SearchDepth(); depth != lastDepth {
			lastDepth = depth
			fmt.Printf("Tiefe %4d: Knoten=%s Rest=%s\n", depth, tools.FormatInt(s.NodeCount()), tools.FormatInt(s.OpenCount()))
		}
	}

	stats := s.GetStats()
	fmt.Printf("\nFertig nach %s: SuchTiefe=%d Knoten=%s\n", time.Since(startTime).Round(time.Millisecond), s.SearchDepth(), tools.FormatInt(s.NodeCount()))

	if stats.FoundMoves < 0 {
		fmt.Println("keine Lösung gefunden")
		return
	}

	solution, err := s.GetSolution()
	if err != nil {
		panic(err)
	}
	fmt.Printf("Lösung: %d Züge, %d Schub-Stellungen\n", len(solution.Moves), len(solution.States))
	fmt.Println(solution.Moves)
}
