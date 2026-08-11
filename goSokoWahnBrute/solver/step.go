package solver

import "goSokoWahnBrute/soko"

// berechnet den nächsten Arbeitsschritt (maximal limit Stellungen) und gibt zurück,
// ob noch weitere Berechnungen anstehen (false = Suche abgeschlossen)
func (s *Solver) Step(limit int) bool {
	if s.done {
		return false
	}

	// den berechneten RAM-Verbrauch für die Auslagerungs-Entscheidung aktuell halten
	SetSpillRamUsage(s.RamBytes())

	// Sonderfall ohne Zielstellungen: reine Vorwärtssuche mit direkter Gelöst-Prüfung
	if s.forwardOnly {
		if (s.foundTotal >= 0 && s.forwardDepth >= s.foundTotal) || s.forwardDepth == len(s.forwardLists) {
			s.done = true
			return false
		}
		return s.searchForward(limit)
	}

	// wurde bereits eine Lösung gefunden, gezielt die Seite verfeinern, die noch etwas verbessern kann
	if s.foundTotal >= 0 {
		pos := s.foundForwardDepth
		if pos > s.forwardDepth && pos-s.forwardDepth < s.foundTotal-s.forwardDepth-s.backwardDepth {
			if s.forwardLists[s.forwardDepth].Count() <= s.backwardLists[s.backwardDepth].Count() {
				return s.searchForward(limit)
			}
			return s.searchBackward(limit)
		}
		if pos > s.forwardDepth {
			return s.searchForward(limit)
		}
		if s.forwardDepth+s.backwardDepth < s.foundTotal {
			return s.searchBackward(limit)
		}
	}

	// Abbruchprüfung: Lösung steht fest oder eine Suchfront ist erschöpft
	if (s.foundTotal >= 0 && s.forwardDepth+s.backwardDepth >= s.foundTotal) ||
		s.forwardDepth == len(s.forwardLists) || s.backwardDepth == len(s.backwardLists) {
		s.done = true
		return false
	}

	// Richtungswahl: pro Suchtiefe einmal entscheiden, welche Seite die kleinere Tabelle hat
	if s.forwardDepth+s.backwardDepth != s.dirDepth {
		s.dirDepth = s.forwardDepth + s.backwardDepth
		s.dirForward = s.forwardKnown.Len() < s.backwardKnown.Len()
		s.hashUsage = append(s.hashUsage, s.forwardKnown.Len()+s.backwardKnown.Len())
	}

	// manuelle Richtungsvorgabe (Tasten 1/2/3 im TUI) übersteuert die automatische Wahl;
	// die Entscheidung oben läuft trotzdem mit (hashUsage-Statistik bleibt vollständig
	// und beim Zurückschalten auf DirAuto greift sofort wieder die gecachte Wahl)
	switch s.dirMode {
	case DirForward:
		return s.searchForward(limit)
	case DirBackward:
		// die Vorwärts-Tiefe 0 (nur die Startstellung) muss abgearbeitet sein, bevor rein
		// rückwärts gesucht werden darf: alle gespeicherten Stellungen sind Schub-Stellungen,
		// die rohe Startstellung kann von Rückwärts-Varianten also nie getroffen werden -
		// erst ihre Tiefe-1-Nachfolger machen Verbindung und Unlösbarkeits-Beweis möglich
		if s.forwardDepth == 0 {
			return s.searchForward(limit)
		}
		return s.searchBackward(limit)
	}

	if s.dirForward {
		return s.searchForward(limit)
	}
	return s.searchBackward(limit)
}

// normale Suche nach vorne (von der Startstellung aus beginnend)
func (s *Solver) searchForward(limit int) bool {
	if s.forwardDepth == len(s.forwardLists) {
		s.done = true
		return false
	}

	list := s.forwardLists[s.forwardDepth]

	// ausgelagerte Listen liefern pro PopBatch höchstens einen Lesepuffer-Block
	// (und Datei- vor RAM-Teil), deshalb bis zum Limit weiterlesen; die Batch-Grenzen
	// ändern das Suchverhalten nicht (abgesichert durch die Spill-Determinismus-Tests)
	for remaining := limit; ; {
		batch := list.PopBatch(remaining)
		records := len(batch) / s.recordSize
		s.processed += int64(records)

		// große Batches parallel verarbeiten (bitgenau identisch, siehe parallel.go);
		// der forwardOnly-Sonderfall (Mini-Levels) bleibt komplett seriell
		if !s.forwardOnly && s.useParallel(len(batch)) {
			s.runSearchWorkers(batch, s.forwardDepth, true)
			s.mergeForward()
		} else {
			s.searchForwardSerial(batch)
		}

		remaining -= records
		if records == 0 || remaining <= 0 {
			break
		}
	}

	if list.Count() == 0 {
		list.Release()
		s.forwardDepth++
	}

	return true
}

// serieller Kern der Vorwärtssuche (Referenz-Verhalten, von den Workern gespiegelt)
func (s *Solver) searchForwardSerial(batch []uint16) {
	for off := 0; off < len(batch); off += s.recordSize {
		s.loadRecord(batch[off:off+s.recordSize], int32(s.forwardDepth))
		if s.forwardKnown.Get(s.curState.Crc) < uint16(s.forwardDepth) {
			continue // Stellung wurde inzwischen mit besserer Tiefe gefunden -> Satz ist veraltet
		}
		s.work.SetState(&s.curState)
		s.varBuf = s.work.SearchVariantsForward(s.varBuf[:0])

		for i := range s.varBuf {
			v := &s.varBuf[i]
			depth := uint16(v.MoveDepth)
			findOwn := s.forwardKnown.Get(v.Crc)

			if findOwn == DepthUnknown { // neue Stellung gefunden
				if s.foundTotal < 0 || int(depth)+s.backwardDepth+1 < s.foundTotal {
					s.forwardKnown.Add(v.Crc, depth)
					s.pushForward(v) // zum weiteren Durchsuchen vormerken
				}
			} else if depth < findOwn { // kürzere Variante zu einer bekannten Stellung
				s.forwardKnown.Update(v.Crc, depth)
				if s.foundTotal >= 0 && v.Crc == s.foundState.Crc {
					s.foundTotal -= int(findOwn - depth)
					s.foundForwardDepth = int(depth)
				}
				if s.foundTotal < 0 || int(depth)+s.backwardDepth+1 < s.foundTotal {
					s.pushForward(v)
				}
			} else {
				continue
			}

			// Sonderfall reine Vorwärtssuche: direkt prüfen, ob die Stellung gelöst ist
			if s.forwardOnly {
				if s.isGoalBoxes(v) && (s.foundTotal < 0 || int(depth) < s.foundTotal) {
					s.foundTotal = int(depth)
					s.copyFoundState(v)
					s.foundForwardDepth = int(depth)
				}
				continue
			}

			// Verbindung zur Rückwärtssuche prüfen
			findOpp := s.backwardKnown.Get(v.Crc)
			if findOpp == DepthUnknown {
				continue
			}
			total := int(depth) + int(findOpp)
			if s.foundTotal < 0 || total < s.foundTotal { // bessere Lösung gefunden?
				s.foundTotal = total
				s.copyFoundState(v)
				s.foundForwardDepth = int(depth)
			}
		}
	}
}

// Rückwärtssuche beginnend bei den Zielstellungen
func (s *Solver) searchBackward(limit int) bool {
	if s.backwardDepth == len(s.backwardLists) {
		s.done = true
		return false
	}

	list := s.backwardLists[s.backwardDepth]

	// wie searchForward: bis zum Limit weiterlesen (PopBatch liefert bei ausgelagerten
	// Listen höchstens einen Lesepuffer-Block pro Aufruf)
	for remaining := limit; ; {
		batch := list.PopBatch(remaining)
		records := len(batch) / s.recordSize
		s.processed += int64(records)

		// große Batches parallel verarbeiten (bitgenau identisch, siehe parallel.go)
		if s.useParallel(len(batch)) {
			s.runSearchWorkers(batch, s.backwardDepth, false)
			s.mergeBackward()
		} else {
			s.searchBackwardSerial(batch)
		}

		remaining -= records
		if records == 0 || remaining <= 0 {
			break
		}
	}

	if list.Count() == 0 {
		list.Release()
		s.backwardDepth++
	}

	return true
}

// serieller Kern der Rückwärtssuche (Referenz-Verhalten, von den Workern gespiegelt)
func (s *Solver) searchBackwardSerial(batch []uint16) {
	for off := 0; off < len(batch); off += s.recordSize {
		s.loadRecord(batch[off:off+s.recordSize], int32(s.backwardDepth))
		s.work.SetState(&s.curState)
		s.varBuf = s.work.SearchVariantsBackward(s.varBuf[:0])

		for i := range s.varBuf {
			v := &s.varBuf[i]
			depth := uint16(v.MoveDepth)
			findOwn := s.backwardKnown.Get(v.Crc)

			if findOwn == DepthUnknown { // neue Stellung gefunden
				if s.foundTotal < 0 || int(depth)+s.forwardDepth+1 < s.foundTotal {
					s.backwardKnown.Add(v.Crc, depth)
					s.pushBackward(v) // zum weiteren Durchsuchen vormerken
				}
			} else if depth < findOwn { // kürzere Variante zu einer bekannten Stellung
				s.backwardKnown.Update(v.Crc, depth)
				if s.foundTotal >= 0 && v.Crc == s.foundState.Crc {
					s.foundTotal -= int(findOwn - depth) // der Rückwärtsanteil der Lösung wurde kürzer
				}
				if s.foundTotal < 0 || int(depth)+s.forwardDepth+1 < s.foundTotal {
					s.pushBackward(v)
				}
			} else {
				continue
			}

			// Verbindung zur Vorwärtssuche prüfen
			findOpp := s.forwardKnown.Get(v.Crc)
			if findOpp == DepthUnknown {
				continue
			}
			total := int(depth) + int(findOpp)
			if s.foundTotal < 0 || total < s.foundTotal { // bessere Lösung gefunden?
				s.foundTotal = total
				s.copyFoundState(v)
				s.foundForwardDepth = int(findOpp)
			}
		}
	}
}

// prüft, ob alle Kisten der Stellung auf den Zielfeldern stehen
// (beide Listen sind aufsteigend sortiert und gleich lang)
func (s *Solver) isGoalBoxes(v *soko.State) bool {
	for i, box := range v.Boxes {
		if box != s.goals[i] {
			return false
		}
	}
	return true
}
