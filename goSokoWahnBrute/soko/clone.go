package soko

func (f *Field) Clone() *Field {
	result := Field{
		fieldData:     f.fieldData,
		width:         f.width,
		height:        f.height,
		walkEof:       f.walkEof,
		walkLeft:      f.walkLeft,
		walkRight:     f.walkRight,
		walkUp:        f.walkUp,
		walkDown:      f.walkDown,
		fieldToWpos:   f.fieldToWpos,
		wposToField:   f.wposToField,
		boxCount:      f.boxCount,
		initPlayer:    f.initPlayer,
		initBoxes:     f.initBoxes,
		goals:         f.goals,
		player:        f.player,
		wposToBoxes:   make([]uint32, len(f.wposToBoxes)),
		boxes:         make([]Wpos, len(f.boxes)),
		boxBits:       make([]uint64, len(f.boxBits)),
		moveDepth:     f.moveDepth,
		tmpCheckPos:   make([]Wpos, len(f.tmpCheckPos)),
		tmpCheckDepth: make([]int32, len(f.tmpCheckDepth)),
		tmpCheckDone:  make([]bool, len(f.tmpCheckDone)),
		blocker:         f.blocker,
		blockerBackward: f.blockerBackward,
	}
	if f.rules != nil {
		result.rules = f.rules.Clone() // eigener Scratch-Puffer je Field-Instanz (Worker-Klone)
	}
	copy(result.wposToBoxes, f.wposToBoxes)
	copy(result.boxes, f.boxes)
	copy(result.boxBits, f.boxBits)
	result.tmpCheckDone[len(result.tmpCheckDone)-1] = true
	return &result
}
