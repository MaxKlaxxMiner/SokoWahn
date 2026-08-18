package rooms

import (
	"fmt"
	"slices"
)

// Kandidaten-Finder der Dominanzsuche (M4b, siehe docs/konzept.md): findet
// für einen Ein-Portal-Raum automatisch eine reduzierte Varianten-Menge,
// die ALLE Außenwörter kostengleich bedient (bewiesen über
// compareUsageGraphs, keine Horizont-Evidenz).
//
// Verfahren: Greedy-Elimination. Jede Variante wird probeweise gestrichen;
// bleibt der Vergleich gegen den vollen Graphen kostengleich, ist die
// Streichung dauerhaft. Ein einziger Durchlauf genügt für ein lokales
// Minimum, denn die Kosten sind monoton in der Varianten-Menge: weniger
// Varianten können nie billiger bedienen, nur teurer (jede Nutzung der
// kleineren Menge existiert auch in der größeren). War eine Variante v aus
// der Menge S nicht streichbar (Differenz), dann erst recht nicht aus einer
// späteren Teilmenge S' - die Differenz kann nur wachsen. Nach dem Durchlauf
// ist also KEINE einzelne verbleibende Variante mehr streichbar.
//
// Gestrichen wird in zwei Phasen:
//   1. ganze ZUSTÄNDE (probeweise alle Varianten streichen, die den Zustand
//      als OldState oder NewState berühren). Zustände sind die teure
//      Größe - sie gehen beim Mergen multiplikativ ins Kreuzprodukt ein -
//      und die Phase entschärft das Überdeckungsproblem: reine Varianten-
//      Elimination in ID-Reihenfolge streicht sonst früh die "richtigen"
//      Varianten und hält dafür ganze Gleichstands-Familien am Leben
//      (202er-Kammer: 9 Varianten auf 8 Zuständen statt 7 auf 5).
//   2. einzelne Varianten auf dem Rest.
// Das Ergebnis hängt bei Gleichstands-Alternativen von der Probier-
// Reihenfolge ab (welche von zwei austauschbaren fällt); beide Phasen
// laufen in aufsteigender ID-Reihenfolge, das Ergebnis ist deterministisch.
// Die Minimalität der GRÖSSE ist nicht garantiert (Überdeckungsproblem),
// nur die lokale: keine einzelne verbleibende Variante ist streichbar.
// Praktisch trifft das Zwei-Phasen-Greedy die Handrechnung (202er-Kammer).

// Ergebnis einer Greedy-Reduktion
type ReduceResult struct {
	Kept          []uint64 // verbleibende Varianten (bewiesen ausreichend)
	Removed       []uint64 // gestrichene Varianten (bewiesen entbehrlich)
	Undecided     []uint64 // konservativ behalten (Vergleichs-Limit erreicht)
	RemovedStates []uint64 // in Phase 1 komplett eliminierte Zustände
	Detail        string   // Beschreibung des Abschluss-Beweises
}

// prüft, ob der Finder auf diesen Raum anwendbar ist
func canReduceVariants(room *Room) bool {
	return len(room.Incoming) == 1 && room.StartVariantCount == 0
}

// reduziert die Varianten-Menge eines Ein-Portal-Raums per Greedy-Elimination.
// maxConfigs begrenzt jeden Einzelvergleich (Sicherheitsnetz gegen
// divergierende Loop-Raten); unentscheidbare Kandidaten bleiben erhalten.
// Der Raum selbst wird NICHT verändert - das Ergebnis beschreibt nur,
// welche Varianten entbehrlich sind.
func reduceVariants(room *Room, maxConfigs int) ReduceResult {
	if !canReduceVariants(room) {
		panic("reduceVariants: nur Ein-Portal-Räume ohne Startvarianten")
	}

	full := buildUsageGraph(room, nil)
	removed := map[uint64]bool{}
	result := ReduceResult{}
	stillEqual := func() bool {
		candidate := buildUsageGraph(room, func(id uint64) bool { return !removed[id] })
		verdict, _ := compareUsageGraphs(full, candidate, maxConfigs)
		return verdict == usageEqual
	}

	// Phase 1: ganze Zustände eliminieren (0 = gelöst und der Startzustand
	// bleiben immer)
	for state := uint64(1); state < room.States.Count(); state++ {
		if state == room.StartState {
			continue
		}
		var batch []uint64
		for vid := uint64(0); vid < room.Variants.Count(); vid++ {
			v := room.Variants.Get(vid)
			if !removed[vid] && (v.OldState == state || v.NewState == state) {
				batch = append(batch, vid)
			}
		}
		if len(batch) == 0 {
			continue
		}
		for _, vid := range batch {
			removed[vid] = true
		}
		if stillEqual() {
			result.Removed = append(result.Removed, batch...)
			result.RemovedStates = append(result.RemovedStates, state)
			continue
		}
		for _, vid := range batch {
			delete(removed, vid)
		}
	}

	// Phase 2: einzelne Varianten auf dem Rest
	for vid := uint64(0); vid < room.Variants.Count(); vid++ {
		if removed[vid] {
			continue
		}
		removed[vid] = true
		candidate := buildUsageGraph(room, func(id uint64) bool { return !removed[id] })
		verdict, _ := compareUsageGraphs(full, candidate, maxConfigs)
		switch verdict {
		case usageEqual:
			result.Removed = append(result.Removed, vid)
		case usageUndecided:
			delete(removed, vid)
			result.Undecided = append(result.Undecided, vid)
		default:
			delete(removed, vid)
		}
	}
	slices.Sort(result.Removed)

	for vid := uint64(0); vid < room.Variants.Count(); vid++ {
		if !removed[vid] {
			result.Kept = append(result.Kept, vid)
		}
	}

	// Abschluss-Beweis der Gesamtmenge (redundant zur Induktion der
	// Einzelschritte, aber billig und ein guter Wächter)
	final := buildUsageGraph(room, func(id uint64) bool { return !removed[id] })
	verdict, detail := compareUsageGraphs(full, final, maxConfigs)
	if verdict != usageEqual {
		panic("reduceVariants: Abschluss-Beweis fehlgeschlagen: " + detail)
	}
	result.Detail = detail
	return result
}

// Sicherheitslimit je Einzelvergleich der Dominanzsuche (Vergleichs-
// Situationen; real sättigen die Räume nach einer Handvoll)
const dominanceMaxConfigs = 100000

// DominanceReduce führt die Dominanzsuche auf einem Raum aus und entfernt
// bewiesen entbehrliche Varianten samt dabei verwaisender Zustände. Nicht
// anwendbare Räume (mehr als ein Portal, Startvarianten) bleiben unberührt.
// info (optional) bekommt Fortschritts-Meldungen; Rückgabe false bricht ab,
// der Raum bleibt dann unverändert.
func (n *Network) DominanceReduce(room *Room, info func(string) bool) (removed uint64, ok bool) {
	if !canReduceVariants(room) || room.Variants.Count() == 0 {
		return 0, true
	}
	if info != nil && !info(fmt.Sprintf("dominance scan room %d: %d variants", room.Index, room.Variants.Count())) {
		return 0, false
	}
	result := reduceVariants(room, dominanceMaxConfigs)
	if len(result.Removed) == 0 {
		return 0, true
	}
	if info != nil && !info(fmt.Sprintf("dominance scan room %d: remove %d variants (%d states)",
		room.Index, len(result.Removed), len(result.RemovedStates))) {
		return 0, false
	}

	used := make([]bool, room.Variants.Count())
	for _, vid := range result.Kept {
		used[vid] = true
	}
	renewVariants(room, used)
	removeUnusedStates(room)
	return uint64(len(result.Removed)), true
}
