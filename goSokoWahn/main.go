package main

import (
	"fmt"
	"goSokoWahn/maps"
	"goSokoWahn/sokofield"
)

func main() {
	test, err := sokofield.Parse(maps.MapVanilla)
	if err != nil {
		panic(err)
	}
	fmt.Println(test)
}
