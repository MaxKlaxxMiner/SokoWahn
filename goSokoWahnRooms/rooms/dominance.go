package rooms

import (
	"fmt"
	"slices"

	"goSokoWahnRooms/tools"
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
// (Ausnahme: bricht der Nutzer per Stop ab, bleiben ungeprüfte Kandidaten
// konservativ erhalten - das bis dahin Bewiesene ist trotzdem gültig und
// wird angewandt; erneutes Optimize macht dort weiter.)
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
	Aborted       bool     // Nutzer-Abbruch (Rest ungeprüft behalten)
	Harvested     bool     // Ernte-Abbruch: genug bewiesen, erst anwenden lohnt
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
	moveLimit  int64 // > 0: Nutzungen über diesem Moves-Budget sind irrelevant
	aborted    bool // Nutzer-Abbruch über den info-Callback
	harvest    bool // Ernte-Kriterium aktiv (Runden-Betrieb von DominanceReduce)
	harvested  bool // Ernte-Kriterium erreicht (siehe tryRemove)
	info       ProgressFunc
	tested     int
	curBatch   int // Größe der gerade getesteten Gruppe (für die Statusmeldung)
	throttle   progressThrottle
	result     ReduceResult
}

// meldet regelmäßig den Zwischenstand; false vom Callback = Nutzer-Stop
// (bereits bewiesene Streichungen werden trotzdem angewandt)
func (r *reducer) report(suffix string) {
	if r.info == nil || !r.throttle.due() {
		return
	}
	if !r.info(fmt.Sprintf("dominance room %d: test %s (gruppe %s), %s variants removed (%s states)%s",
		r.room.Index, tools.FormatInt(r.tested), tools.FormatInt(r.curBatch),
		tools.FormatInt(len(r.result.Removed)), tools.FormatInt(len(r.result.RemovedStates)), suffix),
		[]*Room{r.room}) {
		r.aborted = true
	}
}

// der Haken für lange Einzelvergleiche: Statusmeldung mit Situationszahl,
// Abbruch bei Nutzer-Stop (der Vergleich endet dann als usageUndecided,
// die Gruppe bleibt konservativ erhalten)
func (r *reducer) compareTick(situations int) bool {
	r.report(fmt.Sprintf(" - vergleich läuft, %s situationen", tools.FormatInt(situations)))
	return !r.aborted
}

// testet, ob die Varianten-Gruppe komplett entbehrlich ist; bei Gleichheit
// bleibt sie dauerhaft gestrichen (und wird gebucht), sonst rollt sie zurück
func (r *reducer) tryRemove(batch []uint64) usageVerdict {
	r.tested++
	r.curBatch = len(batch)
	r.report("")
	for _, vid := range batch {
		r.removed[vid] = true
	}
	candidate := buildUsageGraph(r.room, func(id uint64) bool { return !r.removed[id] })
	verdict, _ := compareUsageGraphs(r.full, candidate, r.maxConfigs, r.moveLimit, r.compareTick)
	if verdict == usageEqual {
		r.result.Removed = append(r.result.Removed, batch...)
		// Ernte-Kriterium: ist über die Hälfte der Varianten als entbehrlich
		// bewiesen, lohnt erst das ANWENDEN - auf dem geschrumpften Raum
		// werden alle weiteren Vergleiche deutlich billiger (der Aufrufer
		// wendet an und startet eine frische Runde). Ersetzt den früheren
		// Nebeneffekt des 30s-Zeitbudgets, nur deterministisch.
		if r.harvest && len(r.result.Removed)*2 > int(r.room.Variants.Count()) {
			r.harvested = true
		}
		return verdict
	}
	for _, vid := range batch {
		delete(r.removed, vid)
	}
	return verdict
}

// Gruppen-Test über Varianten (Phase 2)
func (r *reducer) shrinkVariants(batch []uint64) {
	if len(batch) == 0 || r.aborted || r.harvested {
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
	if len(states) == 0 || r.aborted || r.harvested {
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
// Die Suche läuft bis zum Ende oder bis der info-Callback abbricht (Stop);
// beim Stop bleibt der ungeprüfte Rest konservativ erhalten, das bis dahin
// Bewiesene ist gültig (Aborted = true). harvest aktiviert das Ernte-
// Kriterium für den Runden-Betrieb (Harvested = true, sobald über die
// Hälfte der Varianten bewiesen entbehrlich ist - dann lohnt erst das
// Anwenden, siehe DominanceReduce). Der Raum selbst wird NICHT verändert -
// das Ergebnis beschreibt nur, welche Varianten entbehrlich sind.
func reduceVariants(room *Room, maxConfigs int, moveLimit int64, harvest bool, info ProgressFunc) ReduceResult {
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
		moveLimit:  moveLimit,
		harvest:    harvest,
		info:       info,
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
	result.Aborted = r.aborted
	result.Harvested = r.harvested
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
	verdict, detail := compareUsageGraphs(r.full, final, maxConfigs, moveLimit, r.compareTick)
	if verdict == usageDiffers {
		panic("reduceVariants: Abschluss-Beweis fehlgeschlagen: " + detail)
	}
	result.Detail = detail
	return result
}

// Sicherheitslimit je Einzelvergleich der Dominanzsuche (Vergleichs-
// Situationen; real sättigen die Räume nach wenigen hundert)
const dominanceMaxConfigs = 100000

// DominanceReduce führt die Dominanzsuche auf einem Raum aus und entfernt
// bewiesen entbehrliche Varianten samt dabei verwaisender Zustände. Nicht
// anwendbare Räume (mehr als ein Portal, Startvarianten) bleiben unberührt.
// moveLimit > 0 kappt Nutzungen über diesem Moves-Budget (siehe
// OptimizeRooms: Raum-Minimum + Slack aus dem globalen Max-Moves-Limit).
// info (optional) bekommt Fortschritts-Meldungen; Rückgabe false bricht ab,
// der Raum bleibt dann unverändert.
func (n *Network) DominanceReduce(room *Room, moveLimit int64, info ProgressFunc) (removed uint64, ok bool) {
	if !canReduceVariants(room) || room.Variants.Count() == 0 {
		return 0, true
	}
	if info != nil && !info(fmt.Sprintf("dominance scan room %d: %s variants", room.Index, tools.FormatInt(room.Variants.Count())), []*Room{room}) {
		return 0, false
	}
	// Runden bis zum Fixpunkt: nach einer Ernte (über die Hälfte entbehrlich)
	// wird angewandt und auf dem geschrumpften Raum frisch weitergesucht
	for {
		result := reduceVariants(room, dominanceMaxConfigs, moveLimit, true, info)
		if len(result.Removed) == 0 {
			return removed, !result.Aborted
		}
		if info != nil {
			// reine Ergebnis-Meldung: bewiesene Streichungen werden auch nach
			// einem Nutzer-Stop angewandt (Konzept M4b), Rückgabewert egal
			info(fmt.Sprintf("dominance scan room %d: remove %s variants (%s states)",
				room.Index, tools.FormatInt(len(result.Removed)), tools.FormatInt(len(result.RemovedStates))), []*Room{room})
		}

		used := make([]bool, room.Variants.Count())
		for _, vid := range result.Kept {
			used[vid] = true
		}
		renewVariants(room, used)
		removeUnusedStates(room)
		removed += uint64(len(result.Removed))
		if result.Aborted {
			return removed, false
		}
		// auch nach einer regulär beendeten Runde weitersuchen: das Anwenden
		// (renewVariants + removeUnusedStates-Kaskade) kann neue Streichungen
		// freilegen - die Schleife endet erst, wenn eine Runde leer ausgeht
	}
}
