package soko

import "fmt"

func (f *Field) SearchGoalStates() (result []State) {
	tmp := f.Clone()

	if len(tmp.boxes) < len(tmp.goals) {
		panic("todo: permutate boxes")
	}

	// alle Kisten auf Zielfelder setzen
	tmp.SetBoxes(tmp.goals)

	buf := tmp.MakeStateBuffer(len(tmp.boxes) * 4)

	for _, box := range tmp.boxes {
		if pos := tmp.walkLeft[box]; pos < tmp.walkEof && tmp.wposToBoxes[pos] == tmp.boxCount {
			tmp.player = pos
			tmp.moveDepth = 60000
			buf = tmp.SearchVariantsBackward(buf[:0])
			if len(buf) > 0 {
				tmpState := State{}
				tmp.GetState(&tmpState)
				result = append(result, tmpState)
				fmt.Println(tmp)
			}
		}
	}

	return
}
