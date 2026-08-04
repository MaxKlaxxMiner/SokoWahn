package blocker

import "goSokoWahnBrute/solver"

// serieller Vorwärts-Batch (StatusSearchVariants): neue Stellungen registrieren
// (Fallback für workerCount == 1 und sehr kleine Batches, exakt der ursprüngliche Ablauf)
func (b *Blocker) processForwardSerial(batch []uint16) {
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
}

// serieller Rückwärts-Batch (StatusMergeGoals): gute Stellungen markieren,
// unbekannte werden Blocker-Kandidaten (Bx-Semantik)
func (b *Blocker) processBackwardSerial(batch []uint16) {
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
}
