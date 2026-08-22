package rooms

import (
	"fmt"
	"strings"

	"goSokoWahnRooms/soko"
	"goSokoWahnRooms/tools"
)

// Solver: bidirektionale Brute-Force-Suche auf dem Rooms-Netzwerk (C#-Vorbild
// RoomSolver war rein vorwärts, die Rückwärts-/Bidirektional-Technik kommt
// aus goSokoWahnBrute): Aufgaben-Listen je Zugtiefe plus Hash-Dedup, in
// BEIDEN Richtungen mit derselben Aufgaben-Normalform. Eine Aufgabe = Zustand
// je Raum + Spieler (Raum und eingehendes Portal) + Pfad-ID in den Solver-
// eigenen PathStore. Vorwärts bedeutet die Aufgabe "so weit bin ich gekommen"
// (Pfad = Laufweg vom Start hierher), rückwärts "von hier aus kenne ich den
// Rest" (Pfad = Laufweg von hier bis zum gelösten Level). Weil beide Fronten
// dieselben Schlüssel benutzen, erkennt ein Hash-Lookup in der Gegentabelle
// das Fronten-Treffen: Gesamtlösung = Vorwärtstiefe + Rückwärtstiefe, Pfad =
// Vorwärtspfad + Rückwärtspfad. Jeder Treffer wird gegen das echte Spielfeld
// verifiziert - scheitert das, war es eine 64-Bit-Hash-Kollision und der
// Kandidat wird verworfen (wie brutes verifyMeet).
//
// Vorwärts expandiert eine Aufgabe wie das C#-ResolveTask: erst alle reinen
// Lauf-Varianten transitiv sammeln (Dedup je Raum+Variante, billigste Ankunft
// gewinnt), jede erreichte Push-Variante erzeugt eine neue Aufgabe. Rückwärts
// läuft dasselbe spiegelbildlich (siehe solverback.go): Push-Varianten werden
// über ein invertiertes Varianten-Verzeichnis (Austrittsportal, NewState)
// rückgängig gemacht (Kisten-Einschübe per invertiertem BoxSwap zurückziehen),
// dann liefert eine Rückwärts-Laufkette alle Eintrittspunkte, von denen aus
// die Variante erreichbar war - jeder ist eine Vorgänger-Aufgabe.
//
// Die Tiefen werden je Front aufsteigend abgearbeitet - BEWIESEN move-optimal
// ist die beste Lösung, sobald Vorwärts- plus Rückwärts-Abarbeitungstiefe das
// Limit übersteigt (jede unentdeckte Lösung müsste an irgendeiner Push-Grenze
// in eine schon verarbeitete Vorwärts- und eine schon erzeugte Rückwärts-
// Aufgabe zerfallen) - oder wenn eine Front erschöpft ist (dann ist ihr
// Zustandsraum innerhalb des Limits vollständig aufgezählt).
//
// Die Richtung wählt wie in brute eine Automatik einmal je Gesamttiefe: das
// Effizienz-Verhältnis "erreichte Tiefe je Hash-Eintrag" (Kreuzmultiplikation),
// manuell übersteuerbar per SetDirMode (GUI-Tasten 1/2/3).
//
// Pruning, das das C# nicht hatte: je Raum liefert der Zustands-Dijkstra
// (minmoves.go) die bewiesene Untergrenze "Zustand -> gelöst" (vorwärts) bzw.
// "Start -> Zustand" (rückwärts); die Summe über alle Räume ist eine zulässige
// Restkosten-Schranke gegen das Budget (max moves bzw. beste bekannte Lösung
// minus 1), Aufgaben darüber entstehen gar nicht erst.
//
// Speicher-/Tempo-Design (profiliert am frischen 202er): der Hash prüft
// schon beim ERZEUGEN einer Aufgabe (nicht erst beim Abholen), sonst
// bestehen die Tiefenlisten zu >80% aus Duplikaten; Hash und Heuristik
// werden per Delta aus der Basis-Aufgabe fortgeschrieben (O(geänderte Räume)
// statt O(alle Räume) je Kandidat, der XOR-Hash über die Raum-Zustände macht
// das möglich); Laufwege werden erst beim Eintragen einer akzeptierten
// Aufgabe materialisiert (die Lauf-Expansion merkt sich nur Vorgänger-
// Indizes - abgelehnte Ketten kosten keine PathStore-Knoten).
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

// manuelle Richtungsvorgabe (übersteuert die Automatik, GUI-Tasten 1/2/3)
type DirMode int

const (
	DirAuto     DirMode = iota // Effizienz-Verhältnis: erreichte Tiefe je Hash-Eintrag
	DirForward                 // nur vorwärts
	DirBackward                // nur rückwärts
)

// Budget-Sentinel: kein Move-Limit gesetzt
const solveNoBudget = ^uint32(0)

// Untergrenzen-Sentinel: Zielzustand der Richtung von hier aus unerreichbar
const remainInf = ^uint32(0)

// ein Glied der Lauf-Expansion: Variante samt Laufkosten; der Laufweg
// entsteht erst bei Bedarf über die Vorgänger-Kette (path/pathSet memoisieren).
// Vorwärts zählt moves von der Basis-Aufgabe bis zum EINTRITT in die Variante,
// rückwärts von deren Eintritt bis zurück zur Basis-Aufgabe (inkl. der Variante)
type chainStep struct {
	moves   uint32
	parent  int32 // Vorgänger-Glied (-1 = Wurzel: Basis-Pfad der Aufgabe)
	room    *Room
	variant uint64
	path    PathID
	pathSet bool
}

// eine Zustandsänderung (Kisten-Einschub bzw. dessen Rücknahme oder eigener Raum)
type stateChange struct {
	idx   uint32
	state uint64
}

// eine Suchfront (vorwärts bzw. rückwärts): Aufgaben-Listen je Zugtiefe,
// flach kodiert (je Aufgabe len(rooms) Zustände + (RaumIndex<<32|PortalIndex)
// + PathID), dazu der Dedup-Hash und die Untergrenzen ihrer Richtung
type solveFront struct {
	buckets [][]uint64
	hash    map[uint64]uint64 // Aufgaben-Schlüssel -> Tiefe<<32 | PathID (beste bekannte Kopie)
	remain  [][]uint32        // je Raum: Moves-Untergrenze bis zum Ziel der Richtung (remainInf = nie)

	depth     int // aktuelle Abarbeitungs-Tiefe
	offset    int // Verarbeitungsposition im aktuellen Bucket (in uint64)
	processed uint64
	skipped   uint64 // veraltete Aufgaben (bessere Kopie wurde schon verarbeitet)
	created   uint64
}

// gepackter Hash-Eintrag einer Front: beste bekannte Tiefe + zugehöriger Pfad
func packRef(depth uint32, path PathID) uint64 { return uint64(depth)<<32 | uint64(path) }

// rückt die Abarbeitung über leere/fertige Tiefen vor (Buckets freigeben)
func (f *solveFront) skipEmpty() {
	for f.depth < len(f.buckets) && f.offset >= len(f.buckets[f.depth]) {
		f.buckets[f.depth] = nil
		f.depth++
		f.offset = 0
	}
}

// Front erschöpft: alle erzeugten Aufgaben sind verarbeitet
func (f *solveFront) exhausted() bool { return f.depth >= len(f.buckets) }

// hängt eine neue Aufgabe an ihre Ziel-Tiefe an
func (f *solveFront) enqueue(moves uint32, states []uint64, roomPortal uint64, path PathID) {
	for int(moves) >= len(f.buckets) {
		f.buckets = append(f.buckets, nil)
	}
	bucket := append(f.buckets[moves], states...)
	f.buckets[moves] = append(append(bucket, roomPortal), uint64(path))
	f.created++
}

type Solver struct {
	n      *Network
	rooms  []*Room
	budget uint32 // hartes Move-Limit (inklusive), solveNoBudget = keins

	paths *PathStore // Laufwege aller Aufgaben (wächst nur, Sharing über Concat)

	taskSize int
	fwd, bwd solveFront
	rev      []roomReverse // je Raum die Rückwärts-Verzeichnisse (siehe solverback.go)

	// Portal-Kanonisierung (2026-08-22, Max' Befund "brute braucht weniger
	// Hash-Einträge"): Portale mit gleichem To sind verschiedene KANTEN zum
	// selben Feld - ohne Normalisierung liegt dieselbe physische Stellung
	// (Zustände + Spielerfeld) bis zu 4x im Hash, je Herkunfts-Richtung eine
	// Kopie. Aufgaben-Schlüssel benutzen daher das kanonische Portal je
	// Eintritts-Feld (= brutes Normalform), die Expansion läuft über die
	// VEREINIGUNG der Spans aller Portale der Gruppe (die kantenspezifischen
	// Rücklauf-Ausschlüsse der Spans sind Dominanz-Abkürzungen - die
	// Vereinigung enthält nur legale Züge, die Ketten-Dedup fängt Duplikate).
	// AUSNAHME: liegt im Aufgaben-Zustand eine KISTE auf dem Eintritts-Feld,
	// ist die Ankunfts-Richtung semantisch - der Ankunfts-Schritt (gebucht
	// als Austritts-Zug der Vor-Variante) hat sie physisch bereits in seine
	// Richtung geschoben, nur die Buchung (BoxPortals/BoxSwap) übernimmt die
	// nächste Variante. Solche Aufgaben behalten ihr kantenspezifisches
	// Portal (gleiche Zustände + gleiches Feld sind dann VERSCHIEDENE
	// physische Stellungen; Lehre vom ersten Anlauf: kanonisiert spielte
	// Level 200 "spielbar, aber nicht gelöst").
	canonPortal [][]uint32   // je Raum, je Portal: kanonischer Index (kleinster mit gleichem To)
	portalGroup [][][]uint32 // je Raum, je kanonischem Index: alle Portale mit gleichem To (sonst nil)

	// Richtungswahl: Automatik einmal je Gesamttiefe, manuell übersteuerbar
	dirMode    DirMode
	dirDepth   int // Gesamttiefe der letzten Automatik-Entscheidung (-1 = noch keine)
	dirForward bool

	best       *Solution
	collisions uint64 // verworfene Schein-Treffen (64-Bit-Hash-Kollisionen)
	done       bool
	doneMsg    string
	err        error

	// Basis-Werte der gerade expandierten Aufgabe (einmal O(Räume), die
	// Kandidaten schreiben sie per Delta fort)
	basePath   PathID
	baseHash   uint64 // XOR der stateMix aller Räume
	baseRemain uint64 // Summe der Untergrenzen der aktiven Richtung
	baseNonZero int   // Anzahl ungelöster Räume (nur vorwärts genutzt)

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
// (maxMoves > 0 = hartes Budget) und trägt die Start-Aufgaben beider
// Richtungen ein (rückwärts zuerst, damit die Vorwärts-Saat Blitz-Treffen
// über den Gegen-Hash sofort erkennt)
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
		dirDepth:   -1,
		importMemo: map[*PathStore]map[PathID]PathID{},
	}
	s.fwd.hash = map[uint64]uint64{}
	s.bwd.hash = map[uint64]uint64{}
	s.visited = make([][]uint64, len(s.rooms))
	for i, room := range s.rooms {
		s.visited[i] = make([]uint64, room.Variants.Count())
	}
	// Portal-Kanonisierung: je Eintritts-Feld (To) das kleinste Portal als
	// Vertreter, die Gruppe sammelt alle Kanten zum selben Feld
	s.canonPortal = make([][]uint32, len(s.rooms))
	s.portalGroup = make([][][]uint32, len(s.rooms))
	for i, room := range s.rooms {
		canon := make([]uint32, len(room.Incoming))
		group := make([][]uint32, len(room.Incoming))
		first := map[soko.Wpos]uint32{}
		for p, ip := range room.Incoming {
			c, ok := first[ip.To]
			if !ok {
				c = uint32(p)
				first[ip.To] = c
			}
			canon[p] = c
			group[c] = append(group[c], uint32(p))
		}
		s.canonPortal[i] = canon
		s.portalGroup[i] = group
	}
	if maxMoves > 0 {
		s.budget = maxMoves
	}

	// Untergrenzen je Raum und Richtung (Moves-Anteil des Zustands-Dijkstra):
	// vorwärts "Zustand -> gelöst", rückwärts "Start -> Zustand"
	s.fwd.remain = make([][]uint32, len(s.rooms))
	s.bwd.remain = make([][]uint32, len(s.rooms))
	for i, room := range s.rooms {
		s.fwd.remain[i] = movesOf(room.stateDistances(0, true))
		s.bwd.remain[i] = movesOf(room.stateDistances(room.StartState, false))
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
		if r := s.fwd.remain[i][room.StartState]; r == remainInf {
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

	// Rückwärts-Verzeichnisse und Rückwärts-Saat (alle End-Varianten mit
	// gelöstem Endzustand, zurückgerollt auf ihre Vor-Stellungen)
	s.buildReverse()
	s.seedBackward()

	// Vorwärts-Saat: Spieler startet im Startraum, alle Startvarianten
	s.basePath = EmptyPath
	s.baseHash, s.baseRemain, s.baseNonZero = statesHash, lower, nonZero
	s.resolveTask(0, states, EmptyPath, startRoom, NoPortal)

	// Steht das Ergebnis schon nach der Saat fest (z.B. nach einem Voll-Merge:
	// nur noch End-Varianten, keine offenen Aufgaben), schließt Step(0) die
	// Suche sofort ab - die GUI kann die Lösung direkt anbieten
	s.Step(0)
	return s, nil
}

// Moves-Anteil einer Dijkstra-Distanztabelle (minMovesInf wird zu remainInf)
func movesOf(dist []uint64) []uint32 {
	remain := make([]uint32, len(dist))
	for id, d := range dist {
		remain[id] = uint32(d >> 32)
	}
	return remain
}

// SetDirMode stellt die Richtungsvorgabe um (wirkt ab dem nächsten Step)
func (s *Solver) SetDirMode(mode DirMode) { s.dirMode = mode }
func (s *Solver) DirMode() DirMode        { return s.dirMode }

// Step verarbeitet bis zu maxTasks Aufgaben der gewählten Richtung, aber
// höchstens deren AKTUELLE Tiefenzeile (wie im C#-Original und in brute:
// ein Bulk endet an der Tiefengrenze - so bleibt das Abarbeiten in der GUI
// zeilenweise sichtbar). Liefert true, wenn die Suche beendet ist
// (Optimum bewiesen, Budget ausgeschöpft oder Fehler).
func (s *Solver) Step(maxTasks int) bool {
	if s.done {
		return true
	}
	s.fwd.skipEmpty()
	s.bwd.skipEmpty()
	if s.checkFinished() {
		return true
	}

	// die aktuelle Tiefenzeile der gewählten Front abarbeiten (neue Aufgaben
	// landen immer in höheren Tiefen - jede Push-Variante kostet mindestens
	// einen Zug)
	forward := s.chooseDirection()
	f := &s.fwd
	if !forward {
		f = &s.bwd
	}
	bucket := f.buckets[f.depth]
	for maxTasks > 0 && !s.done && f.offset < len(bucket) {
		task := bucket[f.offset : f.offset+s.taskSize]
		f.offset += s.taskSize
		if forward {
			s.processTask(task)
		} else {
			s.processTaskBackward(task)
		}
		maxTasks--
	}

	// Zeile komplett? Tiefe sofort weiterschalten, damit Status-Anzeige und
	// Fertig-Erkennung nicht einen Bulk hinterherhinken
	if !s.done && f.offset >= len(bucket) {
		f.buckets[f.depth] = nil
		f.depth++
		f.offset = 0
		f.skipEmpty()
		s.checkFinished()
	}
	return s.done
}

// prüft die Abbruchbedingungen: eine Front erschöpft (ihr Zustandsraum ist
// innerhalb des Limits vollständig aufgezählt - Treffen wären beim Erzeugen
// der jeweils späteren Kopie erkannt worden) oder die Tiefensumme übersteigt
// das Limit (jede unentdeckte Lösung <= Limit zerfällt an einer Push-Grenze
// in eine Vorwärts-Aufgabe unterhalb der Vorwärtstiefe und eine Rückwärts-
// Aufgabe unterhalb der Rückwärtstiefe - beide wären erzeugt, die spätere
// hätte das Treffen gemeldet)
func (s *Solver) checkFinished() bool {
	if s.fwd.exhausted() || s.bwd.exhausted() ||
		uint64(s.fwd.depth)+uint64(s.bwd.depth) > s.limit() {
		s.finish()
	}
	return s.done
}

// wählt die Richtung des nächsten Bulks: erschöpfte Fronten scheiden aus,
// manuelle Vorgabe gewinnt, sonst entscheidet die Automatik einmal je
// Gesamttiefe (brutes Effizienz-Verhältnis: vertieft wird die Richtung, die
// bisher pro Hash-Eintrag die meisten Züge erreicht hat; Vergleich per
// Kreuzmultiplikation, Anlauf-Kriterium: kleinere Tabelle zuerst)
func (s *Solver) chooseDirection() bool {
	if s.bwd.exhausted() {
		return true
	}
	if s.fwd.exhausted() {
		return false
	}
	switch s.dirMode {
	case DirForward:
		return true
	case DirBackward:
		return false
	}
	if sum := s.fwd.depth + s.bwd.depth; sum != s.dirDepth {
		s.dirDepth = sum
		fd, bd := int64(s.fwd.depth), int64(s.bwd.depth)
		if fd == 0 || bd == 0 {
			s.dirForward = len(s.fwd.hash) < len(s.bwd.hash)
		} else {
			s.dirForward = fd*int64(len(s.bwd.hash)) >= bd*int64(len(s.fwd.hash))
		}
	}
	return s.dirForward
}

// beendet die Suche und formuliert das Ergebnis
func (s *Solver) finish() {
	s.done = true
	s.fwd.buckets, s.bwd.buckets = nil, nil
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

// SplitMix64-Finalizer: volle Lawinenwirkung über alle 64 Bit. Der frühere
// FNV-Mix (crc64-Paket) war hier eine ECHTE Falle: für kleine Zustands-IDs
// liefert FNV hochkorrelierte Werte ((A_i ^ s) * M² - arithmetische
// Progressionen mit derselben Konstante), deren XOR-Differenzen sich über
// mehrere Räume systematisch auslöschen - am Vanilla kollidierten zigtausend
// VERSCHIEDENE Stellungen auf denselben Schlüssel (aufgeflogen durch die
// verifizierten Fronten-Treffen; die Hash-Dedup hätte still Stellungen
// verschmolzen). Ein Zobrist-XOR-Schema braucht unabhängig-zufällige Werte
// je (Raum, Zustand) - genau das liefert der Mixer.
func mix64(x uint64) uint64 {
	x ^= x >> 30
	x *= 0xbf58476d1ce4e5b9
	x ^= x >> 27
	x *= 0x94d049bb133111eb
	x ^= x >> 31
	return x
}

// Hash-Beitrag eines Raum-Zustands (XOR aller Räume = Aufgaben-Hash;
// XOR macht den Hash beim Zustandswechsel eines Raums fortschreibbar)
func stateMix(roomIndex uint32, state uint64) uint64 {
	return mix64(mix64(uint64(roomIndex)+0x9e3779b97f4a7c15) ^ state)
}

// Aufgaben-Schlüssel aus Zustands-Hash und Spieler-Position; wie in brute
// wird das 64-Bit-Hashing ohne Kollisionsbehandlung akzeptiert (Treffen
// werden verifiziert, die Dedup vertraut dem Schlüssel)
func taskKey(statesHash, roomPortal uint64) uint64 {
	return mix64(statesHash ^ mix64(roomPortal+0x9e3779b97f4a7c15))
}

// liegt im Zustand eines Raums eine Kiste auf dem Feld pos?
// (die Kisten-Listen der Zustände sind aufsteigend sortiert)
func stateHasBox(room *Room, state uint64, pos soko.Wpos) bool {
	for _, b := range room.States.Get(state) {
		if b >= pos {
			return b == pos
		}
	}
	return false
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

// verarbeitet eine Vorwärts-Aufgabe: Veraltet-Check (eine billigere Kopie
// derselben Stellung wurde bereits verarbeitet), Basis-Werte aufbauen,
// expandieren
func (s *Solver) processTask(task []uint64) {
	states := task[:len(s.rooms)]
	roomPortal := task[len(s.rooms)]

	// Basis-Werte in einem Durchlauf (Hash, Untergrenzen-Summe, Ungelöst-Zähler)
	var statesHash, remainSum uint64
	nonZero := 0
	for i, state := range states {
		statesHash ^= stateMix(uint32(i), state)
		remainSum += uint64(s.fwd.remain[i][state])
		if state != 0 {
			nonZero++
		}
	}
	if s.fwd.hash[taskKey(statesHash, roomPortal)]>>32 < uint64(s.fwd.depth) {
		s.fwd.skipped++ // eine billigere Kopie wurde bereits eingetragen/verarbeitet
		return
	}
	s.baseHash, s.baseRemain, s.baseNonZero = statesHash, remainSum, nonZero

	room := s.rooms[roomPortal>>32]
	s.fwd.processed++
	s.resolveTask(uint32(s.fwd.depth), states, PathID(task[len(s.rooms)+1]), room, uint32(roomPortal))
}

// expandiert eine Vorwärts-Aufgabe (C#-Vorbild ResolveTask): erst alle über
// reine Lauf-Varianten erreichbaren Varianten sammeln (Dedup je Raum+Variante,
// billigste Ankunft gewinnt), dann jede erreichte Push-Variante anwenden.
// portalIdx == NoPortal expandiert die Startvarianten des Startraums.
func (s *Solver) resolveTask(d uint32, states []uint64, base PathID, room *Room, portalIdx uint32) {
	s.basePath = base
	list := s.chainList[:0]
	gen := s.nextGen()

	if portalIdx == NoPortal {
		for id := uint64(0); id < room.StartVariantCount; id++ {
			s.visited[room.Index][id] = gen
			list = append(list, chainStep{parent: -1, room: room, variant: id})
		}
	} else if stateHasBox(room, states[room.Index], room.Incoming[portalIdx].To) {
		// Kiste auf dem Eintritts-Feld: die Aufgabe ist kantenspezifisch
		// (der Ankunfts-Schritt hat die Kiste bereits in SEINE Richtung
		// geschoben) - nur der eigene Span darf expandieren
		span := room.Incoming[portalIdx].GetVariantSpan(states[room.Index])
		for id := span.Start; id < span.Start+span.Count; id++ {
			s.visited[room.Index][id] = gen
			list = append(list, chainStep{parent: -1, room: room, variant: id})
		}
	} else {
		// portalIdx ist kanonisch: die Aufgabe vertritt alle Ankünfte auf
		// diesem Feld - expandiert wird die Vereinigung der Gruppen-Spans
		// (disjunkte Varianten-Bereiche, kein Dedup nötig)
		for _, pIdx := range s.portalGroup[room.Index][portalIdx] {
			span := room.Incoming[pIdx].GetVariantSpan(states[room.Index])
			for id := span.Start; id < span.Start+span.Count; id++ {
				s.visited[room.Index][id] = gen
				list = append(list, chainStep{parent: -1, room: room, variant: id})
			}
		}
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

// schaltet den Generationszähler der Lauf-Dedups weiter (Überlauf: einmal
// echt leeren) und liefert den Generations-Stempel
func (s *Solver) nextGen() uint64 {
	s.gen++
	if s.gen == 0 {
		for _, v := range s.visited {
			clear(v)
		}
		s.gen = 1
	}
	return uint64(s.gen) << 32
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
		rNew := s.fwd.remain[idx][now]
		if rNew == remainInf {
			return // von hier aus beweisbar unlösbar
		}
		remainSum += int64(rNew) - int64(s.fwd.remain[idx][old])
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
	// beim Abholen (processTask). Der Schlüssel trägt das KANONISCHE Portal
	// des Eintritts-Feldes (Ankünfte über verschiedene Kanten sind dieselbe
	// physische Stellung) - AUSSER eine Kiste liegt auf dem Feld, dann ist
	// die Ankunfts-Richtung semantisch und die Kante bleibt im Schlüssel
	portalIdx := uint64(op.Index)
	if !stateHasBox(toRoom, cur(toRoom.Index), op.To) {
		portalIdx = uint64(s.canonPortal[toRoom.Index][op.Index])
	}
	roomPortal := uint64(toRoom.Index)<<32 | portalIdx
	k := taskKey(newHash, roomPortal)
	if old, ok := s.fwd.hash[k]; ok && old>>32 <= endMoves {
		return
	}

	// erst jetzt wird es teuer: Laufweg materialisieren, Zustände kopieren
	path := s.paths.Concat(s.stepPath(list, i), s.importPath(st.room, v.Path))
	s.fwd.hash[k] = packRef(uint32(endMoves), path)
	newStates := append(s.stateBuf[:0], states...)
	for _, ch := range changes {
		newStates[ch.idx] = ch.state
	}
	s.stateBuf = newStates
	// Fronten-Treffen: kennt die Rückwärtsfront dieselbe Stellung, ergibt
	// Vorwärts- plus Rückwärtsweg eine Lösungs-Kandidatin
	if ref, ok := s.bwd.hash[k]; ok {
		s.tryMeet(path, PathID(ref), endMoves+ref>>32)
	}
	s.fwd.enqueue(uint32(endMoves), newStates, roomPortal, path)
}

// prüft eine Treff-Kandidatin der beiden Fronten: der zusammengesetzte Weg
// wird gegen das echte Spielfeld verifiziert - scheitert das, war es eine
// 64-Bit-Hash-Kollision (Schein-Treffen) und die Kandidatin wird verworfen
func (s *Solver) tryMeet(fwdPath, bwdPath PathID, total uint64) {
	if (s.best != nil && total >= uint64(s.best.Moves)) || total > uint64(s.budget) {
		return
	}
	lurd := s.paths.LURD(s.paths.Concat(fwdPath, bwdPath))
	if err := s.n.Field.CheckSolution(lurd); err != nil {
		s.collisions++
		return
	}
	s.best = &Solution{Moves: uint32(total), Pushes: countPushes(s.n.Field, lurd), Path: lurd}
}

// trägt eine direkt gefundene Lösung ein - verifiziert gegen das echte
// Spielfeld (schlägt das fehl, ist es ein Solver-Bug und die Suche endet
// mit Fehler; anders als bei Treff-Kandidatinnen steckt hier keine
// Gegen-Hash-Kollision als harmlose Erklärung dahinter)
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

// Status liefert den Live-Zustand als mehrzeiligen Text: Kopfzeile, beste
// Lösung und je Front die offenen Aufgaben je Zugtiefe (zwei Spalten wie
// brutes TUI; die manuell fixierte Richtung ist markiert)
func (s *Solver) Status() string {
	var b strings.Builder
	if s.done {
		b.WriteString("solve: fertig - ")
		b.WriteString(s.doneMsg)
		fmt.Fprintf(&b, "\nverarbeitet %s aufgaben (%s veraltet), hash %s",
			tools.FormatInt(s.fwd.processed+s.bwd.processed),
			tools.FormatInt(s.fwd.skipped+s.bwd.skipped),
			tools.FormatInt(len(s.fwd.hash)+len(s.bwd.hash)))
		if s.collisions > 0 {
			fmt.Fprintf(&b, "\nhash-kollisionen verworfen: %s", tools.FormatInt(s.collisions))
		}
		return b.String()
	}

	limitStr := "-"
	if limit := s.limit(); limit != uint64(solveNoBudget) {
		limitStr = tools.FormatInt(limit)
	}
	fmt.Fprintf(&b, "solve: tiefe %s (%s+%s) / limit %s",
		tools.FormatInt(s.fwd.depth+s.bwd.depth),
		tools.FormatInt(s.fwd.depth), tools.FormatInt(s.bwd.depth), limitStr)
	fmt.Fprintf(&b, "\nhash %s - offen %s+%s",
		tools.FormatInt(len(s.fwd.hash)+len(s.bwd.hash)),
		tools.FormatInt(s.openTasks(&s.fwd)), tools.FormatInt(s.openTasks(&s.bwd)))
	if s.best != nil {
		rest := int(s.best.Moves) - s.fwd.depth - s.bwd.depth
		fmt.Fprintf(&b, "\nbeste lösung: %s züge / %s pushes (rest-beweis %s)",
			tools.FormatInt(s.best.Moves), tools.FormatInt(s.best.Pushes), tools.FormatInt(max(rest, 0)))
	}
	if s.collisions > 0 {
		fmt.Fprintf(&b, "\nhash-kollisionen verworfen: %s", tools.FormatInt(s.collisions))
	}
	b.WriteString("\n")

	// zwei Tiefenspalten nebeneinander (leere Tiefen nicht listen)
	fwdTitle, bwdTitle := "vorwärts", "rückwärts"
	switch s.dirMode {
	case DirForward:
		fwdTitle += " [fix]"
	case DirBackward:
		bwdTitle += " [fix]"
	}
	left := frontRows(fwdTitle, &s.fwd, s.taskSize)
	right := frontRows(bwdTitle, &s.bwd, s.taskSize)
	const colWidth = 22
	for i := 0; i < len(left) || i < len(right); i++ {
		var l, r string
		if i < len(left) {
			l = left[i]
		}
		if i < len(right) {
			r = right[i]
		}
		b.WriteString("\n" + l + strings.Repeat(" ", max(colWidth-len([]rune(l)), 1)) + r)
	}
	return strings.TrimRight(b.String(), " \n")
}

// offene (noch nicht verarbeitete) Aufgaben einer Front
func (s *Solver) openTasks(f *solveFront) uint64 {
	var open uint64
	for d := f.depth; d < len(f.buckets); d++ {
		open += uint64(len(f.buckets[d]))
	}
	return (open - uint64(f.offset)) / uint64(s.taskSize)
}

// Tiefenzeilen einer Front (Titel + "[tiefe] anzahl" je nicht-leerer Tiefe)
func frontRows(title string, f *solveFront, taskSize int) []string {
	rows := []string{title}
	for d := f.depth; d < len(f.buckets); d++ {
		count := len(f.buckets[d])
		if d == f.depth {
			count -= f.offset
		}
		if count == 0 {
			continue
		}
		rows = append(rows, fmt.Sprintf("[%4d] %s", d, tools.FormatInt(count/taskSize)))
	}
	return rows
}
