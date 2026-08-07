package soko

// liefert für jede Kiste alle freien Nachbarfelder als mögliche Spielerposition
// (Kandidaten für Ziel-Stellungen der Blocker-Suche, Duplikate werden nicht gefiltert;
// Pendant zu GetVariantenBlockerZiele im C#-Original)
func (f *Field) SearchBoxNeighborStates(result []State) []State {
	startPos := f.player

	for _, box := range f.boxes {
		if p := f.walkLeft[box]; p < f.walkEof && f.wposToBoxes[p] == f.boxCount {
			f.player = p
			result = f.AppendGetState(result)
		}
		if p := f.walkRight[box]; p < f.walkEof && f.wposToBoxes[p] == f.boxCount {
			f.player = p
			result = f.AppendGetState(result)
		}
		if p := f.walkUp[box]; p < f.walkEof && f.wposToBoxes[p] == f.boxCount {
			f.player = p
			result = f.AppendGetState(result)
		}
		if p := f.walkDown[box]; p < f.walkEof && f.wposToBoxes[p] == f.boxCount {
			f.player = p
			result = f.AppendGetState(result)
		}
	}

	f.player = startPos
	return result
}
