package blocker

import "goSokoWahnBrute/solver"

// initialisiert den Kombinations-Odometer für die aktuelle Stufe
// (goalVariant: false = Kombinationen der Start-Kistenfelder, true = der Zielfelder)
func (b *Blocker) initCombos(goalVariant bool) {
	if goalVariant {
		b.comboPositions = b.base.Goals()
	} else {
		b.comboPositions = b.base.InitBoxes()
	}

	k := b.searchBoxCount
	b.combo = make([]int, k)
	for i := range b.combo {
		b.combo[i] = i
	}
	b.combo[k-1]-- // letzte Stelle eins zurück, damit der erste nextCombo() die erste Kombination liefert
}

// setzt den Odometer auf die nächste k-aus-n-Kombination (aufsteigende Indizes)
func (b *Blocker) nextCombo() bool {
	n := len(b.comboPositions)
	k := len(b.combo)

	// von hinten die erste Stelle suchen, die noch erhöht werden kann
	i := k - 1
	for i >= 0 && b.combo[i] >= n-k+i {
		i--
	}
	if i < 0 {
		return false // alle Kombinationen durchlaufen
	}

	b.combo[i]++
	for j := i + 1; j < k; j++ {
		b.combo[j] = b.combo[j-1] + 1
	}
	return true
}

// überträgt die aktuelle Kombination als Kistenpositionen in den curState-Buffer
// (comboPositions ist aufsteigend sortiert, damit bleibt auch die Kisten-Kanonik erhalten)
func (b *Blocker) setComboBoxes() {
	for i, index := range b.combo {
		b.curState.Boxes[i] = b.comboPositions[index]
	}
}

// verarbeitet die nächste Start-Kombination: alle per Kistenschub erreichbaren Folge-Stellungen
// werden als "Prüfung ausstehend" registriert (Spieler startet auf der Original-Startposition)
func (b *Blocker) nextStartCombo() bool {
	if !b.nextCombo() {
		return false
	}

	b.setComboBoxes()
	b.curState.Player = b.startPlayer
	b.curState.MoveDepth = 0
	b.work.SetState(&b.curState)
	b.varBuf = b.work.SearchVariantsForward(b.varBuf[:0])

	for i := range b.varBuf {
		v := &b.varBuf[i]
		if b.known.Get(v.Crc) != solver.DepthUnknown {
			continue // Stellung bereits bekannt
		}
		b.known.Add(v.Crc, markerPending)
		b.collectList.Push(v)
		b.badList.Push(v)
	}

	return true
}

// verarbeitet die nächste Ziel-Kombination: alle freien Nachbarfelder der Kisten werden
// als "gute" Ziel-Stellungen registriert (und zusätzlich vorwärts weiter durchsucht)
func (b *Blocker) nextGoalCombo() bool {
	if !b.nextCombo() {
		return false
	}

	b.setComboBoxes()
	b.curState.Player = b.startPlayer
	b.curState.MoveDepth = 0
	b.work.SetState(&b.curState)
	b.varBuf = b.work.SearchBoxNeighborStates(b.varBuf[:0])

	for i := range b.varBuf {
		v := &b.varBuf[i]
		find := b.known.Get(v.Crc)
		if find == markerGood {
			continue // bereits als gut bekannt
		}
		if find != solver.DepthUnknown {
			// bisher als "Prüfung ausstehend" bekannt -> zu gut hochstufen
			b.known.Update(v.Crc, markerGood)
			b.goodList.Push(v)
			continue
		}
		b.known.Add(v.Crc, markerGood)
		b.goodList.Push(v)
		b.collectList.Push(v) // auch von hier aus vorwärts weiter suchen
	}

	return true
}
