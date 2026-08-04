package blocker

import (
	"sync"
	"sync/atomic"

	"goSokoWahnBrute/crc64"
	"goSokoWahnBrute/soko"
	"goSokoWahnBrute/solver"
)

// Standard-Chunk-Größe der dynamischen Arbeitsverteilung (Sätze pro Zuteilung, siehe SetChunkSize)
const defaultChunkSize = 512

// Arbeitskontext eines Workers: eigenes Feld, eigene Buffer - keine geteilten Schreibzugriffe
type blockerWorker struct {
	field  *soko.Field   // eigener Feld-Clone (inklusive Blocker-Filter)
	cur    soko.State    // Buffer für geladene Stellungen
	varBuf []soko.State  // Buffer für die Variantensuche
	crcs   []crc64.Value // Crcs der vorgefilterten neuen Stellungen
	recs   []uint16      // zugehörige Sätze (Spieler + Kisten), flach
}

// legt die Worker-Kontexte für die aktuelle Stufe an
func (b *Blocker) initWorkers() {
	count := b.workerCount
	b.workers = make([]blockerWorker, count)
	for i := range b.workers {
		b.workers[i] = blockerWorker{
			field:  b.work.Clone(),
			cur:    soko.State{Boxes: make([]soko.Wpos, b.searchBoxCount)},
			varBuf: b.work.MakeStateBuffer(256)[:0],
		}
	}
}

// dekodiert einen Satz in den Worker-Buffer (ohne Crc-Berechnung, die Generatoren brauchen keine)
func (w *blockerWorker) loadRecord(record []uint16, boxCount int) {
	w.cur.Player = soko.Wpos(record[0])
	for i := 0; i < boxCount; i++ {
		w.cur.Boxes[i] = soko.Wpos(record[1+i])
	}
	w.cur.MoveDepth = 0
}

// verarbeitet einen Batch parallel: Varianten generieren + Bekanntes vorfiltern laufen
// in den Workern (nur Lese-Zugriffe auf die Hashtabelle), das Einsortieren übernimmt
// der Aufrufer seriell in fester Worker-Reihenfolge.
// forward true:  SearchVariantsForward, verworfen wird alles bereits Bekannte
// forward false: SearchVariantsBackward, verworfen wird nur bereits als gut Markiertes
func (b *Blocker) runWorkers(batch []uint16, forward bool) {
	records := len(batch) / b.recordSize
	chunk := int64(b.chunkSize)

	var next atomic.Int64
	var wg sync.WaitGroup

	for w := range b.workers {
		wg.Add(1)
		go func(wk *blockerWorker) {
			defer wg.Done()
			wk.crcs = wk.crcs[:0]
			wk.recs = wk.recs[:0]

			for {
				start := int(next.Add(chunk) - chunk)
				if start >= records {
					return
				}
				end := start + int(chunk)
				if end > records {
					end = records
				}

				for r := start; r < end; r++ {
					wk.loadRecord(batch[r*b.recordSize:(r+1)*b.recordSize], b.searchBoxCount)
					wk.field.SetState(&wk.cur)
					if forward {
						wk.varBuf = wk.field.SearchVariantsForward(wk.varBuf[:0])
					} else {
						wk.varBuf = wk.field.SearchVariantsBackward(wk.varBuf[:0])
					}

					for i := range wk.varBuf {
						v := &wk.varBuf[i]
						// Vorfilter: paralleles Lesen ist sicher, geschrieben wird erst nach wg.Wait()
						find := b.known.Get(v.Crc)
						if forward && find != solver.DepthUnknown {
							continue // Stellung bereits bekannt
						}
						if !forward && find == markerGood {
							continue // bereits als gut markiert
						}
						wk.crcs = append(wk.crcs, v.Crc)
						wk.recs = append(wk.recs, uint16(v.Player))
						for _, box := range v.Boxes {
							wk.recs = append(wk.recs, uint16(box))
						}
					}
				}
			}
		}(&b.workers[w])
	}

	wg.Wait()
}

// paralleler Vorwärts-Batch (StatusSearchVariants): neue Stellungen registrieren
func (b *Blocker) processForwardBatch(batch []uint16) {
	if b.workerCount <= 1 || len(batch)/b.recordSize < b.chunkSize {
		b.processForwardSerial(batch)
		return
	}

	b.runWorkers(batch, true)

	for w := range b.workers {
		wk := &b.workers[w]
		for i, crc := range wk.crcs {
			if b.known.Get(crc) != solver.DepthUnknown {
				continue // Duplikat innerhalb des Batches
			}
			b.known.Add(crc, markerPending)
			record := wk.recs[i*b.recordSize : (i+1)*b.recordSize]
			b.collectList.PushRecord(record)
			b.badList.PushRecord(record)
		}
	}
}

// paralleler Rückwärts-Batch (StatusMergeGoals): gute Stellungen markieren,
// unbekannte werden Blocker-Kandidaten (Bx-Semantik)
func (b *Blocker) processBackwardBatch(batch []uint16) {
	if b.workerCount <= 1 || len(batch)/b.recordSize < b.chunkSize {
		b.processBackwardSerial(batch)
		return
	}

	b.runWorkers(batch, false)

	for w := range b.workers {
		wk := &b.workers[w]
		for i, crc := range wk.crcs {
			record := wk.recs[i*b.recordSize : (i+1)*b.recordSize]
			find := b.known.Get(crc)
			if find == markerGood {
				continue // wurde inzwischen im selben Batch als gut markiert
			}
			if find == solver.DepthUnknown {
				b.known.Add(crc, markerPending)
				b.badList.PushRecord(record)
				continue
			}
			b.known.Update(crc, markerGood)
			b.goodList.PushRecord(record)
		}
	}
}
