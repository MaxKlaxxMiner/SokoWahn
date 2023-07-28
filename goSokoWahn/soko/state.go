package soko

import "goSokoWahn/crc64"

type State struct {
	Player    Wpos        // Spielerposition, Index zu begehbaren Feldern
	Boxes     []Wpos      // Positionen der Kisten, jeweils Index zu den begehbaren Feldern
	MoveDepth int         // Zugtiefe, bei welcher diese Stellung erreicht wurde
	Crc       crc64.Value // Crc64-Schlüssel der gesammten Stellung
}

func (f *Field) Debug(state *State) string {
	return state.Debug(f)
}

func (s *State) Debug(refField *Field) string {
	tmp := refField.Clone()
	tmp.SetState(s)
	if len(s.Boxes) < len(tmp.boxes) {
		tmp.boxes = tmp.boxes[:len(s.Boxes)]
		tmp.boxCount = uint32(len(tmp.boxes))
	}
	return tmp.String()
}

func (f *Field) SetState(state *State) {
	f.player = state.Player
	f.moveDepth = state.MoveDepth
	f.SetBoxes(state.Boxes)
}

func (f *Field) SetBoxes(boxes []Wpos) {
	// alte Kisten entfernen
	for _, wpos := range f.boxes {
		f.wposToBoxes[wpos] = f.boxCount
	}

	// neue Kisten setzen
	for i, box := range boxes {
		f.boxes[i] = box
		f.wposToBoxes[box] = uint32(i)
	}
}
