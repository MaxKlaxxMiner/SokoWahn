package main

import (
	"flag"
	"fmt"
	"os"
	"path/filepath"
	"time"

	"goSokoWahnBrute/blocker"
	"goSokoWahnBrute/maps"
	"goSokoWahnBrute/soko"
	"goSokoWahnBrute/solver"
	"goSokoWahnBrute/tui"
)

func main() {
	cliMode := flag.Bool("cli", false, "Kommandozeilen-Modus ohne TUI (für Skripte und Orakel-Vergleiche)")
	useBlocker := flag.Bool("blocker", false, "CLI: Deadlock-Blocker vorberechnen (alle Stufen bis Kistenanzahl-1)")
	ramLimitGB := flag.Int("ram", 100, "TUI: RAM-Notbremse in GB für den Auto-Modus (0 = aus)")
	flag.Parse()

	// optionales Level aus Datei laden
	levelData := ""
	if flag.NArg() >= 1 {
		fileData, err := os.ReadFile(flag.Arg(0))
		if err != nil {
			panic(err)
		}
		levelData = string(fileData)
	}

	if !*cliMode {
		if err := tui.Run(levelData, *ramLimitGB); err != nil {
			panic(err)
		}
		return
	}

	runCli(levelData, *useBlocker)
}

// Kommandozeilen-Modus: Level lösen und Fortschritt als Text ausgeben
// (deterministische Ausgaben, direkt vergleichbar mit dem C#-Orakel refcli)
func runCli(levelData string, useBlocker bool) {
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
		blockerStart := time.Now()
		for blk.Next(1000000000) {
		}
		fmt.Printf("Blocker fertig nach %s:\n%s\n", time.Since(blockerStart).Round(time.Millisecond), blk)
		field.SetBlocker(blk)
	}

	s := solver.New(field)
	startTime := time.Now()
	lastDepth := -1

	// ganze Tiefenstufen pro Schritt (vergleichbar mit refcli-Standardaufruf)
	for s.Step(1000000000) {
		if depth := s.SearchDepth(); depth != lastDepth {
			lastDepth = depth
			fmt.Printf("Tiefe %4d: Knoten=%d Rest=%d\n", depth, s.NodeCount(), s.OpenCount())
		}
	}

	stats := s.GetStats()
	fmt.Printf("\nFertig nach %s: SuchTiefe=%d Knoten=%d\n", time.Since(startTime).Round(time.Millisecond), s.SearchDepth(), s.NodeCount())

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
