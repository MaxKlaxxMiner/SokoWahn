package solver

// Momentaufnahme des Suchfortschritts (Datenbasis für Anzeige und Vergleiche)
type Stats struct {
	ForwardDepth  int   // aktuell bearbeitete Vorwärtstiefe
	BackwardDepth int   // aktuell bearbeitete Rückwärtstiefe
	ForwardNodes  int64 // Anzahl bekannter Stellungen der Vorwärtssuche
	BackwardNodes int64 // Anzahl bekannter Stellungen der Rückwärtssuche
	ForwardOpen   []int // noch zu prüfende Sätze je Vorwärtstiefe
	BackwardOpen  []int // noch zu prüfende Sätze je Rückwärtstiefe
	FoundMoves    int   // beste gefundene Lösungslänge in Zügen, -1 = noch keine
	Done          bool  // gibt an, ob die Suche abgeschlossen ist
}

func (s *Solver) GetStats() Stats {
	stats := Stats{
		ForwardDepth:  s.forwardDepth,
		BackwardDepth: s.backwardDepth,
		ForwardNodes:  s.forwardKnown.Len(),
		BackwardNodes: s.backwardKnown.Len(),
		ForwardOpen:   make([]int, len(s.forwardLists)),
		BackwardOpen:  make([]int, len(s.backwardLists)),
		FoundMoves:    s.foundTotal,
		Done:          s.done,
	}
	for i, list := range s.forwardLists {
		stats.ForwardOpen[i] = list.Count()
	}
	for i, list := range s.backwardLists {
		stats.BackwardOpen[i] = list.Count()
	}
	return stats
}

// Gesamtzahl der noch zu prüfenden Stellungen (Pendant zu KnotenRest im Original)
func (s *Solver) OpenCount() int64 {
	var sum int64
	for _, list := range s.forwardLists {
		sum += int64(list.Count())
	}
	for _, list := range s.backwardLists {
		sum += int64(list.Count())
	}
	return sum
}

// Gesamtzahl der bekannten Stellungen (Pendant zu KnotenAnzahl im Original)
func (s *Solver) NodeCount() int64 {
	return s.forwardKnown.Len() + s.backwardKnown.Len()
}

// aktuelle Suchtiefe (Summe beider Richtungen, Pendant zu SuchTiefe im Original)
func (s *Solver) SearchDepth() int {
	return s.forwardDepth + s.backwardDepth
}
