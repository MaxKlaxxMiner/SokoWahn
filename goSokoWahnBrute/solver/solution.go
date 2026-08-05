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

	start := soko.State{}
	s.base.GetState(&start)

	// --- Rückwärtsteil: von der Verbindungs-Stellung zurück zum Start ---
	// (Vorgänger ist die Stellung, deren Vorwärtstiefe exakt um die Laufzugzahl kleiner ist)
	chain := []soko.State{cloneState(&s.foundState)}
	cur := cloneState(&s.foundState)
	curDepth := s.foundForwardDepth

	for curDepth > 0 {
		cur.MoveDepth = 0 // Rückwärtstiefe zählt ab hier die Züge bis zum Vorgänger
		s.work.SetState(&cur)
		s.varBuf = s.work.SearchVariantsBackward(s.varBuf[:0])

		var pred *soko.State
		for i := range s.varBuf {
			predDepth := curDepth - int(s.varBuf[i].MoveDepth)
			if predDepth >= 0 && s.forwardKnown.Get(s.varBuf[i].Crc) == uint16(predDepth) {
				pred = &s.varBuf[i]
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
	cur = cloneState(&s.foundState)
	curDepth = s.foundForwardDepth

	for curDepth < s.foundTotal {
		cur.MoveDepth = int32(curDepth)
		s.work.SetState(&cur)
		s.varBuf = s.work.SearchVariantsForward(s.varBuf[:0])

		var next *soko.State
		for i := range s.varBuf {
			restDepth := s.foundTotal - int(s.varBuf[i].MoveDepth)
			if restDepth >= 0 && s.backwardKnown.Get(s.varBuf[i].Crc) == uint16(restDepth) {
				next = &s.varBuf[i]
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

	// --- LURD-Zugfolge aus den Schub-Stellungen ableiten ---
	moves := make([]byte, 0, s.foundTotal)
	offsets := make([]int, 1, len(states)) // Startstellung = 0 ausgeführte Züge
	for i := 1; i < len(states); i++ {
		part, err := s.work.Steps(&states[i-1], &states[i])
		if err != nil {
			return nil, err
		}
		moves = append(moves, part...)
		offsets = append(offsets, len(moves))
	}

	if len(moves) != s.foundTotal {
		return nil, fmt.Errorf("solution length mismatch: %d moves, expected %d", len(moves), s.foundTotal)
	}

	return &Solution{States: states, Moves: string(moves), MoveOffsets: offsets}, nil
}

// erstellt eine unabhängige Kopie einer Stellung
func cloneState(v *soko.State) soko.State {
	boxes := make([]soko.Wpos, len(v.Boxes))
	copy(boxes, v.Boxes)
	return soko.State{Player: v.Player, Boxes: boxes, MoveDepth: v.MoveDepth, Crc: v.Crc}
}
