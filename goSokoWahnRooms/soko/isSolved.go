package soko

// gibt an, ob alle Kisten auf den Zielfeldern stehen
func (f *Field) IsSolved() bool {
	for _, goal := range f.goals {
		if f.wposToBoxes[goal] == f.boxCount {
			return false
		}
	}
	return true
}
