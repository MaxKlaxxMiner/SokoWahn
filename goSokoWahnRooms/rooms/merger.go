package rooms

import (
	"fmt"
	"math"
	"slices"

	"goSokoWahnRooms/crc64"
	"goSokoWahnRooms/soko"
	"goSokoWahnRooms/tools"
)

// Merger verschmilzt zwei benachbarte Räume zu einem neuen Raum (M3).
// C#-Vorbild: SokoWahnLib/Rooms/Merger/RoomMerger - Konzept-Port, kein bitgenauer
// Nachbau: die Suche arbeitet hier mit orientierungsfesten Aufgaben (state1/state2
// bleiben immer an Raum 1/Raum 2 gebunden) statt der gespiegelten Parameterlisten
// des Originals. Ablauf in 6 Schritten, siehe Network.MergeRooms.
type Merger struct {
	network *Network
	room1   *Room // Raum mit dem kleineren Kennfeld (Fields[0])
	room2   *Room
	NewRoom *Room

	// > 0: Varianten über diesem Moves-Budget gar nicht erst erzeugen
	// (siehe MergeRooms: min1 + min2 + Slack aus dem Max-Moves-Limit)
	moveLimit uint32

	// Fortschritts-Meldungen und Abbruch: alle heißen Schleifen (Zustands-
	// Kreuzprodukt, BoxSwaps, jede Einzelsuche) ticken zeitgedrosselt über
	// due()/report() - so bleibt auch ein Riesen-Merge sichtbar und stoppbar
	info     ProgressFunc
	throttle progressThrottle
	aborted  bool

	mapOldIncoming []*Portal // neuer Portal-Index -> altes äußeres eingehendes Portal
	mapPortal1     []uint32  // Incoming-Index Raum 1 -> neuer Portal-Index (NoPortal = inneres Portal)
	mapPortal2     []uint32  // Incoming-Index Raum 2 -> neuer Portal-Index (NoPortal = inneres Portal)
	state1Mul      uint64    // Zustands-Multiplikator: kombiniert = s1 * state1Mul + s2
}

// bereitet das Verschmelzen vor: prüft die Räume, legt den neuen Raum mit seinen
// äußeren Portalen an und baut die Portal-Index-Mappings auf
func NewMerger(n *Network, room1, room2 *Room) (*Merger, error) {
	if room1 == nil || room2 == nil {
		return nil, fmt.Errorf("merge: room is nil")
	}
	if room1 == room2 {
		return nil, fmt.Errorf("merge: room1 == room2")
	}
	if int(room1.Index) >= len(n.Rooms) || n.Rooms[room1.Index] != room1 {
		return nil, fmt.Errorf("merge: invalid room1")
	}
	if int(room2.Index) >= len(n.Rooms) || n.Rooms[room2.Index] != room2 {
		return nil, fmt.Errorf("merge: invalid room2")
	}
	connected := false
	for _, op := range room1.Outgoing {
		if op.ToRoom == room2 {
			connected = true
			break
		}
	}
	if !connected {
		return nil, fmt.Errorf("merge: rooms %d and %d are not connected", room1.Index, room2.Index)
	}

	// Raum mit dem kleineren Kennfeld wird Raum 1 (stabile Reihenfolge wie im C#)
	if room1.Fields[0] > room2.Fields[0] {
		room1, room2 = room2, room1
	}

	m := &Merger{
		network:   n,
		room1:     room1,
		room2:     room2,
		state1Mul: room2.States.Count(),
	}

	// äußere eingehende Portale beider Räume (innere lösen sich beim Verschmelzen auf)
	for _, ip := range room1.Incoming {
		if ip.FromRoom != room2 {
			m.mapOldIncoming = append(m.mapOldIncoming, ip)
		}
	}
	for _, ip := range room2.Incoming {
		if ip.FromRoom != room1 {
			m.mapOldIncoming = append(m.mapOldIncoming, ip)
		}
	}

	m.NewRoom = &Room{
		Index:      room1.Index, // endgültig erst in Step6UpdateRooms
		Fields:     mergeSortedWpos(room1.Fields, room2.Fields),
		Goals:      mergeSortedWpos(room1.Goals, room2.Goals),
		StartBoxes: mergeSortedWpos(room1.StartBoxes, room2.StartBoxes),
		MaxBoxes:   room1.MaxBoxes,
		States:     NewStateList(),
		Variants:   NewVariantList(),
		Paths:      NewPathStore(),
	}

	// neue eingehende Portale erstellen und Mappings befüllen
	m.mapPortal1 = make([]uint32, len(room1.Incoming))
	m.mapPortal2 = make([]uint32, len(room2.Incoming))
	for i := range m.mapPortal1 {
		m.mapPortal1[i] = NoPortal
	}
	for i := range m.mapPortal2 {
		m.mapPortal2[i] = NoPortal
	}
	m.NewRoom.Incoming = make([]*Portal, len(m.mapOldIncoming))
	m.NewRoom.Outgoing = make([]*Portal, len(m.mapOldIncoming)) // Verlinkung in Step4
	for i, old := range m.mapOldIncoming {
		if old.ToRoom == room1 {
			m.mapPortal1[old.Index] = uint32(i)
		} else {
			m.mapPortal2[old.Index] = uint32(i)
		}
		m.NewRoom.Incoming[i] = &Portal{
			From:         old.From,
			To:           old.To,
			FromRoom:     old.FromRoom,
			ToRoom:       m.NewRoom,
			Index:        uint32(i),
			Dir:          old.Dir,
			BlockedBox:   old.BlockedBox,
			BoxSwap:      map[uint64]uint64{},
			VariantSpans: map[uint64]Span{},
		}
	}

	return m, nil
}

// due meldet, ob wieder eine Fortschritts-Meldung fällig ist (zeitgedrosselt);
// der Aufrufer baut den Meldungs-Text nur dann und liefert ihn an report().
// WICHTIG: bei gesetztem aborted liefert due() TRUE - dann gibt report()
// sofort false zurück und die "if due() && !report()"-Wächter aller Schleifen
// brechen ab. (Die alte Fassung lieferte bei aborted false; wurde das Flag
// außerhalb eines Wächters gesetzt - etwa durch die Sofort-Meldung beim
// Merge-Start, wenn der Stop-Wunsch noch vom VORIGEN Merge der Auswahl
// stand -, feuerte nie wieder ein Wächter und der Merge lief stumm komplett
// durch: der Stop-Button wirkte ignoriert.)
func (m *Merger) due() bool {
	return m.info != nil && (m.aborted || m.throttle.due())
}

// report übergibt den Arbeitsstand an die Oberfläche; false = Abbruch-Wunsch
func (m *Merger) report(text string) bool {
	if !m.aborted && !m.info(text, []*Room{m.room1, m.room2}) {
		m.aborted = true
	}
	return !m.aborted
}

// vereinigt zwei aufsteigend sortierte, disjunkte Positionslisten
func mergeSortedWpos(a, b []soko.Wpos) []soko.Wpos {
	result := make([]soko.Wpos, 0, len(a)+len(b))
	result = append(append(result, a...), b...)
	slices.Sort(result)
	return result
}

// Schritt 1: Zustands-Kreuzprodukt beider Räume aufbauen.
// Die kombinierte ID ist rechenbar (s1*state1Mul+s2), damit bleibt Zustand 0
// der gelöste Endzustand (0*mul+0 = 0) und der Startzustand direkt ableitbar.
// false = abgebrochen (Netzwerk unverändert).
func (m *Merger) Step1MixStates() bool {
	count1, count2 := m.room1.States.Count(), m.room2.States.Count()
	for s1 := uint64(0); s1 < count1; s1++ {
		boxes1 := m.room1.States.Get(s1)
		for s2 := uint64(0); s2 < count2; s2++ {
			if m.due() && !m.report(fmt.Sprintf("merge: zustands-kreuzprodukt %s/%s",
				tools.FormatInt(s1*count2+s2), tools.FormatInt(count1*count2))) {
				return false
			}
			id := m.NewRoom.States.Add(mergeSortedWpos(boxes1, m.room2.States.Get(s2)))
			if id != s1*m.state1Mul+s2 {
				panic("merge: state id mismatch")
			}
		}
	}
	m.NewRoom.StartState = m.room1.StartState*m.state1Mul + m.room2.StartState
	return true
}

// Schritt 2: Startvarianten verschmelzen (nur wenn der Spieler in einem der
// beiden Räume startet). false = abgebrochen (Netzwerk unverändert).
func (m *Merger) Step2StartVariants() bool {
	if m.room1.StartVariantCount == 0 && m.room2.StartVariantCount == 0 {
		return true
	}
	if m.room1.StartVariantCount > 0 && m.room2.StartVariantCount > 0 {
		panic("merge: both rooms have start variants")
	}
	startRoom, side1 := m.room1, true
	if m.room2.StartVariantCount > 0 {
		startRoom, side1 = m.room2, false
	}

	search := newMergeSearch(m)
	search.label = "startvarianten"
	for id := uint64(0); id < startRoom.StartVariantCount; id++ {
		if m.due() && !m.report(fmt.Sprintf("merge: startvarianten - vorbereiten %s/%s",
			tools.FormatInt(id), tools.FormatInt(startRoom.StartVariantCount))) {
			return false
		}
		base := mergeTask{state1: m.room1.StartState, state2: m.room2.StartState}
		search.follow(&base, startRoom.Variants.Get(id), side1)
	}
	search.run()
	if m.aborted {
		return false
	}
	search.emit(m.NewRoom.StartState, nil, false, nil)
	return !m.aborted // auch emit ist stoppbar (NewRoom wird dann verworfen)
}

// Schritt 3: BoxSwaps und Varianten aller neuen Portale aufbauen. Je (Portal,
// kombinierter Zustand) läuft eine Best-Moves-Suche über beide Teilräume.
// Rückgabe false = abgebrochen (das Netzwerk ist bis hier noch unverändert).
func (m *Merger) Step3PortalVariants() bool {
	newStateCount := m.NewRoom.States.Count()
	maxBoxes := int(m.NewRoom.MaxBoxes)

	for pi, newPortal := range m.NewRoom.Incoming {
		old := m.mapOldIncoming[pi]
		toRoom1 := old.ToRoom == m.room1

		// --- BoxSwaps: alte Swaps mit allen Zuständen des anderen Teilraums kombinieren ---
		ownCount, otherCount := m.room1.States.Count(), m.room2.States.Count()
		if !toRoom1 {
			ownCount, otherCount = otherCount, ownCount
		}
		for sOwn := uint64(0); sOwn < ownCount; sOwn++ {
			sOwnTo, ok := old.BoxSwap[sOwn]
			if !ok {
				continue
			}
			for sOther := uint64(0); sOther < otherCount; sOther++ {
				if m.due() && !m.report(fmt.Sprintf("merge: portal %d/%d - boxswaps %s/%s",
					pi+1, len(m.NewRoom.Incoming), tools.FormatInt(sOwn), tools.FormatInt(ownCount))) {
					return false
				}
				var from, to uint64
				if toRoom1 {
					from, to = sOwn*m.state1Mul+sOther, sOwnTo*m.state1Mul+sOther
				} else {
					from, to = sOther*m.state1Mul+sOwn, sOther*m.state1Mul+sOwnTo
				}
				if m.NewRoom.States.BoxCount(to) > maxBoxes {
					continue // mehr Kisten im Verbund, als insgesamt existieren
				}
				newPortal.AddBoxSwap(from, to)
			}
		}

		// --- Varianten je kombiniertem Zustand ---
		for state := uint64(0); state < newStateCount; state++ {
			if m.due() && !m.report(fmt.Sprintf("merge: portal %d/%d - state %s/%s - %s varianten",
				pi+1, len(m.NewRoom.Incoming), tools.FormatInt(state), tools.FormatInt(newStateCount),
				tools.FormatInt(m.NewRoom.Variants.Count()))) {
				return false
			}
			if m.NewRoom.States.BoxCount(state) > maxBoxes {
				continue
			}
			s1, s2 := state/m.state1Mul, state%m.state1Mul
			entryState := s1
			if !toRoom1 {
				entryState = s2
			}
			span := old.GetVariantSpan(entryState)
			if span.Count == 0 {
				continue
			}
			search := newMergeSearch(m)
			search.label = fmt.Sprintf("portal %d/%d - state %s/%s",
				pi+1, len(m.NewRoom.Incoming), tools.FormatInt(state), tools.FormatInt(newStateCount))
			for id := span.Start; id < span.Start+span.Count; id++ {
				if m.due() && !m.report(fmt.Sprintf("merge: %s - vorbereiten %s/%s",
					search.label, tools.FormatInt(id-span.Start), tools.FormatInt(span.Count))) {
					return false
				}
				base := mergeTask{state1: s1, state2: s2}
				search.follow(&base, old.ToRoom.Variants.Get(id), toRoom1)
			}
			search.run()
			if m.aborted {
				return false
			}
			search.emit(state, old, toRoom1, newPortal)
			if m.aborted {
				return false // auch emit ist stoppbar (NewRoom wird dann verworfen)
			}
		}
	}
	return true
}

// Schritt 4: neue Portale mit den Nachbarräumen verlinken - ab hier ist der
// neue Raum Teil des Netzwerks, die alten inneren Portale lösen sich auf
func (m *Merger) Step4UpdatePortals() {
	for i, ip := range m.NewRoom.Incoming {
		op := m.mapOldIncoming[i].Opposite // Gegenportal = eingehendes Portal des Nachbarn
		ip.Opposite = op
		m.NewRoom.Outgoing[i] = op
		op.Opposite = ip
		op.FromRoom = m.NewRoom
		op.ToRoom.Outgoing[op.Index] = ip
	}
}

// Schritt 5: unbenutzte Zustände des neuen Raumes entfernen (samt Varianten,
// deren Zielzustand wegfällt - Fixpunkt-Iteration, siehe optimize.go)
func (m *Merger) Step5OptimizeStates() {
	removeUnusedStates(m.NewRoom)
}

// Schritt 6: Raum-Liste des Netzwerks kompaktieren (Raum 1 wird ersetzt,
// Raum 2 entfällt) und die Raum-Indizes neu vergeben
func (m *Merger) Step6UpdateRooms() {
	fill := uint32(0)
	for _, room := range m.network.Rooms {
		if room == m.room1 {
			room = m.NewRoom
		}
		if room == m.room2 {
			continue
		}
		room.Index = fill
		m.network.Rooms[fill] = room
		fill++
	}
	if int(fill)+1 != len(m.network.Rooms) {
		panic("merge: room index error")
	}
	m.network.Rooms = m.network.Rooms[:fill]
}

// ---------- Best-Moves-Suche über beide Teilräume ----------

// Zwischenstand der Suche: der Spieler hat zuletzt "variant" (im Teilraum side1
// bzw. 2) abgeschlossen; deren PlayerPortal bestimmt, wie es weitergeht
type mergeTask struct {
	state1, state2 uint64       // Zustände von Raum 1 und Raum 2 (Orientierung fest)
	boxes          []uint32     // bisher rausgeschobene Kisten (neue Portal-Indizes, sortiert)
	moves          uint32       // Laufschritte insgesamt
	pushes         uint32       // Kistenverschiebungen insgesamt
	path           PathID       // zurückgelegter Pfad insgesamt (ID in die Arena der Suche)
	side1          bool         // variant gehört zu Raum 1 (sonst Raum 2)
	variant        *VariantData // zuletzt verarbeitete Variante
}

// Dedup-Schlüssel der Aufgabe: gleiche Zustände + rausgeschobene Kisten +
// gleicher Fortsetzungspunkt (Seite und Austritts-Portal) -> nur die Variante
// mit den wenigsten Moves überlebt
func (t *mergeTask) key() uint64 {
	c := crc64.Start.UpdateUInt64(t.state1).UpdateUInt64(t.state2)
	c = c.UpdateInt(len(t.boxes))
	for _, b := range t.boxes {
		c = c.UpdateUInt32(b)
	}
	return uint64(c.UpdateBool(t.side1).UpdateUInt32(t.variant.PlayerPortal))
}

// Kosten einer Aufgabe: move-perfekt zuerst, Pushes als Tie-Break
// (Optimalitäts-Standard, siehe docs/konzept.md Kap. 4b)
type mergeCost struct {
	moves  uint32
	pushes uint32
}

func (c mergeCost) better(other mergeCost) bool {
	return c.moves < other.moves || (c.moves == other.moves && c.pushes < other.pushes)
}

type mergeSearch struct {
	m     *Merger
	label string               // Kontext für Fortschritts-Meldungen aus run()
	best  map[uint64]mergeCost // Aufgaben-Schlüssel -> beste bekannte Kosten
	tasks []mergeTask          // FIFO-Arbeitsliste (wächst beim Expandieren)

	// Pfad-Arena der Suche: jede Verkettung kostet einen 8-Byte-Knoten statt
	// einer Präfix-Kopie; die Arena samt aller toten Zwischenketten fliegt
	// nach emit (Copy-Out der Überlebenden in den neuen Raum) am Stück weg
	arena      *PathStore
	importMemo [2]map[PathID]PathID // Quell-Pfad (Raum 1/2) -> Arena-ID

	moveTasks []int // fertige reine Laufwege (Indizes in tasks)
	pushTasks []int // fertige Varianten mit Kistenverschiebungen
	endTasks  []int // fertige End-Varianten (Spieler bleibt drin, alles gelöst)
}

func newMergeSearch(m *Merger) *mergeSearch {
	return &mergeSearch{
		m:          m,
		best:       map[uint64]mergeCost{},
		arena:      NewPathStore(),
		importMemo: [2]map[PathID]PathID{{}, {}},
	}
}

// holt den Pfad einer Quell-Variante in die Arena (memoisiert je Teilraum,
// Sharing der Quell-Ketten bleibt erhalten)
func (s *mergeSearch) importPath(v *VariantData, side1 bool) PathID {
	src, memo := s.m.room1.Paths, s.importMemo[0]
	if !side1 {
		src, memo = s.m.room2.Paths, s.importMemo[1]
	}
	return s.arena.CopyFrom(src, v.Path, memo)
}

func (t *mergeTask) cost() mergeCost {
	return mergeCost{moves: t.moves, pushes: t.pushes}
}

// hängt Variante v (des Teilraums side1) an eine Aufgabe an und nimmt das
// Ergebnis auf, sofern es die beste bekannte Variante ihres Schlüssels ist
func (s *mergeSearch) follow(prev *mergeTask, v *VariantData, side1 bool) {
	t := mergeTask{
		state1:  prev.state1,
		state2:  prev.state2,
		moves:   prev.moves + v.Moves,
		pushes:  prev.pushes + v.Pushes,
		side1:   side1,
		variant: v,
	}
	if limit := s.m.moveLimit; limit > 0 && t.moves > limit {
		// über dem Moves-Budget: kann in keiner Lösung innerhalb der
		// Schranke vorkommen - und alle Fortsetzungen (Kosten wachsen
		// monoton) auch nicht. Der billigste Vertreter jeder Wirkung
		// liegt unter dem Limit und überlebt den effectKey-Dedup normal.
		return
	}
	if !s.m.resolveBoxes(&t, prev.boxes, v, side1) {
		return // Variante ist im Verbund ungültig
	}
	k := t.key()
	if bestCost, ok := s.best[k]; ok && !t.cost().better(bestCost) {
		return // eine bessere (oder gleich gute) Variante ist bereits bekannt
	}
	s.best[k] = t.cost()
	// Pfad-Knoten erst jetzt anlegen: verworfene Kandidaten (Budget-Cutoff,
	// ungültige Kisten, Dedup) hinterlassen so keinen Müll in der Arena
	t.path = s.arena.Concat(prev.path, s.importPath(v, side1))
	s.tasks = append(s.tasks, t)
}

// wendet die Kisten-Schübe einer Variante auf die Aufgabe an: Kisten durch innere
// Portale lösen den BoxSwap des anderen Teilraums aus, Kisten durch äußere Portale
// landen in der Liste der rausgeschobenen Kisten. false = Variante ungültig.
func (m *Merger) resolveBoxes(t *mergeTask, oldBoxes []uint32, v *VariantData, side1 bool) bool {
	roomCur, mapCur := m.room1, m.mapPortal1
	stateCur, stateOther := &t.state1, &t.state2
	if !side1 {
		roomCur, mapCur = m.room2, m.mapPortal2
		stateCur, stateOther = &t.state2, &t.state1
	}
	if *stateCur != v.OldState {
		panic("merge: variant does not match room state")
	}

	if v.Pushes == 0 && len(oldBoxes) == 0 {
		return true // reine Laufvariante ohne Vorgeschichte (OldState == NewState)
	}

	boxes := slices.Clone(oldBoxes)
	for _, bp := range v.BoxPortals {
		if newIdx := mapCur[bp]; newIdx == NoPortal {
			// Kiste wandert durch ein inneres Portal in den anderen Teilraum
			op := roomCur.Outgoing[bp]
			next := op.GetBoxSwap(*stateOther)
			if next == *stateOther {
				return false // der andere Teilraum kann die Kiste nicht aufnehmen
			}
			*stateOther = next
		} else {
			if slices.Contains(boxes, newIdx) {
				return false // zweite Kiste durch dasselbe äußere Portal
			}
			boxes = append(boxes, newIdx)
		}
	}

	// Spieler will durch ein äußeres Portal raus, vor dem schon eine rausgeschobene
	// Kiste liegt, die dort feststeckt (BlockedBox)? Dann käme er nicht durch.
	if v.PlayerPortal != NoPortal {
		if newIdx := mapCur[v.PlayerPortal]; newIdx != NoPortal &&
			roomCur.Outgoing[v.PlayerPortal].BlockedBox && slices.Contains(boxes, newIdx) {
			return false
		}
	}

	*stateCur = v.NewState
	slices.Sort(boxes)
	t.boxes = boxes
	return true
}

// arbeitet die Aufgabenliste ab: Austritt durch ein inneres Portal expandiert in
// den anderen Teilraum, Austritt durch ein äußeres Portal (oder Spielende) macht
// die Aufgabe zur fertigen Variante. Setzt bei Abbruch-Wunsch m.aborted und
// kehrt vorzeitig zurück (der Aufrufer darf emit() dann nicht mehr rufen) -
// eine einzelne Suche kann Millionen Aufgaben erzeugen und wäre sonst blind.
// Fortschritts-Anzeige: die Gesamtzahl wächst beim Expandieren mit, als
// ehrliches Restmaß dient daher "offen" (sinkt es, konvergiert die Suche);
// auch innerhalb einer Aufgabe wird getickt, weil ein einzelner Anschluss-Span
// Millionen Varianten haben kann (sonst stünde die Anzeige minutenlang still).
func (s *mergeSearch) run() {
	for i := 0; i < len(s.tasks); i++ {
		if s.m.due() && !s.m.report(fmt.Sprintf("merge: %s - suche %s/%s aufgaben (offen %s)",
			s.label, tools.FormatInt(i), tools.FormatInt(len(s.tasks)),
			tools.FormatInt(len(s.tasks)-i))) {
			return
		}
		t := s.tasks[i] // Kopie: tasks kann beim Expandieren umziehen
		if s.best[t.key()] != t.cost() {
			continue // von einer besseren Variante überholt
		}

		exit := t.variant.PlayerPortal
		if exit == NoPortal {
			// Spieler bleibt drin: gültiges Spielende nur, wenn beide Teilräume gelöst sind
			if t.state1 == 0 && t.state2 == 0 {
				s.endTasks = append(s.endTasks, i)
			}
			continue
		}

		roomCur, mapCur := s.m.room1, s.m.mapPortal1
		otherRoom, otherState, otherSide1 := s.m.room2, t.state2, false
		if !t.side1 {
			roomCur, mapCur = s.m.room2, s.m.mapPortal2
			otherRoom, otherState, otherSide1 = s.m.room1, t.state1, true
		}

		if mapCur[exit] != NoPortal {
			// Austritt durch ein äußeres Portal -> fertige Variante
			if t.pushes > 0 {
				s.pushTasks = append(s.pushTasks, i)
			} else {
				s.moveTasks = append(s.moveTasks, i)
			}
			continue
		}

		// inneres Portal: im anderen Teilraum jede Anschluss-Variante verfolgen
		span := roomCur.Outgoing[exit].GetVariantSpan(otherState)
		for id := span.Start; id < span.Start+span.Count; id++ {
			if s.m.due() && !s.m.report(fmt.Sprintf("merge: %s - suche %s/%s aufgaben (offen %s) - anschluss %s/%s",
				s.label, tools.FormatInt(i), tools.FormatInt(len(s.tasks)), tools.FormatInt(len(s.tasks)-i),
				tools.FormatInt(id-span.Start), tools.FormatInt(span.Count))) {
				return
			}
			s.follow(&t, otherRoom.Variants.Get(id), otherSide1)
		}
	}
}

// gibt den neuen Portal-Index zurück, über den der Spieler den Verbund verlässt
func (s *mergeSearch) mapExit(t *mergeTask) uint32 {
	if t.side1 {
		return s.m.mapPortal1[t.variant.PlayerPortal]
	}
	return s.m.mapPortal2[t.variant.PlayerPortal]
}

// Netto-Wirkung einer fertigen Variante aus Außensicht: Endzustand des Raumes,
// rausgeschobene Kisten und Austritts-Portal - die interne Historie zählt nicht
// (Optimalitäts-Standard Kap. 4b: je Wirkung überlebt nur die beste Variante)
type effectKey struct {
	endState uint64
	exit     uint32
	boxes    string
}

func effectKeyOf(endState uint64, exit uint32, boxes []uint32) effectKey {
	packed := make([]byte, 0, len(boxes)*4)
	for _, b := range boxes {
		packed = append(packed, byte(b), byte(b>>8), byte(b>>16), byte(b>>24))
	}
	return effectKey{endState: endState, exit: exit, boxes: string(packed)}
}

// trägt die fertigen Varianten in den neuen Raum ein: je Netto-Wirkung nur die
// beste (moves, dann pushes); Reihenfolge Laufwege, Kisten-Varianten,
// End-Varianten (hält die Span-Invariante "Moves vor Pushes").
// entry/entrySide1: eingehendes altes Portal samt Ziel-Teilraum (nil = Startvarianten),
// newPortal: neues Portal fürs Span-Verzeichnis (nil = Startvarianten)
func (s *mergeSearch) emit(startState uint64, entry *Portal, entrySide1 bool, newPortal *Portal) {
	type candidate struct {
		task     *mergeTask
		exit     uint32 // neuer Portal-Index (NoPortal = Spielende)
		endState uint64
	}
	bestEffect := map[effectKey]int{} // Wirkung -> Index in candidates
	var candidates []candidate
	consider := func(t *mergeTask, exit uint32, endState uint64) {
		if s.best[t.key()] != t.cost() {
			return // von einer besseren Aufgabe gleichen Schlüssels überholt
		}
		key := effectKeyOf(endState, exit, t.boxes)
		if idx, exists := bestEffect[key]; exists {
			if t.cost().better(candidates[idx].task.cost()) {
				candidates[idx] = candidate{task: t, exit: exit, endState: endState}
			}
			return
		}
		bestEffect[key] = len(candidates)
		candidates = append(candidates, candidate{task: t, exit: exit, endState: endState})
	}

	// Fortschritt und Stop-Check auch beim Auswerten: bei Monster-Suchen
	// laufen hier Millionen fertiger Aufgaben durch den Wirkungs-Dedup -
	// ohne Ticks hinge der Stop-Wunsch bis zur nächsten Suche fest
	finished := len(s.moveTasks) + len(s.pushTasks) + len(s.endTasks)
	done := 0
	tick := func() bool {
		done++
		return !s.m.due() || s.m.report(fmt.Sprintf("merge: %s - auswerten %s/%s",
			s.label, tools.FormatInt(done), tools.FormatInt(finished)))
	}

	for _, idx := range s.moveTasks {
		t := &s.tasks[idx]
		if !tick() {
			return
		}
		// sinnloser Rückweg: reiner Laufweg zurück durch das Eintritts-Portal
		if entry != nil && t.side1 == entrySide1 && t.variant.PlayerPortal == entry.Index {
			continue
		}
		consider(t, s.mapExit(t), startState)
	}
	for _, idx := range s.pushTasks {
		t := &s.tasks[idx]
		if !tick() {
			return
		}
		endState := t.state1*s.m.state1Mul + t.state2
		// sinnloser Rückweg mit folgenlosen Kistenverschiebungen
		if entry != nil && t.side1 == entrySide1 && t.variant.PlayerPortal == entry.Index && endState == startState {
			continue
		}
		consider(t, s.mapExit(t), endState)
	}
	for _, idx := range s.endTasks {
		t := &s.tasks[idx]
		if !tick() {
			return
		}
		if t.state1 != 0 || t.state2 != 0 {
			panic("merge: end variant without solved state")
		}
		consider(t, NoPortal, 0)
	}

	// Copy-Out: nur die Ketten der Überlebenden wandern in den Store des neuen
	// Raums (memoisiert - geteilte Präfixe bleiben geteilt); die Arena mit
	// allen toten Zwischenketten stirbt mit der Suche. Der Längen-Check läuft
	// über die Tabelle (ein Durchlauf) statt je Kandidat einen Ketten-Walk.
	exportMemo := map[PathID]PathID{}
	pathLens := s.arena.lens()
	add := func(c candidate) {
		t := c.task
		if uint64(pathLens[t.path]) != uint64(t.moves) {
			panic("merge: path length != moves")
		}
		id := s.m.NewRoom.Variants.Add(VariantData{
			OldState:     startState,
			NewState:     c.endState,
			Moves:        t.moves,
			Pushes:       t.pushes,
			BoxPortals:   t.boxes,
			PlayerPortal: c.exit,
			Path:         s.m.NewRoom.Paths.CopyFrom(s.arena, t.path, exportMemo),
		})
		if newPortal != nil {
			newPortal.AddVariant(startState, id)
		} else {
			if id != s.m.NewRoom.StartVariantCount {
				panic("merge: start variants must come first")
			}
			s.m.NewRoom.StartVariantCount++
		}
	}

	// eintragen in Gruppen: 0 = reine Laufwege, 1 = Kisten-Varianten, 2 = Spielende
	group := func(c candidate) int {
		switch {
		case c.exit == NoPortal:
			return 2
		case c.task.pushes == 0:
			return 0
		default:
			return 1
		}
	}
	added := 0
	for pass := 0; pass <= 2; pass++ {
		for _, c := range candidates {
			if group(c) != pass {
				continue
			}
			added++
			if s.m.due() && !s.m.report(fmt.Sprintf("merge: %s - eintragen %s/%s",
				s.label, tools.FormatInt(added), tools.FormatInt(len(candidates)))) {
				return
			}
			add(c)
		}
	}
}

// ---------- öffentliche Einstiege am Netzwerk ----------

// MergeRooms verschmilzt zwei direkt verbundene Räume zu einem neuen Raum
// (C#-Vorbild: RoomNetwork.MergeRooms) und validiert danach das ganze Netzwerk.
// info (optional) bekommt Fortschritts-Meldungen; liefert der Callback false,
// wird abgebrochen (Rückgabe nil, nil) - das Netzwerk bleibt dann unverändert,
// weil erst Schritt 4 die Verlinkungen anfasst.
// maxMoves > 0 ist eine VERIFIZIERTE obere Schranke der Gesamtlösung (siehe
// OptimizeRooms): Verbund-Varianten, deren Kosten min1 + min2 + Slack
// überschreiten, werden gar nicht erst erzeugt - das kappt die Varianten-
// Explosion an der Wurzel (der nachgelagerte Budget-Scan mit Distanz-Korridor
// bleibt schärfer, siehe BudgetScan).
func (n *Network) MergeRooms(room1, room2 *Room, maxMoves uint64, info ProgressFunc) (*Room, error) {
	m, err := NewMerger(n, room1, room2)
	if err != nil {
		return nil, err
	}
	m.info = info
	if info != nil {
		// Sofort-Meldung mit der Größenordnung (das Kreuzprodukt entscheidet,
		// ob der Merge überhaupt eine gute Idee ist); ein hier schon stehender
		// Stop-Wunsch (z.B. aus der stummen Endphase des vorigen Merges der
		// Auswahl) bricht sofort ab, bevor irgendetwas gerechnet wird
		if !m.report(fmt.Sprintf("merge: räume %s x %s zustände, %s x %s varianten",
			tools.FormatInt(m.room1.States.Count()), tools.FormatInt(m.room2.States.Count()),
			tools.FormatInt(m.room1.Variants.Count()), tools.FormatInt(m.room2.Variants.Count()))) {
			return nil, nil
		}
	}
	if maxMoves > 0 {
		total := uint64(0)
		for _, room := range n.Rooms {
			total += room.MinMoves()
		}
		if total > maxMoves {
			return nil, fmt.Errorf("max moves %s liegt unter dem bewiesenen Minimum %s - Schranke unerreichbar",
				tools.FormatInt(maxMoves), tools.FormatInt(total))
		}
		limit := room1.MinMoves() + room2.MinMoves() + (maxMoves - total)
		if limit > uint64(^uint32(0)) {
			limit = uint64(^uint32(0))
		}
		m.moveLimit = uint32(limit)
	}
	if !m.Step1MixStates() || !m.Step2StartVariants() || !m.Step3PortalVariants() {
		return nil, nil // abgebrochen, Netzwerk unverändert
	}
	m.Step4UpdatePortals()
	m.Step5OptimizeStates()
	m.Step6UpdateRooms()

	// Deadlock-Scan auf dem neuen Raum (M4), mit dem Kosten-Gating des Originals:
	// die Zustands-Masken wachsen mit 2^Portale, und beim (fast) fertig gemergten
	// Netzwerk lohnt der Scan nicht mehr. Ein Abbruch im Scan lässt den bereits
	// gemergten Raum einfach unoptimiert stehen.
	if len(m.NewRoom.Incoming) <= 12 && len(n.Rooms) > 2 && m.NewRoom.Variants.Count() < 10_000_000 {
		n.DeadlockScan(m.NewRoom, info)
	}

	// Struktur komplett, Varianten nur vom neuen Raum: die übrigen Räume hat
	// der Merge nachweislich nicht angefasst (nur Portal-Verweise, die die
	// Struktur-Prüfung abdeckt) - ein Voll-Validate machte sonst JEDEN Merge
	// so teuer wie die Monster-Räume des Netzwerks
	if err := n.ValidateRooms(m.NewRoom); err != nil {
		return nil, fmt.Errorf("validate after merge: %w", err)
	}
	n.warmMinMoves() // Caches vorwärmen (lesende API-Zugriffe bleiben race-frei)
	return m.NewRoom, nil
}

// MergeSelection verschmilzt eine Raum-Auswahl: solange zwei ausgewählte Räume
// direkt verbunden sind, wird (in Index-Reihenfolge) paarweise gemergt. Nicht
// verbundene Reste bleiben stehen. Liefert die Anzahl der ausgeführten Merges.
func (n *Network) MergeSelection(indices []uint32, maxMoves uint64, info ProgressFunc) (int, error) {
	selected := map[*Room]bool{}
	for _, idx := range indices {
		if int(idx) >= len(n.Rooms) {
			return 0, fmt.Errorf("merge: invalid room index %d", idx)
		}
		selected[n.Rooms[idx]] = true
	}

	merges := 0
	for {
		// wie das C#-Original: das verbundene Paar mit dem kleinsten Effort
		// (Produkt der Variantenzahlen) zuerst - eine schlechte Reihenfolge
		// lässt Zwischen-Räume explodieren, die der Auto-Scan nicht mehr
		// einfangen kann (Gating). Gleichstand: kleinster Raum-Index gewinnt.
		var a, b *Room
		bestEffort := math.Inf(1)
		for _, room := range n.Rooms {
			if !selected[room] {
				continue
			}
			for _, op := range room.Outgoing {
				if !selected[op.ToRoom] || op.ToRoom.Index < room.Index {
					continue // jedes Paar nur einmal betrachten
				}
				effort := float64(room.Variants.Count()) * float64(op.ToRoom.Variants.Count())
				if effort < bestEffort {
					bestEffort, a, b = effort, room, op.ToRoom
				}
			}
		}
		if a == nil {
			return merges, nil // kein verbundenes Paar mehr in der Auswahl
		}
		delete(selected, a)
		delete(selected, b)
		merged, err := n.MergeRooms(a, b, maxMoves, info)
		if err != nil {
			return merges, err
		}
		if merged == nil {
			return merges, nil // abgebrochen
		}
		selected[merged] = true
		merges++
	}
}
