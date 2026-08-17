package solver

import (
	"errors"
	"fmt"
	"strings"

	"goSokoWahnBrute/tools"
)

// CheckSolution prüft nach Abschluss der Suche, wie eine extern gefundene
// zugoptimale Lösung (LURD-Notation, z.B. aus JSOKO) in den Hashtabellen und
// der Anker-Sammlung repräsentiert ist - die Diagnose zur Push-Optimierung:
// findet GetSolutionBestPushes eine bekannte bessere Lösung nicht, zeigt der
// Report, an welchem Schub die Kette reißt (Stellung fehlt in der Tabelle,
// falsche Tiefe, kein begehbarer Anker) und ob einer der Deckel zugeschlagen
// hat. CLI-Flag -checksol; das Ergebnis ist ein mehrzeiliger Text-Report.
func (s *Solver) CheckSolution(lurd string) (string, error) {
	if s.foundTotal < 0 {
		return "", errors.New("no solution found")
	}
	lurd = strings.TrimSpace(lurd)
	if lurd == "" {
		return "", errors.New("leere LURD-Zugfolge")
	}

	// Lösung abspielen (base steht unverändert auf der Startstellung)
	states, err := s.base.ReplayLurd(lurd)
	if err != nil {
		return "", err
	}
	last := &states[len(states)-1]
	for i, box := range last.Boxes {
		if box != s.goals[i] {
			return "", fmt.Errorf("Endstellung der LURD-Folge ist nicht gelöst (Kiste %d)", i)
		}
	}

	total := len(lurd)
	pushes := len(states) - 1
	var sb strings.Builder
	fmt.Fprintf(&sb, "Checksol: Referenz-Lösung %d Züge / %d Schübe, Suche fand %d Züge\n", total, pushes, s.foundTotal)
	if total != s.foundTotal {
		fmt.Fprintf(&sb, "ACHTUNG: Zuglängen weichen ab - die Referenz ist keine zugoptimale Lösung dieser Suche,\n")
		fmt.Fprintf(&sb, "die Tiefen-Vergleiche unten beziehen sich auf die Referenz-Länge %d\n", total)
	}

	st := s.pushOptStats
	fmt.Fprintf(&sb, "Pushopt: Anker %d", st.Anchors)
	if st.AnchorCap {
		fmt.Fprintf(&sb, " (Deckel %d erreicht - weitere Treffen verworfen!)", meetAnchorLimit)
	}
	if st.Ran {
		fmt.Fprintf(&sb, ", DP %s Knoten", tools.FormatInt(int64(st.DPNodes)))
		if st.Overflow {
			fmt.Fprintf(&sb, " (Limit %s gerissen - Fallback auf einfache Rekonstruktion!)", tools.FormatInt(int64(PushOptimizeNodeLimit)))
		}
		fmt.Fprintf(&sb, ", Schübe einfach %d / optimiert %d", st.PlainPushes, st.BestPushes)
	} else {
		fmt.Fprintf(&sb, ", DP nicht gelaufen (Schübe einfach %d)", st.PlainPushes)
	}
	sb.WriteString("\n\n")

	// je Schub-Stellung: exakte Vorwärtstiefe, exakter Rückwärtsrest, Anker-Status
	fwdExact, bwdExact, anchorCount := 0, 0, 0
	fwdBreak := pushes + 1 // erster Schub, dessen Stellung vorwärts nicht exakt bekannt ist
	bwdBreak := 0          // letzter Schub, dessen Stellung rückwärts nicht exakt bekannt ist
	anchorOK := -1         // ein Anker, über den das DP den Pfad komplett ablaufen könnte
	for i := 1; i < len(states); i++ {
		depth := int(states[i].MoveDepth)
		rest := total - depth

		fwd := s.forwardKnown.Get(states[i].Crc)
		fwdInfo := "fehlt"
		if fwd == uint16(depth) {
			fwdInfo = "ok"
			fwdExact++
		} else if fwd != DepthUnknown {
			fwdInfo = fmt.Sprintf("Tiefe %d statt %d", fwd, depth)
		}
		if fwdInfo != "ok" && i < fwdBreak {
			fwdBreak = i
		}

		bwd := s.backwardKnown.Get(states[i].Crc)
		bwdInfo := "fehlt"
		if bwd == uint16(rest) {
			bwdInfo = "ok"
			bwdExact++
		} else if bwd != DepthUnknown {
			bwdInfo = fmt.Sprintf("Rest %d statt %d", bwd, rest)
		}
		if bwdInfo != "ok" && i > bwdBreak {
			bwdBreak = i
		}

		anchorInfo := ""
		if s.meetSeen != nil {
			if _, ok := s.meetSeen[states[i].Crc]; ok {
				anchorInfo = " | ANKER"
				anchorCount++
			}
		}

		fmt.Fprintf(&sb, "Schub %3d (Zug %3d): vorwärts %-18s | rückwärts %-16s%s\n",
			i, depth, fwdInfo, bwdInfo, anchorInfo)
	}

	// Kettenanalyse: das DP braucht einen Anker i, dessen Präfix (Schübe 1..i-1)
	// komplett vorwärts-exakt und dessen Suffix (i+1..Ende) komplett rückwärts-exakt
	// bekannt ist - dann wäre dieser Pfad für die Push-Optimierung voll begehbar.
	// Der Anker selbst braucht keinen Tabellen-Eintrag (bestToStart/bestToGoal
	// fragen nur seine Nachbarn ab; ein besserer Zweit-Fund wird wegen der
	// Nach-Fund-Beschneidung selbst gar nicht mehr gespeichert)
	if s.meetSeen != nil {
		for i := 1; i < len(states); i++ {
			if _, ok := s.meetSeen[states[i].Crc]; !ok {
				continue
			}
			if fwdBreak >= i && bwdBreak <= i {
				anchorOK = i
				break
			}
		}
	}

	fmt.Fprintf(&sb, "\nZusammenfassung: vorwärts exakt %d/%d (erste Lücke: Schub %s), rückwärts exakt %d/%d (letzte Lücke: Schub %s), Anker auf dem Pfad: %d\n",
		fwdExact, pushes, breakInfo(fwdBreak, pushes+1), bwdExact, pushes, breakInfo(bwdBreak, 0), anchorCount)
	if anchorOK > 0 {
		fmt.Fprintf(&sb, "Pfad wäre über Anker Schub %d komplett begehbar - wenn das DP trotzdem mehr Schübe liefert, liegt der Fehler im DP selbst (oder im Overflow-Fallback)\n", anchorOK)
	} else {
		fmt.Fprintf(&sb, "kein Anker mit durchgängiger Kette: dieser Pfad ist in den Tabellen nicht vollständig repräsentiert (Stellungen nach dem ersten Fund verworfen oder nie erzeugt)\n")
	}

	return sb.String(), nil
}

// formatiert die Lücken-Position der Zusammenfassung ("keine" beim Sentinel-Wert)
func breakInfo(pos, sentinel int) string {
	if pos == sentinel {
		return "keine"
	}
	return fmt.Sprintf("%d", pos)
}
