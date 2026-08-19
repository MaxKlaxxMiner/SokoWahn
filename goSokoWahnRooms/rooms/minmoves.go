package rooms

// Pflicht-Minimum eines Raums (Max' Idee, 2026-08-19): die billigste Art,
// den Raum von seinem Start- in den gelösten Zustand zu bringen - Kisten
// müssen interne Ziele erreichen oder den Raum verlassen, Ziele müssen
// befüllt werden. Für ein reines MINIMUM genügt ein Dijkstra über die
// Zustände: jede Variante ist eine Kante (OldState -> NewState, egal an
// welchem Portal - die Verfügbarkeit des Spielers wird über-approximiert),
// jeder Kisten-Einschub (BoxSwap) eine 0-Kosten-Kante (der Schub-Zug zählt
// beim Nachbarraum). Jede echte Spielweise induziert einen solchen Pfad,
// das Ergebnis ist also eine beweisbar sichere UNTERGRENZE der Züge, die in
// diesem Raum anfallen - für JEDEN Raum, auch Mehr-Portal und Startvarianten
// (liegt ein Ziel direkt am Portal, kann per Einschub auch 0 herauskommen).
// Grundlage der Budget-Zerlegung in OptimizeRooms.
//
// Der Wert wird am Raum gecacht und bei jeder Strukturänderung verworfen
// (renewVariants/removeUnusedStates) - neue Räume starten unberechnet.

// MinMoves liefert die bewiesene Untergrenze der Pflicht-Moves des Raums
// (0 auch, wenn der gelöste Zustand im Modell unerreichbar ist - dann ist
// das Level ab hier ohnehin tot, eine Untergrenze 0 bleibt korrekt)
func (r *Room) MinMoves() uint64 {
	if !r.minMovesValid {
		r.minMoves = r.computeMinMoves()
		r.minMovesValid = true
	}
	return r.minMoves
}

// verwirft den Cache (nach jeder Strukturänderung des Raums)
func (r *Room) invalidateMinMoves() {
	r.minMovesValid = false
}

func (r *Room) computeMinMoves() uint64 {
	if r.StartState == 0 {
		return 0 // startet gelöst
	}
	stateCount := r.States.Count()

	// Kosten lexikographisch als gepackter Schlüssel (moves << 32 | pushes)
	const inf = ^uint64(0)
	dist := make([]uint64, stateCount)
	for i := range dist {
		dist[i] = inf
	}
	dist[r.StartState] = 0

	// Kanten je Zustand einsammeln: Varianten (inkl. Start- und End-Varianten)
	// und Einschübe aller Portale
	type edge struct {
		to  uint64
		key uint64
	}
	edges := make([][]edge, stateCount)
	for vid := uint64(0); vid < r.Variants.Count(); vid++ {
		v := r.Variants.Get(vid)
		edges[v.OldState] = append(edges[v.OldState],
			edge{to: v.NewState, key: uint64(v.Moves)<<32 | uint64(v.Pushes)})
	}
	for _, ip := range r.Incoming {
		for from, to := range ip.BoxSwap {
			edges[from] = append(edges[from], edge{to: to})
		}
	}

	// kleiner binärer Heap über (Kosten, Zustand)
	heapNodes := []uint64{r.StartState}
	heapKeys := []uint64{0}
	push := func(node, key uint64) {
		heapNodes = append(heapNodes, node)
		heapKeys = append(heapKeys, key)
		for i := len(heapNodes) - 1; i > 0; {
			p := (i - 1) / 2
			if heapKeys[p] <= heapKeys[i] {
				break
			}
			heapNodes[p], heapNodes[i] = heapNodes[i], heapNodes[p]
			heapKeys[p], heapKeys[i] = heapKeys[i], heapKeys[p]
			i = p
		}
	}
	pop := func() (uint64, uint64) {
		node, key := heapNodes[0], heapKeys[0]
		last := len(heapNodes) - 1
		heapNodes[0], heapKeys[0] = heapNodes[last], heapKeys[last]
		heapNodes, heapKeys = heapNodes[:last], heapKeys[:last]
		for i := 0; ; {
			l, rr := 2*i+1, 2*i+2
			small := i
			if l < len(heapNodes) && heapKeys[l] < heapKeys[small] {
				small = l
			}
			if rr < len(heapNodes) && heapKeys[rr] < heapKeys[small] {
				small = rr
			}
			if small == i {
				break
			}
			heapNodes[small], heapNodes[i] = heapNodes[i], heapNodes[small]
			heapKeys[small], heapKeys[i] = heapKeys[i], heapKeys[small]
			i = small
		}
		return node, key
	}

	for len(heapNodes) > 0 {
		node, key := pop()
		if key > dist[node] {
			continue // veralteter Heap-Eintrag
		}
		if node == 0 {
			return key >> 32 // Moves-Anteil der billigsten Lösung
		}
		for _, e := range edges[node] {
			if k := key + e.key; k < dist[e.to] {
				dist[e.to] = k
				push(e.to, k)
			}
		}
	}
	return 0 // gelöster Zustand unerreichbar: konservativ 0
}

// wärmt die MinMoves-Caches aller Räume vor. Wird am Ende jeder mutierenden
// Operation (Init, Merge, Optimize) unter der Schreibsperre der Aufrufer
// gerufen, damit lesende API-Zugriffe (roomToJSON unter der Lesesperre) nie
// in die Cache-Berechnung laufen - der Lazy-Cache bleibt dadurch race-frei.
func (n *Network) warmMinMoves() {
	for _, room := range n.Rooms {
		room.MinMoves()
	}
}
