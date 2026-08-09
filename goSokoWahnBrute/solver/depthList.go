package solver

import (
	"fmt"
	"os"
	"path/filepath"
	"sync"
	"time"
	"unsafe"

	"goSokoWahnBrute/soko"
)

// --- Konfiguration der Disk-Auslagerung (einmalig beim Programmstart setzen) ---
var (
	// Ordner für die Auslagerungsdateien der Suchlisten ("" = Auslagerung deaktiviert,
	// alle Listen bleiben komplett im RAM - so laufen z.B. die Tests)
	SpillDir = ""

	// ab dieser Größe in Bytes lagert eine Liste ihre Sätze auf die Festplatte aus;
	// gleichzeitig die Puffer-/Blockgröße der einzelnen Schreib- und Lesezugriffe
	// (das C#-Original nutzte 16 MB: list2Multi 512 * 32-KByte-Blöcke)
	SpillBufferBytes = 64 << 20
)

// Namensmuster der Auslagerungsdateien: den Zufallsteil (*) vergibt os.CreateTemp
// kollisionssicher, damit sich mehrere Prozesse im selben Ordner nicht in die Quere
// kommen (das Original nutzte den TickCount - bei parallelen Starts nicht eindeutig)
const spillPattern = "sokolist_*.tmp"

// löscht liegengebliebene Auslagerungsdateien abgestürzter Läufe (älter als maxAge).
// Dateien laufender Prozesse sind geöffnet und lassen sich unter Windows ohnehin
// nicht löschen - Fehler werden deshalb bewusst ignoriert.
func CleanupSpillFiles(dir string, maxAge time.Duration) {
	matches, _ := filepath.Glob(filepath.Join(dir, spillPattern))
	for _, path := range matches {
		if info, err := os.Stat(path); err == nil && time.Since(info.ModTime()) > maxAge {
			os.Remove(path)
		}
	}
}

// Suchliste für noch zu prüfende Stellungen einer bestimmten Zugtiefe (FIFO)
// Satz = Spielerposition + Kistenpositionen, jeweils als uint16
// (die Zugtiefe selbst steckt im Listenindex des Solvers, nicht im Satz)
//
// Wächst die Liste über SpillBufferBytes hinaus, wandert der volle Schreibpuffer
// blockweise in eine Temp-Datei (Muster von SokowahnLinearList2 aus dem Original).
// Schreiben und Lesen laufen dabei im Hintergrund (Doppel-Pufferung): die Suche
// füllt sofort den nächsten Puffer weiter bzw. arbeitet den aktuellen Leseblock ab,
// während die Platte den vorigen wegschreibt oder den nächsten voraus liest.
// Gewartet wird nur, wenn die Platte nicht hinterherkommt. Die Sätze kommen in
// exakt derselben FIFO-Reihenfolge wieder heraus wie in der reinen RAM-Variante,
// das Suchverhalten bleibt also bitgenau identisch. Release löscht die Datei wieder.
//
// Alle Methoden gehören auf EINE Goroutine (wie bisher) - nebenläufig sind nur die
// intern gestarteten IO-Goroutinen, synchronisiert über die beiden WaitGroups.
type DepthList struct {
	recordSize int // Satzgröße in uint16-Werten = Kistenanzahl + 1

	data     []uint16 // RAM-Schreibpuffer (ohne Auslagerung: die komplette Liste)
	dataRead int      // Leseposition im Schreibpuffer in Sätzen
	noSpill  bool     // true nach einem Auslagerungs-Fehler: Liste bleibt im RAM

	file     *os.File // Auslagerungsdatei (nil = bisher nichts ausgelagert)
	writeOff int64    // fertig geschriebene Bytes in der Datei
	readOff  int64    // bis hierhin gelesen bzw. fürs Vorauslesen reserviert (Bytes)

	readBuf []uint16 // aktueller Leseblock
	readLen int      // gültige Sätze im Leseblock
	readPos int      // bereits entnommene Sätze im Leseblock

	// --- Hintergrund-Schreiben (höchstens ein Vorgang) ---
	writeWG  sync.WaitGroup
	pending  []uint16 // Puffer des laufenden Schreibvorgangs (nil = keiner)
	writeErr error    // Ergebnis des Schreibvorgangs (gültig nach writeWG.Wait)
	spare    []uint16 // ausgedienter Schreibpuffer zur Wiederverwendung

	// --- Hintergrund-Vorauslesen (höchstens ein Vorgang) ---
	readWG          sync.WaitGroup
	prefetch        []uint16 // Puffer des Vorauslesens
	prefetchRecords int      // Sätze des laufenden/fertigen Vorauslesens (0 = keins)
	readErr         error    // Ergebnis des Vorauslesens (gültig nach readWG.Wait)
}

func NewDepthList(recordSize int) *DepthList {
	return &DepthList{recordSize: recordSize}
}

// trägt eine Stellung ein
func (l *DepthList) Push(state *soko.State) {
	l.data = append(l.data, uint16(state.Player))
	for _, box := range state.Boxes {
		l.data = append(l.data, uint16(box))
	}
	l.spillIfFull()
}

// trägt einen oder mehrere bereits kodierte Sätze ein (Spielerposition + Kistenpositionen)
func (l *DepthList) PushRecord(record []uint16) {
	l.data = append(l.data, record...)
	l.spillIfFull()
}

// reicht den vollen Schreibpuffer an eine Hintergrund-Goroutine weiter und füllt sofort
// den Tausch-Puffer weiter (die Datei wird erst beim ersten Überlauf angelegt); bei
// Fehlern bleibt die Liste einfach komplett im RAM.
// Der dataRead-Schutz: wird bereits aus dem Schreibpuffer gelesen, darf er nicht mehr
// ausgelagert werden (die FIFO-Buchführung säße sonst schief) - im Solver/Blocker sind
// Push- und Lesephase einer Liste aber ohnehin strikt getrennt.
func (l *DepthList) spillIfFull() {
	if l.noSpill || SpillDir == "" || l.dataRead > 0 || len(l.data)*2 < SpillBufferBytes {
		return
	}
	if l.file == nil {
		file, err := os.CreateTemp(SpillDir, spillPattern)
		if err != nil {
			l.noSpill = true
			return
		}
		l.file = file
	}
	l.finishWrite() // vorigen Schreibvorgang verbuchen (dank Doppel-Puffer meist längst fertig)
	if l.noSpill {
		return
	}

	l.pending, l.data, l.spare = l.data, l.spare[:0], nil
	buf, off := l.pending, l.writeOff
	l.writeWG.Add(1)
	go func() {
		defer l.writeWG.Done()
		_, l.writeErr = l.file.WriteAt(u16Bytes(buf), off)
	}()
}

// wartet auf den laufenden Hintergrund-Schreibvorgang und verbucht ihn
func (l *DepthList) finishWrite() {
	if l.pending == nil {
		return
	}
	l.writeWG.Wait()
	if l.writeErr != nil {
		// Schreiben fehlgeschlagen: die Sätze bleiben im RAM erhalten
		// (pending ist älter als data, also vorne anfügen - FIFO bleibt gewahrt)
		l.noSpill = true
		l.writeErr = nil
		l.data = append(l.pending, l.data...)
		l.pending = nil
		return
	}
	l.writeOff += int64(len(l.pending) * 2)
	l.spare = l.pending[:0] // Puffer für den nächsten Tausch wiederverwenden
	l.pending = nil
}

// entnimmt bis zu maxRecords Sätze und gibt sie als flaches Slice zurück.
// Bei ausgelagerten Listen kann das Slice auch weniger Sätze enthalten (höchstens ein
// Lesepuffer-Block pro Aufruf) und bleibt nur bis zum nächsten PopBatch/Release gültig -
// alle Aufrufer verarbeiten den Batch komplett, bevor sie weiterlesen.
func (l *DepthList) PopBatch(maxRecords int) []uint16 {
	// zuerst den Datei-Teil in FIFO-Reihenfolge ausliefern (blockweise über den Lesepuffer)
	if l.readPos == l.readLen && (l.readOff < l.writeOff || l.prefetchRecords > 0 || l.pending != nil) {
		l.fillReadBuf()
	}
	if l.readPos < l.readLen {
		count := l.readLen - l.readPos
		if maxRecords < count {
			count = maxRecords
		}
		from := l.readPos * l.recordSize
		l.readPos += count
		return l.readBuf[from : from+count*l.recordSize]
	}

	// danach der Schreibpuffer (ohne Auslagerung: die komplette Liste, wie bisher)
	if remain := len(l.data)/l.recordSize - l.dataRead; maxRecords > remain {
		maxRecords = remain
	}
	from := l.dataRead * l.recordSize
	l.dataRead += maxRecords
	return l.data[from : from+maxRecords*l.recordSize]
}

// stellt den nächsten Block im Lesepuffer bereit: den vorausgelesenen Block übernehmen
// (und sofort den übernächsten anfordern) oder - nach einem Phasenwechsel - synchron lesen
func (l *DepthList) fillReadBuf() {
	if l.prefetchRecords > 0 {
		l.readWG.Wait()
		if l.readErr != nil {
			panic(fmt.Sprintf("DepthList: Lesefehler in Auslagerungsdatei %q: %v", l.file.Name(), l.readErr))
		}
		l.readBuf, l.prefetch = l.prefetch, l.readBuf
		l.readLen, l.readPos = l.prefetchRecords, 0
		l.prefetchRecords = 0
		l.startPrefetch()
		return
	}

	// kein Vorauslesen unterwegs: eventuellen Schreib-Rückstand einholen und synchron lesen
	if l.readOff == l.writeOff {
		l.finishWrite()
	}
	if l.readOff == l.writeOff {
		return // nichts (mehr) auf der Platte, z.B. nach einem noSpill-Rückzug in den RAM
	}
	records := l.nextChunkRecords()
	values := records * l.recordSize
	if cap(l.readBuf) < values {
		l.readBuf = make([]uint16, values)
	}
	l.readBuf = l.readBuf[:values]
	if _, err := l.file.ReadAt(u16Bytes(l.readBuf), l.readOff); err != nil {
		panic(fmt.Sprintf("DepthList: Lesefehler in Auslagerungsdatei %q: %v", l.file.Name(), err))
	}
	l.readOff += int64(values * 2)
	l.readLen, l.readPos = records, 0
	l.startPrefetch()
}

// stößt das Vorauslesen des nächsten Blocks an (falls schon fertig geschriebene Daten
// vorliegen; läuft der Schreibvorgang noch, holt der nächste fillReadBuf ihn ein)
func (l *DepthList) startPrefetch() {
	if l.readOff == l.writeOff {
		return
	}
	records := l.nextChunkRecords()
	values := records * l.recordSize
	if cap(l.prefetch) < values {
		l.prefetch = make([]uint16, values)
	}
	l.prefetch = l.prefetch[:values]
	l.prefetchRecords = records

	buf, off := l.prefetch, l.readOff
	l.readOff += int64(values * 2) // Bereich gilt ab jetzt als gelesen (reserviert)
	l.readWG.Add(1)
	go func() {
		defer l.readWG.Done()
		_, l.readErr = l.file.ReadAt(u16Bytes(buf), off)
	}()
}

// Sätze des nächsten Leseblocks (ein Puffer voll, am Dateiende entsprechend weniger)
func (l *DepthList) nextChunkRecords() int {
	records := SpillBufferBytes / 2 / l.recordSize
	if records < 1 {
		records = 1
	}
	if rest := int((l.writeOff - l.readOff) / 2 / int64(l.recordSize)); records > rest {
		records = rest
	}
	return records
}

// Anzahl der noch nicht entnommenen Sätze
func (l *DepthList) Count() int {
	onDisk := int((l.writeOff-l.readOff)/2/int64(l.recordSize)) + l.prefetchRecords
	return onDisk + l.readLen - l.readPos +
		len(l.pending)/l.recordSize + len(l.data)/l.recordSize - l.dataRead
}

// fertig in die Auslagerungsdatei geschriebene Bytes (0 = Liste liegt komplett im RAM)
func (l *DepthList) SpillBytes() int64 {
	return l.writeOff
}

// gibt den Speicher frei und löscht eine eventuelle Auslagerungsdatei
// (sinnvoll, sobald die Liste komplett abgearbeitet ist oder verworfen wird)
func (l *DepthList) Release() {
	// laufende Hintergrund-Zugriffe abwarten, sonst arbeiten sie auf der gelöschten Datei
	if l.pending != nil {
		l.writeWG.Wait()
		l.pending, l.writeErr = nil, nil
	}
	if l.prefetchRecords > 0 {
		l.readWG.Wait()
		l.prefetchRecords, l.readErr = 0, nil
	}
	l.data, l.spare, l.prefetch = nil, nil, nil
	l.dataRead = 0
	l.noSpill = false
	l.readBuf = nil
	l.readLen, l.readPos = 0, 0
	l.writeOff, l.readOff = 0, 0
	if l.file != nil {
		name := l.file.Name()
		l.file.Close()
		os.Remove(name)
		l.file = nil
	}
}

// interpretiert das uint16-Slice als Byte-Slice (native Byte-Reihenfolge - unkritisch,
// die Auslagerungsdateien werden nur vom selben Prozess wieder eingelesen)
func u16Bytes(data []uint16) []byte {
	if len(data) == 0 {
		return nil
	}
	return unsafe.Slice((*byte)(unsafe.Pointer(&data[0])), len(data)*2)
}
