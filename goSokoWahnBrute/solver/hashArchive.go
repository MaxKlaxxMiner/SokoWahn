package solver

import (
	"runtime"
	"sync"
	"sync/atomic"

	"goSokoWahnBrute/crc64"
)

// ArchiveTable ("SlowCompactArchiveTable"): speicheroptimierte Stellungs-Tabelle
// nach dem Vorbild von SokowahnHash_Index24Multi aus dem C#-Original (2013),
// modernisiert um adaptive Bucket-Bits, 256 Shards und parallelen Merge.
//
// Neue Einträge sammelt ein kleines CompactTable-Delta (volle Geschwindigkeit).
// Läuft es voll, wandert der Inhalt in das unveränderlich gruppierte Archiv:
// ein uint64 pro Eintrag (Bits 0..47 Rest-Schlüssel = Schlüssel-Bits 16..63,
// Bits 48..63 Zugtiefe - restlos voll), aufgeteilt in 256 Shards über die
// unteren 8 Schlüssel-Bits. Historie der Record-Formate: gepackte 7-Byte-Records
// (+ Unsafe-Load) und ein cacheline-ausgerichtetes 9er-Zeilen-Layout liegen in
// der Git-Historie - das volle uint64 kostet 1 Byte je Eintrag mehr, ist aber
// 12-17% schneller und braucht weder Offset-Arithmetik noch unsafe; das
// Zusatz-Byte finanziert nebenbei die 48 Rest-Bits (Index- plus Rest-Bits
// wären damit schon ab 16 Bucket-Bits verlustfreie 64).
// Innerhalb eines Shards sind die Einträge nach Bucket gruppiert (Bucket =
// untere bits Schlüssel-Bits, Minimum siehe archiveMinBits - eine Sortierung
// innerhalb des Buckets ist unnötig). Der Bucket-Index sind shard-relative
// uint32-Offsets, ein Lookup ist damit ein Offset-Lesen plus linearer Scan
// über wenige Einträge (1-2 Cachelines). Die Bucket-Bits wachsen beim Merge
// mit dem Bestand (ArchiveBucketGoal Einträge je Bucket, Maximum 32).
//
// Tiefen-Updates schreiben direkt in das Archiv (die Schlüssel liegen fest);
// Add prüft deshalb zuerst das Archiv - Delta und Archiv bleiben disjunkt und
// Len() ist jederzeit exakt. Speicherbilanz gegenüber der CompactTable:
// 8 statt effektiv 13,3 Byte je Eintrag. Der Merge baut die Shards einzeln um
// (alte Shard-Arrays werden während des Umbaus frei) und läuft parallel über
// alle Kerne; Schreibzugriffe passieren nur in der seriellen Merge-Phase des
// Solvers, Lookups sind reine Lesezugriffe und damit worker-sicher.
type ArchiveTable struct {
	delta        *CompactTable // schneller Sammel-Teil für neue Einträge
	shards       [archiveShardCount]archiveShard
	bits         uint   // Bucket-Bits (Bucket = untere bits Schlüssel-Bits), 24..32
	mask         uint64 // 1<<bits - 1 (vorberechnet für den Lookup)
	archiveCount int64  // Einträge im Archiv (ohne Delta)
	archiveBytes int64  // reservierte Bytes aller Shard-Arrays
}

// ein Shard des Archivs: alle Einträge, deren Schlüssel in den unteren 8 Bits
// der Shard-Nummer entsprechen, gruppiert nach Bucket. Ein Record pro uint64:
// ein normaler Slice-Zugriff liefert Schlüsselvergleich UND Tiefe, natürlich
// ausgerichtet und ohne jede Byte-Arithmetik
type archiveShard struct {
	offsets []uint32 // Startindex je Bucket-im-Shard (Bucket >> 8), plus End-Sentinel
	data    []uint64 // Records: Bits 0..47 Rest-Schlüssel, Bits 48..63 Zugtiefe
}

const (
	archiveShardCount = 256       // Shard = untere 8 Schlüssel-Bits
	archiveRestMask   = 1<<48 - 1 // gespeicherter Rest-Schlüssel (Schlüssel-Bits 16..63)
	// Bucket-Bits-Minimum: die 48 Rest-Bits würden 16 erlauben (verlustfreie 64),
	// aber ein überdimensionierter Index ist in der echten Suche schneller: die
	// meisten Buckets sind leer, Fehlschläge enden dann schon am Offsets-Vergleich
	// ohne Daten-Zugriff, und frühe Bit-Wachstums-Merges (mit Rekonstruktion)
	// entfallen. Minuten-Sweep von Max (Archiv ab Start, ~35M Einträge je
	// Richtung): 16 -> 71,9 | 24 -> 72,3 | 25 -> 73,4 | 26 -> 73,7 | 27 -> 71,7 |
	// 28 -> 68,2 - ab 27 kippt es, der fast leere Riesen-Index (2^28 = 1 GB
	// Offsets je Tabelle) kostet dann selbst (TLB, Zero-Pages). Preis des 26er
	// Sweet Spots: 268 MB Index-Floor je Tabelle; oberhalb von ~134M Einträgen
	// übernimmt ohnehin die ArchiveBucketGoal-Leiter
	archiveMinBits = 26
	archiveMaxBits  = 32 // Maximum: 2^32 Buckets (16 GB Index) reichen für >50 Mrd Einträge
	archiveDeltaDiv = 16 // Merge-Schwelle: Delta ab 1/16 des Archiv-Bestands
)

// Ziel-Einträge je Bucket - der Speed/RAM-Regler des Archivs (wirkt ab dem
// nächsten Merge; die Zweierpotenz-Leiter pendelt real zwischen Ziel/2 und Ziel).
// Der Index kostet 4/Ziel Byte je Eintrag, der Lookup scannt im Schnitt
// Ziel/2 Records. Messkurve (i5-11400H, 16,7M Einträge, Hit+Miss-Paar):
// Ziel 12 -> 201 ns bei 8,3 B/Eintrag | 4 -> 160 ns bei ~9 | 2 -> 133 ns bei ~10
// (zum Vergleich CompactTable: 48 ns bei 13,3-26,7 B/Eintrag je nach Füllstand).
// Default 2 nach Minuten-Messung von Max in der echten Suche: Ziel 4 kostete
// dort ~3% Gesamtdurchsatz - das letzte Byte Index-Ersparnis ist es nicht wert
var ArchiveBucketGoal int64 = 2

// Mindestgröße des Deltas, bevor der erste bzw. nächste Merge ansteht
// (klein stellbar in Tests; 4M Einträge = ~56 MB CompactTable)
var ArchiveDeltaMin int64 = 4 << 20

func NewArchiveTable() PosTable {
	return &ArchiveTable{
		delta: newCompactTable(1 << 12),
		bits:  archiveMinBits,
		mask:  1<<archiveMinBits - 1,
	}
}

// NewArchiveTableFrom übernimmt alle Einträge einer bestehenden CompactTable
// (Taste h): die Tabelle wird als übergroßes Delta eingehängt und sofort
// gemerged - danach arbeitet sie mit einem frischen, kleinen Delta weiter
func NewArchiveTableFrom(src *CompactTable) *ArchiveTable {
	t := &ArchiveTable{
		delta: src,
		bits:  archiveMinBits,
		mask:  1<<archiveMinBits - 1,
	}
	t.merge()
	return t
}

// Reihenfolge bewusst Archiv zuerst: es hält per Konstruktion immer mindestens
// das 16-fache des Deltas, die meisten Treffer sparen so den zweiten Lookup
// (Delta und Archiv sind disjunkt, die Reihenfolge ändert nie das Ergebnis)
func (t *ArchiveTable) Get(crc crc64.Value) uint16 {
	if depth := t.archiveGet(uint64(crc)); depth != DepthUnknown {
		return depth
	}
	return t.delta.Get(crc)
}

func (t *ArchiveTable) Add(crc crc64.Value, depth uint16) {
	if t.archiveSet(uint64(crc), depth) {
		return // Schlüssel liegt im Archiv -> Tiefe dort in-place aktualisiert
	}
	t.delta.Add(crc, depth) // neu oder bereits im Delta (map-Semantik)
	if t.delta.count >= t.deltaLimit() {
		t.merge()
	}
}

func (t *ArchiveTable) Update(crc crc64.Value, depth uint16) {
	t.Add(crc, depth)
}

func (t *ArchiveTable) Len() int64 {
	return t.archiveCount + t.delta.count
}

func (t *ArchiveTable) Bytes() int64 {
	return t.archiveBytes + t.delta.Bytes()
}

// Füllstand des Deltas relativ zur Merge-Schwelle (1.0 = der nächste
// neue Eintrag löst den Merge aus; das Archiv selbst kennt kein Resize)
func (t *ArchiveTable) Fill() float64 {
	return float64(t.delta.count) / float64(t.deltaLimit())
}

// Merge-Schwelle des Deltas: wächst mit dem Archiv-Bestand, damit die
// Gesamtzahl der Umkopier-Läufe logarithmisch bleibt
func (t *ArchiveTable) deltaLimit() int64 {
	limit := t.archiveCount / archiveDeltaDiv
	if limit < ArchiveDeltaMin {
		limit = ArchiveDeltaMin
	}
	return limit
}

// sucht einen Schlüssel im Archiv (DepthUnknown = nicht vorhanden)
func (t *ArchiveTable) archiveGet(key uint64) uint16 {
	s := &t.shards[key&(archiveShardCount-1)]
	if len(s.offsets) == 0 {
		return DepthUnknown
	}
	bucket := (key & t.mask) >> 8
	rest := key >> 16
	for i := s.offsets[bucket]; i < s.offsets[bucket+1]; i++ {
		if record := s.data[i]; record&archiveRestMask == rest {
			return uint16(record >> 48) // Tiefe steckt im selben Load
		}
	}
	return DepthUnknown
}

// aktualisiert die Tiefe eines Archiv-Eintrags in-place; false = Schlüssel nicht im Archiv
func (t *ArchiveTable) archiveSet(key uint64, depth uint16) bool {
	s := &t.shards[key&(archiveShardCount-1)]
	if len(s.offsets) == 0 {
		return false
	}
	bucket := (key & t.mask) >> 8
	rest := key >> 16
	for i := s.offsets[bucket]; i < s.offsets[bucket+1]; i++ {
		if s.data[i]&archiveRestMask == rest {
			s.data[i] = rest | uint64(depth)<<48
			return true
		}
	}
	return false
}

// verschmilzt das komplette Delta in die Archiv-Shards und beginnt mit einem
// frischen Delta; wählt dabei die Bucket-Bits passend zum neuen Bestand
func (t *ArchiveTable) merge() {
	newCount := t.archiveCount + t.delta.count
	newBits := t.bits
	for newBits < archiveMaxBits && newCount > ArchiveBucketGoal<<newBits {
		newBits++
	}
	t.mergeBits(newBits)
}

// Kern des Merges mit fest vorgegebenen Bucket-Bits (separat, damit der
// Bit-Wachstums-Pfad mit der Schlüssel-Rekonstruktion testbar bleibt)
func (t *ArchiveTable) mergeBits(newBits uint) {
	delta := t.delta
	oldBits := t.bits
	bucketsPerShard := 1 << (newBits - 8)
	newMask := uint64(1)<<newBits - 1

	// --- Phase 1: Bucket-Zähler (werden per Präfixsumme zu den neuen Offsets;
	// um 1 versetzt gezählt, damit die Summe direkt die Startpositionen ergibt) ---
	counts := make([][]uint32, archiveShardCount)

	// Bestand: jeder Shard zählt seine eigenen Einträge (die Shard-Bits eines
	// Schlüssels ändern sich nie), bei unveränderten Bits direkt die Bucket-Größen
	parallelShards(func(shard int) {
		c := make([]uint32, bucketsPerShard+1)
		s := &t.shards[shard]
		if newBits == oldBits {
			for b := 0; b+1 < len(s.offsets); b++ {
				c[b+1] = s.offsets[b+1] - s.offsets[b]
			}
		} else {
			for b := 0; b+1 < len(s.offsets); b++ {
				low16 := (uint64(b)<<8 | uint64(shard)) & (1<<16 - 1)
				for i := s.offsets[b]; i < s.offsets[b+1]; i++ {
					// voller Schlüssel aus Rest + Bucket rekonstruiert
					key := (s.data[i]&archiveRestMask)<<16 | low16
					c[(key&newMask)>>8+1]++
				}
			}
		}
		counts[shard] = c
	})

	// Delta: parallel über das Slot-Array, atomare Zähler im jeweiligen Ziel-Shard
	parallelRanges(len(delta.crcs), func(from, to int) {
		for i := from; i < to; i++ {
			key := uint64(delta.crcs[i])
			if key == 0 {
				continue
			}
			atomic.AddUint32(&counts[key&(archiveShardCount-1)][(key&newMask)>>8+1], 1)
		}
	})
	if delta.zeroDepth != DepthUnknown {
		counts[0][1]++ // Sonderfall Schlüssel 0: Shard 0, Bucket 0
	}

	// --- Phase 2: je Shard Präfixsumme, neue Arrays, Bestand umkopieren ---
	// die alten Shard-Arrays werden sofort ersetzt (und damit für den GC frei),
	// die Cursor stehen danach hinter dem Bestands-Anteil jedes Buckets
	cursors := make([][]uint32, archiveShardCount)
	parallelShards(func(shard int) {
		offsets := counts[shard]
		var sum uint32
		for b := 1; b < len(offsets); b++ {
			sum += offsets[b]
			offsets[b] = sum
		}
		total := offsets[len(offsets)-1]
		data := make([]uint64, total)
		cursor := make([]uint32, bucketsPerShard)
		copy(cursor, offsets[:bucketsPerShard])

		s := &t.shards[shard]
		for b := 0; b+1 < len(s.offsets); b++ {
			low16 := (uint64(b)<<8 | uint64(shard)) & (1<<16 - 1)
			for i := s.offsets[b]; i < s.offsets[b+1]; i++ {
				record := s.data[i]
				bucket := uint64(b)
				if newBits != oldBits {
					bucket = (((record&archiveRestMask)<<16 | low16) & newMask) >> 8
				}
				pos := cursor[bucket]
				cursor[bucket]++
				data[pos] = record // Rest + Tiefe wandern unverändert mit
			}
		}
		t.shards[shard] = archiveShard{offsets: offsets, data: data}
		cursors[shard] = cursor
	})

	// --- Phase 3: Delta einsortieren (atomare Cursor, die Reihenfolge innerhalb
	// eines Buckets ist beliebig - Lookups scannen den ganzen Bucket) ---
	if delta.zeroDepth != DepthUnknown { // Sonderfall seriell, solange die Cursor unumkämpft sind
		pos := cursors[0][0]
		cursors[0][0]++
		t.shards[0].data[pos] = uint64(delta.zeroDepth) << 48
	}
	parallelRanges(len(delta.crcs), func(from, to int) {
		for i := from; i < to; i++ {
			key := uint64(delta.crcs[i])
			if key == 0 {
				continue
			}
			shard := key & (archiveShardCount - 1)
			bucket := (key & newMask) >> 8
			pos := atomic.AddUint32(&cursors[shard][bucket], 1) - 1
			t.shards[shard].data[pos] = key>>16 | uint64(delta.depths[i])<<48
		}
	})

	// --- Abschluss: Kennzahlen aktualisieren, frisches Delta ---
	t.bits = newBits
	t.mask = newMask
	t.archiveCount += delta.count
	t.archiveBytes = 0
	for shard := range t.shards {
		s := &t.shards[shard]
		t.archiveBytes += int64(len(s.offsets))*4 + int64(len(s.data))*8
	}
	t.delta = newCompactTable(1 << 12)
}

// führt die Arbeit für alle 256 Shards parallel über die verfügbaren Kerne aus
func parallelShards(work func(shard int)) {
	workers := runtime.NumCPU()
	if workers > archiveShardCount {
		workers = archiveShardCount
	}
	var next atomic.Int64
	var wg sync.WaitGroup
	for w := 0; w < workers; w++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			for {
				shard := int(next.Add(1)) - 1
				if shard >= archiveShardCount {
					return
				}
				work(shard)
			}
		}()
	}
	wg.Wait()
}

// teilt den Indexbereich 0..n in zusammenhängende Stücke für die verfügbaren Kerne
func parallelRanges(n int, work func(from, to int)) {
	workers := runtime.NumCPU()
	chunk := (n + workers - 1) / workers
	var wg sync.WaitGroup
	for from := 0; from < n; from += chunk {
		to := from + chunk
		if to > n {
			to = n
		}
		wg.Add(1)
		go func(from, to int) {
			defer wg.Done()
			work(from, to)
		}(from, to)
	}
	wg.Wait()
}
