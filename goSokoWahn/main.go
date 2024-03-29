package main

import (
	"fmt"
	"goSokoWahn/maps"
	"goSokoWahn/soko"
)

func main() {
	test, err := soko.Parse(maps.MapVanilla)
	if err != nil {
		panic(err)
	}

	goals := test.SearchGoalStates()

	fmt.Println(len(goals))
}
