package blocker

import (
	"goSokoWahnBrute/soko"
	"goSokoWahnBrute/solver"
)

// berechnet die nächsten Blocker-Arbeitsschritte (maximal limit Kombinationen bzw. Stellungen)
// und gibt zurück, ob noch weitere Berechnungen anstehen; die Erstellung endet automatisch,
// sobald die Stufe maxBoxes-1 fertig berechnet wurde
func (b *Blocker) Next(limit int) bool {
	if limit <= 0 {
		return false
	}

	switch b.status {
	case StatusInit:
		if b.searchBoxCount+1 >= b.maxBoxes {
			b.Abort() // Kisten-Limit erreicht -> Erstellung automatisch beenden
			return false
		}
		b.searchBoxCount++
		b.initStage()
		b.initCombos(false)
		b.status = StatusCollectStart
		return true

	case StatusCollectStart:
		for i := 0; i < limit; i++ {
			if !b.nextStartCombo() {
				b.initCombos(true)
				b.status = StatusCollectGoals
				return true
			}
		}
		return true

	case StatusCollectGoals:
		for i := 0; i < limit; i++ {
			if !b.nextGoalCombo() {
				b.status = StatusSearchVariants
				return true
			}
		}
		return true

	case StatusSearchVariants:
		if b.checkList.Count() == 0 {
			// abgearbeitete Liste gegen den Sammler tauschen
			b.checkList.Release()
			b.checkList, b.collectList = b.collectList, solver.NewDepthList(b.recordSize, b.walkCount)
			if b.checkList.Count() == 0 {
				b.status = StatusMergeGoals // keine neuen Stellungen mehr -> alles Erreichbare ist erfasst
				b.mergeRest = int64(b.badList.Count())
				return true
			}
		}

		b.processForwardBatch(b.checkList.PopBatch(limit))
		return true

	case StatusMergeGoals:
		if b.checkList.Count() == 0 {
			// abgearbeitete Liste gegen die frisch markierten guten Stellungen tauschen
			b.checkList.Release()
			b.checkList, b.goodList = b.goodList, solver.NewDepthList(b.recordSize, b.walkCount)
			if b.checkList.Count() == 0 {
				// alle erreichbaren guten Stellungen sind markiert -> Muster einsammeln
				b.stageChecked = b.known.Len()
				b.tempPatterns = make([][]soko.Wpos, b.walkCount)
				b.tempPatternCount = 0
				b.status = StatusCreatePatterns
				return true
			}
		}

		batch := b.checkList.PopBatch(limit)
		b.mergeRest -= int64(len(batch) / b.recordSize)
		b.processBackwardBatch(batch)
		return true

	case StatusCreatePatterns:
		batch := b.badList.PopBatch(limit)
		for off := 0; off < len(batch); off += b.recordSize {
			b.loadRecord(batch[off : off+b.recordSize])
			if b.known.Get(b.curState.Crc) != markerPending {
				continue // Stellung wurde als gut markiert -> kein Blocker
			}
			player := b.curState.Player
			b.tempPatterns[player] = append(b.tempPatterns[player], b.curState.Boxes...)
			b.tempPatternCount++
		}

		if b.badList.Count() == 0 {
			// Stufe abschließen und ablegen
			b.stages = append(b.stages, stage{
				boxCount:      b.searchBoxCount,
				patterns:      b.tempPatterns,
				checkedStates: b.stageChecked,
			})
			b.rebuildCheckIndex()
			b.releaseStageWork()
			if b.cachePath != "" {
				b.saveCache() // Fehler ignorieren: Cache ist nur eine Beschleunigung
			}
			b.status = StatusInit
		}
		return true

	default: // StatusDone
		return false
	}
}

// Summe der geprüften Stellungen über alle fertigen Stufen
func (b *Blocker) totalChecked() int64 {
	var sum int64
	for i := range b.stages {
		sum += b.stages[i].checkedStates
	}
	return sum
}
