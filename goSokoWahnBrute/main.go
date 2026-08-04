package main

import (
	"fmt"
	"os"
	"time"

	"goSokoWahnBrute/maps"
	"goSokoWahnBrute/soko"
	"goSokoWahnBrute/solver"
)

func main() {
	levelData := maps.MapVanilla
	if len(os.Args) >= 2 {
		fileData, err := os.ReadFile(os.Args[1])
		if err != nil {
			panic(err)
		}
		levelData = string(fileData)
	}

	field, err := soko.Parse(levelData)
	if err != nil {
		panic(err)
	}

	fmt.Println(field)

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
