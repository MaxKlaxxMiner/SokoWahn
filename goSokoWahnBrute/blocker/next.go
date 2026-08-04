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
			b.checkList, b.collectList = b.collectList, solver.NewDepthList(b.recordSize)
			if b.checkList.Count() == 0 {
				b.status = StatusMergeGoals // keine neuen Stellungen mehr -> alles Erreichbare ist erfasst
				return true
			}
		}

		batch := b.checkList.PopBatch(limit)
		for off := 0; off < len(batch); off += b.recordSize {
			b.loadRecord(batch[off : off+b.recordSize])
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
		}
		return true

	case StatusMergeGoals:
		if b.checkList.Count() == 0 {
			// abgearbeitete Liste gegen die frisch markierten guten Stellungen tauschen
			b.checkList.Release()
			b.checkList, b.goodList = b.goodList, solver.NewDepthList(b.recordSize)
			if b.checkList.Count() == 0 {
				// alle erreichbaren guten Stellungen sind markiert -> Muster einsammeln
				b.stageChecked = b.known.Len()
				b.tempPatterns = make([][]soko.Wpos, b.walkCount)
				b.status = StatusCreatePatterns
				return true
			}
		}

		batch := b.checkList.PopBatch(limit)
		for off := 0; off < len(batch); off += b.recordSize {
			b.loadRecord(batch[off : off+b.recordSize])
			b.work.SetState(&b.curState)
			b.varBuf = b.work.SearchVariantsBackward(b.varBuf[:0])

			for i := range b.varBuf {
				v := &b.varBuf[i]
				find := b.known.Get(v.Crc)
				if find == markerGood {
					continue // bereits als gut markiert
				}
				if find == solver.DepthUnknown {
					// rückwärts erreichbar, aber vorwärts nie gesehen -> kann im echten Spiel
					// nicht vorkommen und wird als Blocker-Kandidat registriert (Bx-Semantik)
					b.known.Add(v.Crc, markerPending)
					b.badList.Push(v)
					continue
				}
				b.known.Update(v.Crc, markerGood)
				b.goodList.Push(v)
			}
		}
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
		}

		if b.badList.Count() == 0 {
			// Stufe abschließen und ablegen
			b.stages = append(b.stages, stage{
				boxCount:      b.searchBoxCount,
				patterns:      b.tempPatterns,
				checkedStates: b.stageChecked,
			})
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
