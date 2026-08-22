package rooms

import "slices"

// Rückwärtssuche des Solvers (Gegenstück zu resolveTask/emitPush in
// solver.go): dieselben Vorwärts-Varianten werden nur rückwärts indiziert
// (Idee aus dem C#-RoomReverse des Deadlock-Scanners). Eine Rückwärts-Aufgabe
// hat exakt die Vorwärts-Normalform "Spieler ist gerade über Portal p in Raum
// R eingetreten, Zustände je Raum" - ihr Pfad ist aber der RESTWEG von dort
// bis zum gelösten Level und ihre Tiefe dessen Zuglänge. Expandiert wird in
// die Vergangenheit: welcher Push kann diesen Eintritt erzeugt haben?
//
//  1. Der Vor-Raum X steht fest (das Portal kommt aus ihm); alle Push-
//     Varianten von X mit diesem Austrittsportal und NewState == Zustand
//     von X kommen infrage (invertiertes Varianten-Verzeichnis byExitPush).
//  2. Ihre Kisten-Exporte werden in umgekehrter Reihenfolge zurückgezogen
//     (invertierter BoxSwap; mehrdeutige Umkehrungen verzweigen die Suche).
//  3. Eine Rückwärts-Laufkette zählt alle Eintrittspunkte auf, von denen
//     aus die Variante über reine Lauf-Varianten erreichbar war - jeder
//     ist eine Vorgänger-Aufgabe (Restweg = Laufkette + Variante + Rest).
//
// Startvarianten bleiben komplett außen vor: alles, was direkt ab dem
// Spielstart erreichbar ist, zählt die Vorwärts-Saat vollständig auf
// (NewSolver ruft sie immer, auch bei manuell fixierter Rückwärtssuche -
// brutes Lehre: die Rückwärtsfront kann die rohe Startstellung nie treffen,
// wohl aber deren Push-Nachfolger).

// Rückwärts-Verzeichnisse eines Raums (einmal je Solver-Sitzung, O(Varianten));
// Startvarianten sind überall ausgenommen
type roomReverse struct {
	entryPortal []uint32 // je Variante ihr Eintritts-Portal (aus den Spans; NoPortal = Startvariante)

	byExitPush []map[uint64][]uint64 // je Austritts-Portal: NewState -> Push-Varianten
	byExitWalk []map[uint64][]uint64 // je Austritts-Portal: Zustand -> Lauf-Varianten (OldState == NewState)
	invSwap    []map[uint64][]uint64 // je Austritts-Portal: invertierter BoxSwap des Nachbarn (Nachher -> mögliche Vorher)

	endVariants []uint64 // End-Varianten (PlayerPortal == NoPortal) mit gelöstem NewState
}

// baut die Rückwärts-Verzeichnisse aller Räume (deterministisch: Varianten
// in ID-Reihenfolge, mehrdeutige BoxSwap-Umkehrungen sortiert)
func (s *Solver) buildReverse() {
	s.rev = make([]roomReverse, len(s.rooms))
	for i, room := range s.rooms {
		rev := &s.rev[i]

		rev.entryPortal = make([]uint32, room.Variants.Count())
		for id := range rev.entryPortal {
			rev.entryPortal[id] = NoPortal
		}
		for pIdx, ip := range room.Incoming {
			for _, span := range ip.VariantSpans {
				for id := span.Start; id < span.Start+span.Count; id++ {
					rev.entryPortal[id] = uint32(pIdx)
				}
			}
		}

		portalCount := len(room.Outgoing)
		rev.byExitPush = make([]map[uint64][]uint64, portalCount)
		rev.byExitWalk = make([]map[uint64][]uint64, portalCount)
		for id := room.StartVariantCount; id < room.Variants.Count(); id++ {
			v := room.Variants.Get(id)
			if v.PlayerPortal == NoPortal {
				if v.NewState == 0 {
					rev.endVariants = append(rev.endVariants, id)
				}
				continue
			}
			byExit := rev.byExitWalk
			if v.Pushes > 0 {
				byExit = rev.byExitPush
			}
			m := byExit[v.PlayerPortal]
			if m == nil {
				m = map[uint64][]uint64{}
				byExit[v.PlayerPortal] = m
			}
			m[v.NewState] = append(m[v.NewState], id)
		}

		rev.invSwap = make([]map[uint64][]uint64, portalCount)
		for j, op := range room.Outgoing {
			inv := map[uint64][]uint64{}
			for from, to := range op.BoxSwap {
				inv[to] = append(inv[to], from)
			}
			for _, list := range inv {
				slices.Sort(list)
			}
			rev.invSwap[j] = inv
		}
	}
}

// Rückwärts-Saat: jede End-Variante mit gelöstem Endzustand, aus der
// Ziel-Stellung (alle Räume Zustand 0) zurückgerollt - ihr Eintrittspunkt
// ist die erste Rückwärts-Aufgabe, Tiefe = Restweg der Variante selbst
func (s *Solver) seedBackward() {
	zero := make([]uint64, len(s.rooms))
	var statesHash, remainSum uint64
	for i := range s.rooms {
		statesHash ^= stateMix(uint32(i), 0)
		r := s.bwd.remain[i][0]
		if r == remainInf {
			return // ein Raum erreicht den gelösten Zustand nie - NewSolver hat das Level längst als unlösbar erledigt
		}
		remainSum += uint64(r)
	}
	s.basePath = EmptyPath
	s.baseHash, s.baseRemain = statesHash, remainSum
	for _, room := range s.rooms {
		for _, id := range s.rev[room.Index].endVariants {
			s.pullVariant(0, zero, room, id)
		}
	}
}

// verarbeitet eine Rückwärts-Aufgabe: Veraltet-Check, Basis-Werte aufbauen,
// alle Push-Varianten des Vor-Raums mit passendem Austritt zurücknehmen
func (s *Solver) processTaskBackward(task []uint64) {
	states := task[:len(s.rooms)]
	roomPortal := task[len(s.rooms)]

	var statesHash, remainSum uint64
	for i, state := range states {
		statesHash ^= stateMix(uint32(i), state)
		remainSum += uint64(s.bwd.remain[i][state])
	}
	if s.bwd.hash[taskKey(statesHash, roomPortal)]>>32 < uint64(s.bwd.depth) {
		s.bwd.skipped++ // eine billigere Kopie wurde bereits eingetragen/verarbeitet
		return
	}
	s.basePath = PathID(task[len(s.rooms)+1])
	s.baseHash, s.baseRemain = statesHash, remainSum
	s.bwd.processed++

	// kanonische Aufgabe (kein Kiste auf dem Eintritts-Feld): der Vorgänger-
	// Push kann über JEDE Kante des Feldes gekommen sein - alle Gruppen-
	// Portale zurücknehmen; kantenspezifische Aufgabe (Kiste liegt dort,
	// siehe emitBackTask): nur die eigene Kante
	room := s.rooms[roomPortal>>32]
	p := uint32(roomPortal)
	group := s.portalGroup[room.Index][p]
	if stateHasBox(room, states[room.Index], room.Incoming[p].To) {
		single := [1]uint32{p}
		group = single[:]
	}
	for _, pIdx := range group {
		ip := room.Incoming[pIdx]
		from := ip.FromRoom
		exitIdx := ip.Opposite.Index // Position des Portals in from.Outgoing (Validate: Outgoing[i].Opposite == Incoming[i])
		for _, id := range s.rev[from.Index].byExitPush[exitIdx][states[from.Index]] {
			s.pullVariant(uint32(s.bwd.depth), states, from, id)
		}
	}
}

// nimmt eine Push-Variante zurück: Vor-Zustand des eigenen Raums setzen,
// dann die Kisten-Exporte rückwärts abwickeln (pullBoxes verzweigt über
// mehrdeutige Umkehrungen und mündet je Kombination in emitPull)
func (s *Solver) pullVariant(d uint32, states []uint64, room *Room, vid uint64) {
	v := room.Variants.Get(vid)
	if uint64(d)+uint64(v.Moves) > s.limit() {
		return
	}
	changes := append(s.changeBuf[:0], stateChange{idx: room.Index, state: v.OldState})
	s.changeBuf = s.pullBoxes(d, states, changes, room, vid, len(v.BoxPortals)-1)[:0]
}

// zieht die Kisten-Exporte einer Variante in umgekehrter Reihenfolge zurück:
// je Export alle Vorher-Zustände des Nachbarn durchprobieren (invertierter
// BoxSwap); boxIdx < 0 = alle zurückgezogen -> Laufkette und Aufgaben
func (s *Solver) pullBoxes(d uint32, states []uint64, changes []stateChange, room *Room, vid uint64, boxIdx int) []stateChange {
	if boxIdx < 0 {
		s.emitPull(d, states, changes, room, vid)
		return changes
	}
	v := room.Variants.Get(vid)
	bp := v.BoxPortals[boxIdx]
	idx := room.Outgoing[bp].ToRoom.Index
	now := states[idx]
	for j := len(changes) - 1; j >= 0; j-- {
		if changes[j].idx == idx {
			now = changes[j].state
			break
		}
	}
	mark := len(changes)
	for _, pre := range s.rev[room.Index].invSwap[bp][now] {
		changes = append(changes[:mark], stateChange{idx: idx, state: pre})
		changes = s.pullBoxes(d, states, changes, room, vid, boxIdx-1)
	}
	return changes[:mark]
}

// eine vollständig zurückgerollte Variante: Deltas prüfen, dann per
// Rückwärts-Laufkette alle Eintrittspunkte aufzählen, von denen aus die
// Variante erreichbar war - jeder wird eine Vorgänger-Aufgabe (und ein
// möglicher Treffpunkt mit der Vorwärtsfront)
func (s *Solver) emitPull(d uint32, states []uint64, changes []stateChange, room *Room, vid uint64) {
	// Deltas über die effektiv geänderten Räume (die LETZTE Änderung je Raum
	// ist der älteste = gesuchte Vorher-Zustand)
	newHash, remainSum := s.baseHash, int64(s.baseRemain)
	for j := range changes {
		idx := changes[j].idx
		stale := false
		for k := j + 1; k < len(changes); k++ {
			if changes[k].idx == idx {
				stale = true
				break
			}
		}
		if stale {
			continue
		}
		old, now := states[idx], changes[j].state
		if old == now {
			continue
		}
		rNew := s.bwd.remain[idx][now]
		if rNew == remainInf {
			return // dieser Vorher-Zustand ist vom Spielstart aus beweisbar unerreichbar
		}
		remainSum += int64(rNew) - int64(s.bwd.remain[idx][old])
		newHash ^= stateMix(idx, old) ^ stateMix(idx, now)
	}
	if uint64(d)+uint64(room.Variants.Get(vid).Moves)+uint64(remainSum) > s.limit() {
		return
	}

	// Vorher-Zustand eines Raums (letzte Änderung gewinnt, sonst unverändert)
	cur := func(idx uint32) uint64 {
		for j := len(changes) - 1; j >= 0; j-- {
			if changes[j].idx == idx {
				return changes[j].state
			}
		}
		return states[idx]
	}

	// --- Rückwärts-Laufkette (Wurzel: die zurückgenommene Variante selbst;
	// moves = Restweg-Beitrag ab Eintritt in das Glied, Dedup wie vorwärts) ---
	list := s.chainList[:0]
	gen := s.nextGen()
	rootMoves := room.Variants.Get(vid).Moves
	s.visited[room.Index][vid] = gen | uint64(rootMoves)
	list = append(list, chainStep{moves: rootMoves, parent: -1, room: room, variant: vid})

	for i := 0; i < len(list); i++ {
		st := list[i]
		if st.moves > uint32(s.visited[st.room.Index][st.variant]) {
			continue // billigerer Restweg inzwischen bekannt
		}
		entry := s.rev[st.room.Index].entryPortal[st.variant]
		ip := st.room.Incoming[entry]
		from := ip.FromRoom
		exitIdx := ip.Opposite.Index

		// Vorgänger-Aufgabe an diesem Eintritts-Feld: kanonisches Portal,
		// außer eine Kiste liegt dort (Ankunfts-Richtung semantisch, siehe
		// emitPush) - dann bleibt die Kante im Schlüssel. Erzeugt wird nur,
		// wenn eine Push-Variante mit dem KONKRETEN Zustand ihres Vor-Raums
		// hereinführen kann: sonst existiert weder ein Vorwärts-Partner
		// (dessen Aufgaben entstehen genau durch solche Pushes) noch eine
		// Rückwärts-Fortsetzung. (Früher wurde nur zustandsUNabhängig
		// geprüft, ob am Portal je eine Push-Variante austritt - das ließ
		// tote Rückwärts-Aufgaben durch.)
		taskPortal := entry
		group := [1]uint32{entry}
		members := group[:]
		if !stateHasBox(st.room, cur(st.room.Index), ip.To) {
			taskPortal = s.canonPortal[st.room.Index][entry]
			members = s.portalGroup[st.room.Index][taskPortal]
		}
		viable := false
		for _, pIdx := range members {
			gip := st.room.Incoming[pIdx]
			if len(s.rev[gip.FromRoom.Index].byExitPush[gip.Opposite.Index][cur(gip.FromRoom.Index)]) > 0 {
				viable = true
				break
			}
		}
		if viable {
			s.emitBackTask(uint64(d)+uint64(st.moves), states, changes, newHash, remainSum,
				uint64(st.room.Index)<<32|uint64(taskPortal), list, int32(i))
		}

		// Kette verlängern: Lauf-Varianten des Vor-Raums, die hier austreten
		for _, wid := range s.rev[from.Index].byExitWalk[exitIdx][cur(from.Index)] {
			nextMoves := st.moves + from.Variants.Get(wid).Moves
			if uint64(d)+uint64(nextMoves) > s.limit() {
				continue
			}
			if e := s.visited[from.Index][wid]; e >= gen && uint32(e) <= nextMoves {
				continue // gleiche Generation, billigerer/gleicher Restweg bekannt
			}
			s.visited[from.Index][wid] = gen | uint64(nextMoves)
			list = append(list, chainStep{moves: nextMoves, parent: int32(i), room: from, variant: wid})
		}
	}
	s.chainList = list[:0] // Puffer (samt Wachstum) behalten
}

// trägt eine Vorgänger-Aufgabe der Rückwärtsfront ein (Hash-Dedup beim
// Erzeugen wie vorwärts) und prüft das Fronten-Treffen über den Vorwärts-Hash
func (s *Solver) emitBackTask(depth uint64, states []uint64, changes []stateChange,
	newHash uint64, remainSum int64, roomPortal uint64, list []chainStep, i int32) {
	if depth+uint64(remainSum) > s.limit() {
		return
	}
	key := taskKey(newHash, roomPortal)
	if old, ok := s.bwd.hash[key]; ok && old>>32 <= depth {
		return
	}

	// erst jetzt wird es teuer: Restweg materialisieren, Zustände kopieren
	path := s.stepPathBack(list, i)
	s.bwd.hash[key] = packRef(uint32(depth), path)
	newStates := append(s.stateBuf[:0], states...)
	for _, ch := range changes {
		newStates[ch.idx] = ch.state // sequenziell: die letzte Änderung je Raum gewinnt
	}
	s.stateBuf = newStates
	if ref, ok := s.fwd.hash[key]; ok {
		s.tryMeet(PathID(ref), path, ref>>32+depth)
	}
	s.bwd.enqueue(uint32(depth), newStates, roomPortal, path)
}

// Restweg ab dem Eintritt in das Ketten-Glied i bis zum gelösten Level -
// spiegelbildlich zu stepPath: die Variante des Glieds läuft zeitlich VOR
// dem Weg des Vorgänger-Glieds, die Wurzel endet im Basis-Restweg der Aufgabe
func (s *Solver) stepPathBack(list []chainStep, i int32) PathID {
	st := &list[i]
	if st.pathSet {
		return st.path
	}
	suffix := s.basePath
	if st.parent >= 0 {
		suffix = s.stepPathBack(list, st.parent)
	}
	st.path = s.paths.Concat(s.importPath(st.room, st.room.Variants.Get(st.variant).Path), suffix)
	st.pathSet = true
	return st.path
}
