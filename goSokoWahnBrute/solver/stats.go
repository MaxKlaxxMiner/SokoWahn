package solver

import "math"

// Momentaufnahme des Suchfortschritts (Datenbasis für Anzeige und Vergleiche)
type Stats struct {
	ForwardDepth  int   // aktuell bearbeitete Vorwärtstiefe
	BackwardDepth int   // aktuell bearbeitete Rückwärtstiefe
	ForwardNodes  int64 // Anzahl bekannter Stellungen der Vorwärtssuche
	BackwardNodes int64 // Anzahl bekannter Stellungen der Rückwärtssuche
	ForwardOpen   []int // noch zu prüfende Sätze je Vorwärtstiefe
	BackwardOpen  []int // noch zu prüfende Sätze je Rückwärtstiefe
	FoundMoves    int   // beste gefundene Lösungslänge in Zügen, -1 = noch keine
	FoundForward  int   // Vorwärtstiefe der Verbindungs-Stellung (nur gültig wenn FoundMoves >= 0)
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
		FoundForward:  s.foundForwardDepth,
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

// auf die Festplatte ausgelagerte Bytes aller Suchlisten (0 = alles im RAM)
func (s *Solver) SpillBytes() int64 {
	var sum int64
	for _, list := range s.forwardLists {
		sum += list.SpillBytes()
	}
	for _, list := range s.backwardLists {
		sum += list.SpillBytes()
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

// schätzt anhand des Hash-Wachstums der letzten 20 Suchtiefen, bis zu welcher Suchtiefe
// die Hashtabellen mit 100 Mio, 1 Mrd bzw. 3 Mrd Einträgen reichen (wie das List2-Original:
// mittlerer Anstieg der letzten 10 Stufen, Wachstumsfaktor aus dem Vergleich zu den 10 davor);
// ok = false, wenn noch zu wenige Messpunkte vorliegen oder die Nutzung außerhalb 1M..3G liegt
func (s *Solver) EstimateMaxDepths() (depth100M, depth1G, depth3G int, ok bool) {
	n := len(s.hashUsage)
	if n <= 20 {
		return 0, 0, 0, false
	}
	last := s.hashUsage[n-1]
	if last <= 1000000 || last >= 3000000000 {
		return 0, 0, 0, false
	}

	riseLast := float64(s.hashUsage[n-1] - s.hashUsage[n-11])
	riseBefore := float64(s.hashUsage[n-11] - s.hashUsage[n-21])
	mulPerDepth := 1.0
	if riseBefore > 0 && riseLast > riseBefore {
		mulPerDepth = math.Pow(riseLast/riseBefore, 1.0/10)
	}

	depth100M, depth1G, depth3G = s.SearchDepth(), s.SearchDepth(), s.SearchDepth()
	expect := float64(last)
	rise := riseLast * 0.1
	for expect < 3000000000 && depth3G < 9999 {
		if expect < 100000000 {
			depth100M++
		}
		if expect < 1000000000 {
			depth1G++
		}
		depth3G++
		expect += rise
		rise *= mulPerDepth
	}
	return depth100M, depth1G, depth3G, true
}
