package solver

import (
	"encoding/binary"
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
// 7 Byte pro Eintrag (5 Byte Rest-Schlüssel = Bits 24..63 little-endian plus
// 2 Byte Zugtiefe), aufgeteilt in 256 Shards über die unteren 8 Schlüssel-Bits.
// Innerhalb eines Shards sind die Einträge nach Bucket gruppiert (Bucket =
// untere bits Schlüssel-Bits; mindestens 24, dann sind Index- plus Rest-Bits
// zusammen verlustfreie 64 - eine Sortierung innerhalb des Buckets ist unnötig).
// Der Bucket-Index sind shard-relative uint32-Offsets, ein Lookup ist damit ein
// Offset-Lesen plus linearer Scan über durchschnittlich unter 12 Einträge
// (1-2 Cachelines). Die Bucket-Bits wachsen beim Merge mit dem Bestand
// (Ziel ~12 Einträge je Bucket, Maximum 32).
//
// Tiefen-Updates schreiben direkt in das Archiv (die Schlüssel liegen fest);
// Add prüft deshalb zuerst das Archiv - Delta und Archiv bleiben disjunkt und
// Len() ist jederzeit exakt. Speicherbilanz gegenüber der CompactTable:
// 7 statt effektiv 13,3 Byte je Eintrag. Der Merge baut die Shards einzeln um
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
// der Shard-Nummer entsprechen, gruppiert nach Bucket. Die Sätze liegen
// verschränkt als 7-Byte-Records (5 Byte Rest + 2 Byte Tiefe): ein einziger
// 8-Byte-Load liefert Schlüsselvergleich UND Tiefe aus derselben Cacheline
// (+1 Byte Padding am Ende für den überstehenden Load des letzten Satzes)
type archiveShard struct {
	offsets []uint32 // Startindex je Bucket-im-Shard (Bucket >> 8), plus End-Sentinel
	data    []byte   // 7-Byte-Records: Bytes 0-4 Rest-Schlüssel, Bytes 5-6 Zugtiefe (beides little-endian)
}

const (
	archiveShardCount = 256        // Shard = untere 8 Schlüssel-Bits
	archiveRestMask   = 1<<40 - 1  // gespeicherter Rest-Schlüssel (Bits 24..63)
	archiveMinBits    = 24         // Minimum: 24 Bucket- + 40 Rest-Bits = verlustfreie 64
	archiveMaxBits    = 32         // Maximum: 2^32 Buckets (16 GB Index) reichen für >50 Mrd Einträge
	archiveBucketGoal = 12         // Ziel-Einträge je Bucket (1-2 Cachelines linearer Scan)
	archiveDeltaDiv   = 16         // Merge-Schwelle: Delta ab 1/16 des Archiv-Bestands
)

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
	rest := key >> 24
	for i := s.offsets[bucket]; i < s.offsets[bucket+1]; i++ {
		if record := loadRecord(s.data, i); record&archiveRestMask == rest {
			return uint16(record >> 40) // Tiefe steckt im selben 8-Byte-Load
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
	rest := key >> 24
	for i := s.offsets[bucket]; i < s.offsets[bucket+1]; i++ {
		if loadRecord(s.data, i)&archiveRestMask == rest {
			binary.LittleEndian.PutUint16(s.data[int(i)*7+5:], depth)
			return true
		}
	}
	return false
}

// liest den 7-Byte-Record des Eintrags i als uint64 (8-Byte-Load dank Padding;
// Bits 0..39 = Rest-Schlüssel, Bits 40..55 = Tiefe, Bits 56..63 = Fremd-Byte)
func loadRecord(data []byte, i uint32) uint64 {
	return binary.LittleEndian.Uint64(data[int(i)*7:])
}

// schreibt den kompletten 7-Byte-Record des Eintrags i
func storeRecord(data []byte, i uint32, rest uint64, depth uint16) {
	offset := int(i) * 7
	binary.LittleEndian.PutUint32(data[offset:], uint32(rest))
	data[offset+4] = byte(rest >> 32)
	binary.LittleEndian.PutUint16(data[offset+5:], depth)
}

// verschmilzt das komplette Delta in die Archiv-Shards und beginnt mit einem
// frischen Delta; wählt dabei die Bucket-Bits passend zum neuen Bestand
func (t *ArchiveTable) merge() {
	newCount := t.archiveCount + t.delta.count
	newBits := t.bits
	for newBits < archiveMaxBits && newCount > int64(archiveBucketGoal)<<newBits {
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
				low24 := (uint64(b)<<8 | uint64(shard)) & (1<<24 - 1)
				for i := s.offsets[b]; i < s.offsets[b+1]; i++ {
					// voller Schlüssel aus Rest + Bucket rekonstruiert
					key := (loadRecord(s.data, i)&archiveRestMask)<<24 | low24
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
		data := make([]byte, int(total)*7+1)
		cursor := make([]uint32, bucketsPerShard)
		copy(cursor, offsets[:bucketsPerShard])

		s := &t.shards[shard]
		for b := 0; b+1 < len(s.offsets); b++ {
			low24 := (uint64(b)<<8 | uint64(shard)) & (1<<24 - 1)
			for i := s.offsets[b]; i < s.offsets[b+1]; i++ {
				record := loadRecord(s.data, i)
				rest := record & archiveRestMask
				bucket := uint64(b)
				if newBits != oldBits {
					bucket = ((rest<<24 | low24) & newMask) >> 8
				}
				pos := cursor[bucket]
				cursor[bucket]++
				storeRecord(data, pos, rest, uint16(record>>40))
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
		storeRecord(t.shards[0].data, pos, 0, delta.zeroDepth)
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
			storeRecord(t.shards[shard].data, pos, key>>24, delta.depths[i])
		}
	})

	// --- Abschluss: Kennzahlen aktualisieren, frisches Delta ---
	t.bits = newBits
	t.mask = newMask
	t.archiveCount += delta.count
	t.archiveBytes = 0
	for shard := range t.shards {
		s := &t.shards[shard]
		t.archiveBytes += int64(len(s.offsets))*4 + int64(len(s.data))
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
