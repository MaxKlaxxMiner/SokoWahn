package rooms

import (
	"encoding/binary"
	"fmt"
	"sort"
)

// Graph-Sicht auf die Nutzungen eines Ein-Portal-Raums (M4b, siehe docs/konzept.md).
//
// Das Dominanz-Labor (domlab_test.go) enumeriert Nutzungs-Ketten nur bis zu
// einem festen Horizont - das liefert Evidenz, aber keinen Beweis, weil eine
// Nutzung beliebig lang werden kann (Garagen-Loops). Die Lösung ist Max' Loop-
// Idee (Präfix + N x Loop + Abschluss) in Graph-Form: der Nutzungsraum ist ein
// ENDLICHER Graph, und was sich bei langen Nutzungen wiederholt, sind Zyklen
// darin. Damit wird die Frage "bleibt eine reduzierte Varianten-Menge für ALLE
// Außenwörter kostengleich?" endlich entscheidbar (compareUsageGraphs).
//
// Modell (identisch zum Labor):
//   - Knoten = (Raum-Zustand, blocked). blocked = der letzte Schritt war ein
//     exportloser Besuch; die bewiesene Selbes-Portal-Regel verbietet dann den
//     direkt nächsten Besuch (fusionierbar = dominiert), Einschub geht immer.
//   - Kanten = Ereignisse mit Kosten (moves, pushes; lexikographisch):
//       'E' = Einschub durch die Außenwelt (BoxSwap, kostenlos für den Raum)
//       'X' = Besuch mit Kisten-Export
//        0  = exportloser Besuch (B): außen UNSICHTBAR (B-Transparenz-Theorem,
//             Max 2026-08-18) - im Automaten eine Epsilon-Kante
//   - End-Kanten = End-Varianten (Spieler bleibt drin, Zustand 0): Terminal
//     "!" bzw. "X!" (mit Kisten-Export beim letzten Besuch)
//   - Akzeptanz = die Kammer wird im Zustand 0 hinterlassen (Wort ohne "!")
//     oder eine End-Kante wird genommen (Wort mit "!").
// Das Außenwort einer Nutzung ist die Folge ihrer sichtbaren Ereignisse -
// exakt die Signatur des Labors.

// Kostenpaar (moves, pushes), lexikographisch verglichen; als int64, weil
// Konfigurations-Offsets nach der Normalisierung negative pushes haben können
type usageCost struct{ moves, pushes int64 }

func (c usageCost) add(o usageCost) usageCost {
	return usageCost{c.moves + o.moves, c.pushes + o.pushes}
}

func (c usageCost) sub(o usageCost) usageCost {
	return usageCost{c.moves - o.moves, c.pushes - o.pushes}
}

func (c usageCost) less(o usageCost) bool {
	return c.moves < o.moves || (c.moves == o.moves && c.pushes < o.pushes)
}

func (c usageCost) String() string {
	return fmt.Sprintf("%d/%d", c.moves, c.pushes)
}

const (
	usageInvisible = byte(0)   // exportloser Besuch (B), außen unsichtbar
	usageInsert    = byte('E') // Einschub durch die Außenwelt
	usageExport    = byte('X') // Besuch mit Kisten-Export
)

const usageNoVariant = ^uint64(0) // Einschub-Kanten benutzen keine Variante

type usageNodeKey struct {
	state   uint64
	blocked bool
}

type usageEdge struct {
	to      int
	label   byte // usageInsert / usageExport / usageInvisible
	cost    usageCost
	variant uint64 // benutzte Variante (usageNoVariant bei Einschub)
}

type usageEnd struct {
	label   string // "!" oder "X!"
	cost    usageCost
	variant uint64
}

type usageGraph struct {
	room  *Room
	nodes []usageNodeKey
	index map[usageNodeKey]int
	edges [][]usageEdge
	ends  [][]usageEnd
	start int // -1 = der Raum hat keine einzige akzeptierende Nutzung

	// Kanten nach Label vorsortiert (für die heiße Konfigurations-Rechnung)
	epsEdges [][]usageEdge // exportlose Besuche (unsichtbar)
	insEdges [][]usageEdge // Einschübe 'E'
	expEdges [][]usageEdge // Export-Besuche 'X'
}

// leitet die Label-sortierten Kanten-Listen aus edges ab (nach dem Pruning)
func (g *usageGraph) buildLabelIndex() {
	n := len(g.nodes)
	g.epsEdges = make([][]usageEdge, n)
	g.insEdges = make([][]usageEdge, n)
	g.expEdges = make([][]usageEdge, n)
	for id := range g.nodes {
		for _, e := range g.edges[id] {
			switch e.label {
			case usageInvisible:
				g.epsEdges[id] = append(g.epsEdges[id], e)
			case usageInsert:
				g.insEdges[id] = append(g.insEdges[id], e)
			default:
				g.expEdges[id] = append(g.expEdges[id], e)
			}
		}
	}
}

// baut den Nutzungs-Graphen eines Ein-Portal-Raums; allowed (optional)
// schränkt die nutzbaren Varianten ein (nil = alle). Startvarianten werden
// wie im Labor ignoriert (der Spieler kommt von außen). Knoten, von denen
// keine Akzeptanz mehr erreichbar ist, werden entfernt - dadurch bedeutet
// "Konfiguration nicht leer" im Vergleich immer "es gibt noch akzeptierende
// Fortsetzungen".
func buildUsageGraph(room *Room, allowed func(id uint64) bool) *usageGraph {
	if len(room.Incoming) != 1 {
		panic(fmt.Sprintf("buildUsageGraph: Raum hat %d Portale, unterstützt ist genau 1", len(room.Incoming)))
	}
	ip := room.Incoming[0]

	g := &usageGraph{room: room, index: map[usageNodeKey]int{}}
	nodeID := func(key usageNodeKey) int {
		if id, exists := g.index[key]; exists {
			return id
		}
		id := len(g.nodes)
		g.index[key] = id
		g.nodes = append(g.nodes, key)
		g.edges = append(g.edges, nil)
		g.ends = append(g.ends, nil)
		return id
	}

	startKey := usageNodeKey{state: room.StartState}
	g.start = nodeID(startKey)
	for queue := []usageNodeKey{startKey}; len(queue) > 0; queue = queue[1:] {
		key := queue[0]
		id := g.index[key]
		addEdge := func(to usageNodeKey, e usageEdge) {
			if _, exists := g.index[to]; !exists {
				queue = append(queue, to)
			}
			e.to = nodeID(to)
			g.edges[id] = append(g.edges[id], e)
		}

		// Einschub durch die Außenwelt (löst die Besuchs-Sperre)
		if next, exists := ip.BoxSwap[key.state]; exists {
			addEdge(usageNodeKey{state: next},
				usageEdge{label: usageInsert, variant: usageNoVariant})
		}

		// Besuche (Selbes-Portal-Regel: nach exportlosem Besuch gesperrt)
		if key.blocked {
			continue
		}
		span := ip.GetVariantSpan(key.state)
		for vid := span.Start; vid < span.Start+span.Count; vid++ {
			if allowed != nil && !allowed(vid) {
				continue
			}
			v := room.Variants.Get(vid)
			cost := usageCost{int64(v.Moves), int64(v.Pushes)}
			exported := len(v.BoxPortals) > 0
			if v.PlayerPortal == NoPortal {
				if v.NewState == 0 { // Spielende nur im gelösten Zustand
					label := "!"
					if exported {
						label = "X!"
					}
					g.ends[id] = append(g.ends[id], usageEnd{label: label, cost: cost, variant: vid})
				}
				continue
			}
			label := usageInvisible
			if exported {
				label = usageExport
			}
			addEdge(usageNodeKey{state: v.NewState, blocked: !exported},
				usageEdge{label: label, cost: cost, variant: vid})
		}
	}

	g.pruneNonAccepting()
	g.buildLabelIndex()
	return g
}

// entfernt alle Knoten, von denen keine Akzeptanz (Zustand 0 oder End-Kante)
// mehr erreichbar ist, und indiziert den Graphen neu
func (g *usageGraph) pruneNonAccepting() {
	// Rückwärts-Kanten sammeln, akzeptierende Knoten als Saat
	reverse := make([][]int, len(g.nodes))
	var queue []int
	keep := make([]bool, len(g.nodes))
	for id := range g.nodes {
		for _, e := range g.edges[id] {
			reverse[e.to] = append(reverse[e.to], id)
		}
		if g.nodes[id].state == 0 || len(g.ends[id]) > 0 {
			keep[id] = true
			queue = append(queue, id)
		}
	}
	for len(queue) > 0 {
		id := queue[0]
		queue = queue[1:]
		for _, from := range reverse[id] {
			if !keep[from] {
				keep[from] = true
				queue = append(queue, from)
			}
		}
	}

	remap := make([]int, len(g.nodes))
	var nodes []usageNodeKey
	var edges [][]usageEdge
	var ends [][]usageEnd
	index := map[usageNodeKey]int{}
	for id, key := range g.nodes {
		remap[id] = -1
		if keep[id] {
			remap[id] = len(nodes)
			index[key] = len(nodes)
			nodes = append(nodes, key)
			ends = append(ends, g.ends[id])
			edges = append(edges, nil)
		}
	}
	for id := range g.nodes {
		if !keep[id] {
			continue
		}
		for _, e := range g.edges[id] {
			if keep[e.to] {
				e.to = remap[e.to]
				edges[remap[id]] = append(edges[remap[id]], e)
			}
		}
	}
	start := -1
	if keep[g.start] {
		start = remap[g.start]
	}
	g.nodes, g.edges, g.ends, g.index, g.start = nodes, edges, ends, index, start
}

func (g *usageGraph) nodeName(id int) string {
	key := g.nodes[id]
	name := fmt.Sprintf("s%d", key.state)
	if key.blocked {
		name += "*" // Besuchs-Sperre aktiv (Selbes-Portal-Regel)
	}
	return name
}

// ---------------------------------------------------------------------------
// Zyklen (die "Loops" aus Max' Präfix + N x Loop + Abschluss-Struktur)

type usageLoop struct {
	word     string // sichtbares Außenwort des Zyklus (z.B. "EX")
	cost     usageCost
	nodes    []int    // Knoten in Durchlauf-Reihenfolge (erster = kleinster Index)
	variants []uint64 // benutzte Varianten (ohne Einschübe)
}

// enumeriert alle einfachen Zyklen (vereinfachter Johnson: nur Zyklen, deren
// kleinster Knoten der Anker ist). Die Graphen sind winzig (Zustände x 2),
// maxLoops ist nur ein Sicherheitsnetz. Reine Epsilon-Zyklen kann es nicht
// geben (ein B-Besuch sperrt den nächsten Besuch, entsperrt wird nur durch
// einen sichtbaren Einschub) - wird per Panic abgesichert.
func (g *usageGraph) enumerateLoops(maxLoops int) []usageLoop {
	var loops []usageLoop
	var nodePath []int
	var edgePath []usageEdge
	var walk func(id int)

	for anchor := range g.nodes {
		onPath := make([]bool, len(g.nodes))
		walk = func(id int) {
			for _, e := range g.edges[id] {
				if e.to < anchor || len(loops) >= maxLoops {
					continue
				}
				if e.to == anchor {
					// Zyklus komplett: Kanten des Pfads plus die Schluss-Kante
					loop := usageLoop{nodes: append([]int{anchor}, nodePath...)}
					for _, edge := range append(append([]usageEdge{}, edgePath...), e) {
						loop.cost = loop.cost.add(edge.cost)
						if edge.label != usageInvisible {
							loop.word += string(edge.label)
						}
						if edge.variant != usageNoVariant {
							loop.variants = append(loop.variants, edge.variant)
						}
					}
					if loop.word == "" {
						panic("usageGraph: unsichtbarer Zyklus gefunden (darf es nicht geben)")
					}
					loops = append(loops, loop)
					continue
				}
				if onPath[e.to] {
					continue
				}
				onPath[e.to] = true
				nodePath = append(nodePath, e.to)
				edgePath = append(edgePath, e)
				walk(e.to)
				nodePath = nodePath[:len(nodePath)-1]
				edgePath = edgePath[:len(edgePath)-1]
				onPath[e.to] = false
			}
		}
		onPath[anchor] = true
		walk(anchor)
	}
	return loops
}

// ---------------------------------------------------------------------------
// Konfigurationen: "wo könnte eine Optimal-Nutzung nach diesem Wort-Präfix
// stehen, und was hat sie mindestens gekostet" - eine min-plus-Potenzmenge.
// Kompakte Form: nach Knoten sortierte (Knoten, Kosten)-Paare; gerechnet
// wird in einem wiederverwendeten Workspace (dichte Arrays mit Versions-
// Zähler statt Maps - der Vergleich läuft in der Dominanzsuche tausendfach).

type usageEntry struct {
	node int
	cost usageCost
}

type usageConfig []usageEntry

// Arbeitsflächen für einen Konfigurations-Schritt (je Graph-Seite eine)
type usageWorkspace struct {
	cost    []usageCost
	version []uint32
	current uint32
	active  []int // erreichte Knoten (kann Duplikate nach Verbesserungen enthalten)
}

func (ws *usageWorkspace) reset(n int) {
	if len(ws.cost) < n {
		ws.cost = make([]usageCost, n)
		ws.version = make([]uint32, n)
		ws.current = 0
	}
	ws.current++
	ws.active = ws.active[:0]
}

// nimmt einen Kandidaten auf, wenn er billiger ist als das bisher Bekannte
func (ws *usageWorkspace) relax(node int, c usageCost) (improved, isNew bool) {
	if ws.version[node] != ws.current {
		ws.version[node] = ws.current
		ws.cost[node] = c
		ws.active = append(ws.active, node)
		return true, true
	}
	if c.less(ws.cost[node]) {
		ws.cost[node] = c
		return true, false
	}
	return false, false
}

// Epsilon-Abschluss über die Worklist (exportlose Besuche sind außen
// unsichtbar, eine Nutzung kann beliebig viele einstreuen; Kosten sind
// positiv und Epsilon-Zyklen gibt es nicht) und Verdichtung des Ergebnisses
func (g *usageGraph) closeAndPack(ws *usageWorkspace) usageConfig {
	for i := 0; i < len(ws.active); i++ {
		node := ws.active[i]
		cost := ws.cost[node]
		for _, e := range g.epsEdges[node] {
			if improved, isNew := ws.relax(e.to, cost.add(e.cost)); improved && !isNew {
				ws.active = append(ws.active, e.to) // mit besseren Kosten erneut expandieren
			}
		}
	}
	sort.Ints(ws.active)
	result := make(usageConfig, 0, len(ws.active))
	prev := -1
	for _, node := range ws.active {
		if node != prev {
			result = append(result, usageEntry{node: node, cost: ws.cost[node]})
			prev = node
		}
	}
	return result
}

// Start-Konfiguration (Epsilon-abgeschlossen)
func (g *usageGraph) startConfig(ws *usageWorkspace) usageConfig {
	ws.reset(len(g.nodes))
	ws.relax(g.start, usageCost{})
	return g.closeAndPack(ws)
}

// ein sichtbares Zeichen konsumieren, inklusive Epsilon-Abschluss danach
func (g *usageGraph) stepClose(src usageConfig, label byte, ws *usageWorkspace) usageConfig {
	ws.reset(len(g.nodes))
	edges := g.insEdges
	if label == usageExport {
		edges = g.expEdges
	}
	for _, entry := range src {
		for _, e := range edges[entry.node] {
			ws.relax(e.to, entry.cost.add(e.cost))
		}
	}
	return g.closeAndPack(ws)
}

// Akzeptanzkosten einer Konfiguration je Terminal-Art:
//
//	""   = Kammer im Zustand 0 hinterlassen, Spieler draußen
//	"!"  = End-Variante ohne Export, "X!" = End-Variante mit Export
type usageAccept struct {
	out, end, endX          usageCost
	hasOut, hasEnd, hasEndX bool
}

func (g *usageGraph) accept(c usageConfig) usageAccept {
	var a usageAccept
	for _, entry := range c {
		if g.nodes[entry.node].state == 0 && (!a.hasOut || entry.cost.less(a.out)) {
			a.out, a.hasOut = entry.cost, true
		}
		if g.nodes[entry.node].blocked {
			continue // End-Variante ist ein Besuch (Selbes-Portal-Regel)
		}
		for _, end := range g.ends[entry.node] {
			cost := entry.cost.add(end.cost)
			if end.label == "!" {
				if !a.hasEnd || cost.less(a.end) {
					a.end, a.hasEnd = cost, true
				}
			} else {
				if !a.hasEndX || cost.less(a.endX) {
					a.endX, a.hasEndX = cost, true
				}
			}
		}
	}
	return a
}

// Anmerkung zur Out-Akzeptanz: das nackte Startwort "" würde bei
// StartState == 0 fälschlich mit Kosten 0 akzeptiert (das Labor verlangt
// mindestens eine benutzte Variante). Für den Vergleich zweier Graphen ist
// das harmlos (beide Seiten teilen den Fehler), die Kreuzvalidierung
// filtert den Fall.

// ---------------------------------------------------------------------------
// Signatur-Enumeration bis fester Ereigniszahl (Kreuzvalidierung gegen das
// Labor: muss exakt enumerateUsages entsprechen)

func (g *usageGraph) signatures(maxEvents int) map[string]usageCost {
	result := map[string]usageCost{}
	if g.start < 0 {
		return result
	}
	takeMin := func(sig string, cost usageCost) {
		old, exists := result[sig]
		if !exists || cost.less(old) {
			result[sig] = cost
		}
	}

	type item struct {
		word string
		cfg  usageConfig
	}
	ws := &usageWorkspace{}
	queue := []item{{word: "", cfg: g.startConfig(ws)}}

	for len(queue) > 0 {
		it := queue[0]
		queue = queue[1:]
		a := g.accept(it.cfg)
		if a.hasOut && !(it.word == "" && a.out == (usageCost{})) {
			takeMin(it.word, a.out) // nacktes Startwort zählt nicht als Nutzung
		}
		if a.hasEnd {
			takeMin(it.word+"!", a.end)
		}
		if a.hasEndX {
			takeMin(it.word+"X!", a.endX)
		}
		if len(it.word) >= maxEvents {
			continue
		}
		for _, label := range []byte{usageInsert, usageExport} {
			next := g.stepClose(it.cfg, label, ws)
			if len(next) == 0 {
				continue
			}
			queue = append(queue, item{word: it.word + string(label), cfg: next})
		}
	}
	return result
}

// ---------------------------------------------------------------------------
// Der Beweis-Vergleich: bleibt "reduced" für JEDES Außenwort kostengleich
// zu "full"? Synchrone Breitensuche über Konfigurations-PAARE mit Offset-
// Normalisierung und Memoisierung:
//
//   - Beide Automaten lesen dasselbe Wort; verglichen werden nur die
//     Akzeptanzkosten je Terminal-Art.
//   - Von jedem Konfigurations-Paar wird das gemeinsame Kosten-Minimum
//     abgezogen (Normalisierung). Die normalisierte Form beschreibt die
//     Vergleichs-SITUATION vollständig: jede Fortsetzung hängt nur noch
//     von den relativen Offsets ab, nicht von der absoluten Vergangenheit.
//   - Wiederholt sich eine normalisierte Situation, ist alles Weitere dort
//     eine exakte Wiederholung - der Zweig ist abgehandelt. Das ist Max'
//     Loop-Idee auf Vergleichs-Ebene: die Wiederholung der Situation IST der
//     Loop, und die Memoisierung ersetzt die Induktion über N Durchläufe.
//   - Terminiert die Suche ohne Differenz, gilt die Kostengleichheit für
//     ALLE Wortlängen - ein Beweis, kein Horizont mehr.
//
// Sättigung ist garantiert, wenn die Offsets beschränkt bleiben; das ist
// bei Kostengleichheit der konkurrierenden Loops der Fall (gleiche Raten).
// Laufen die Offsets davon (verschiedene Loop-Raten, aber noch keine
// Differenz gefunden), greift das Sicherheitslimit -> usageUndecided.

type usageVerdict int

const (
	usageEqual     usageVerdict = iota // bewiesen kostengleich für alle Wörter
	usageDiffers                       // Gegenbeispiel gefunden
	usageUndecided                     // Sicherheitslimit erreicht
)

func (v usageVerdict) String() string {
	switch v {
	case usageEqual:
		return "kostengleich (bewiesen für alle Nutzungslängen)"
	case usageDiffers:
		return "Differenz gefunden"
	default:
		return "unentschieden (Limit erreicht)"
	}
}

// Fingerprint einer normalisierten Vergleichs-Situation (binär, in einen
// wiederverwendeten Buffer)
func usageFingerprint(full, reduced usageConfig, buf []byte) (string, []byte) {
	buf = buf[:0]
	writeSide := func(c usageConfig) {
		for _, entry := range c {
			buf = binary.AppendVarint(buf, int64(entry.node))
			buf = binary.AppendVarint(buf, entry.cost.moves)
			buf = binary.AppendVarint(buf, entry.cost.pushes)
		}
	}
	writeSide(full)
	buf = append(buf, 0xff)
	writeSide(reduced)
	return string(buf), buf
}

// zieht das gemeinsame (lexikographisch) minimale Kostenpaar beider Seiten ab
func usageNormalize(full, reduced usageConfig) usageCost {
	first := true
	var min usageCost
	for _, c := range []usageConfig{full, reduced} {
		for _, entry := range c {
			if first || entry.cost.less(min) {
				min = entry.cost
				first = false
			}
		}
	}
	for i := range full {
		full[i].cost = full[i].cost.sub(min)
	}
	for i := range reduced {
		reduced[i].cost = reduced[i].cost.sub(min)
	}
	return min
}

// vergleicht die Optimal-Kosten beider Graphen über ALLE Außenwörter.
// Bei usageDiffers beschreibt detail das Gegenbeispiel (Wort + Kosten).
func compareUsageGraphs(full, reduced *usageGraph, maxConfigs int) (usageVerdict, string) {
	if full.start < 0 && reduced.start < 0 {
		return usageEqual, "beide Räume ohne akzeptierende Nutzung"
	}
	if full.start < 0 || reduced.start < 0 {
		return usageDiffers, "nur eine Seite hat akzeptierende Nutzungen"
	}

	type item struct {
		word    string
		base    usageCost // Summe der abnormalisierten Minima (für Meldungen)
		full    usageConfig
		reduced usageConfig
	}

	wsFull, wsReduced := &usageWorkspace{}, &usageWorkspace{}
	var buf []byte
	seen := map[string]bool{}
	queue := []item{{full: full.startConfig(wsFull), reduced: reduced.startConfig(wsReduced)}}
	queue[0].base = usageNormalize(queue[0].full, queue[0].reduced)
	fp, buf := usageFingerprint(queue[0].full, queue[0].reduced, buf)
	seen[fp] = true

	for len(queue) > 0 {
		it := queue[0]
		queue = queue[1:]

		// Akzeptanzen vergleichen (reduced ist bei uns eine Teilmenge der
		// Kanten, kann also nie billiger sein - geprüft wird trotzdem
		// symmetrisch, der Vergleich ist allgemein)
		fa := full.accept(it.full)
		ra := reduced.accept(it.reduced)
		for _, term := range [3]struct {
			label  string
			fh, rh bool
			fc, rc usageCost
		}{
			{"", fa.hasOut, ra.hasOut, fa.out, ra.out},
			{"!", fa.hasEnd, ra.hasEnd, fa.end, ra.end},
			{"X!", fa.hasEndX, ra.hasEndX, fa.endX, ra.endX},
		} {
			switch {
			case term.fh && !term.rh:
				return usageDiffers, fmt.Sprintf("Wort %q: nur voll bedienbar (Kosten %s)",
					it.word+term.label, term.fc.add(it.base))
			case !term.fh && term.rh:
				return usageDiffers, fmt.Sprintf("Wort %q: nur reduziert bedienbar", it.word+term.label)
			case term.fh && term.fc != term.rc:
				return usageDiffers, fmt.Sprintf("Wort %q: reduziert %s statt %s",
					it.word+term.label, term.rc.add(it.base), term.fc.add(it.base))
			}
		}

		// Übergänge für beide sichtbaren Zeichen
		for _, label := range []byte{usageInsert, usageExport} {
			nextFull := full.stepClose(it.full, label, wsFull)
			nextReduced := reduced.stepClose(it.reduced, label, wsReduced)
			if len(nextFull) == 0 && len(nextReduced) == 0 {
				continue // Zweig tot (dank Pruning: keine akzeptierende Fortsetzung)
			}
			if len(nextFull) == 0 || len(nextReduced) == 0 {
				side := "voll"
				if len(nextFull) == 0 {
					side = "reduziert"
				}
				return usageDiffers, fmt.Sprintf("ab Wort %q: nur %s fortsetzbar",
					it.word+string(label), side)
			}
			delta := usageNormalize(nextFull, nextReduced)
			var fp string
			fp, buf = usageFingerprint(nextFull, nextReduced, buf)
			if seen[fp] {
				continue // Situation bekannt: jede Fortsetzung wiederholt sich
			}
			seen[fp] = true
			if len(seen) > maxConfigs {
				return usageUndecided, fmt.Sprintf("mehr als %d Vergleichs-Situationen", maxConfigs)
			}
			queue = append(queue, item{
				word:    it.word + string(label),
				base:    it.base.add(delta),
				full:    nextFull,
				reduced: nextReduced,
			})
		}
	}
	return usageEqual, fmt.Sprintf("%d Vergleichs-Situationen gesättigt", len(seen))
}
