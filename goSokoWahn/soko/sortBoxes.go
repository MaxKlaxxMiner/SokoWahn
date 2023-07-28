package soko

func (f *Field) sortBoxesFull() {
	for box := f.boxCount - 1; box > 0; box-- {
		f.sortBoxesUp(box)
	}
}

func (f *Field) sortBoxesUp(box uint32) {
	for box > 0 {
		pos := f.boxes[box]
		posUp := f.boxes[box-1]
		if posUp < pos { // correct order?
			return
		}
		f.wposToBoxes[pos], f.wposToBoxes[posUp] = f.wposToBoxes[posUp], f.wposToBoxes[pos]
		f.boxes[box], f.boxes[box-1] = posUp, pos
		box--
	}
}

func (f *Field) sortBoxesDown(box uint32) {
	for box < f.boxCount-1 {
		pos := f.boxes[box]
		posDown := f.boxes[box+1]
		if posDown > pos { // correct order?
			return
		}
		f.wposToBoxes[pos], f.wposToBoxes[posDown] = f.wposToBoxes[posDown], f.wposToBoxes[pos]
		f.boxes[box], f.boxes[box+1] = posDown, pos
		box++
	}
}
