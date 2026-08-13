package solver

import (
	"runtime"

	"goSokoWahnBrute/crc64"
	"goSokoWahnBrute/soko"
)

// manuelle Richtungsvorgabe der Suche (Tasten 1/2/3 im TUI)
type DirMode int

const (
	DirAuto     DirMode = iota // Richtung automatisch anhand der Hashtabellen-Größen wählen
	DirForward                 // nur vorwärts suchen
	DirBackward                // nur rückwärts suchen
)

// bidirektionaler Brute-Force-Solver: Vorwärtssuche von der Startstellung und
// Rückwärtssuche von den Zielstellungen laufen aufeinander zu, bis sich beide
// Suchfronten in einer gemeinsamen Stellung treffen (Nachbau von SokoWahn_4th)
type Solver struct {
	base *soko.Field // Basisfeld mit der Startstellung (bleibt unverändert)
	work *soko.Field // Arbeitsfeld für die Variantensuche

	boxCount   int         // Anzahl der Kisten
	recordSize int         // Satzgröße der Suchlisten = boxCount + 1
	goals      []soko.Wpos // Zielfelder (aufsteigend sortiert)

	forwardKnown  PosTable     // alle bekannten Stellungen der Vorwärtssuche
	backwardKnown PosTable     // alle bekannten Stellungen der Rückwärtssuche
	forwardLists  []*DepthList // noch zu prüfende Stellungen je Vorwärtstiefe
	backwardLists []*DepthList // noch zu prüfende Stellungen je Rückwärtstiefe
	forwardDepth  int          // aktuell bearbeitete Vorwärtstiefe
	backwardDepth int          // aktuell bearbeitete Rückwärtstiefe

	foundTotal        int        // beste gefundene Lösungslänge in Zügen, -1 = noch keine
	foundState        soko.State // Verbindungs-Stellung der besten Lösung
	foundForwardDepth int        // Vorwärtstiefe der Verbindungs-Stellung
	collisionRejects  int64      // verworfene Schein-Verbindungen (64-Bit-Hash-Kollisionen, siehe verifyMeet)

	// alle verifizierten Verbindungs-Stellungen der aktuell besten Lösungslänge
	// (Anker für die Push-Optimierung, siehe pushopt.go; gedeckelt und dedupliziert)
	meetAnchors []meetAnchor
	meetSeen    map[crc64.Value]struct{}

	forwardOnly bool // Sonderfall: keine Zielstellungen vorhanden (sehr kurze Level) -> reine Vorwärtssuche
	done        bool // gibt an, ob die Suche abgeschlossen ist

	dirDepth   int     // Suchtiefe, für welche die Richtungswahl zuletzt getroffen wurde
	dirForward bool    // gecachte Richtungswahl (true = vorwärts)
	dirMode    DirMode // manuelle Richtungsvorgabe (übersteuert dirForward, Default: DirAuto)

	hashUsage []int64 // Hash-Gesamtnutzung je Suchtiefe (Datenbasis der Max-Tiefen-Schätzung)
	processed int64   // insgesamt verarbeitete Sätze (Datenbasis der Stellungen/s-Anzeige)

	varBuf   []soko.State // wiederverwendbarer Buffer für die Variantensuche
	curState soko.State   // Buffer für die aktuell geladene Stellung

	// --- Parallelisierung (siehe parallel.go) ---
	workerCount int            // Anzahl der Such-Worker (1 = komplett seriell)
	workers     []searchWorker // Worker-Kontexte (lazy beim ersten parallelen Batch)
}

func New(field *soko.Field) *Solver {
	base := field.Clone()

	// das Arbeitsfeld filtert auch rückwärts mit dem Blocker (wie die List2-Variante des
	// Originals): rückwärts erreichbare, aber vorwärts unerreichbare Stellungen entfallen.
	// Das Zielstellungs-Seeding über base bleibt ungefiltert (wie im Original).
	work := base.Clone()
	work.SetBlockerBackward(base.Blocker())

	s := &Solver{
		base:          base,
		work:          work,
		boxCount:      base.BoxCount(),
		recordSize:    base.BoxCount() + 1,
		goals:         base.Goals(),
		forwardKnown:  TableFactory(),
		backwardKnown: TableFactory(),
		forwardDepth:  0,
		backwardDepth: 0,
		foundTotal:    -1,
		dirDepth:      -1,
		workerCount:   runtime.NumCPU() * 4, // Überbelegung kaschiert die Speicherlatenz (Benchmark-Sweep lid4208, siehe docs/architektur.md)
	}

	s.varBuf = base.MakeStateBuffer(256)[:0]
	s.curState = soko.State{Boxes: make([]soko.Wpos, s.boxCount)}
	s.foundState = soko.State{Boxes: make([]soko.Wpos, s.boxCount)}

	// --- Vorwärtssuche initialisieren ---
	// Achtung: eine bereits gelöste Startstellung ist KEINE 0-Züge-Lösung - das Spiel
	// prüft den Zielzustand erst nach einem Zug. Solche Levels löst die normale Suche
	// von selbst korrekt: jede Variante endet mit einem Schub, die kürzeste Lösung
	// schiebt also eine Kiste heraus und stellt die Zielstellung wieder her.
	start := soko.State{}
	base.GetState(&start)
	s.forwardKnown.Add(start.Crc, 0)
	s.pushForward(&start)

	// --- Rückwärtssuche initialisieren ---
	for _, goalState := range base.SearchGoalStates() {
		if s.backwardKnown.Get(goalState.Crc) == DepthUnknown {
			s.backwardKnown.Add(goalState.Crc, 0)
			s.pushBackward(&goalState)
		}
	}

	// ohne Zielstellungen (1-Schub-Level u.ä.) bleibt nur die reine Vorwärtssuche
	s.forwardOnly = s.backwardKnown.Len() == 0

	return s
}

// setzt die manuelle Richtungsvorgabe der Suche (DirAuto = automatische Wahl je Suchtiefe).
// Wirkt nur auf die normale Suchphase: nach gefundener Lösung ist der Rest der
// Beweisführung vorgegeben und der forwardOnly-Sonderfall kennt ohnehin nur eine Richtung.
// Bei DirBackward wird die Vorwärts-Tiefe 0 trotzdem zuerst abgearbeitet (siehe Step).
func (s *Solver) SetDirMode(mode DirMode) {
	s.dirMode = mode
}

// aktuelle manuelle Richtungsvorgabe
func (s *Solver) DirMode() DirMode {
	return s.dirMode
}

// gibt alle Suchlisten samt eventueller Auslagerungsdateien frei; die Hashtabellen
// bleiben erhalten, GetStats und GetSolution funktionieren also weiterhin.
// Nach dem Abschluss oder beim Verwerfen der Suche aufrufen - nicht mittendrin.
func (s *Solver) Close() {
	for _, list := range s.forwardLists {
		list.Release()
	}
	for _, list := range s.backwardLists {
		list.Release()
	}
}

// trägt eine Stellung in die Vorwärts-Suchliste ihrer Zugtiefe ein
func (s *Solver) pushForward(v *soko.State) {
	depth := int(v.MoveDepth)
	for depth >= len(s.forwardLists) {
		s.forwardLists = append(s.forwardLists, NewDepthList(s.recordSize, s.base.WalkCount()))
	}
	s.forwardLists[depth].Push(v)
}

// trägt eine Stellung in die Rückwärts-Suchliste ihrer Zugtiefe ein
func (s *Solver) pushBackward(v *soko.State) {
	depth := int(v.MoveDepth)
	for depth >= len(s.backwardLists) {
		s.backwardLists = append(s.backwardLists, NewDepthList(s.recordSize, s.base.WalkCount()))
	}
	s.backwardLists[depth].Push(v)
}

// lädt einen Suchlisten-Satz in den curState-Buffer
func (s *Solver) loadRecord(record []uint16, depth int32) {
	s.curState.Player = soko.Wpos(record[0])
	for i := 0; i < s.boxCount; i++ {
		s.curState.Boxes[i] = soko.Wpos(record[1+i])
	}
	s.curState.MoveDepth = depth
	s.curState.UpdateCrc()
}

// merkt sich eine Kopie der übergebenen Stellung als beste Verbindungs-Stellung
func (s *Solver) copyFoundState(v *soko.State) {
	s.foundState.Player = v.Player
	copy(s.foundState.Boxes, v.Boxes)
	s.foundState.MoveDepth = v.MoveDepth
	s.foundState.Crc = v.Crc
}
