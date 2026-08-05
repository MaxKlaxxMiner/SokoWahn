package solver

import "goSokoWahnBrute/soko"

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

	forwardOnly bool // Sonderfall: keine Zielstellungen vorhanden (sehr kurze Level) -> reine Vorwärtssuche
	done        bool // gibt an, ob die Suche abgeschlossen ist

	dirDepth   int  // Suchtiefe, für welche die Richtungswahl zuletzt getroffen wurde
	dirForward bool // gecachte Richtungswahl (true = vorwärts)

	hashUsage []int64 // Hash-Gesamtnutzung je Suchtiefe (Datenbasis der Max-Tiefen-Schätzung)

	varBuf   []soko.State // wiederverwendbarer Buffer für die Variantensuche
	curState soko.State   // Buffer für die aktuell geladene Stellung
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
		forwardKnown:  NewCompactTable(),
		backwardKnown: NewCompactTable(),
		forwardDepth:  0,
		backwardDepth: 0,
		foundTotal:    -1,
		dirDepth:      -1,
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

// trägt eine Stellung in die Vorwärts-Suchliste ihrer Zugtiefe ein
func (s *Solver) pushForward(v *soko.State) {
	depth := int(v.MoveDepth)
	for depth >= len(s.forwardLists) {
		s.forwardLists = append(s.forwardLists, NewDepthList(s.recordSize))
	}
	s.forwardLists[depth].Push(v)
}

// trägt eine Stellung in die Rückwärts-Suchliste ihrer Zugtiefe ein
func (s *Solver) pushBackward(v *soko.State) {
	depth := int(v.MoveDepth)
	for depth >= len(s.backwardLists) {
		s.backwardLists = append(s.backwardLists, NewDepthList(s.recordSize))
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
