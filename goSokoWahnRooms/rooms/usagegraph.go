package rooms

import (
	"encoding/binary"
	"fmt"
	"sort"
)

// Graph-Sicht auf die Nutzungen eines Raums (M4b, siehe docs/konzept.md).
//
// Das Dominanz-Labor (domlab_test.go) enumeriert Nutzungs-Ketten nur bis zu
// einem festen Horizont - das liefert Evidenz, aber keinen Beweis, weil eine
// Nutzung beliebig lang werden kann (Garagen-Loops). Die Lösung ist Max' Loop-
// Idee (Präfix + N x Loop + Abschluss) in Graph-Form: der Nutzungsraum ist ein
// ENDLICHER Graph, und was sich bei langen Nutzungen wiederholt, sind Zyklen
// darin. Damit wird die Frage "bleibt eine reduzierte Varianten-Menge für ALLE
// Außenwörter kostengleich?" endlich entscheidbar (compareUsageGraphs).
//
// Seit der Mehr-Portal-Erweiterung (2026-08-20) gilt das Modell für JEDEN
// Raum - beliebig viele Portale, auch Startvarianten. Die Zeichen des
// Außenworts (Symbole) tragen ihre Portale:
//   - 'E' Einschub durch Portal p (BoxSwap, kostenlos für den Raum)
//   - 'V' Besuch: Eintritts-Portal, Export-Portale in Schub-Reihenfolge,
//     Austritts-Portal (-1 = Spieler bleibt drin = End-Variante, Terminal).
//     Die Export-SEQUENZ ist sichtbar inklusive Anzahl - bei mehreren
//     Portalen ist die Verteilung der Kisten auf die Portale außen relevant.
//   - 'S' Start-Besuch (nur bei Räumen mit Startvarianten): der Spieler
//     startet im Raum, jedes Wort beginnt zwingend mit einem S-Zeichen -
//     solange er drin ist, kann draußen nichts passieren (er ist der einzige
//     Akteur).
// Die Spieler-Position nach jedem Zeichen ist damit WORTBESTIMMT (das
// Austritts-Portal steht im Zeichen). Daraus folgen die B-Regeln strukturell:
//   - Epsilon-Transparenz: ein exportloser Selbst-Besuch B(q,q) ist nur
//     DIREKT NACH einem Ereignis mit Spieler-Endposition q unsichtbar - dort
//     steht der Spieler nachweislich, und der Durchgang ist nachweislich
//     frei (nach Austritt: er kam gerade durch; nach Einschub E@q: die Kiste
//     ist drin und im Zustand verbucht). VOR einem Ereignis gibt es keine
//     Transparenz - vor einem Einschub kann die noch draußen liegende Kiste
//     die Zufahrt versperren (die 5005-Lektion, siehe docs/konzept.md).
//   - Sichtbares B: dasselbe Symbol V(q,[],q) ist als eigenes Zeichen
//     zulässig, wenn der Spieler laut Wort NICHT bei q steht ("Außenwelt
//     liefert den Spieler bei q an"). Ein-Portal-Spezialfall: nur an
//     Position 0 möglich = exakt die frühere Erste-Aktion-Regel.
// Akzeptanz = die Kammer wird im Zustand 0 hinterlassen (Spieler draußen)
// oder ein Terminal-Zeichen wird genommen.
//
// Spieler-Konnektivität (Max' Idee, 2026-08-20, usageenv.go): die Außenwelt
// zerfällt je Raum in statische Komponenten; ein Ereignis an Portal q ist
// nur legal, wenn q in der Komponente der aktuellen Spieler-Position liegt.
// Illegale Wörter fallen aus der Anforderungsmenge - reiner Filter, die
// Korrektheit hängt nicht an ihm.

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

const usageNoVariant = ^uint64(0) // Einschub-Kanten benutzen keine Variante

// ---------------------------------------------------------------------------
// Symbole: die Zeichen-Klassen des Außen-Alphabets

const (
	usageKindInsert = byte('E') // Einschub durch die Außenwelt
	usageKindVisit  = byte('V') // Besuch (Eintritt, Exporte, Austritt)
	usageKindStart  = byte('S') // Start-Besuch (Spieler startet im Raum)
)

type usageSymbolData struct {
	kind    byte
	entry   int   // Portal-Slot des Eintritts/Einschubs (-1 bei 'S')
	exit    int   // Slot des Spieler-Austritts (-1 = bleibt drin = Terminal)
	exports []int // Slots der exportierten Kisten in Schub-Reihenfolge
	eps     bool  // exportloser Selbst-Besuch (Kandidat für Epsilon-Transparenz)
	name    string
}

// Interning-Tabelle der Symbole eines Raums plus die vorberechneten
// Zuordnungen (Symbol je Variante / je Einschub-Portal) - wird einmal pro
// Raum(-Stand) gebaut und von allen Graphen (voll wie reduziert) GETEILT,
// damit die Symbol-IDs im Vergleich übereinstimmen und der tausendfache
// Graphenbau der Dominanzsuche keine String-Arbeit mehr leistet.
type usageAlphabet struct {
	portals    int
	index      map[string]int32
	list       []usageSymbolData
	variantSym []int32 // Symbol je Varianten-ID des Raums
	insertSym  []int32 // Symbol je Portal-Slot (Einschub E@p)
}

func (a *usageAlphabet) intern(kind byte, entry, exit int, exports []int) int32 {
	key := fmt.Sprintf("%c|%d|%d|%v", kind, entry, exit, exports)
	if id, exists := a.index[key]; exists {
		return id
	}
	d := usageSymbolData{kind: kind, entry: entry, exit: exit, exports: exports}
	d.eps = kind == usageKindVisit && entry == exit && len(exports) == 0
	d.name = a.symbolName(d)
	id := int32(len(a.list))
	a.index[key] = id
	a.list = append(a.list, d)
	return id
}

// Anzeige-Name eines Symbols; bei Ein-Portal-Räumen exakt die historische
// Kurzschrift (E, X, B, !, X!) - darauf sind Labor-Kreuzcheck und Log-Anker
// geeicht, Mehr-Portal-Räume bekommen die ausführliche Schreibweise
func (a *usageAlphabet) symbolName(d usageSymbolData) string {
	if a.portals == 1 {
		switch {
		case d.kind == usageKindInsert:
			return "E"
		case d.eps:
			return "B"
		}
		name := ""
		if d.kind == usageKindStart {
			name = "S"
		}
		if k := len(d.exports); k == 1 {
			name += "X"
		} else if k > 1 {
			name += fmt.Sprintf("X%d", k)
		}
		if d.exit < 0 {
			name += "!"
		}
		return name
	}
	exports := ""
	if len(d.exports) > 0 {
		exports = fmt.Sprintf("%v", d.exports)
	}
	switch {
	case d.kind == usageKindInsert:
		return fmt.Sprintf("E%d", d.entry)
	case d.kind == usageKindStart:
		if d.exit < 0 {
			return fmt.Sprintf("S%s>!", exports)
		}
		return fmt.Sprintf("S%s>%d", exports, d.exit)
	case d.eps:
		return fmt.Sprintf("B%d", d.entry)
	case d.exit < 0:
		return fmt.Sprintf("%d%s>!", d.entry, exports)
	}
	return fmt.Sprintf("%d%s>%d", d.entry, exports, d.exit)
}

// hängt ein Zeichen an ein Außenwort an (Ein-Portal klebt wie früher,
// Mehr-Portal trennt mit Leerzeichen)
func (a *usageAlphabet) join(word, name string) string {
	if a.portals > 1 && word != "" {
		return word + " " + name
	}
	return word + name
}

// Fortschritts-Haken der Aufbau-Phasen: Alphabet und Graph laufen über ALLE
// Varianten eines Raums - bei Mehr-Portal-Monstern (zig Millionen Varianten)
// dauert das Minuten und muss Status liefern und abbrechbar sein (Max'
// Befund 2026-08-20: "dominance scan room 1: 30.524.857 variants" stand
// ewig ohne Update und ohne Stop-Möglichkeit). false = Nutzer-Stop, der
// Aufbau endet sofort mit nil-Ergebnis. Die Drosselung (100 ms) übernimmt
// der Aufrufer; gerufen wird nur alle usageTickStep Schritte.
type usageTick func(phase string, done, total uint64) bool

const usageTickStep = 1 << 16

// baut die Symbol-Tabelle eines Raums (alle Varianten + alle Einschübe);
// tick (optional) meldet Fortschritt, nil bei Nutzer-Stop
func newUsageAlphabet(room *Room, tick usageTick) *usageAlphabet {
	a := &usageAlphabet{portals: len(room.Incoming), index: map[string]int32{}}
	a.insertSym = make([]int32, len(room.Incoming))
	for p := range room.Incoming {
		a.insertSym[p] = a.intern(usageKindInsert, p, p, nil)
	}
	a.variantSym = make([]int32, room.Variants.Count())
	entryOf := make([]int, room.Variants.Count())
	for i := range entryOf {
		entryOf[i] = -1 // Startvarianten haben kein Eintritts-Portal
	}
	for p, ip := range room.Incoming {
		for _, span := range ip.VariantSpans {
			for vid := span.Start; vid < span.Start+span.Count; vid++ {
				entryOf[vid] = p
			}
		}
	}
	for vid := uint64(0); vid < room.Variants.Count(); vid++ {
		if tick != nil && vid%usageTickStep == 0 && !tick("alphabet", vid, room.Variants.Count()) {
			return nil // Nutzer-Stop
		}
		v := room.Variants.Get(vid)
		var exports []int
		for _, bp := range v.BoxPortals {
			exports = append(exports, int(bp))
		}
		kind := usageKindVisit
		if vid < room.StartVariantCount {
			kind = usageKindStart
		}
		exit := -1
		if v.PlayerPortal != NoPortal {
			exit = int(v.PlayerPortal)
		}
		a.variantSym[vid] = a.intern(kind, entryOf[vid], exit, exports)
	}
	return a
}

// ---------------------------------------------------------------------------
// Der Graph

const usageUnblocked = int16(-1)

type usageNodeKey struct {
	state   uint64
	blocked int16 // gesperrter Eintritts-Slot (Selbes-Portal-Regel), -1 = frei
}

type usageEdge struct {
	to      int
	symbol  int32
	cost    usageCost
	variant uint64 // benutzte Variante (usageNoVariant bei Einschub)
}

type usageEnd struct {
	symbol  int32 // Terminal-Symbol (exit -1)
	cost    usageCost
	variant uint64
}

type usageGraph struct {
	room  *Room
	alpha *usageAlphabet
	nodes []usageNodeKey
	index map[usageNodeKey]int
	edges [][]usageEdge
	ends  [][]usageEnd
	start int // -1 = der Raum hat keine einzige akzeptierende Nutzung

	// Vorsortierung für die heiße Konfigurations-Rechnung
	bySym    [][][]usageEdge // Symbol -> Kanten je Knoten (nil-Zeilen erlaubt)
	epsEdges [][]usageEdge   // Epsilon-Kandidaten (exportlose Selbst-Besuche) je Knoten
	outSyms  [][]int32       // je Knoten: Symbole der ausgehenden Kanten (sortiert, unique)
	termSyms []int32         // alle Terminal-Symbole des Graphen (sortiert, unique)
}

// leitet die vorsortierten Zugriffs-Strukturen aus edges/ends ab (nach Pruning)
func (g *usageGraph) buildSymbolIndex() {
	n := len(g.nodes)
	symCount := len(g.alpha.list)
	g.bySym = make([][][]usageEdge, symCount)
	g.epsEdges = make([][]usageEdge, n)
	g.outSyms = make([][]int32, n)
	for id := range g.nodes {
		var syms []int32
		for _, e := range g.edges[id] {
			if g.bySym[e.symbol] == nil {
				g.bySym[e.symbol] = make([][]usageEdge, n)
			}
			g.bySym[e.symbol][id] = append(g.bySym[e.symbol][id], e)
			if g.alpha.list[e.symbol].eps {
				g.epsEdges[id] = append(g.epsEdges[id], e)
			}
			syms = append(syms, e.symbol)
		}
		sort.Slice(syms, func(i, j int) bool { return syms[i] < syms[j] })
		g.outSyms[id] = syms[:0]
		prev := int32(-1)
		for _, s := range syms {
			if s != prev {
				g.outSyms[id] = append(g.outSyms[id], s)
				prev = s
			}
		}
	}
	seen := map[int32]bool{}
	for id := range g.nodes {
		for _, end := range g.ends[id] {
			if !seen[end.symbol] {
				seen[end.symbol] = true
				g.termSyms = append(g.termSyms, end.symbol)
			}
		}
	}
	sort.Slice(g.termSyms, func(i, j int) bool { return g.termSyms[i] < g.termSyms[j] })
}

// baut den Nutzungs-Graphen eines Raums; alpha (optional) ist die geteilte
// Symbol-Tabelle (nil = eigene bauen), allowed (optional) schränkt die
// nutzbaren Varianten ein (nil = alle). Knoten, von denen keine Akzeptanz
// mehr erreichbar ist, werden entfernt - dadurch bedeutet "Konfiguration
// nicht leer" im Vergleich immer "es gibt noch akzeptierende Fortsetzungen"
// (über-approximiert: der Konnektivitäts-Filter des Vergleichs kann Zweige
// zusätzlich sperren, das kostet höchstens Reduktionen, nie Korrektheit).
func buildUsageGraph(room *Room, alpha *usageAlphabet, allowed func(id uint64) bool, tick usageTick) *usageGraph {
	if alpha == nil {
		alpha = newUsageAlphabet(room, tick)
		if alpha == nil {
			return nil // Nutzer-Stop beim Alphabet-Aufbau
		}
	}
	// grobe Fortschritts-Skala: verarbeitete Varianten-Besuche (kann durch
	// Sperr-Slots über die Gesamtzahl hinauslaufen - reine Anzeige)
	var visited uint64
	g := &usageGraph{room: room, alpha: alpha, index: map[usageNodeKey]int{}}
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

	startKey := usageNodeKey{state: room.StartState, blocked: usageUnblocked}
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
		useVariant := func(vid uint64) {
			v := room.Variants.Get(vid)
			cost := usageCost{int64(v.Moves), int64(v.Pushes)}
			sym := alpha.variantSym[vid]
			if v.PlayerPortal == NoPortal {
				if v.NewState == 0 { // Spielende nur im gelösten Zustand
					g.ends[id] = append(g.ends[id], usageEnd{symbol: sym, cost: cost, variant: vid})
				}
				return
			}
			// Selbes-Portal-Sperre: ein exportloser Besuch sperrt den direkt
			// nächsten Eintritt an seinem Austritts-Portal (fusionierbar =
			// dominiert); Exporte lösen (die Außenwelt hat dazwischen zu tun),
			// nach Startvarianten keine Sperre (Fusions-Beweis nicht geführt)
			blocked := usageUnblocked
			if alpha.list[sym].kind == usageKindVisit && len(v.BoxPortals) == 0 {
				blocked = int16(v.PlayerPortal)
			}
			addEdge(usageNodeKey{state: v.NewState, blocked: blocked},
				usageEdge{symbol: sym, cost: cost, variant: vid})
		}

		// Startvarianten (nur an unblockierten Knoten angeboten; genutzt
		// werden sie im Vergleich ohnehin nur als allererstes Zeichen)
		if key.blocked == usageUnblocked {
			for vid := uint64(0); vid < room.StartVariantCount; vid++ {
				if allowed != nil && !allowed(vid) {
					continue
				}
				if room.Variants.Get(vid).OldState != key.state {
					continue
				}
				useVariant(vid)
			}
		}

		for p, ip := range room.Incoming {
			// Einschub durch die Außenwelt (löst die Besuchs-Sperre)
			if next, exists := ip.BoxSwap[key.state]; exists {
				addEdge(usageNodeKey{state: next, blocked: usageUnblocked},
					usageEdge{symbol: alpha.insertSym[p], variant: usageNoVariant})
			}
			// Besuche über dieses Portal (Selbes-Portal-Sperre beachten)
			if key.blocked == int16(p) {
				continue
			}
			span := ip.GetVariantSpan(key.state)
			for vid := span.Start; vid < span.Start+span.Count; vid++ {
				visited++
				if tick != nil && visited%usageTickStep == 0 && !tick("graph", visited, room.Variants.Count()) {
					return nil // Nutzer-Stop
				}
				if allowed != nil && !allowed(vid) {
					continue
				}
				useVariant(vid)
			}
		}
	}

	// Aufräumen und Indizieren sind ebenfalls O(Knoten+Kanten) - vorher den
	// Stop-Wunsch prüfen und die Phase sichtbar machen
	if tick != nil && !tick("graph aufräumen", visited, room.Variants.Count()) {
		return nil
	}
	g.pruneNonAccepting()
	g.buildSymbolIndex()
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
	if key.blocked >= 0 {
		if g.alpha.portals == 1 {
			name += "*" // Besuchs-Sperre aktiv (Selbes-Portal-Regel)
		} else {
			name += fmt.Sprintf("*%d", key.blocked)
		}
	}
	return name
}

// ---------------------------------------------------------------------------
// Zyklen (die "Loops" aus Max' Präfix + N x Loop + Abschluss-Struktur)

type usageLoop struct {
	word     string // Außenwort des Zyklus aus Symbol-Namen (z.B. "EX")
	cost     usageCost
	nodes    []int    // Knoten in Durchlauf-Reihenfolge (erster = kleinster Index)
	variants []uint64 // benutzte Varianten (ohne Einschübe)
}

// enumeriert alle einfachen Zyklen (vereinfachter Johnson: nur Zyklen, deren
// kleinster Knoten der Anker ist). Die Graphen sind winzig (Zustände x
// Sperr-Slots), maxLoops ist nur ein Sicherheitsnetz. Anders als früher
// erscheinen auch B-Besuche im Wort (ob sie transparent sind, hängt jetzt
// von der Wort-Position ab - für die Anschauung sind sie sichtbar).
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
						loop.word = g.alpha.join(loop.word, g.alpha.list[edge.symbol].name)
						if edge.variant != usageNoVariant {
							loop.variants = append(loop.variants, edge.variant)
						}
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
	active  []int // erreichte Knoten
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
func (ws *usageWorkspace) relax(node int, c usageCost) {
	if ws.version[node] != ws.current {
		ws.version[node] = ws.current
		ws.cost[node] = c
		ws.active = append(ws.active, node)
		return
	}
	if c.less(ws.cost[node]) {
		ws.cost[node] = c
	}
}

// ein sichtbares Zeichen konsumieren, inklusive Epsilon-Abschluss danach.
// Der Abschluss ist EINSCHRITTIG: ein Epsilon-B(q,q) direkt nach einem
// Ereignis mit Endposition q sperrt selbst das Portal q - ein zweites B dort
// wäre die verbotene Fusion, entsprechende Kanten existieren gar nicht.
func (g *usageGraph) stepClose(src usageConfig, sym int32, ws *usageWorkspace) usageConfig {
	ws.reset(len(g.nodes))
	if int(sym) < len(g.bySym) && g.bySym[sym] != nil {
		perNode := g.bySym[sym]
		for _, entry := range src {
			for _, e := range perNode[entry.node] {
				ws.relax(e.to, entry.cost.add(e.cost))
			}
		}
	}
	exit := g.alpha.list[sym].exit
	base := len(ws.active)
	for i := 0; i < base; i++ {
		node := ws.active[i]
		cost := ws.cost[node]
		for _, e := range g.epsEdges[node] {
			if g.alpha.list[e.symbol].entry == exit {
				ws.relax(e.to, cost.add(e.cost))
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

// Start-Konfiguration: NUR der Startknoten, bewusst OHNE Epsilon-Abschluss
// (vor dem ersten Ereignis steht der Spieler an keinem Portal - ein
// exportloser Besuch dort ist das sichtbare Zeichen B)
func (g *usageGraph) startConfig() usageConfig {
	return usageConfig{{node: g.start}}
}

// ---------------------------------------------------------------------------
// Wort-Zustand des Vergleichs: die Spieler-Position (wortbestimmt)

const (
	usagePosInRoom = -2 // Spieler steht noch im Raum (vor dem S-Zeichen)
	usagePosStart  = -1 // Spieler auf dem globalen Startfeld (draußen)
)

// prüft, ob ein Symbol an der aktuellen Spieler-Position zulässig ist
// (env optional: Spieler-Konnektivität, nil = kein Filter)
func usageSymbolLegal(alpha *usageAlphabet, env *usageEnv, sym int32, pos int) bool {
	s := &alpha.list[sym]
	if pos == usagePosInRoom {
		return s.kind == usageKindStart
	}
	if s.kind == usageKindStart {
		return false
	}
	if env != nil && env.compAt(pos) != env.portalComp[s.entry] {
		return false // Portal von der Spieler-Position aus nicht erreichbar
	}
	if s.eps && pos == s.entry {
		return false // dort wäre der Besuch transparent (kanonisch weglassen)
	}
	return true
}

// Akzeptanzkosten einer Konfiguration: buf sammelt die Minima je
// Terminal-Symbol (dichte Arrays über die Symbol-IDs), Rückgabe ist die
// Out-Akzeptanz (Kammer im Zustand 0 hinterlassen, Spieler draußen)
type usageAcceptBuf struct {
	has  []bool
	cost []usageCost
}

func (b *usageAcceptBuf) reset(n int) {
	if len(b.has) < n {
		b.has = make([]bool, n)
		b.cost = make([]usageCost, n)
	}
	for i := 0; i < n; i++ {
		b.has[i] = false
	}
}

func (g *usageGraph) acceptInto(c usageConfig, buf *usageAcceptBuf) (out usageCost, hasOut bool) {
	buf.reset(len(g.alpha.list))
	for _, entry := range c {
		if g.nodes[entry.node].state == 0 && (!hasOut || entry.cost.less(out)) {
			out, hasOut = entry.cost, true
		}
		blocked := g.nodes[entry.node].blocked
		for _, end := range g.ends[entry.node] {
			// Selbes-Portal-Sperre: End-Besuche am gesperrten Portal sind tabu
			// (Start-End-Varianten haben entry -1 und sind nie gesperrt)
			if e := g.alpha.list[end.symbol].entry; e >= 0 && int16(e) == blocked {
				continue
			}
			cost := entry.cost.add(end.cost)
			if !buf.has[end.symbol] || cost.less(buf.cost[end.symbol]) {
				buf.has[end.symbol], buf.cost[end.symbol] = true, cost
			}
		}
	}
	return out, hasOut
}

// Anmerkung zur Out-Akzeptanz: das nackte Startwort "" würde bei
// StartState == 0 fälschlich mit Kosten 0 akzeptiert (das Labor verlangt
// mindestens eine benutzte Variante). Für den Vergleich zweier Graphen ist
// das harmlos (beide Seiten teilen den Fehler), die Kreuzvalidierung
// filtert den Fall.

// ---------------------------------------------------------------------------
// Signatur-Enumeration bis fester Ereigniszahl (Kreuzvalidierung gegen das
// Labor: muss bei Ein-Portal-Räumen exakt enumerateUsages entsprechen)

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
		word  string
		depth int
		pos   int
		cfg   usageConfig
	}
	ws := &usageWorkspace{}
	buf := &usageAcceptBuf{}
	startPos := usagePosStart
	if g.room.StartVariantCount > 0 {
		startPos = usagePosInRoom
	}
	queue := []item{{pos: startPos, cfg: g.startConfig()}}

	for len(queue) > 0 {
		it := queue[0]
		queue = queue[1:]
		out, hasOut := g.acceptInto(it.cfg, buf)
		if hasOut && it.pos != usagePosInRoom && !(it.depth == 0 && out == (usageCost{})) {
			takeMin(it.word, out) // nacktes Startwort zählt nicht als Nutzung
		}
		for _, sym := range g.termSyms {
			if buf.has[sym] && usageSymbolLegal(g.alpha, nil, sym, it.pos) {
				takeMin(g.alpha.join(it.word, g.alpha.list[sym].name), buf.cost[sym])
			}
		}
		if it.depth >= maxEvents {
			continue
		}
		// Symbole der Konfiguration einsammeln (unique über die Knoten)
		var syms []int32
		seen := map[int32]bool{}
		for _, entry := range it.cfg {
			for _, sym := range g.outSyms[entry.node] {
				if !seen[sym] {
					seen[sym] = true
					syms = append(syms, sym)
				}
			}
		}
		sort.Slice(syms, func(i, j int) bool { return syms[i] < syms[j] })
		for _, sym := range syms {
			if !usageSymbolLegal(g.alpha, nil, sym, it.pos) {
				continue
			}
			next := g.stepClose(it.cfg, sym, ws)
			if len(next) == 0 {
				continue
			}
			queue = append(queue, item{
				word:  g.alpha.join(it.word, g.alpha.list[sym].name),
				depth: it.depth + 1,
				pos:   g.alpha.list[sym].exit,
				cfg:   next,
			})
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
//     Akzeptanzkosten je Terminal-Symbol.
//   - Von jedem Konfigurations-Paar wird das gemeinsame Kosten-Minimum
//     abgezogen (Normalisierung). Die normalisierte Form beschreibt die
//     Vergleichs-SITUATION vollständig: jede Fortsetzung hängt nur noch
//     von den relativen Offsets und der Spieler-Position ab, nicht von der
//     absoluten Vergangenheit.
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
// wiederverwendeten Buffer); die Spieler-Position gehört dazu (sie bestimmt
// die legalen Fortsetzungen)
func usageFingerprint(pos int, full, reduced usageConfig, buf []byte) (string, []byte) {
	buf = buf[:0]
	buf = binary.AppendVarint(buf, int64(pos))
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

// vergleicht die Optimal-Kosten beider Graphen über ALLE legalen Außenwörter.
// Beide Graphen müssen dieselbe Symbol-Tabelle teilen (gleiche IDs).
// env (optional) filtert Wörter über die Spieler-Konnektivität (usageenv.go);
// nil = kein Filter (konservativ: mehr Wörter, nie unsound).
// Bei usageDiffers beschreibt detail das Gegenbeispiel (Wort + Kosten).
// moveLimit > 0 kappt den Vergleich: Nutzungen, deren Moves das Limit
// überschreiten, sind irrelevant (sie können in keiner Lösung innerhalb des
// bewiesenen Gesamt-Budgets vorkommen) - Akzeptanzen darüber werden
// ignoriert, Zweige mit Mindestkosten über dem Limit gekappt. Nebeneffekt:
// divergierende Loop-Raten laufen ins Limit statt ins maxConfigs-Netz.
// tick (optional) wird zeitgedrosselt mit der aktuellen Situationszahl
// gerufen - für Statusmeldungen während LANGER Vergleiche (riesige Räume)
// und als Abbruch: false beendet den Vergleich mit usageUndecided (Stop;
// ohne den Haken wäre ein einzelner Riesen-Vergleich unabbrechbar).
func compareUsageGraphs(full, reduced *usageGraph, env *usageEnv, maxConfigs int, moveLimit int64, tick func(situations int) bool) (usageVerdict, string) {
	if full.alpha != reduced.alpha {
		panic("compareUsageGraphs: Graphen teilen die Symbol-Tabelle nicht")
	}
	if full.start < 0 && reduced.start < 0 {
		return usageEqual, "beide Räume ohne akzeptierende Nutzung"
	}
	if full.start < 0 || reduced.start < 0 {
		return usageDiffers, "nur eine Seite hat akzeptierende Nutzungen"
	}
	alpha := full.alpha

	type item struct {
		word    string
		pos     int
		base    usageCost // Summe der abnormalisierten Minima (für Meldungen)
		full    usageConfig
		reduced usageConfig
	}

	// Vereinigung der Terminal-Symbole beider Seiten (einmalig)
	var termSyms []int32
	{
		seen := map[int32]bool{}
		for _, list := range [][]int32{full.termSyms, reduced.termSyms} {
			for _, sym := range list {
				if !seen[sym] {
					seen[sym] = true
					termSyms = append(termSyms, sym)
				}
			}
		}
		sort.Slice(termSyms, func(i, j int) bool { return termSyms[i] < termSyms[j] })
	}

	wsFull, wsReduced := &usageWorkspace{}, &usageWorkspace{}
	bufFull, bufReduced := &usageAcceptBuf{}, &usageAcceptBuf{}
	symMark := make([]uint32, len(alpha.list))
	symCur := uint32(0)
	var symList []int32
	var throttle progressThrottle
	var buf []byte
	seen := map[string]bool{}

	startPos := usagePosStart
	if full.room.StartVariantCount > 0 {
		startPos = usagePosInRoom
	}
	queue := []item{{pos: startPos, full: full.startConfig(), reduced: reduced.startConfig()}}
	queue[0].base = usageNormalize(queue[0].full, queue[0].reduced)
	fp, buf := usageFingerprint(queue[0].pos, queue[0].full, queue[0].reduced, buf)
	seen[fp] = true

	for len(queue) > 0 {
		it := queue[0]
		queue = queue[1:]

		if tick != nil && throttle.due() && !tick(len(seen)) {
			return usageUndecided, fmt.Sprintf("abgebrochen nach %d Vergleichs-Situationen", len(seen))
		}

		// Akzeptanzen vergleichen (reduced ist bei uns eine Teilmenge der
		// Kanten, kann also nie billiger sein - geprüft wird trotzdem
		// symmetrisch, der Vergleich ist allgemein)
		fOut, fHasOut := full.acceptInto(it.full, bufFull)
		rOut, rHasOut := reduced.acceptInto(it.reduced, bufReduced)
		if it.pos == usagePosInRoom {
			fHasOut, rHasOut = false, false // Spieler steht noch im Raum
		}
		check := func(label string, fh, rh bool, fc, rc usageCost) (usageVerdict, string) {
			word := it.word
			if label != "" {
				word = alpha.join(word, label)
			}
			if moveLimit > 0 {
				// Akzeptanzen über dem Budget sind irrelevant (beidseitig)
				if fh && fc.add(it.base).moves > moveLimit {
					fh = false
				}
				if rh && rc.add(it.base).moves > moveLimit {
					rh = false
				}
			}
			switch {
			case fh && !rh:
				return usageDiffers, fmt.Sprintf("Wort %q: nur voll bedienbar (Kosten %s)",
					word, fc.add(it.base))
			case !fh && rh:
				return usageDiffers, fmt.Sprintf("Wort %q: nur reduziert bedienbar", word)
			case fh && fc != rc:
				return usageDiffers, fmt.Sprintf("Wort %q: reduziert %s statt %s",
					word, rc.add(it.base), fc.add(it.base))
			}
			return usageEqual, ""
		}
		if v, detail := check("", fHasOut, rHasOut, fOut, rOut); v != usageEqual {
			return v, detail
		}
		for _, sym := range termSyms {
			if !usageSymbolLegal(alpha, env, sym, it.pos) {
				continue // Wort außen nicht spielbar - muss nicht bedient werden
			}
			if v, detail := check(alpha.list[sym].name,
				bufFull.has[sym], bufReduced.has[sym], bufFull.cost[sym], bufReduced.cost[sym]); v != usageEqual {
				return v, detail
			}
		}

		// Übergangs-Symbole: Vereinigung beider Konfigurationen
		symCur++
		symList = symList[:0]
		for _, side := range [2]struct {
			g   *usageGraph
			cfg usageConfig
		}{{full, it.full}, {reduced, it.reduced}} {
			for _, entry := range side.cfg {
				for _, sym := range side.g.outSyms[entry.node] {
					if symMark[sym] != symCur {
						symMark[sym] = symCur
						symList = append(symList, sym)
					}
				}
			}
		}
		sort.Slice(symList, func(i, j int) bool { return symList[i] < symList[j] })

		for _, sym := range symList {
			if !usageSymbolLegal(alpha, env, sym, it.pos) {
				continue
			}
			nextFull := full.stepClose(it.full, sym, wsFull)
			nextReduced := reduced.stepClose(it.reduced, sym, wsReduced)
			if len(nextFull) == 0 && len(nextReduced) == 0 {
				continue // Zweig tot (dank Pruning: keine akzeptierende Fortsetzung)
			}
			if len(nextFull) == 0 || len(nextReduced) == 0 {
				side := "voll"
				if len(nextFull) == 0 {
					side = "reduziert"
				}
				return usageDiffers, fmt.Sprintf("ab Wort %q: nur %s fortsetzbar",
					alpha.join(it.word, alpha.list[sym].name), side)
			}
			delta := usageNormalize(nextFull, nextReduced)
			if moveLimit > 0 && it.base.add(delta).moves > moveLimit {
				continue // alle Fortsetzungen kosten mindestens base - über Budget
			}
			nextPos := alpha.list[sym].exit
			var fp string
			fp, buf = usageFingerprint(nextPos, nextFull, nextReduced, buf)
			if seen[fp] {
				continue // Situation bekannt: jede Fortsetzung wiederholt sich
			}
			seen[fp] = true
			if len(seen) > maxConfigs {
				return usageUndecided, fmt.Sprintf("mehr als %d Vergleichs-Situationen", maxConfigs)
			}
			queue = append(queue, item{
				word:    alpha.join(it.word, alpha.list[sym].name),
				pos:     nextPos,
				base:    it.base.add(delta),
				full:    nextFull,
				reduced: nextReduced,
			})
		}
	}
	return usageEqual, fmt.Sprintf("%d Vergleichs-Situationen gesättigt", len(seen))
}
