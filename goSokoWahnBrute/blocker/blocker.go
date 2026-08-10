package blocker

import (
	"math/bits"
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

// Anker-Index der Deadlock-Muster einer Spielerposition (über alle fertigen Stufen):
// jedes Muster als Bitmaske über die begehbaren Felder, gebucketet nach dem Ankerfeld
// (= kleinstes Muster-Feld). CheckAllowed muss dadurch nur Buckets anfassen, deren
// Ankerfeld tatsächlich eine Kiste trägt - alle anderen Muster können nicht zutreffen.
type playerIndex struct {
	masks   []uint64 // flache Muster-Masken, nach Ankerfeld sortiert (Länge = Musteranzahl * maskWords)
	anchors []int32  // je Feld der Start-Offset (in Mustern) im masks-Array (Länge walkCount+2, inkl. Sentinel)
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

	maskWords  int           // uint64-Wörter je Bitmaske (= Länge der Field-boxBits)
	checkIndex []playerIndex // Anker-Index aller Muster je Spielerposition (nil = keine Muster)

	status         Status
	searchBoxCount int // Kistenanzahl der aktuellen Stufe

	// --- Arbeitszustand der laufenden Stufe ---
	work           *soko.Field       // Arbeitsfeld mit searchBoxCount Kisten
	known          solver.PosTable   // Stellungs-Marker der laufenden Stufe
	checkList      *solver.DepthList // Liste, die gerade abgearbeitet wird
	collectList    *solver.DepthList // Sammler für neu gefundene Stellungen
	badList        *solver.DepthList // alle möglicherweise verbotenen Stellungen
	goodList       *solver.DepthList // gute Stellungen, welche noch rückwärts zu verarbeiten sind
	combo          []int             // Kombinations-Odometer (Indizes in comboPositions)
	comboPositions []soko.Wpos       // Positionen für die Kombinationen (Start- oder Zielfelder)
	tempPatterns     [][]soko.Wpos // Muster-Sammler während CreatePatterns
	tempPatternCount int           // bereits eingesammelte Muster (für die Fortschrittsanzeige)
	mergeRest        int64         // Countdown der Verschmelzen-Phase (wie verschmelzenRest im Original, nur Anzeige)
	stageChecked     int64         // geprüfte Stellungen der Stufe (Hash-Stand beim Abschluss)
	recordSize     int               // Satzgröße der Listen = searchBoxCount + 1

	varBuf   []soko.State // Buffer für die Variantensuche
	curState soko.State   // Buffer für geladene Stellungen

	// --- Parallelisierung ---
	workerCount int             // Anzahl der Worker (1 = komplett seriell)
	chunkSize   int             // Sätze pro Arbeits-Zuteilung an einen Worker
	workers     []blockerWorker // Worker-Kontexte der laufenden Stufe

	tableFactory  func() solver.PosTable // erzeugt die Stufen-Hashtabelle (für Benchmarks austauschbar)
	directFactory func() DirectTable     // Direct-Write-Modus: Worker schreiben atomar selbst (nil = seriell mergen)
	directTable   DirectTable            // aktive Direct-Write-Tabelle der laufenden Stufe (nil = Standard-Modus)
}

func New(field *soko.Field, cachePath string) *Blocker {
	base := field.Clone()
	start := soko.State{}
	base.GetState(&start)

	b := &Blocker{
		base:          base,
		walkCount:     base.WalkCount(),
		maxBoxes:      base.BoxCount(),
		startPlayer:   start.Player,
		cachePath:     cachePath,
		status:        StatusInit,
		maskWords:     (base.WalkCount() + 64) / 64, // walkEof+1 Bits, gleiche Länge wie Field.boxBits
		workerCount:   runtime.NumCPU() * 8, // deutliche Überbelegung: die Worker warten meist auf den Speicher (siehe docs/architektur.md)
		chunkSize:     defaultChunkSize,
		tableFactory:  solver.NewCompactTable,
		directFactory: NewShardDirect, // Standard: Direct-Write ohne seriellen Merge, speicherschonende Shard-Variante
		// (NewXsyncDirect ist ca. 9% schneller, braucht aber ca. 3x mehr RAM - siehe docs/architektur.md)
	}

	if cachePath != "" {
		b.loadCache() // Fehler werden ignoriert: dann wird schlicht neu gerechnet
	}
	b.rebuildCheckIndex()

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

// tauscht die Hashtabellen-Implementierung aus (für Benchmarks unter Realbedingungen);
// wirkt ab der nächsten Stufe
func (b *Blocker) SetTableFactory(factory func() solver.PosTable) {
	b.tableFactory = factory
}

// gibt an, ob die Blocker-Erstellung noch läuft
func (b *Blocker) Creating() bool {
	return b.status != StatusDone
}

// beendet die Erstellung vorzeitig; bereits fertige Stufen bleiben für CheckAllowed aktiv
func (b *Blocker) Abort() {
	b.status = StatusDone
	b.releaseStageWork()
}

// prüft, ob eine Stellung erlaubt ist (false = als Deadlock erkannt);
// boxBits ist die Kisten-Bitmaske des abfragenden Feldes (Field.boxBits).
// Über den Anker-Index werden nur Muster geprüft, deren Ankerfeld eine Kiste trägt;
// der Mustervergleich selbst ist ein Bitmasken-Subset-Test.
//
// Bedingte Kill-Regel (Fix des Bx-Hinterland-Bugs, siehe docs/architektur.md):
// Ein zutreffendes Muster allein reicht NICHT - die Stellung wird erst verworfen,
// wenn JEDER Schub-Pose-Kandidat (jede Kiste, die als "zuletzt geschobene" infrage
// kommt) von einem zutreffenden Muster abgedeckt ist. Hintergrund: Muster aus dem
// Ziel-Hinterland ("rückwärts erreichbar, vorwärts nie gesehen") beweisen nur, dass
// die Stellung nicht durch den Schub einer MUSTER-Kiste entstanden sein kann. Steht
// der Spieler nach dem Schub einer fremden Kiste zufällig in der Muster-Pose, ist
// die Stellung trotzdem legal (so verlor Level 29632 seine optimale 304er-Lösung).
// Erst wenn alle Kandidaten abgedeckt sind, ist jede mögliche Entstehung der
// Stellung entweder widerlegt oder ein bewiesener Deadlock.
// (Implementierung von soko.BlockerCheck, wird von den Zuggeneratoren aufgerufen)
func (b *Blocker) CheckAllowed(player soko.Wpos, boxBits []uint64) bool {
	if b.checkIndex == nil {
		return true
	}
	idx := &b.checkIndex[player]
	masks := idx.masks
	if len(masks) == 0 {
		return true
	}
	words := b.maskWords
	anchors := idx.anchors

	// Schub-Pose-Kandidaten erst beim ersten Muster-Treffer ermitteln
	// (der mit Abstand häufigste Fall ist "kein Muster trifft zu")
	var candidates [4]soko.Wpos
	candCount := 0
	covered, allCovered := 0, 0

	for w := range boxBits {
		bitsWord := boxBits[w]
		for bitsWord != 0 {
			field := w<<6 | bits.TrailingZeros64(bitsWord)
			bitsWord &= bitsWord - 1

			for m, mEnd := int(anchors[field])*words, int(anchors[field+1])*words; m < mEnd; m += words {
				match := true
				for j := 0; j < words; j++ {
					if masks[m+j]&^boxBits[j] != 0 {
						match = false // Muster-Feld ohne Kiste -> Muster trifft nicht zu
						break
					}
				}
				if !match {
					continue
				}

				if allCovered == 0 { // erster Treffer -> Kandidaten aufbauen
					candidates, candCount = b.base.PushPoseCandidates(player, boxBits)
					if candCount == 0 {
						return true // keine Schub-Pose -> Muster nicht anwendbar (praktisch nur bei künstlichen Abfragen)
					}
					allCovered = 1<<candCount - 1
				}
				for c := 0; c < candCount; c++ {
					pos := candidates[c]
					if masks[m+int(pos>>6)]&(1<<(pos&63)) != 0 {
						covered |= 1 << c // dieses Muster deckt den Kandidaten ab
					}
				}
				if covered == allCovered {
					return false // jede mögliche zuletzt geschobene Kiste ist abgedeckt -> verbotene Stellung
				}
			}
		}
	}
	return true
}

// baut den Anker-Index für CheckAllowed neu auf (nach jeder fertigen Stufe und nach dem
// Cache-Laden): alle Muster aller Stufen als Bitmasken, je Spielerposition nach ihrem
// Ankerfeld einsortiert (die Muster sind kanonisch aufsteigend sortiert -> Anker = erstes Feld)
func (b *Blocker) rebuildCheckIndex() {
	if len(b.stages) == 0 {
		b.checkIndex = nil
		return
	}

	words := b.maskWords
	index := make([]playerIndex, b.walkCount)

	for player := 0; player < b.walkCount; player++ {
		// Muster je Ankerfeld zählen
		anchors := make([]int32, b.walkCount+2)
		total := 0
		for s := range b.stages {
			k := b.stages[s].boxCount
			pat := b.stages[s].patterns[player]
			for p := 0; p < len(pat); p += k {
				anchors[pat[p]+2]++
				total++
			}
		}
		if total == 0 {
			continue
		}

		// Zähler zu Start-Offsets aufsummieren (anchors[f+1] = Start des Buckets von Feld f)
		for i := 2; i < len(anchors); i++ {
			anchors[i] += anchors[i-1]
		}

		// Masken in die Buckets einsortieren (anchors[f+1] dient dabei als Schreibzeiger
		// und steht am Ende genau auf dem Bucket-Start von Feld f)
		masks := make([]uint64, total*words)
		for s := range b.stages {
			k := b.stages[s].boxCount
			pat := b.stages[s].patterns[player]
			for p := 0; p < len(pat); p += k {
				slot := anchors[pat[p]+1]
				anchors[pat[p]+1]++
				off := int(slot) * words
				for i := 0; i < k; i++ {
					fld := pat[p+i]
					masks[off+int(fld>>6)] |= 1 << (fld & 63)
				}
			}
		}

		index[player] = playerIndex{masks: masks, anchors: anchors[:b.walkCount+2]}
	}

	b.checkIndex = index
}

// initialisiert den Arbeitszustand für eine neue Stufe
func (b *Blocker) initStage() {
	k := b.searchBoxCount
	b.recordSize = k + 1
	b.work = b.base.CloneWithBoxCount(k)
	b.work.SetBlocker(b)         // bereits fertige Stufen filtern schon beim Stufenbau mit
	b.work.SetBlockerBackward(b) // auch rückwärts filtern (Bx-Semantik, vermeidet redundante Muster)
	if b.directFactory != nil {
		b.directTable = b.directFactory()
		b.known = b.directTable
	} else {
		b.directTable = nil
		b.known = b.tableFactory()
	}
	b.checkList = solver.NewDepthList(b.recordSize, b.walkCount)
	b.collectList = solver.NewDepthList(b.recordSize, b.walkCount)
	b.badList = solver.NewDepthList(b.recordSize, b.walkCount)
	b.goodList = solver.NewDepthList(b.recordSize, b.walkCount)
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
	for _, list := range b.stageLists() {
		if list != nil {
			list.Release() // löscht auch eine eventuelle Auslagerungsdatei
		}
	}
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

// die vier Suchlisten der laufenden Stufe (Einträge können nil sein)
func (b *Blocker) stageLists() [4]*solver.DepthList {
	return [4]*solver.DepthList{b.checkList, b.collectList, b.badList, b.goodList}
}

// auf die Festplatte ausgelagerte Bytes der Stufen-Suchlisten (0 = alles im RAM)
func (b *Blocker) SpillBytes() int64 {
	var sum int64
	for _, list := range b.stageLists() {
		if list != nil {
			sum += list.SpillBytes()
		}
	}
	return sum
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
