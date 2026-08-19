package rooms

import (
	"fmt"
	"slices"
	"time"
)

// Kandidaten-Finder der Dominanzsuche (M4b, siehe docs/konzept.md): findet
// für einen Ein-Portal-Raum automatisch eine reduzierte Varianten-Menge,
// die ALLE Außenwörter kostengleich bedient (bewiesen über
// compareUsageGraphs, keine Horizont-Evidenz).
//
// Verfahren: Greedy-Elimination per Gruppen-Test (divide & conquer). Eine
// Gruppe wird probeweise komplett gestrichen; bleibt der Vergleich gegen den
// vollen Graphen kostengleich, ist die Streichung dauerhaft, sonst wird die
// Gruppe geteilt und beide Hälften einzeln probiert - die Blätter sind
// Einzeltests. Das nutzt eine Asymmetrie des Vergleichs: Differenz-Antworten
// sind billig (Abbruch am ersten Gegenbeispiel-Wort), Kostengleichheit ist
// teuer, räumt dafür aber ganze Gruppen auf einen Schlag ab. Grosse Räume
// (5018er Ziel-Trakt: 42887 Varianten) brauchen so nur Hunderte statt
// Zigtausende Vergleiche.
//
// Korrektheit der Greedy-Streichung: die Kosten sind monoton in der
// Varianten-Menge - weniger Varianten können nie billiger bedienen, nur
// teurer (jede Nutzung der kleineren Menge existiert auch in der größeren).
// War eine Gruppe aus der Menge S nicht streichbar (Differenz), dann erst
// recht nicht aus einer späteren Teilmenge S' - die Differenz kann nur
// wachsen. Jede dauerhafte Streichung ist zum Zeitpunkt der Entscheidung
// gegen den vollen Graphen bewiesen und bleibt es. Weil jede behaltene
// Variante irgendwann als Einzeltest durchfällt, ist das Ergebnis lokal
// minimal: KEINE einzelne verbleibende Variante ist noch streichbar.
// (Ausnahme: läuft das Zeitbudget ab, bleiben ungeprüfte Kandidaten
// konservativ erhalten - das bis dahin Bewiesene ist trotzdem gültig.)
//
// Gestrichen wird in zwei Phasen:
//   1. ganze ZUSTÄNDE (Einheit = alle Varianten, die den Zustand als
//      OldState oder NewState berühren). Zustände sind die teure Größe -
//      sie gehen beim Mergen multiplikativ ins Kreuzprodukt ein - und die
//      Phase entschärft das Überdeckungsproblem: reine Varianten-Elimination
//      in ID-Reihenfolge streicht sonst früh die "richtigen" Varianten und
//      hält dafür ganze Gleichstands-Familien am Leben (202er-Kammer:
//      9 Varianten auf 8 Zuständen statt 7 auf 5).
//   2. einzelne Varianten auf dem Rest.
// Das Ergebnis hängt bei Gleichstands-Alternativen von der Probier-
// Reihenfolge ab (welche von zwei austauschbaren fällt); beide Phasen
// laufen in aufsteigender ID-Reihenfolge, das Ergebnis ist deterministisch.
// Die Minimalität der GRÖSSE ist nicht garantiert (Überdeckungsproblem),
// nur die lokale. Praktisch trifft das Zwei-Phasen-Greedy die Handrechnung
// (202er-Kammer: exakt die 7 Varianten auf 5 Zuständen).

// Ergebnis einer Greedy-Reduktion
type ReduceResult struct {
	Kept          []uint64 // verbleibende Varianten (bewiesen ausreichend)
	Removed       []uint64 // gestrichene Varianten (bewiesen entbehrlich)
	Undecided     []uint64 // konservativ behalten (Vergleichs-Limit erreicht)
	RemovedStates []uint64 // in Phase 1 komplett eliminierte Zustände
	TimedOut      bool     // Zeitbudget abgelaufen (Rest ungeprüft behalten)
	Aborted       bool     // Nutzer-Abbruch (Rest ungeprüft behalten)
	Detail        string   // Beschreibung des Abschluss-Beweises
}

// prüft, ob der Finder auf diesen Raum anwendbar ist
func canReduceVariants(room *Room) bool {
	return len(room.Incoming) == 1 && room.StartVariantCount == 0
}

// Arbeitszustand einer laufenden Reduktion
type reducer struct {
	room       *Room
	full       *usageGraph
	removed    map[uint64]bool
	maxConfigs int
	deadline   time.Time // Null-Wert = kein Budget
	timedOut   bool
	aborted    bool // Nutzer-Abbruch über den info-Callback
	info       ProgressFunc
	tested     int
	result     ReduceResult
}

func (r *reducer) expired() bool {
	if !r.timedOut && !r.deadline.IsZero() && time.Now().After(r.deadline) {
		r.timedOut = true
	}
	return r.timedOut || r.aborted
}

// meldet regelmäßig den Zwischenstand; false vom Callback = Nutzer-Stop
// (bereits bewiesene Streichungen werden trotzdem angewandt)
func (r *reducer) report() {
	if r.info == nil || r.tested&63 != 0 {
		return
	}
	if !r.info(fmt.Sprintf("dominance room %d: %d tests, %d variants removed (%d states)",
		r.room.Index, r.tested, len(r.result.Removed), len(r.result.RemovedStates)), []*Room{r.room}) {
		r.aborted = true
	}
}

// testet, ob die Varianten-Gruppe komplett entbehrlich ist; bei Gleichheit
// bleibt sie dauerhaft gestrichen (und wird gebucht), sonst rollt sie zurück
func (r *reducer) tryRemove(batch []uint64) usageVerdict {
	r.tested++
	r.report()
	for _, vid := range batch {
		r.removed[vid] = true
	}
	candidate := buildUsageGraph(r.room, func(id uint64) bool { return !r.removed[id] })
	verdict, _ := compareUsageGraphs(r.full, candidate, r.maxConfigs)
	if verdict == usageEqual {
		r.result.Removed = append(r.result.Removed, batch...)
		return verdict
	}
	for _, vid := range batch {
		delete(r.removed, vid)
	}
	return verdict
}

// Gruppen-Test über Varianten (Phase 2)
func (r *reducer) shrinkVariants(batch []uint64) {
	if len(batch) == 0 || r.expired() {
		return
	}
	verdict := r.tryRemove(batch)
	if verdict == usageEqual {
		return
	}
	if len(batch) == 1 {
		if verdict == usageUndecided {
			r.result.Undecided = append(r.result.Undecided, batch[0])
		}
		return
	}
	mid := len(batch) / 2
	r.shrinkVariants(batch[:mid])
	r.shrinkVariants(batch[mid:])
}

// alle noch vorhandenen Varianten, die einen Zustand der Gruppe berühren
func (r *reducer) touching(states []uint64) []uint64 {
	inGroup := map[uint64]bool{}
	for _, s := range states {
		inGroup[s] = true
	}
	var batch []uint64
	for vid := uint64(0); vid < r.room.Variants.Count(); vid++ {
		v := r.room.Variants.Get(vid)
		if !r.removed[vid] && (inGroup[v.OldState] || inGroup[v.NewState]) {
			batch = append(batch, vid)
		}
	}
	return batch
}

// Gruppen-Test über Zustände (Phase 1)
func (r *reducer) shrinkStates(states []uint64) {
	if len(states) == 0 || r.expired() {
		return
	}
	batch := r.touching(states)
	if len(batch) == 0 {
		return // verwaiste Zustände räumt removeUnusedStates
	}
	verdict := r.tryRemove(batch)
	if verdict == usageEqual {
		r.result.RemovedStates = append(r.result.RemovedStates, states...)
		return
	}
	if len(states) == 1 {
		return // Zustand bleibt; seine Varianten prüft Phase 2 einzeln
	}
	mid := len(states) / 2
	r.shrinkStates(states[:mid])
	r.shrinkStates(states[mid:])
}

// reduziert die Varianten-Menge eines Ein-Portal-Raums per Greedy-Elimination.
// maxConfigs begrenzt jeden Einzelvergleich (Sicherheitsnetz gegen
// divergierende Loop-Raten); unentscheidbare Kandidaten bleiben erhalten.
// budget > 0 begrenzt die Gesamtzeit: bei Ablauf bleibt der ungeprüfte Rest
// konservativ erhalten, das bis dahin Bewiesene ist gültig (TimedOut = true).
// Der Raum selbst wird NICHT verändert - das Ergebnis beschreibt nur,
// welche Varianten entbehrlich sind.
func reduceVariants(room *Room, maxConfigs int, budget time.Duration, info ProgressFunc) ReduceResult {
	if !canReduceVariants(room) {
		panic("reduceVariants: nur Ein-Portal-Räume ohne Startvarianten")
	}

	full := buildUsageGraph(room, nil)
	if full.start < 0 {
		// Der Raum hat im Nutzungs-Modell KEINE akzeptierende Nutzung (der
		// gelöste Zustand ist unerreichbar). Der Vergleich würde dann jede
		// Streichung als "kostengleich leer" durchwinken und den Raum
		// komplett leeren - stattdessen: Finger weg und melden. Entweder ist
		// das Level ab hier wirklich unlösbar, oder das Modell hat ein Loch;
		// beides muss sichtbar bleiben statt stillschweigend aufgeräumt.
		return ReduceResult{Detail: "keine akzeptierende Nutzung - Raum unangetastet (Level ab hier unlösbar?)"}
	}
	r := &reducer{
		room:       room,
		full:       full,
		removed:    map[uint64]bool{},
		maxConfigs: maxConfigs,
		info:       info,
	}
	if budget > 0 {
		r.deadline = time.Now().Add(budget)
	}

	// Phase 1: ganze Zustände eliminieren (0 = gelöst und der Startzustand
	// bleiben immer)
	var states []uint64
	for state := uint64(1); state < room.States.Count(); state++ {
		if state != room.StartState {
			states = append(states, state)
		}
	}
	r.shrinkStates(states)

	// Phase 2: die verbleibenden Varianten
	var rest []uint64
	for vid := uint64(0); vid < room.Variants.Count(); vid++ {
		if !r.removed[vid] {
			rest = append(rest, vid)
		}
	}
	r.shrinkVariants(rest)

	result := r.result
	result.TimedOut = r.timedOut
	result.Aborted = r.aborted
	slices.Sort(result.Removed)
	slices.Sort(result.RemovedStates)
	for vid := uint64(0); vid < room.Variants.Count(); vid++ {
		if !r.removed[vid] {
			result.Kept = append(result.Kept, vid)
		}
	}

	// Abschluss-Beweis der Gesamtmenge (redundant zur Induktion der
	// Einzelschritte, aber billig und ein guter Wächter)
	final := buildUsageGraph(room, func(id uint64) bool { return !r.removed[id] })
	verdict, detail := compareUsageGraphs(r.full, final, maxConfigs)
	if verdict == usageDiffers {
		panic("reduceVariants: Abschluss-Beweis fehlgeschlagen: " + detail)
	}
	result.Detail = detail
	return result
}

// Sicherheitslimit je Einzelvergleich der Dominanzsuche (Vergleichs-
// Situationen; real sättigen die Räume nach wenigen hundert)
const dominanceMaxConfigs = 100000

// Zeitbudget der Dominanzsuche je Raum am Optimize-Button; bei Ablauf wird
// das bis dahin Bewiesene angewandt (erneutes Drücken macht dort weiter -
// die inkrementelle Endlos-Funktion aus dem Konzept, Kapitel M4b)
const dominanceBudget = 30 * time.Second

// DominanceReduce führt die Dominanzsuche auf einem Raum aus und entfernt
// bewiesen entbehrliche Varianten samt dabei verwaisender Zustände. Nicht
// anwendbare Räume (mehr als ein Portal, Startvarianten) bleiben unberührt.
// info (optional) bekommt Fortschritts-Meldungen; Rückgabe false bricht ab,
// der Raum bleibt dann unverändert.
func (n *Network) DominanceReduce(room *Room, info ProgressFunc) (removed uint64, ok bool) {
	if !canReduceVariants(room) || room.Variants.Count() == 0 {
		return 0, true
	}
	if info != nil && !info(fmt.Sprintf("dominance scan room %d: %d variants", room.Index, room.Variants.Count()), []*Room{room}) {
		return 0, false
	}
	result := reduceVariants(room, dominanceMaxConfigs, dominanceBudget, info)
	if len(result.Removed) == 0 {
		return 0, !result.Aborted
	}
	if info != nil {
		// reine Ergebnis-Meldung: bewiesene Streichungen werden auch nach
		// einem Nutzer-Stop angewandt (Konzept M4b), Rückgabewert egal
		info(fmt.Sprintf("dominance scan room %d: remove %d variants (%d states, timeout=%v)",
			room.Index, len(result.Removed), len(result.RemovedStates), result.TimedOut), []*Room{room})
	}

	used := make([]bool, room.Variants.Count())
	for _, vid := range result.Kept {
		used[vid] = true
	}
	renewVariants(room, used)
	removeUnusedStates(room)
	return uint64(len(result.Removed)), !result.Aborted
}
