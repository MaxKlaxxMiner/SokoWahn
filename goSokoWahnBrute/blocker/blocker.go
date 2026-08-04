package blocker

import (
	"runtime"

	"goSokoWahnBrute/soko"
	"goSokoWahnBrute/solver"
)

// Status der Blocker-Erstellung
type Status int

const (
	StatusInit           Status = iota // Start einer neuen Stufe (nächste Kistenanzahl)
	StatusCollectStart                 // sammelt alle Kombinationen der Start-Kistenfelder
	StatusCollectGoals                 // sammelt alle Kombinationen der Zielfelder
	StatusSearchVariants               // Vorwärtssuche über alle erreichbaren Stellungen
	StatusMergeGoals                   // Rückwärtssuche markiert alle Stellungen, die ein Ziel erreichen
	StatusCreatePatterns               // restliche Stellungen werden zu Blocker-Mustern
	StatusDone                         // Erstellung beendet, nur noch CheckAllowed aktiv
)

// Marker in der Stufen-Hashtabelle (Werte wie im C#-Original)
const (
	markerPending = uint16(12345) // Stellung gefunden, Prüfung steht noch aus
	markerGood    = uint16(60000) // Stellung kann ein Ziel erreichen
)

// eine fertig berechnete Blocker-Stufe: verbotene Kistenmuster je Spielerposition
type stage struct {
	boxCount      int           // Kistenanzahl dieser Stufe
	patterns      [][]soko.Wpos // je Spielerposition ein flaches Muster-Array (Länge = Musteranzahl * boxCount)
	checkedStates int64         // Anzahl der geprüften Stellungen (Statistik)
}

// Deadlock-Vorberechnung: findet für steigende Kistenanzahlen alle Kistenkombinationen,
// die nie mehr ein Ziel erreichen können (Nachbau von SokowahnBlocker aus dem C#-Original).
// Die Muster filtern anschließend die Vorwärtssuche des Solvers über CheckAllowed.
type Blocker struct {
	base        *soko.Field // Original-Feld mit voller Kistenanzahl (bleibt unverändert)
	walkCount   int         // Anzahl der begehbaren Felder
	maxBoxes    int         // Kistenanzahl des Levels
	startPlayer soko.Wpos   // Startposition des Spielers
	cachePath   string      // Pfad der Cache-Datei ("" = kein Cache)

	stages []stage // fertig berechnete Stufen (k = 1, 2, ...)

	status         Status
	searchBoxCount int    // Kistenanzahl der aktuellen Stufe
	emptyBoxNumber uint32 // Leer-Marker des abfragenden Feldes (Stufenbau: k, Hauptsuche: maxBoxes)

	// --- Arbeitszustand der laufenden Stufe ---
	work           *soko.Field       // Arbeitsfeld mit searchBoxCount Kisten
	known          solver.PosTable   // Stellungs-Marker der laufenden Stufe
	checkList      *solver.DepthList // Liste, die gerade abgearbeitet wird
	collectList    *solver.DepthList // Sammler für neu gefundene Stellungen
	badList        *solver.DepthList // alle möglicherweise verbotenen Stellungen
	goodList       *solver.DepthList // gute Stellungen, welche noch rückwärts zu verarbeiten sind
	combo          []int             // Kombinations-Odometer (Indizes in comboPositions)
	comboPositions []soko.Wpos       // Positionen für die Kombinationen (Start- oder Zielfelder)
	tempPatterns   [][]soko.Wpos     // Muster-Sammler während CreatePatterns
	stageChecked   int64             // geprüfte Stellungen der Stufe (Hash-Stand beim Abschluss)
	recordSize     int               // Satzgröße der Listen = searchBoxCount + 1

	varBuf   []soko.State // Buffer für die Variantensuche
	curState soko.State   // Buffer für geladene Stellungen

	// --- Parallelisierung ---
	workerCount int             // Anzahl der Worker (1 = komplett seriell)
	chunkSize   int             // Sätze pro Arbeits-Zuteilung an einen Worker
	workers     []blockerWorker // Worker-Kontexte der laufenden Stufe
}

func New(field *soko.Field, cachePath string) *Blocker {
	base := field.Clone()
	start := soko.State{}
	base.GetState(&start)

	b := &Blocker{
		base:           base,
		walkCount:      base.WalkCount(),
		maxBoxes:       base.BoxCount(),
		startPlayer:    start.Player,
		cachePath:      cachePath,
		status:         StatusInit,
		emptyBoxNumber: uint32(base.BoxCount()),
		workerCount:    runtime.NumCPU() * 8, // deutliche Überbelegung: die Worker warten meist auf den Speicher (siehe docs/architektur.md)
		chunkSize:      defaultChunkSize,
	}

	if cachePath != "" {
		b.loadCache() // Fehler werden ignoriert: dann wird schlicht neu gerechnet
	}

	return b
}

// setzt die Anzahl der Worker (1 = komplett seriell, z.B. für Debugging und Vergleiche);
// wirkt ab der nächsten Stufe
func (b *Blocker) SetWorkers(count int) {
	if count < 1 {
		count = 1
	}
	b.workerCount = count
}

// setzt die Chunk-Größe der Arbeitsverteilung (Sätze pro Zuteilung an einen Worker)
func (b *Blocker) SetChunkSize(size int) {
	if size < 1 {
		size = 1
	}
	b.chunkSize = size
}

// gibt an, ob die Blocker-Erstellung noch läuft
func (b *Blocker) Creating() bool {
	return b.status != StatusDone
}

// beendet die Erstellung vorzeitig; bereits fertige Stufen bleiben für CheckAllowed aktiv
func (b *Blocker) Abort() {
	b.status = StatusDone
	b.emptyBoxNumber = uint32(b.maxBoxes)
	b.releaseStageWork()
}

// prüft, ob eine Stellung erlaubt ist (false = als Deadlock erkannt);
// wposToBoxes ist der Kisten-Index des abfragenden Feldes, Werte >= emptyBoxNumber bedeuten "leer"
// (Implementierung von soko.BlockerCheck, wird von SearchVariantsForward aufgerufen)
func (b *Blocker) CheckAllowed(player soko.Wpos, wposToBoxes []uint32) bool {
	for s := range b.stages {
		st := &b.stages[s]
		pat := st.patterns[player]
		k := st.boxCount

	patternLoop:
		for p := 0; p < len(pat); p += k {
			for i := 0; i < k; i++ {
				if wposToBoxes[pat[p+i]] >= b.emptyBoxNumber {
					continue patternLoop // Feld ist leer -> Muster trifft nicht zu
				}
			}
			return false // alle Muster-Felder tragen Kisten -> verbotene Stellung
		}
	}
	return true
}

// initialisiert den Arbeitszustand für eine neue Stufe
func (b *Blocker) initStage() {
	k := b.searchBoxCount
	b.recordSize = k + 1
	b.work = b.base.CloneWithBoxCount(k)
	b.work.SetBlocker(b)         // bereits fertige Stufen filtern schon beim Stufenbau mit
	b.work.SetBlockerBackward(b) // auch rückwärts filtern (Bx-Semantik, vermeidet redundante Muster)
	b.emptyBoxNumber = uint32(k)
	b.known = solver.NewCompactTable()
	b.checkList = solver.NewDepthList(b.recordSize)
	b.collectList = solver.NewDepthList(b.recordSize)
	b.badList = solver.NewDepthList(b.recordSize)
	b.goodList = solver.NewDepthList(b.recordSize)
	b.varBuf = b.work.MakeStateBuffer(256)[:0]
	b.curState = soko.State{Boxes: make([]soko.Wpos, k)}
	if b.workerCount > 1 {
		b.initWorkers()
	}
}

// gibt den Arbeitszustand der laufenden Stufe frei
func (b *Blocker) releaseStageWork() {
	b.work = nil
	b.known = nil
	b.checkList = nil
	b.collectList = nil
	b.badList = nil
	b.goodList = nil
	b.combo = nil
	b.comboPositions = nil
	b.tempPatterns = nil
	b.varBuf = nil
	b.workers = nil
}

// lädt einen Suchlisten-Satz in den curState-Buffer
func (b *Blocker) loadRecord(record []uint16) {
	b.curState.Player = soko.Wpos(record[0])
	for i := 0; i < b.searchBoxCount; i++ {
		b.curState.Boxes[i] = soko.Wpos(record[1+i])
	}
	b.curState.MoveDepth = 0
	b.curState.UpdateCrc()
}
