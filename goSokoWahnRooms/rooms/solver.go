package rooms

import (
	"fmt"
	"strings"

	"goSokoWahnRooms/crc64"
	"goSokoWahnRooms/soko"
	"goSokoWahnRooms/tools"
)

// Solver: Brute-Force-Vorwärtssuche auf dem Rooms-Netzwerk (C#-Vorbild
// RoomSolver, Technik wie goSokoWahnBrute): Aufgaben-Listen je Zugtiefe
// plus Hash-Dedup. Eine Aufgabe = Zustand je Raum + Spieler (Raum und
// eingehendes Portal) + Pfad-ID in den Solver-eigenen PathStore (damit
// liefert die Suche den echten LURD-Laufweg - das C# hat das nie fertig
// bekommen). Je Aufgabe werden erst alle reinen Lauf-Varianten transitiv
// expandiert (Dedup je Raum+Variante, billigste Ankunft gewinnt), jede
// Push-Variante erzeugt eine neue Aufgabe bei ihrer Gesamt-Zugtiefe.
// Gelöst = alle Räume in Zustand 0 und der Spieler bleibt drin.
//
// Die Tiefen werden aufsteigend abgearbeitet - die erste gefundene Lösung
// ist eine Kandidatin, BEWIESEN move-optimal ist sie erst, wenn die
// Abarbeitungs-Tiefe ihre Zugzahl erreicht (dann endet die Suche).
//
// Pruning, das das C# nicht hatte: je Raum liefert der Zustands-Dijkstra
// (siehe minmoves.go, rückwärts) die bewiesene Untergrenze "Zustand ->
// gelöst"; die Summe über alle Räume ist eine zulässige Restkosten-
// Schranke gegen das Budget (max moves bzw. beste bekannte Lösung minus 1),
// Aufgaben darüber entstehen gar nicht erst.
//
// Speicher-/Tempo-Design (profiliert am frischen 202er): der Hash prüft
// schon beim ERZEUGEN einer Aufgabe (nicht erst beim Abholen), sonst
// bestehen die Tiefenlisten zu >80% aus Duplikaten; Hash, Heuristik und
// Gelöst-Zähler werden per Delta aus der Basis-Aufgabe fortgeschrieben
// (O(geänderte Räume) statt O(alle Räume) je Push-Kandidat, der XOR-Hash
// über die Raum-Zustände macht das möglich); Laufwege werden erst beim
// Eintragen einer akzeptierten Aufgabe materialisiert (die Lauf-Expansion
// merkt sich nur Vorgänger-Indizes - abgelehnte Ketten kosten keine
// PathStore-Knoten).
//
// Der Solver ist ein schrittbarer Zustandsautomat: Step(n) verarbeitet
// bis zu n Aufgaben, die Interaktivität (Bulk/Auto/Stop wie in brute)
// liegt beim Aufrufer (web). Er LIEST das Netzwerk nur - während einer
// Solver-Sitzung darf es sich aber nicht ändern (Merge & Co. sperren).

// Lösung des Solvers: kompletter LURD-Laufweg vom Spielerstart bis zum
// gelösten Level (Moves == len(Path), gegen das Spielfeld verifiziert)
type Solution struct {
	Moves  uint32
	Pushes uint32
	Path   string
}

// Budget-Sentinel: kein Move-Limit gesetzt
const solveNoBudget = ^uint32(0)

// Untergrenzen-Sentinel: gelöster Zustand von hier aus unerreichbar
const remainInf = ^uint32(0)

// ein Glied der Lauf-Expansion: Variante samt Ankunftskosten; der Laufweg
// entsteht erst bei Bedarf über die Vorgänger-Kette (path/pathSet memoisieren)
type chainStep struct {
	moves   uint32 // Moves von der Basis-Aufgabe bis zum Eintritt in die Variante
	parent  int32  // Vorgänger-Glied (-1 = Wurzel: Basis-Pfad der Aufgabe)
	room    *Room
	variant uint64
	path    PathID // memoisiert: Laufweg vom Spielstart bis zum Eintritt
	pathSet bool
}

// eine Zustandsänderung einer Push-Variante (Kisten-Einschub oder eigener Raum)
type stateChange struct {
	idx   uint32
	state uint64
}

type Solver struct {
	n      *Network
	rooms  []*Room
	budget uint32 // hartes Move-Limit (inklusive), solveNoBudget = keins

	remain [][]uint32 // je Raum: Moves-Untergrenze Zustand -> gelöst (remainInf = nie)
	paths  *PathStore // Laufwege aller Aufgaben (wächst nur, Sharing über Concat)

	// Aufgaben-Listen je Gesamt-Zugtiefe, flach kodiert:
	// je Aufgabe len(rooms) Zustände + (RaumIndex<<32|PortalIndex) + PathID
	taskSize int
	buckets  [][]uint64
	hash     map[uint64]uint32 // Aufgaben-Schlüssel -> beste bekannte Zugtiefe

	depth     int    // aktuelle Abarbeitungs-Tiefe
	offset    int    // Verarbeitungsposition im aktuellen Bucket (in uint64)
	processed uint64 // verarbeitete Aufgaben gesamt
	skipped   uint64 // veraltete Aufgaben (bessere Kopie wurde schon verarbeitet)
	created   uint64 // eingetragene Aufgaben gesamt

	best    *Solution
	done    bool
	doneMsg string
	err     error

	// Basis-Werte der gerade expandierten Aufgabe (einmal O(Räume), die
	// Push-Kandidaten schreiben sie per Delta fort)
	basePath    PathID
	baseHash    uint64 // XOR der stateMix aller Räume
	baseRemain  uint64 // Summe der Untergrenzen (endlich, sonst wäre die Aufgabe nie entstanden)
	baseNonZero int    // Anzahl ungelöster Räume

	// Dedup der Lauf-Expansion: je Raum und Variante die billigste Ankunft
	// der AKTUELLEN Aufgabe (gen<<32|moves; der Generationszähler erspart
	// das Leeren zwischen den Aufgaben, Speicher = 8 Bytes je Variante)
	visited [][]uint64
	gen     uint32

	// wiederverwendete Puffer (eine Aufgabe = ein Durchlauf)
	chainList  []chainStep
	changeBuf  []stateChange
	stateBuf   []uint64
	importMemo map[*PathStore]map[PathID]PathID // Varianten-Pfade je Quell-Store einmal importieren
}

// NewSolver initialisiert die Suche auf dem aktuellen Netzwerk-Stand
// (maxMoves > 0 = hartes Budget) und trägt die Start-Aufgaben ein
func NewSolver(n *Network, maxMoves uint32) (*Solver, error) {
	var startRoom *Room
	for _, room := range n.Rooms {
		if room.StartVariantCount > 0 {
			if startRoom != nil {
				return nil, fmt.Errorf("solve: mehrere start-räume")
			}
			startRoom = room
		}
	}
	if startRoom == nil {
		return nil, fmt.Errorf("solve: kein start-raum")
	}

	s := &Solver{
		n:          n,
		rooms:      n.Rooms,
		budget:     solveNoBudget,
		paths:      NewPathStore(),
		taskSize:   len(n.Rooms) + 2,
		hash:       map[uint64]uint32{},
		importMemo: map[*PathStore]map[PathID]PathID{},
	}
	s.visited = make([][]uint64, len(s.rooms))
	for i, room := range s.rooms {
		s.visited[i] = make([]uint64, room.Variants.Count())
	}
	if maxMoves > 0 {
		s.budget = maxMoves
	}

	// Untergrenzen "Zustand -> gelöst" je Raum (Moves-Anteil des Dijkstra)
	s.remain = make([][]uint32, len(s.rooms))
	for i, room := range s.rooms {
		dist := room.stateDistances(0, true)
		remain := make([]uint32, len(dist))
		for id, d := range dist {
			remain[id] = uint32(d >> 32) // minMovesInf wird dabei zu remainInf
		}
		s.remain[i] = remain
	}

	states := make([]uint64, len(s.rooms))
	solved := true
	infeasible := false
	var lower, statesHash uint64
	nonZero := 0
	for i, room := range s.rooms {
		states[i] = room.StartState
		statesHash ^= stateMix(uint32(i), room.StartState)
		if room.StartState != 0 {
			solved = false
			nonZero++
		}
		if r := s.remain[i][room.StartState]; r == remainInf {
			infeasible = true
		} else {
			lower += uint64(r)
		}
	}
	if solved {
		// Level startet gelöst - keine Suche nötig
		s.best = &Solution{}
		s.finish()
		return s, nil
	}

	// Machbarkeits-Check über die Untergrenzen, bevor irgendwas rechnet
	if infeasible {
		s.done, s.doneMsg = true, "keine lösung - level im modell unlösbar"
		return s, nil
	}
	if lower > uint64(s.budget) {
		s.done = true
		s.doneMsg = fmt.Sprintf("keine lösung bis budget %s möglich (bewiesene untergrenze %s)",
			tools.FormatInt(s.budget), tools.FormatInt(lower))
		return s, nil
	}

	// Start-Aufgaben: Spieler startet im Startraum, alle Startvarianten
	s.baseHash, s.baseRemain, s.baseNonZero = statesHash, lower, nonZero
	s.resolveTask(0, states, EmptyPath, startRoom, NoPortal)
	return s, nil
}

// Step verarbeitet bis zu maxTasks Aufgaben, aber höchstens die AKTUELLE
// Tiefenzeile (wie im C#-Original und in brute: ein Bulk endet an der
// Tiefengrenze - so bleibt das Abarbeiten in der GUI zeilenweise sichtbar).
// Liefert true, wenn die Suche beendet ist (Optimum bewiesen, Budget
// ausgeschöpft oder Fehler).
func (s *Solver) Step(maxTasks int) bool {
	if s.done {
		return true
	}
	// zur nächsten nicht-leeren Tiefe vorrücken (Buckets freigeben)
	for s.depth < len(s.buckets) && s.offset >= len(s.buckets[s.depth]) {
		s.buckets[s.depth] = nil
		s.depth++
		s.offset = 0
	}
	if s.depth >= len(s.buckets) ||
		(s.best != nil && uint32(s.depth) >= s.best.Moves) {
		s.finish()
		return true
	}

	// die aktuelle Tiefenzeile abarbeiten (neue Aufgaben landen immer in
	// höheren Tiefen - jede Push-Variante kostet mindestens einen Zug)
	bucket := s.buckets[s.depth]
	for maxTasks > 0 && !s.done && s.offset < len(bucket) {
		task := bucket[s.offset : s.offset+s.taskSize]
		s.offset += s.taskSize
		s.processTask(task)
		maxTasks--
	}

	// Zeile komplett? Tiefe sofort weiterschalten, damit Status-Anzeige und
	// Fertig-Erkennung nicht einen Bulk hinterherhinken
	if !s.done && s.offset >= len(bucket) {
		s.buckets[s.depth] = nil
		s.depth++
		s.offset = 0
		for s.depth < len(s.buckets) && len(s.buckets[s.depth]) == 0 {
			s.depth++
		}
		if s.depth >= len(s.buckets) ||
			(s.best != nil && uint32(s.depth) >= s.best.Moves) {
			s.finish()
		}
	}
	return s.done
}

// beendet die Suche und formuliert das Ergebnis
func (s *Solver) finish() {
	s.done = true
	s.buckets = nil
	switch {
	case s.err != nil:
		s.doneMsg = s.err.Error()
	case s.best != nil:
		s.doneMsg = fmt.Sprintf("lösung: %s züge / %s pushes - optimum bewiesen",
			tools.FormatInt(s.best.Moves), tools.FormatInt(s.best.Pushes))
	case s.budget != solveNoBudget:
		s.doneMsg = fmt.Sprintf("keine lösung bis budget %s (bewiesen)", tools.FormatInt(s.budget))
	default:
		s.doneMsg = "keine lösung - level im modell unlösbar"
	}
}

func (s *Solver) Done() bool          { return s.done }
func (s *Solver) Err() error          { return s.err }
func (s *Solver) Solution() *Solution { return s.best }

// ResultText liefert die Abschluss-Beschreibung (erst nach Done gültig)
func (s *Solver) ResultText() string { return s.doneMsg }

func (s *Solver) fail(err error) {
	s.err = err
	s.finish()
}

// wirksames Move-Limit: Budget bzw. beste bekannte Lösung minus 1
// (eine gleich lange Lösung brächte nichts Neues)
func (s *Solver) limit() uint64 {
	limit := uint64(s.budget)
	if s.best != nil && uint64(s.best.Moves)-1 < limit {
		limit = uint64(s.best.Moves) - 1
	}
	return limit
}

// Hash-Beitrag eines Raum-Zustands (XOR aller Räume = Aufgaben-Hash;
// XOR macht den Hash beim Zustandswechsel eines Raums fortschreibbar)
func stateMix(roomIndex uint32, state uint64) uint64 {
	return uint64(crc64.Start.UpdateUInt32(roomIndex).UpdateUInt64(state))
}

// Aufgaben-Schlüssel aus Zustands-Hash und Spieler-Position; wie in brute
// wird das 64-Bit-Hashing ohne Kollisionsbehandlung akzeptiert
func taskKey(statesHash, roomPortal uint64) uint64 {
	return uint64(crc64.Value(statesHash).UpdateUInt64(roomPortal))
}

// importiert den Laufweg einer Raum-Variante in den Solver-Store
// (memoisiert je Quell-Store: jede Variante wird höchstens einmal kopiert)
func (s *Solver) importPath(room *Room, id PathID) PathID {
	memo := s.importMemo[room.Paths]
	if memo == nil {
		memo = map[PathID]PathID{}
		s.importMemo[room.Paths] = memo
	}
	return s.paths.CopyFrom(room.Paths, id, memo)
}

// Laufweg vom Spielstart bis zum Eintritt in das Ketten-Glied i - läuft
// die Vorgänger-Kette hoch und memoisiert jedes Glied (abgelehnte Ketten
// materialisieren so nie einen Pfad)
func (s *Solver) stepPath(list []chainStep, i int32) PathID {
	st := &list[i]
	if st.pathSet {
		return st.path
	}
	prefix := s.basePath
	if st.parent >= 0 {
		p := &list[st.parent]
		prefix = s.paths.Concat(s.stepPath(list, st.parent),
			s.importPath(p.room, p.room.Variants.Get(p.variant).Path))
	}
	st.path, st.pathSet = prefix, true
	return prefix
}

// hängt eine neue Aufgabe an ihre Ziel-Tiefe an
func (s *Solver) enqueue(moves uint32, states []uint64, roomPortal uint64, path PathID) {
	for int(moves) >= len(s.buckets) {
		s.buckets = append(s.buckets, nil)
	}
	bucket := append(s.buckets[moves], states...)
	s.buckets[moves] = append(append(bucket, roomPortal), uint64(path))
	s.created++
}

// verarbeitet eine Aufgabe: Veraltet-Check (eine billigere Kopie derselben
// Stellung wurde bereits verarbeitet), Basis-Werte aufbauen, expandieren
func (s *Solver) processTask(task []uint64) {
	states := task[:len(s.rooms)]
	roomPortal := task[len(s.rooms)]

	// Basis-Werte in einem Durchlauf (Hash, Untergrenzen-Summe, Ungelöst-Zähler)
	var statesHash, remainSum uint64
	nonZero := 0
	for i, state := range states {
		statesHash ^= stateMix(uint32(i), state)
		remainSum += uint64(s.remain[i][state])
		if state != 0 {
			nonZero++
		}
	}
	if s.hash[taskKey(statesHash, roomPortal)] < uint32(s.depth) {
		s.skipped++ // eine billigere Kopie wurde bereits eingetragen/verarbeitet
		return
	}
	s.baseHash, s.baseRemain, s.baseNonZero = statesHash, remainSum, nonZero

	room := s.rooms[roomPortal>>32]
	s.processed++
	s.resolveTask(uint32(s.depth), states, PathID(task[len(s.rooms)+1]), room, uint32(roomPortal))
}

// expandiert eine Aufgabe (C#-Vorbild ResolveTask): erst alle über reine
// Lauf-Varianten erreichbaren Varianten sammeln (Dedup je Raum+Variante,
// billigste Ankunft gewinnt), dann jede erreichte Push-Variante anwenden.
// portalIdx == NoPortal expandiert die Startvarianten des Startraums.
func (s *Solver) resolveTask(d uint32, states []uint64, base PathID, room *Room, portalIdx uint32) {
	s.basePath = base
	list := s.chainList[:0]
	s.gen++
	if s.gen == 0 { // Generationszähler übergelaufen: einmal echt leeren
		for _, v := range s.visited {
			clear(v)
		}
		s.gen = 1
	}
	gen := uint64(s.gen) << 32

	span := Span{Start: 0, Count: room.StartVariantCount}
	if portalIdx != NoPortal {
		span = room.Incoming[portalIdx].GetVariantSpan(states[room.Index])
	}
	for id := span.Start; id < span.Start+span.Count; id++ {
		s.visited[room.Index][id] = gen
		list = append(list, chainStep{parent: -1, room: room, variant: id})
	}

	// --- Lauf-Expansion (Einfüge-Reihenfolge, veraltete Einträge überspringen) ---
	for i := 0; i < len(list); i++ {
		st := list[i]
		if st.moves > uint32(s.visited[st.room.Index][st.variant]) {
			continue // billigere Ankunft inzwischen bekannt
		}
		v := st.room.Variants.Get(st.variant)
		if v.Pushes > 0 || v.PlayerPortal == NoPortal {
			continue // Push-Varianten emittiert der zweite Pass; Lauf-Ende gibt es nicht
		}
		nextMoves := st.moves + v.Moves
		if uint64(d)+uint64(nextMoves) > s.limit() {
			continue
		}
		op := st.room.Outgoing[v.PlayerPortal]
		toSpan := op.GetVariantSpan(states[op.ToRoom.Index])
		visited := s.visited[op.ToRoom.Index]
		for id := toSpan.Start; id < toSpan.Start+toSpan.Count; id++ {
			if e := visited[id]; e >= gen && uint32(e) <= nextMoves {
				continue // gleiche Generation, billigere/gleiche Ankunft bekannt
			}
			visited[id] = gen | uint64(nextMoves)
			list = append(list, chainStep{moves: nextMoves, parent: int32(i), room: op.ToRoom, variant: id})
		}
	}

	// --- Push-Varianten anwenden (nur die jeweils billigste Ankunft) ---
	for i := range list {
		st := &list[i]
		if st.moves > uint32(s.visited[st.room.Index][st.variant]) {
			continue
		}
		if st.room.Variants.Get(st.variant).Pushes > 0 {
			s.emitPush(d, states, list, int32(i))
		}
	}
	s.chainList = list[:0] // Puffer (samt Wachstum) behalten
}

// wendet eine Push-Variante an: Kisten in die Nachbarräume schieben,
// Endstellung prüfen bzw. die Folge-Aufgabe eintragen. Alle Ableitungen
// (Hash, Untergrenze, Gelöst-Zähler) laufen als Delta über die geänderten
// Räume; Zustände werden erst beim Eintragen wirklich kopiert.
func (s *Solver) emitPush(d uint32, states []uint64, list []chainStep, i int32) {
	st := &list[i]
	v := st.room.Variants.Get(st.variant)
	endMoves := uint64(d) + uint64(st.moves) + uint64(v.Moves)
	if endMoves > s.limit() {
		return
	}

	// Zustandsänderungen einsammeln (sequenziell: mehrere Kisten können
	// nacheinander in denselben Nachbarraum gehen)
	changes := s.changeBuf[:0]
	defer func() { s.changeBuf = changes[:0] }()
	cur := func(idx uint32) uint64 {
		for j := len(changes) - 1; j >= 0; j-- {
			if changes[j].idx == idx {
				return changes[j].state
			}
		}
		return states[idx]
	}
	for _, bp := range v.BoxPortals {
		op := st.room.Outgoing[bp]
		idx := op.ToRoom.Index
		old := cur(idx)
		next := op.GetBoxSwap(old)
		if next == old {
			return // Nachbarraum kann die Kiste nicht aufnehmen
		}
		changes = append(changes, stateChange{idx: idx, state: next})
	}
	changes = append(changes, stateChange{idx: st.room.Index, state: v.NewState})

	// Deltas über die effektiv geänderten Räume (nur die letzte Änderung je Raum)
	newHash, remainSum := s.baseHash, int64(s.baseRemain)
	nonZero := s.baseNonZero
	for j := range changes {
		idx := changes[j].idx
		stale := false
		for k := j + 1; k < len(changes); k++ {
			if changes[k].idx == idx {
				stale = true // spätere Änderung desselben Raums gewinnt
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
		rNew := s.remain[idx][now]
		if rNew == remainInf {
			return // von hier aus beweisbar unlösbar
		}
		remainSum += int64(rNew) - int64(s.remain[idx][old])
		newHash ^= stateMix(idx, old) ^ stateMix(idx, now)
		if old != 0 {
			nonZero--
		}
		if now != 0 {
			nonZero++
		}
	}

	if v.PlayerPortal == NoPortal {
		// Spielende: zählt nur, wenn wirklich ALLE Räume gelöst sind
		if nonZero != 0 {
			return
		}
		if s.best == nil || endMoves < uint64(s.best.Moves) {
			s.recordSolution(uint32(endMoves),
				s.paths.Concat(s.stepPath(list, i), s.importPath(st.room, v.Path)))
		}
		return
	}
	if nonZero == 0 {
		return // gelöst, aber der Spieler läuft noch raus: ineffizientes Ende
	}
	if endMoves+uint64(remainSum) > s.limit() {
		return // Untergrenze schlägt das Limit: Aufgabe entsteht gar nicht erst
	}

	// mindestens eine Folge-Variante muss ihre Kisten loswerden können
	// (C#-Vorbild CheckTask, Tiefe 0: jedes Kisten-Portal einzeln geprüft)
	op := st.room.Outgoing[v.PlayerPortal]
	toRoom := op.ToRoom
	span := op.GetVariantSpan(cur(toRoom.Index))
	viable := false
	for id := span.Start; id < span.Start+span.Count && !viable; id++ {
		viable = true
		for _, bp := range toRoom.Variants.Get(id).BoxPortals {
			bop := toRoom.Outgoing[bp]
			if bop.GetBoxSwap(cur(bop.ToRoom.Index)) == cur(bop.ToRoom.Index) {
				viable = false
				break
			}
		}
	}
	if !viable {
		return
	}

	// Hash-Dedup beim ERZEUGEN (sonst fluten Duplikate die Tiefenlisten);
	// eine billigere Kopie entwertet die teurere über den Veraltet-Check
	// beim Abholen (processTask)
	roomPortal := uint64(toRoom.Index)<<32 | uint64(op.Index)
	k := taskKey(newHash, roomPortal)
	if old, ok := s.hash[k]; ok && uint64(old) <= endMoves {
		return
	}
	s.hash[k] = uint32(endMoves)

	// erst jetzt wird es teuer: Laufweg materialisieren, Zustände kopieren
	path := s.paths.Concat(s.stepPath(list, i), s.importPath(st.room, v.Path))
	newStates := append(s.stateBuf[:0], states...)
	for _, ch := range changes {
		newStates[ch.idx] = ch.state
	}
	s.stateBuf = newStates
	s.enqueue(uint32(endMoves), newStates, roomPortal, path)
}

// trägt eine gefundene Lösung ein - verifiziert gegen das echte Spielfeld
// (schlägt das fehl, ist es ein Solver-Bug und die Suche endet mit Fehler)
func (s *Solver) recordSolution(moves uint32, path PathID) {
	lurd := s.paths.LURD(path)
	if err := s.n.Field.CheckSolution(lurd); err != nil {
		s.fail(fmt.Errorf("solver-lösung ungültig (bug): %w", err))
		return
	}
	s.best = &Solution{Moves: moves, Pushes: countPushes(s.n.Field, lurd), Path: lurd}
}

// zählt die Kistenverschiebungen einer (bereits verifizierten) Lösung
func countPushes(f *soko.Field, lurd string) uint32 {
	eof := f.WalkEof()
	boxes := make([]bool, eof)
	for _, b := range f.InitBoxes() {
		boxes[b] = true
	}
	pos := f.InitPlayer()
	var pushes uint32
	for i := 0; i < len(lurd); i++ {
		next := f.Neighbor(pos, lurd[i])
		if next < eof && boxes[next] {
			boxes[next] = false
			boxes[f.Neighbor(next, lurd[i])] = true
			pushes++
		}
		pos = next
	}
	return pushes
}

// Status liefert den Live-Zustand als mehrzeiligen Text: Kopfzeile,
// beste Lösung und je Zugtiefe die offenen Aufgaben (wie brutes Tiefenzeilen)
func (s *Solver) Status() string {
	var b strings.Builder
	if s.done {
		b.WriteString("solve: fertig - ")
		b.WriteString(s.doneMsg)
		fmt.Fprintf(&b, "\nverarbeitet %s aufgaben (%s veraltet), hash %s",
			tools.FormatInt(s.processed), tools.FormatInt(s.skipped), tools.FormatInt(len(s.hash)))
		return b.String()
	}

	var open uint64
	for d := s.depth; d < len(s.buckets); d++ {
		open += uint64(len(s.buckets[d]))
	}
	open = (open - uint64(s.offset)) / uint64(s.taskSize)
	limitStr := "-"
	if limit := s.limit(); limit != uint64(solveNoBudget) {
		limitStr = tools.FormatInt(limit)
	}
	fmt.Fprintf(&b, "solve: tiefe %s / limit %s - offen %s - hash %s - verarbeitet %s",
		tools.FormatInt(s.depth), limitStr, tools.FormatInt(open),
		tools.FormatInt(len(s.hash)), tools.FormatInt(s.processed))
	if s.best != nil {
		fmt.Fprintf(&b, "\nbeste lösung: %s züge / %s pushes",
			tools.FormatInt(s.best.Moves), tools.FormatInt(s.best.Pushes))
	}
	b.WriteString("\n")
	for d := s.depth; d < len(s.buckets); d++ {
		count := len(s.buckets[d])
		if d == s.depth {
			count -= s.offset
		}
		if count == 0 {
			continue // leere Tiefen nicht listen (auch nicht die aktuelle)
		}
		fmt.Fprintf(&b, "\n[%4d] %s", d, tools.FormatInt(count/s.taskSize))
	}
	return b.String()
}
