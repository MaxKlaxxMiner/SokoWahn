// goSokoWahnRooms - Rooms-Framework (Nachbau des C#-Raum-/Portal-Konzepts).
// Konzept siehe docs/konzept.md. Aktueller Stand: M1 (Netzwerk-Init).
// Die Web-Debug-GUI folgt mit M2.
package main

import (
	"fmt"
	"os"

	"goSokoWahnRooms/maps"
	"goSokoWahnRooms/rooms"
	"goSokoWahnRooms/soko"
)

func main() {
	sokoMap := maps.MapVanilla
	if len(os.Args) > 1 {
		data, err := os.ReadFile(os.Args[1])
		if err != nil {
			fmt.Println("read error:", err)
			os.Exit(1)
		}
		sokoMap = string(data)
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
