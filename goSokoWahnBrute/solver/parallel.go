package solver

import (
	"sync"

	"goSokoWahnBrute/crc64"
	"goSokoWahnBrute/soko"
)

// ab dieser Batch-Größe (in Sätzen) wird auf die Worker aufgeteilt; darunter ist der
// serielle Weg schneller (Startwert, per Benchmark-Sweep zu verfeinern;
// Variable statt Konstante, damit Tests den Parallel-Pfad gezielt erzwingen können)
var parallelMinRecords = 128

// Arbeitskontext eines Such-Workers: eigenes Feld, eigene Buffer - keine geteilten
// Schreibzugriffe (die Hashtabellen werden während der Generierung nur gelesen,
// geschrieben wird erst im seriellen Merge nach wg.Wait, wie im Blocker)
type searchWorker struct {
	field  *soko.Field   // eigener Clone des Arbeitsfeldes (inklusive Blocker-Filter)
	cur    soko.State    // Buffer für geladene Stellungen
	varBuf []soko.State  // Buffer für die Variantensuche
	crcs   []crc64.Value // Crc je überlebender Variante
	depths []uint16      // Zugtiefe je überlebender Variante
	recs   []uint16      // Sätze der Varianten (Spieler + Kisten), flach
}

// aktuelle Anzahl der Such-Worker
func (s *Solver) Workers() int {
	return s.workerCount
}

// setzt die Anzahl der Such-Worker (1 = komplett seriell, z.B. für Debugging
// und Orakel-Vergleiche); wirkt ab dem nächsten Batch und ist damit auch
// zur Laufzeit gefahrlos umschaltbar
func (s *Solver) SetWorkers(count int) {
	if count < 1 {
		count = 1
	}
	s.workerCount = count
	s.workers = nil // beim nächsten parallelen Batch neu anlegen
}

// legt die Worker-Kontexte an (lazy beim ersten parallelen Batch)
func (s *Solver) initWorkers() {
	s.workers = make([]searchWorker, s.workerCount)
	for i := range s.workers {
		s.workers[i] = searchWorker{
			field:  s.work.Clone(),
			cur:    soko.State{Boxes: make([]soko.Wpos, s.boxCount)},
			varBuf: s.work.MakeStateBuffer(256)[:0],
		}
	}
}

// entscheidet, ob ein Batch auf die Worker aufgeteilt wird
func (s *Solver) useParallel(batchValues int) bool {
	if s.workerCount <= 1 || batchValues/s.recordSize < parallelMinRecords {
		return false
	}
	if s.workers == nil {
		s.initWorkers()
	}
	return true
}

// verarbeitet einen Batch parallel: jeder Worker generiert die Varianten eines festen,
// zusammenhängenden Satz-Bereichs und filtert Bekanntes vor. Statische Bereiche statt
// dynamischer Chunks wie im Blocker, denn nur so bleibt die Suche bitgenau: der Merge
// läuft in Worker-Reihenfolge und damit in exakt der FIFO-Reihenfolge des seriellen Codes
// (beim Solver hängen foundTotal-Pruning und Tiefen-Updates von der Reihenfolge ab,
// die monotonen Blocker-Marker waren dagegen reihenfolge-unabhängig).
//
// Der Vorfilter liest die während der Generierung eingefrorene Hashtabelle - trotzdem
// exakt äquivalent zum seriellen Live-Zugriff: innerhalb eines Tiefen-Batches schreiben
// Add/Update nur Tiefen > Listentiefe (Varianten sind immer tiefer als ihre Eltern),
// die Tabellen-Tiefen können generell nur kleiner werden, und was der Vorfilter
// durchlässt, prüft der Merge erneut mit der Live-Tabelle und der Originallogik.
func (s *Solver) runSearchWorkers(batch []uint16, listDepth int, forward bool) {
	records := len(batch) / s.recordSize
	count := len(s.workers)
	table := s.forwardKnown
	if !forward {
		table = s.backwardKnown
	}

	var wg sync.WaitGroup
	for w := 0; w < count; w++ {
		wk := &s.workers[w]
		wk.crcs, wk.depths, wk.recs = wk.crcs[:0], wk.depths[:0], wk.recs[:0]

		from, to := records*w/count, records*(w+1)/count
		if from == to {
			continue // leerer Bereich (mehr Worker als Sätze)
		}

		wg.Add(1)
		go func(wk *searchWorker, part []uint16) {
			defer wg.Done()

			for off := 0; off < len(part); off += s.recordSize {
				wk.cur.Player = soko.Wpos(part[off])
				for i := 0; i < s.boxCount; i++ {
					wk.cur.Boxes[i] = soko.Wpos(part[off+1+i])
				}
				wk.cur.MoveDepth = int32(listDepth)

				if forward {
					wk.cur.UpdateCrc()
					if table.Get(wk.cur.Crc) < uint16(listDepth) {
						continue // Stellung wurde inzwischen mit besserer Tiefe gefunden -> Satz ist veraltet
					}
				}

				wk.field.SetState(&wk.cur)
				if forward {
					wk.varBuf = wk.field.SearchVariantsForward(wk.varBuf[:0])
				} else {
					wk.varBuf = wk.field.SearchVariantsBackward(wk.varBuf[:0])
				}

				for i := range wk.varBuf {
					v := &wk.varBuf[i]
					depth := uint16(v.MoveDepth)
					if findOwn := table.Get(v.Crc); findOwn != DepthUnknown && depth >= findOwn {
						continue // bereits mit gleicher oder besserer Tiefe bekannt
					}
					wk.crcs = append(wk.crcs, v.Crc)
					wk.depths = append(wk.depths, depth)
					wk.recs = append(wk.recs, uint16(v.Player))
					for _, box := range v.Boxes {
						wk.recs = append(wk.recs, uint16(box))
					}
				}
			}
		}(wk, batch[from*s.recordSize:to*s.recordSize])
	}

	wg.Wait()
}

// serieller Merge der Vorwärts-Worker (identische Logik wie searchForwardSerial,
// nur ohne den Sonderfall forwardOnly - der läuft immer seriell)
func (s *Solver) mergeForward() {
	for w := range s.workers {
		wk := &s.workers[w]
		for i, crc := range wk.crcs {
			depth := wk.depths[i]
			record := wk.recs[i*s.recordSize : (i+1)*s.recordSize]
			findOwn := s.forwardKnown.Get(crc)

			if findOwn == DepthUnknown { // neue Stellung gefunden
				if s.foundTotal < 0 || int(depth)+s.backwardDepth+1 < s.foundTotal {
					s.forwardKnown.Add(crc, depth)
					s.pushForwardRecord(int(depth), record)
				}
			} else if depth < findOwn { // kürzere Variante zu einer bekannten Stellung
				s.forwardKnown.Update(crc, depth)
				if s.foundTotal >= 0 && crc == s.foundState.Crc {
					s.foundTotal -= int(findOwn - depth)
					s.foundForwardDepth = int(depth)
				}
				if s.foundTotal < 0 || int(depth)+s.backwardDepth+1 < s.foundTotal {
					s.pushForwardRecord(int(depth), record)
				}
			} else {
				continue
			}

			// Verbindung zur Rückwärtssuche prüfen
			findOpp := s.backwardKnown.Get(crc)
			if findOpp == DepthUnknown {
				continue
			}
			total := int(depth) + int(findOpp)
			if s.foundTotal < 0 || total < s.foundTotal { // bessere Lösung gefunden?
				s.foundTotal = total
				s.copyFoundRecord(record, int32(depth), crc)
				s.foundForwardDepth = int(depth)
			}
		}
	}
}

// serieller Merge der Rückwärts-Worker (identische Logik wie searchBackwardSerial)
func (s *Solver) mergeBackward() {
	for w := range s.workers {
		wk := &s.workers[w]
		for i, crc := range wk.crcs {
			depth := wk.depths[i]
			record := wk.recs[i*s.recordSize : (i+1)*s.recordSize]
			findOwn := s.backwardKnown.Get(crc)

			if findOwn == DepthUnknown { // neue Stellung gefunden
				if s.foundTotal < 0 || int(depth)+s.forwardDepth+1 < s.foundTotal {
					s.backwardKnown.Add(crc, depth)
					s.pushBackwardRecord(int(depth), record)
				}
			} else if depth < findOwn { // kürzere Variante zu einer bekannten Stellung
				s.backwardKnown.Update(crc, depth)
				if s.foundTotal >= 0 && crc == s.foundState.Crc {
					s.foundTotal -= int(findOwn - depth) // der Rückwärtsanteil der Lösung wurde kürzer
				}
				if s.foundTotal < 0 || int(depth)+s.forwardDepth+1 < s.foundTotal {
					s.pushBackwardRecord(int(depth), record)
				}
			} else {
				continue
			}

			// Verbindung zur Vorwärtssuche prüfen
			findOpp := s.forwardKnown.Get(crc)
			if findOpp == DepthUnknown {
				continue
			}
			total := int(depth) + int(findOpp)
			if s.foundTotal < 0 || total < s.foundTotal { // bessere Lösung gefunden?
				s.foundTotal = total
				s.copyFoundRecord(record, int32(depth), crc)
				s.foundForwardDepth = int(findOpp)
			}
		}
	}
}

// trägt einen kodierten Satz in die Vorwärts-Suchliste seiner Zugtiefe ein
func (s *Solver) pushForwardRecord(depth int, record []uint16) {
	for depth >= len(s.forwardLists) {
		s.forwardLists = append(s.forwardLists, NewDepthList(s.recordSize, s.base.WalkCount()))
	}
	s.forwardLists[depth].PushRecord(record)
}

// trägt einen kodierten Satz in die Rückwärts-Suchliste seiner Zugtiefe ein
func (s *Solver) pushBackwardRecord(depth int, record []uint16) {
	for depth >= len(s.backwardLists) {
		s.backwardLists = append(s.backwardLists, NewDepthList(s.recordSize, s.base.WalkCount()))
	}
	s.backwardLists[depth].PushRecord(record)
}

// merkt sich einen kodierten Satz als beste Verbindungs-Stellung
func (s *Solver) copyFoundRecord(record []uint16, depth int32, crc crc64.Value) {
	s.foundState.Player = soko.Wpos(record[0])
	for i := 0; i < s.boxCount; i++ {
		s.foundState.Boxes[i] = soko.Wpos(record[1+i])
	}
	s.foundState.MoveDepth = depth
	s.foundState.Crc = crc
}
