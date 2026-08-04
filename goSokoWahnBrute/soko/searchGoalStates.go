package soko

// ermittelt alle möglichen Zielstellungen: alle Kisten stehen auf den Zielfeldern und der
// Spieler steht auf einem freien Nachbarfeld, von dem aus ein Rückwärtszug möglich ist
func (f *Field) SearchGoalStates() (result []State) {
	tmp := f.Clone()

	// alle Kisten auf die Zielfelder setzen
	tmp.SetBoxes(tmp.goals)

	buf := tmp.MakeStateBuffer(int(tmp.boxCount) * 4)

	// alle begehbaren Felder als Spielerposition prüfen
	for pos := Wpos(0); pos < tmp.walkEof; pos++ {
		if tmp.wposToBoxes[pos] < tmp.boxCount {
			continue // Feld ist von einer Kiste belegt
		}

		// mindestens ein Nachbarfeld muss eine Kiste tragen, sonst ist kein Rückwärtszug möglich
		if tmp.wposToBoxes[tmp.walkLeft[pos]] == tmp.boxCount &&
			tmp.wposToBoxes[tmp.walkRight[pos]] == tmp.boxCount &&
			tmp.wposToBoxes[tmp.walkUp[pos]] == tmp.boxCount &&
			tmp.wposToBoxes[tmp.walkDown[pos]] == tmp.boxCount {
			continue
		}

		tmp.player = pos
		tmp.moveDepth = 0
		buf = tmp.SearchVariantsBackward(buf[:0])
		if len(buf) > 0 {
			tmpState := State{}
			tmp.GetState(&tmpState)
			result = append(result, tmpState)
		}
	}

	return
}
