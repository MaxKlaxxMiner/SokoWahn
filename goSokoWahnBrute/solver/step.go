package solver

import "goSokoWahnBrute/soko"

// berechnet den nächsten Arbeitsschritt (maximal limit Stellungen) und gibt zurück,
// ob noch weitere Berechnungen anstehen (false = Suche abgeschlossen)
func (s *Solver) Step(limit int) bool {
	if s.done {
		return false
	}

	// den berechneten RAM-Verbrauch für die Auslagerungs-Entscheidung aktuell halten;
	// steht bei einer Stellungs-Tabelle eine Verdopplung an, die die RAM-Notbremse
	// rechnerisch reißen würde, wandert sie stattdessen jetzt ins Archiv-Format
	ram := s.RamBytes()
	SetSpillRamUsage(ram)
	s.autoArchive(ram)

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

	// Richtungswahl: pro Suchtiefe einmal entscheiden, welche Seite vertieft wird
	if s.forwardDepth+s.backwardDepth != s.dirDepth {
		s.dirDepth = s.forwardDepth + s.backwardDepth
		s.dirForward = s.chooseForward()
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

// Automatik der Richtungswahl. Default ist das Effizienz-Verhältnis: vertieft wird
// die Richtung, die bisher pro Hash-Eintrag die meisten Züge erreicht hat
// (Erfahrungswert von Max aus der manuellen Steuerung: die Rückwärtssuche kommt
// mit demselben Hash-Budget oft um Faktoren weiter und verdient dann mehr Budget).
// Die Wahl reguliert sich selbst: das Verhältnis der vertieften Seite sinkt durch
// ihr exponentielles Hash-Wachstum wieder, beide Seiten pendeln sich dort ein, wo
// sie gleich viele Züge je Knoten liefern. Vergleich per Kreuzmultiplikation statt
// Division (int64 reicht: Milliarden Einträge mal vierstellige Tiefen ~ 2^41).
// Solange eine Richtung noch keine fertige Tiefe hat - und im DirClassic-Modus
// immer - entscheidet das Kriterium des Originals: kleinere Tabelle zuerst
// (bitgenau zu SokoWahn_4th Z. 519-523, Basis der refcli-Orakel-Vergleiche).
func (s *Solver) chooseForward() bool {
	fd, bd := int64(s.forwardDepth), int64(s.backwardDepth)
	if s.dirMode == DirClassic || fd == 0 || bd == 0 {
		return s.forwardKnown.Len() < s.backwardKnown.Len()
	}
	return fd*s.backwardKnown.Len() >= bd*s.forwardKnown.Len()
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

// entscheidet, ob eine neu erzeugte Vorwärts-Stellung der Tiefe depth noch
// gespeichert und expandiert wird. Vor dem ersten Fund immer; danach nur, wenn
// sie die Lösung verkürzen könnte - mit keepEqual (Default) auch bei exaktem
// Gleichstand: solche Stellungen liegen nur auf alternativen zugoptimalen
// Pfaden und sind das Futter der Push-Optimierung (ohne sie fehlen dem DP
// Kanten und Anker an der Naht der Suchfronten - Level 361 fand deshalb 110
// statt der 108 Schübe). keepEqual=false ist die Beschneidung des Originals
// (CLI -dirclassic, bitgenaue Orakel-Vergleiche); im forwardOnly-Sonderfall
// sind Gleichstands-Stellungen nutzlos (keine Push-Optimierung möglich).
func (s *Solver) keepForward(depth int) bool {
	if s.foundTotal < 0 {
		return true
	}
	total := depth + s.backwardDepth + 1
	return total < s.foundTotal || (s.keepEqual && !s.forwardOnly && total == s.foundTotal)
}

// Gegenstück rückwärts (Gleichstand über die Vorwärts-Suchtiefe)
func (s *Solver) keepBackward(depth int) bool {
	if s.foundTotal < 0 {
		return true
	}
	total := depth + s.forwardDepth + 1
	return total < s.foundTotal || (s.keepEqual && total == s.foundTotal)
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
				if s.keepForward(int(depth)) {
					s.forwardKnown.Add(v.Crc, depth)
					s.pushForward(v) // zum weiteren Durchsuchen vormerken
				}
			} else if depth < findOwn { // kürzere Variante zu einer bekannten Stellung
				s.forwardKnown.Update(v.Crc, depth)
				if s.foundTotal >= 0 && v.Crc == s.foundState.Crc {
					s.adjustFoundForward(int(findOwn), int(depth))
				}
				if s.keepForward(int(depth)) {
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

			// Verbindung zur Rückwärtssuche prüfen (verifiziert gegen Hash-Kollisionen)
			findOpp := s.backwardKnown.Get(v.Crc)
			if findOpp == DepthUnknown {
				continue
			}
			total := int(depth) + int(findOpp)
			if s.foundTotal < 0 || total < s.foundTotal {
				if s.verifyMeet(v, int(depth), total) {
					s.foundTotal = total
					s.copyFoundState(v)
					s.foundForwardDepth = int(depth)
					s.resetMeetAnchors()
				}
			} else if total == s.foundTotal {
				s.collectEqualMeet(v, int(depth)) // weiterer Anker für die Push-Optimierung
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
				if s.keepBackward(int(depth)) {
					s.backwardKnown.Add(v.Crc, depth)
					s.pushBackward(v) // zum weiteren Durchsuchen vormerken
				}
			} else if depth < findOwn { // kürzere Variante zu einer bekannten Stellung
				s.backwardKnown.Update(v.Crc, depth)
				if s.foundTotal >= 0 && v.Crc == s.foundState.Crc {
					s.adjustFoundBackward(int(findOwn), int(depth))
				}
				if s.keepBackward(int(depth)) {
					s.pushBackward(v)
				}
			} else {
				continue
			}

			// Verbindung zur Vorwärtssuche prüfen (verifiziert gegen Hash-Kollisionen)
			findOpp := s.forwardKnown.Get(v.Crc)
			if findOpp == DepthUnknown {
				continue
			}
			total := int(depth) + int(findOpp)
			if s.foundTotal < 0 || total < s.foundTotal {
				if s.verifyMeet(v, int(findOpp), total) {
					s.foundTotal = total
					s.copyFoundState(v)
					s.foundForwardDepth = int(findOpp)
					s.resetMeetAnchors()
				}
			} else if total == s.foundTotal {
				s.collectEqualMeet(v, int(findOpp)) // weiterer Anker für die Push-Optimierung
			}
		}
	}
}

// verkürzt die gefundene Lösung, weil die Verbindungs-Stellung vorwärts früher
// erreicht wurde - mit Kollisions-Absicherung: meldet die Verifikation die neue
// Gesamtlänge als Schein-Verbindung (der Treffer auf foundState.Crc kam von einer
// kollidierenden fremden Stellung), bleibt die alte verifizierte Lösung bestehen
func (s *Solver) adjustFoundForward(oldDepth, newDepth int) {
	prevTotal, prevForward := s.foundTotal, s.foundForwardDepth
	s.foundTotal -= oldDepth - newDepth
	s.foundForwardDepth = newDepth
	if !s.verifyMeet(&s.foundState, s.foundForwardDepth, s.foundTotal) {
		s.foundTotal, s.foundForwardDepth = prevTotal, prevForward
		return
	}
	s.resetMeetAnchors() // Anker der alten (längeren) Lösung sind hinfällig
}

// Gegenstück rückwärts: der Rückwärtsanteil der Lösung wurde kürzer
func (s *Solver) adjustFoundBackward(oldDepth, newDepth int) {
	prevTotal := s.foundTotal
	s.foundTotal -= oldDepth - newDepth
	if !s.verifyMeet(&s.foundState, s.foundForwardDepth, s.foundTotal) {
		s.foundTotal = prevTotal
		return
	}
	s.resetMeetAnchors()
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
