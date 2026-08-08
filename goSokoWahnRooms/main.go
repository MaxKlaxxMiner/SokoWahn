// goSokoWahnRooms - Rooms-Framework (Nachbau des C#-Raum-/Portal-Konzepts).
// Konzept siehe docs/konzept.md. Aktueller Stand: M1 (Netzwerk-Init).
// Die Web-Debug-GUI folgt mit M2.
//
// Aufruf: goSokoWahnRooms.exe [level.txt | level-nummer | game-sokoban.com-URL]
// (ohne Argument: eingebautes Vanilla-Level; Web-Levels landen im geteilten
// levelcache/-Ordner, derselbe wie bei goSokoWahnBrute)
package main

import (
	"fmt"
	"os"

	"goSokoWahnRooms/maps"
	"goSokoWahnRooms/rooms"
	"goSokoWahnRooms/soko"
	"goSokoWahnRooms/weblevel"
)

func main() {
	sokoMap := maps.MapVanilla
	var webInfo *weblevel.Info
	if len(os.Args) > 1 {
		arg := os.Args[1]
		if weblevel.IsWebInput(arg) {
			level, info, err := weblevel.Load(arg)
			if err != nil {
				fmt.Println("web error:", err)
				os.Exit(1)
			}
			sokoMap, webInfo = level, info
		} else {
			data, err := os.ReadFile(arg)
			if err != nil {
				fmt.Println("read error:", err)
				os.Exit(1)
			}
			sokoMap = string(data)
		}
	}

	field, err := soko.Parse(sokoMap)
	if err != nil {
		fmt.Println("parse error:", err)
		os.Exit(1)
	}

	network, err := rooms.NewNetwork(field)
	if err != nil {
		fmt.Println("network error:", err)
		os.Exit(1)
	}

	if webInfo != nil {
		source := "web"
		if webInfo.Cached {
			source = "levelcache"
		}
		fmt.Printf("level %s: %s - %s (%s), quelle: %s\n", webInfo.ID, webInfo.Catalog, webInfo.Name, webInfo.Number, source)
		if webInfo.BestMoves > 0 {
			fmt.Println("bestmoves:", webInfo.BestMoves)
		}
		fmt.Println()
	}
	fmt.Print(field.String())
	fmt.Println()
	states, variants := uint64(0), uint64(0)
	for _, room := range network.Rooms {
		states += room.States.Count()
		variants += room.Variants.Count()
	}
	fmt.Println("rooms:   ", len(network.Rooms))
	fmt.Println("states:  ", states)
	fmt.Println("variants:", variants)
	fmt.Println("effort:  ", network.EffortString())
}
