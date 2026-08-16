package blocker

import (
	"runtime"
	"sort"
	"sync"
	"sync/atomic"

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

// Set-Trie der Deadlock-Muster einer Spielerposition (über alle fertigen Stufen):
// jedes Muster liegt als Pfad über seine kanonisch aufsteigend sortierten Felder in
// einem Präfix-Baum. CheckAllowed steigt nur in Kinder ab, deren Feld eine Kiste
// trägt - der Aufwand ist dadurch durch die Teilmengen der aktuellen Kistenmenge
// begrenzt (bei k Kisten höchstens 2^k besuchte Knoten, real weit weniger) und
// praktisch unabhängig von der Musteranzahl. Der frühere flache Anker-Index
// (Bitmasken-Buckets je kleinstem Muster-Feld) scannte dagegen linear über die
// Muster und brach bei Muster-Explosionen ein (Level 25523: 3,4 Mio 6-Steiner-
// Muster -> bis zu ~280.000 Maskenvergleiche pro Schub-Check, Suche Faktor ~30
// langsamer; der Trie besucht dort nur noch wenige hundert Knoten).
// Layout in BFS-Reihenfolge: die Kinder jedes Knotens liegen zusammenhängend und
// aufsteigend sortiert in fields (Cache-freundlicher linearer Scan je Knoten).
type playerTrie struct {
	fields     []uint16 // Muster-Feld je Knoten (Knoten 0 = Wurzel, Wert unbenutzt)
	childStart []int32  // Kinder von Knoten i: Knoten childStart[i] bis childStart[i+1]-1 (Länge Knotenzahl+1)
	isPattern  []uint64 // Bitset über die Knoten: an diesem Knoten endet ein Muster
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

	checkTries []playerTrie // Set-Trie aller Muster je Spielerposition (nil = keine Muster)

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

// Muster-Schwelle der adaptiven Regel-Filterung im Stufenbau (Feinanpassung von
// Max 08/2026, seit Cache-Version 8 bei 10240 - mittelgroße Stufen wie die
// 5.061 Muster der 201-Stufe-4 bauen damit wieder klassisch): solange alle
// fertigen Stufen höchstens so viele Muster haben, baut der Stufenbau
// klassisch - kleine Mustermengen kosten kaum Platz, filtern aber billiger als
// die Regeln (Set-Trie-Test schlägt den Freeze-Fixpunkt pro Schub) und
// bleiben bitgenau vergleichbar mit dem C#-Orakel (refcli blockerbx).
// Überschreitet eine fertige Stufe die Schwelle (Muster-Explosion, typisch für
// sehr große Levels), filtern alle WEITEREN Stufen ihre Vorwärts-Phasen mit den
// Regeln (Stufe 1 + Ziel-Matching, "sticky", entscheidet sich deterministisch
// aus den fertigen Stufen - auch bei Wiederaufnahme aus dem Cache identisch).
// Zahme Levels bauen damit komplett klassisch, nur Muster-Explosions-Levels
// bezahlen den Regel-Aufpreis dort, wo er sich lohnt.
var RulesPatternThreshold = 10240

// erstellt einen Blocker. Nach einer Muster-Explosion (siehe
// RulesPatternThreshold) filtert der Stufenbau seine Vorwärts-Phasen
// (CollectStart/CollectGoals-Varianten, SearchVariants) mit den Stufe-1-Regeln:
// die explodierenden Hüllen verlieren ihr totes Gewebe (schnellerer Bau,
// weniger RAM) und es entstehen weniger Muster - genau die, welche die
// Live-Regeln der Suche ohnehin fangen (Freeze/Diagonale sind monoton unter
// Kisten-Hinzufügen: ein toter k-Cluster wird in jeder Oberstellung bei seiner
// Entstehung live erkannt). Fehlende Muster kosten nie Korrektheit, nur
// Filterleistung. Die Rückwärtswelle (MergeGoals) bleibt bewusst ungefiltert -
// ihre Vollständigkeit trägt den Beweis der bedingten Kill-Regel.
// Gefilterte Stufen sind NICHT mehr bitgenau vergleichbar mit dem C#-Orakel -
// Referenz sind die in den Tests verankerten Go-Werte.
func New(field *soko.Field, cachePath string) *Blocker {
	base := field.Clone()
	base.SetRules(soko.NewRules(base)) // frische Instanz: eigene Statistik, unabhängig von den Such-Regeln des Aufrufers
	base.SetRulesBackward(nil)         // die Rückwärtswelle bleibt immer regel-frei
	start := soko.State{}
	base.GetState(&start)

	b := &Blocker{
		base:          base,
		walkCount:     base.WalkCount(),
		maxBoxes:      base.BoxCount(),
		startPlayer:   start.Player,
		cachePath:     cachePath,
		status:        StatusInit,
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

// maximale Trie-Tiefe von CheckAllowed (= größte Musterlänge + Reserve): die
// Musterlänge entspricht der Kistenzahl der größten Stufe, und mehr als 32 Stufen
// sind praktisch unmöglich (CollectStart müsste C(n,32) Kombinationen aufzählen);
// buildPlayerTrie erzwingt die Grenze beim Bau
const checkMaxDepth = 34

// prüft, ob eine Stellung erlaubt ist (false = als Deadlock erkannt);
// boxBits ist die Kisten-Bitmaske des abfragenden Feldes (Field.boxBits).
// Tiefensuche im Set-Trie der Spielerposition: abgestiegen wird nur in Kinder,
// deren Feld eine Kiste trägt - jeder erreichte Muster-End-Knoten ist damit ein
// zutreffendes Muster (alle Pfad-Felder tragen Kisten, Subset-Match).
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
// (Implementierung von soko.BlockerCheck, wird von den Zuggeneratoren aufgerufen;
// läuft parallel aus vielen Workern - nur Lese-Zugriffe und Stack-Zustand)
func (b *Blocker) CheckAllowed(player soko.Wpos, boxBits []uint64) bool {
	if b.checkTries == nil {
		return true
	}
	trie := &b.checkTries[player]
	if len(trie.fields) <= 1 {
		return true // keine Muster für diese Spielerposition
	}
	fields, childStart, isPattern := trie.fields, trie.childStart, trie.isPattern

	// Schub-Pose-Kandidaten erst beim ersten Muster-Treffer ermitteln
	// (der mit Abstand häufigste Fall ist "kein Muster trifft zu")
	var candidates [4]soko.Wpos
	candCount := 0
	covered, allCovered := 0, 0

	// Tiefensuche mit explizitem Stack: je Ebene der Kinder-Cursor des betretenen
	// Knotens und sein Feld (für den Kandidaten-Test über die Pfad-Felder)
	var stkIdx, stkEnd [checkMaxDepth]int32
	var stkField [checkMaxDepth]uint16
	sp := 0
	stkIdx[0], stkEnd[0] = childStart[0], childStart[1]

	for sp >= 0 {
		j := stkIdx[sp]
		if j == stkEnd[sp] {
			sp--
			continue
		}
		stkIdx[sp]++
		f := fields[j]
		if boxBits[f>>6]&(1<<(f&63)) == 0 {
			continue // Feld ohne Kiste -> kein Muster dieses Teilbaums kann zutreffen
		}

		if isPattern[j>>6]&(1<<(j&63)) != 0 {
			// Muster gefunden: alle Pfad-Felder (stkField[1..sp] plus f) tragen Kisten
			if allCovered == 0 { // erster Treffer -> Kandidaten aufbauen
				candidates, candCount = b.base.PushPoseCandidates(player, boxBits)
				if candCount == 0 {
					return true // keine Schub-Pose -> Muster nicht anwendbar (praktisch nur bei künstlichen Abfragen)
				}
				allCovered = 1<<candCount - 1
			}
			for c := 0; c < candCount; c++ {
				pos := candidates[c]
				if pos == soko.Wpos(f) {
					covered |= 1 << c // dieses Muster deckt den Kandidaten ab
					continue
				}
				for l := 1; l <= sp; l++ {
					if pos == soko.Wpos(stkField[l]) {
						covered |= 1 << c
						break
					}
				}
			}
			if covered == allCovered {
				return false // jede mögliche zuletzt geschobene Kiste ist abgedeckt -> verbotene Stellung
			}
		}

		if childStart[j] != childStart[j+1] { // Teilbaum mit längeren Mustern betreten
			sp++
			stkIdx[sp], stkEnd[sp] = childStart[j], childStart[j+1]
			stkField[sp] = f
		}
	}
	return true
}

// baut die Set-Tries für CheckAllowed neu auf (nach jeder fertigen Stufe und nach
// dem Cache-Laden): alle Muster aller Stufen je Spielerposition als Präfix-Baum
// über ihre kanonisch aufsteigend sortierten Felder
func (b *Blocker) rebuildCheckIndex() {
	if len(b.stages) == 0 {
		b.checkTries = nil
		return
	}

	// paralleler Bau über die Spielerpositionen (unabhängig und deterministisch;
	// CPU-gebunden, daher ohne die Speicher-Überbelegung der Suchphasen)
	tries := make([]playerTrie, b.walkCount)
	workers := min(b.workerCount, runtime.NumCPU())
	if workers < 1 {
		workers = 1
	}
	var nextPlayer atomic.Int64
	var wg sync.WaitGroup
	for w := 0; w < workers; w++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			for {
				player := int(nextPlayer.Add(1)) - 1
				if player >= b.walkCount {
					return
				}
				tries[player] = b.buildPlayerTrie(player)
			}
		}()
	}
	wg.Wait()
	b.checkTries = tries
}

// baut den Set-Trie einer Spielerposition: die Muster werden lexikografisch
// sortiert und sequenziell als Pfade eingefügt (gemeinsame Präfixe teilen sich
// die Knoten), danach wird der Baum in ein flaches BFS-Layout umkopiert -
// die Kinder jedes Knotens liegen dort zusammenhängend und aufsteigend sortiert
func (b *Blocker) buildPlayerTrie(player int) playerTrie {
	// Muster-Referenzen einsammeln (Subslices in die Stufen-Arrays, keine Kopien)
	total, totalFields := 0, 0
	for s := range b.stages {
		count := len(b.stages[s].patterns[player])
		total += count / b.stages[s].boxCount
		totalFields += count
	}
	if total == 0 {
		return playerTrie{}
	}
	refs := make([][]soko.Wpos, 0, total)
	for s := range b.stages {
		k := b.stages[s].boxCount
		if k+1 >= checkMaxDepth {
			panic("blocker: Musterlänge übersteigt checkMaxDepth")
		}
		pat := b.stages[s].patterns[player]
		for p := 0; p < len(pat); p += k {
			refs = append(refs, pat[p:p+k:p+k])
		}
	}

	// lexikografische Ordnung; bei gleichem Präfix kommt das kürzere Muster zuerst,
	// dadurch hängt jedes Muster nur am gemeinsamen Präfix mit seinem Vorgänger
	sort.Slice(refs, func(a, c int) bool {
		ra, rc := refs[a], refs[c]
		n := min(len(ra), len(rc))
		for i := 0; i < n; i++ {
			if ra[i] != rc[i] {
				return ra[i] < rc[i]
			}
		}
		return len(ra) < len(rc)
	})

	// Einfüge-Phase: Kinder als verkettete Liste (firstChild/nextSibling); dank der
	// Sortierung wird jedes neue Kind hinten angehängt -> Kinder automatisch sortiert
	fieldsPre := make([]uint16, 1, totalFields+1) // Knoten 0 = Wurzel
	firstChild := make([]int32, 1, totalFields+1)
	nextSibling := make([]int32, 1, totalFields+1)
	lastChild := make([]int32, 1, totalFields+1)
	isPatPre := make([]bool, 1, totalFields+1)

	var pathNodes [checkMaxDepth]int32
	var prev []soko.Wpos
	for _, pat := range refs {
		lcp := 0
		for lcp < len(prev) && lcp < len(pat) && prev[lcp] == pat[lcp] {
			lcp++
		}
		for i := lcp; i < len(pat); i++ {
			parent := int32(0)
			if i > 0 {
				parent = pathNodes[i-1]
			}
			node := int32(len(fieldsPre))
			fieldsPre = append(fieldsPre, uint16(pat[i]))
			firstChild = append(firstChild, 0)
			nextSibling = append(nextSibling, 0)
			lastChild = append(lastChild, 0)
			isPatPre = append(isPatPre, false)
			if lastChild[parent] == 0 {
				firstChild[parent] = node
			} else {
				nextSibling[lastChild[parent]] = node
			}
			lastChild[parent] = node
			pathNodes[i] = node
		}
		isPatPre[pathNodes[len(pat)-1]] = true
		prev = pat
	}

	// BFS-Umbau ins flache Layout (Kinder zusammenhängend, Wurzel = Knoten 0)
	nodeCount := len(fieldsPre)
	fields := make([]uint16, nodeCount)
	childStart := make([]int32, nodeCount+1)
	isPattern := make([]uint64, (nodeCount+63)/64)
	order := make([]int32, 1, nodeCount) // alte Knoten-Indizes in BFS-Reihenfolge
	next := int32(1)
	for newIdx := 0; newIdx < nodeCount; newIdx++ {
		old := order[newIdx]
		childStart[newIdx] = next
		for c := firstChild[old]; c != 0; c = nextSibling[c] {
			fields[next] = fieldsPre[c]
			if isPatPre[c] {
				isPattern[next>>6] |= 1 << (next & 63)
			}
			order = append(order, c)
			next++
		}
	}
	childStart[nodeCount] = next

	return playerTrie{fields: fields, childStart: childStart, isPattern: isPattern}
}

// prüft, ob eine der fertigen Stufen die Muster-Schwelle überschritten hat
// (dann filtern alle weiteren Stufen mit den Regeln, siehe RulesPatternThreshold)
func (b *Blocker) patternThresholdExceeded() bool {
	for i := range b.stages {
		st := &b.stages[i]
		count := 0
		for _, pat := range st.patterns {
			count += len(pat) / st.boxCount
		}
		if count > RulesPatternThreshold {
			return true
		}
	}
	return false
}

// initialisiert den Arbeitszustand für eine neue Stufe
func (b *Blocker) initStage() {
	k := b.searchBoxCount
	b.recordSize = k + 1
	b.work = b.base.CloneWithBoxCount(k)
	b.work.SetBlocker(b)         // bereits fertige Stufen filtern schon beim Stufenbau mit
	b.work.SetBlockerBackward(b) // auch rückwärts filtern (Bx-Semantik, vermeidet redundante Muster)
	if !b.patternThresholdExceeded() {
		b.work.SetRules(nil) // vor der Muster-Explosion klassisch bauen (siehe RulesPatternThreshold)
	}
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

// im RAM reservierte Bytes der laufenden Stufe: Stellungs-Tabelle plus die Puffer
// der Stufen-Suchlisten (Gegenstück zu Solver.RamBytes)
func (b *Blocker) RamBytes() int64 {
	var sum int64
	if b.known != nil {
		sum = b.known.Bytes()
	}
	for _, list := range b.stageLists() {
		if list != nil {
			sum += list.RamBytes()
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
