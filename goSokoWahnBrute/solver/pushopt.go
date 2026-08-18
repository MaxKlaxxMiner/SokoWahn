package solver

import (
	"errors"

	"goSokoWahnBrute/crc64"
	"goSokoWahnBrute/soko"
)

// Push-Optimierung unter den zugoptimalen Lösungen (Webseiten-Bewertung:
// erst Züge, dann Schübe). Die Hashtabellen enthalten nach der Suche implizit
// einen ganzen DAG zugoptimaler Pfade - buildSolution nimmt davon nur den
// erstbesten. Hier läuft stattdessen von jeder gesammelten Verbindungs-
// Stellung (meetAnchors) ein memoisiertes DP in beide Richtungen, das die
// Anzahl der Kanten minimiert: jede Kante ist genau ein Schub, die Zugsumme
// ist über die exakten Tabellen-Tiefen ohnehin auf die optimale Länge fixiert.
//
// Vollständigkeits-Hinweis: die Suche prunt nach dem ersten Fund - Stellungen,
// die NUR auf alternativen Optimalpfaden liegen, können fehlen. Das Ergebnis
// ist also die beste Push-Zahl unter den in den Tabellen repräsentierten
// zugoptimalen Lösungen (und nie schlechter als GetSolution).

// eine verifizierte Verbindungs-Stellung der aktuell besten Lösungslänge
type meetAnchor struct {
	state        soko.State
	forwardDepth int
}

// Deckel der Anker-Sammlung (Verifikations- und Speicherkosten begrenzen)
const meetAnchorLimit = 1024

// Deckel für die DP-Knoten beider Richtungen zusammen; darüber fällt
// GetSolutionBestPushes auf die einfache Rekonstruktion zurück
var PushOptimizeNodeLimit = 1 << 20

// setzt die Anker-Sammlung nach einer neuen besten Lösung zurück
// (foundState/foundForwardDepth müssen bereits aktualisiert sein)
func (s *Solver) resetMeetAnchors() {
	s.meetAnchors = s.meetAnchors[:0]
	s.meetSeen = map[crc64.Value]struct{}{s.foundState.Crc: {}}
	s.meetAnchors = append(s.meetAnchors, meetAnchor{state: cloneState(&s.foundState), forwardDepth: s.foundForwardDepth})
}

// lohnt sich die Aufnahme eines Gleichstand-Treffens? (billiger Vorabtest
// für die parallelen Merge-Pfade, bevor der Satz zur Stellung entpackt wird)
func (s *Solver) wantEqualMeet(crc crc64.Value) bool {
	if len(s.meetAnchors) >= meetAnchorLimit || s.meetSeen == nil {
		return false
	}
	_, seen := s.meetSeen[crc]
	return !seen
}

// nimmt ein Treffen mit exakt der besten Gesamtlänge in die Anker-Sammlung auf
// (verifiziert gegen Hash-Kollisionen wie die Haupt-Verbindung)
func (s *Solver) collectEqualMeet(state *soko.State, forwardDepth int) {
	if !s.wantEqualMeet(state.Crc) {
		return
	}
	if !s.verifyMeet(state, forwardDepth, s.foundTotal) {
		return
	}
	s.meetSeen[state.Crc] = struct{}{}
	s.meetAnchors = append(s.meetAnchors, meetAnchor{state: cloneState(state), forwardDepth: forwardDepth})
}

// Kennzahlen eines GetSolutionBestPushes-Laufs (für Statuszeile und -checksol:
// zeigt, ob die Optimierung überhaupt lief und ob einer der Deckel zugeschlagen hat)
type PushOptStats struct {
	Ran         bool // DP wurde ausgeführt (false = forwardOnly oder keine Anker)
	Anchors     int  // gesammelte Verbindungs-Anker
	AnchorCap   bool // Anker-Deckel (meetAnchorLimit) erreicht - weitere Treffen wurden verworfen
	DPNodes     int  // memoisierte DP-Knoten beider Richtungen zusammen
	Overflow    bool // Knoten-Limit gerissen - Ergebnis ist die einfache Rekonstruktion
	PlainPushes int  // Schübe der einfachen Rekonstruktion (GetSolution)
	BestPushes  int  // Schübe des zurückgegebenen Ergebnisses
}

// Kennzahlen des letzten GetSolutionBestPushes-Laufs
func (s *Solver) PushOptStats() PushOptStats {
	return s.pushOptStats
}

// Anzahl der aktuell gesammelten Verbindungs-Anker (wächst nach dem ersten Fund
// mit der Beweis-Endphase; Änderungs-Signal für die Live-Schub-Anzeige im TUI)
func (s *Solver) MeetAnchorCount() int {
	return len(s.meetAnchors)
}

// GetSolutionBestPushes rekonstruiert unter den zugoptimalen Lösungen der
// Tabellen eine mit minimaler Schub-Zahl; fällt bei Sonderfällen oder
// Knoten-Überlauf auf GetSolution zurück. Auch während der laufenden Suche
// (zwischen zwei Steps) aufrufbar: das Ergebnis ist dann der Stand der bisher
// repräsentierten zugoptimalen Pfade - eine echte, spielbare Lösung, deren
// Schub-Zahl mit dem Suchfortschritt noch sinken kann. Bricht mitten in der
// Suche eine Kette (Tiefen-Updates), kommt ein Fehler zurück - der nächste
// Aufruf nach mehr Suchfortschritt heilt das.
func (s *Solver) GetSolutionBestPushes() (*Solution, error) {
	if s.foundTotal < 0 {
		return nil, errors.New("no solution found")
	}
	plain, err := s.GetSolution()
	if err != nil {
		return nil, err
	}
	stats := PushOptStats{
		Anchors:     len(s.meetAnchors),
		AnchorCap:   len(s.meetAnchors) >= meetAnchorLimit,
		PlainPushes: CountPushes(plain.Moves),
		BestPushes:  CountPushes(plain.Moves),
	}
	s.pushOptStats = stats
	if s.forwardOnly || len(s.meetAnchors) == 0 {
		return plain, nil
	}

	opt := &pushOptimizer{
		s:       s,
		toStart: map[crc64.Value]*pushNode{},
		toGoal:  map[crc64.Value]*pushNode{},
		varBuf:  s.base.MakeStateBuffer(256)[:0],
	}
	s.base.GetState(&opt.start)
	opt.toStart[opt.start.Crc] = &pushNode{state: cloneState(&opt.start)}

	bestPushes := -1
	var bestAnchor *meetAnchor
	for i := range s.meetAnchors {
		anchor := &s.meetAnchors[i]
		down := opt.bestToStart(&anchor.state, anchor.forwardDepth)
		if down == nil {
			continue
		}
		up := opt.bestToGoal(&anchor.state, anchor.forwardDepth)
		if up == nil {
			continue
		}
		if sum := down.pushes + up.pushes; bestPushes < 0 || sum < bestPushes {
			bestPushes = sum
			bestAnchor = anchor
		}
	}
	stats.Ran = true
	stats.DPNodes = len(opt.toStart) + len(opt.toGoal)
	stats.Overflow = opt.overflow
	s.pushOptStats = stats
	if bestAnchor == nil || opt.overflow {
		return plain, nil // Überlauf oder (theoretisch) kein Anker begehbar
	}

	// Kette zusammensetzen: Start ... Anker (toStart-Parents rückwärts),
	// dann Anker ... Ziel (toGoal-Parents vorwärts)
	var reversed []soko.State
	for node := opt.toStart[bestAnchor.state.Crc]; node != nil; node = node.parent {
		reversed = append(reversed, node.state)
	}
	states := make([]soko.State, 0, len(reversed)+8)
	for i := len(reversed) - 1; i >= 0; i-- {
		states = append(states, reversed[i])
	}
	for node := opt.toGoal[bestAnchor.state.Crc].parent; node != nil; node = node.parent {
		states = append(states, node.state)
	}

	solution, err := s.statesToSolution(states, s.foundTotal)
	if err != nil {
		return nil, err
	}
	stats.BestPushes = CountPushes(solution.Moves)
	s.pushOptStats = stats
	return solution, nil
}

// DP-Knoten: minimale Schübe von dieser Stellung bis zum Start bzw. Ziel
type pushNode struct {
	state  soko.State // eigene Kopie für die Ketten-Rekonstruktion
	pushes int        // minimale Schübe in Richtung Start bzw. Ziel
	parent *pushNode  // nächster Knoten der optimalen Kette
}

type pushOptimizer struct {
	s        *Solver
	start    soko.State
	toStart  map[crc64.Value]*pushNode // memoisiert; nil-Wert = kein gültiger Weg
	toGoal   map[crc64.Value]*pushNode
	varBuf   []soko.State
	scratch  soko.State
	overflow bool
}

// minimale Schübe von state (Vorwärtstiefe depth) zurück zum Start.
// Kandidaten sind alle Schub-Vorgänger mit exakt passender Tabellen-Tiefe
// (wie buildSolution, nur ohne den Abbruch beim ersten Treffer) plus die
// Direkt-Kante zum Start (dessen Spieler-Pose auf keine Pull-Variante passt).
func (o *pushOptimizer) bestToStart(state *soko.State, depth int) *pushNode {
	if node, ok := o.toStart[state.Crc]; ok {
		return node
	}
	if len(o.toStart)+len(o.toGoal) >= PushOptimizeNodeLimit {
		o.overflow = true
		return nil
	}

	// Vorgänger-Kandidaten einsammeln (Puffer wird in der Rekursion wiederverwendet)
	o.scratch = cloneState(state)
	o.scratch.MoveDepth = 0
	o.s.work.SetState(&o.scratch)
	o.varBuf = o.s.work.SearchVariantsBackward(o.varBuf[:0])
	type candidate struct {
		state soko.State
		depth int
	}
	var preds []candidate
	for i := range o.varBuf {
		predDepth := depth - int(o.varBuf[i].MoveDepth)
		if predDepth >= 0 && o.s.forwardKnown.Get(o.varBuf[i].Crc) == uint16(predDepth) {
			preds = append(preds, candidate{state: cloneState(&o.varBuf[i]), depth: predDepth})
		}
	}

	var best *pushNode
	for i := range preds {
		if sub := o.bestToStart(&preds[i].state, preds[i].depth); sub != nil {
			if best == nil || sub.pushes+1 < best.pushes {
				best = &pushNode{state: cloneState(state), pushes: sub.pushes + 1, parent: sub}
			}
		}
	}
	// Direkt-Kante: Stellung ist der erste Schub ab der Startstellung
	if best == nil || best.pushes > 1 {
		if part, err := o.s.work.Steps(&o.start, state); err == nil && len(part) == depth {
			best = &pushNode{state: cloneState(state), pushes: 1, parent: o.toStart[o.start.Crc]}
		}
	}

	o.toStart[state.Crc] = best
	return best
}

// minimale Schübe von state (Vorwärtstiefe depth) bis zur gelösten Stellung
// (Nachfolger mit exakt passender Rückwärts-Resttiefe, wie buildSolution)
func (o *pushOptimizer) bestToGoal(state *soko.State, depth int) *pushNode {
	if node, ok := o.toGoal[state.Crc]; ok {
		return node
	}
	if depth == o.s.foundTotal {
		node := &pushNode{state: cloneState(state)}
		o.toGoal[state.Crc] = node
		return node
	}
	if len(o.toStart)+len(o.toGoal) >= PushOptimizeNodeLimit {
		o.overflow = true
		return nil
	}

	o.scratch = cloneState(state)
	o.scratch.MoveDepth = int32(depth)
	o.s.work.SetState(&o.scratch)
	o.varBuf = o.s.work.SearchVariantsForward(o.varBuf[:0])
	type candidate struct {
		state soko.State
		depth int
	}
	var nexts []candidate
	for i := range o.varBuf {
		restDepth := o.s.foundTotal - int(o.varBuf[i].MoveDepth)
		if restDepth >= 0 && o.s.backwardKnown.Get(o.varBuf[i].Crc) == uint16(restDepth) {
			nexts = append(nexts, candidate{state: cloneState(&o.varBuf[i]), depth: int(o.varBuf[i].MoveDepth)})
		}
	}

	var best *pushNode
	for i := range nexts {
		if sub := o.bestToGoal(&nexts[i].state, nexts[i].depth); sub != nil {
			if best == nil || sub.pushes+1 < best.pushes {
				best = &pushNode{state: cloneState(state), pushes: sub.pushes + 1, parent: sub}
			}
		}
	}

	o.toGoal[state.Crc] = best
	return best
}

// zählt die Schübe einer LURD-Zugfolge (Großbuchstaben)
func CountPushes(moves string) int {
	count := 0
	for i := 0; i < len(moves); i++ {
		if moves[i] >= 'A' && moves[i] <= 'Z' {
			count++
		}
	}
	return count
}
