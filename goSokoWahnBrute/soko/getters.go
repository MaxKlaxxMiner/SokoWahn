package soko

// gibt die Anzahl der Kisten auf dem Spielfeld zurück
func (f *Field) BoxCount() int {
	return int(f.boxCount)
}

// gibt die Zielfelder zurück (aufsteigend sortiert, nur lesen!)
func (f *Field) Goals() []Wpos {
	return f.goals
}

// gibt die Anzahl der begehbaren Felder zurück
func (f *Field) WalkCount() int {
	return int(f.walkEof)
}
