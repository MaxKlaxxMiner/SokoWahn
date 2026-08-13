package solver

import (
	"errors"
	"fmt"
	"goSokoWahnBrute/soko"
)

// fertiger Lösungsweg
type Solution struct {
	States      []soko.State // Stellungsfolge vom Start bis zum Ziel (nur die Schub-Stellungen)
	Moves       string       // komplette Zugfolge in LURD-Notation (Grossbuchstaben = Schiebeschritte)
	MoveOffsets []int        // je Stellung die Anzahl der bis dahin ausgeführten Züge (Index in Moves)
}

// rekonstruiert den Lösungsweg über die beiden Hashtabellen
// (erst sinnvoll, nachdem eine Lösung gefunden wurde)
func (s *Solver) GetSolution() (*Solution, error) {
	if s.foundTotal < 0 {
		return nil, errors.New("no solution found")
	}
	return s.buildSolution(&s.foundState, s.foundForwardDepth, s.foundTotal)
}

// prüft einen Verbindungs-Kandidaten sofort per Probe-Rekonstruktion.
// Hintergrund Geburtstagsparadoxon: die Stellungs-Schlüssel sind 64-Bit-Hashes,
// ab Milliarden Einträgen werden Kollisionen real (N_v*N_r/2^64; bei je 2 Mrd
// schon ~20%). Ein kollidierender Schlüssel in der Gegentabelle gaukelt dann ein
// Treffen der Suchfronten vor - unverifiziert würde das falsche foundTotal die
// Suche beschneiden und die echte Lösung unbeweisbar machen. Die Kette einer
// Schein-Verbindung reißt bei der Rekonstruktion garantiert (die Nachfolger
// existieren nicht mit den exakten Tiefen bzw. Steps findet keinen Laufweg).
func (s *Solver) verifyMeet(found *soko.State, forwardDepth, total int) bool {
	if _, err := s.buildSolution(found, forwardDepth, total); err != nil {
		s.collisionRejects++
		return false
	}
	return true
}

// rekonstruiert den Lösungsweg für einen Verbindungs-Kandidaten. Nutzt eigene
// Varianten-Puffer (läuft auch mitten in der Varianten-Schleife der Suche);
// s.work wird nur zwischengenutzt - die Suche setzt ihren Zustand je Satz neu.
func (s *Solver) buildSolution(found *soko.State, forwardDepth, total int) (*Solution, error) {
	start := soko.State{}
	s.base.GetState(&start)
	varBuf := s.base.MakeStateBuffer(256)[:0]

	// --- Rückwärtsteil: von der Verbindungs-Stellung zurück zum Start ---
	// (Vorgänger ist die Stellung, deren Vorwärtstiefe exakt um die Laufzugzahl kleiner ist)
	chain := []soko.State{cloneState(found)}
	cur := cloneState(found)
	curDepth := forwardDepth

	for curDepth > 0 {
		cur.MoveDepth = 0 // Rückwärtstiefe zählt ab hier die Züge bis zum Vorgänger
		s.work.SetState(&cur)
		varBuf = s.work.SearchVariantsBackward(varBuf[:0])

		var pred *soko.State
		for i := range varBuf {
			predDepth := curDepth - int(varBuf[i].MoveDepth)
			if predDepth >= 0 && s.forwardKnown.Get(varBuf[i].Crc) == uint16(predDepth) {
				pred = &varBuf[i]
				break
			}
		}
		if pred == nil {
			break // kein Schub-Vorgänger in der Tabelle -> der Vorgänger ist die Startstellung selbst
		}

		curDepth -= int(pred.MoveDepth)
		cur = cloneState(pred)
		chain = append(chain, cur)
	}
	if chain[len(chain)-1].Crc != start.Crc {
		chain = append(chain, cloneState(&start))
	}

	// Kette umdrehen -> Start ... Verbindungs-Stellung
	states := make([]soko.State, 0, len(chain)+8)
	for i := len(chain) - 1; i >= 0; i-- {
		states = append(states, chain[i])
	}

	// --- Vorwärtsteil: von der Verbindungs-Stellung zum Ziel ---
	// (Nachfolger ist die Stellung, deren Rückwärtstiefe exakt zur Gesamtlösung passt)
	cur = cloneState(found)
	curDepth = forwardDepth

	for curDepth < total {
		cur.MoveDepth = int32(curDepth)
		s.work.SetState(&cur)
		varBuf = s.work.SearchVariantsForward(varBuf[:0])

		var next *soko.State
		for i := range varBuf {
			restDepth := total - int(varBuf[i].MoveDepth)
			if restDepth >= 0 && s.backwardKnown.Get(varBuf[i].Crc) == uint16(restDepth) {
				next = &varBuf[i]
				break
			}
		}
		if next == nil {
			return nil, errors.New("solution path interrupted")
		}

		curDepth = int(next.MoveDepth)
		cur = cloneState(next)
		states = append(states, cur)
	}

	return s.statesToSolution(states, total)
}

// leitet die LURD-Zugfolge aus einer Schub-Stellungsfolge ab und prüft die
// Gesamtlänge (der finale Integritäts-Check jeder Rekonstruktion)
func (s *Solver) statesToSolution(states []soko.State, total int) (*Solution, error) {
	moves := make([]byte, 0, total)
	offsets := make([]int, 1, len(states)) // Startstellung = 0 ausgeführte Züge
	for i := 1; i < len(states); i++ {
		part, err := s.work.Steps(&states[i-1], &states[i])
		if err != nil {
			return nil, err
		}
		moves = append(moves, part...)
		offsets = append(offsets, len(moves))
	}

	if len(moves) != total {
		return nil, fmt.Errorf("solution length mismatch: %d moves, expected %d", len(moves), total)
	}

	return &Solution{States: states, Moves: string(moves), MoveOffsets: offsets}, nil
}

// erstellt eine unabhängige Kopie einer Stellung
func cloneState(v *soko.State) soko.State {
	boxes := make([]soko.Wpos, len(v.Boxes))
	copy(boxes, v.Boxes)
	return soko.State{Player: v.Player, Boxes: boxes, MoveDepth: v.MoveDepth, Crc: v.Crc}
}
