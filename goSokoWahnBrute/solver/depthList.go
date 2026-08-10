package solver

import (
	"fmt"
	"os"
	"path/filepath"
	"runtime"
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
	// gleichzeitig die Puffer-/Blockgröße der einzelnen Schreib- und Lesezugriffe.
	// 16 MB wie das C#-Original (list2Multi 512 * 32-KByte-Blöcke): bei Laufzug-Levels
	// halten hunderte aktive Listen je einen Puffer - 64 MB summierten sich dort auf
	// zweistellige GB RAM, bevor überhaupt gespillt wurde
	SpillBufferBytes = 16 << 20

	// erst ab diesem Heap-Verbrauch des Prozesses (runtime.MemStats.Alloc, dieselbe
	// Messgröße wie die RAM-Notbremse der TUI) beginnen Listen auszulagern - solange
	// reichlich RAM frei ist, bleibt alles im RAM und die Platte wird geschont.
	// Die Entscheidung fällt je Liste einmalig beim ersten Erreichen der Puffergröße
	// und gilt dauerhaft: unterhalb der Schwelle entschiedene Listen bleiben auch dann
	// im RAM, wenn der Verbrauch später steigt - erst danach volllaufende (also frische)
	// Listen nehmen den Auslagerungs-Standard. 0 = immer sofort auslagern (Tests)
	SpillRamThresholdBytes = uint64(32) << 30
)

// Namensmuster der Auslagerungsdateien: den Zufallsteil (*) vergibt os.CreateTemp
// kollisionssicher, damit sich mehrere Prozesse im selben Ordner nicht in die Quere
// kommen (das Original nutzte den TickCount - bei parallelen Starts nicht eindeutig)
const spillPattern = "sokolist_*.tmp"

// löscht liegengebliebene Auslagerungsdateien abgestürzter Läufe (älter als maxAge;
// der Aufruf beim Programmstart nutzt eine Woche - mehr als genug Reserve, denn
// länger als ein paar Stunden läuft keine Suche: die Hashtabellen füllen selbst
// 128 GB RAM in 3-4 Stunden). Fehler werden bewusst ignoriert.
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
// Wächst die Liste über SpillBufferBytes hinaus UND liegt der Heap-Verbrauch des
// Prozesses über SpillRamThresholdBytes, wandert der volle Schreibpuffer blockweise
// in eine Temp-Datei (Muster von SokowahnLinearList2 aus dem Original); unterhalb der
// Schwelle bleibt die Liste dauerhaft im RAM (Details bei SpillRamThresholdBytes).
// Auf der Platte belegt jeder Wert bei Feldern mit höchstens 256 begehbaren Positionen
// nur ein Byte statt der vollen uint16 (halbiert Dateigröße und IO-Volumen; die
// RAM-Puffer bleiben uint16, es ändert sich nur das Disk-Format).
// Schreiben und Vorauslesen laufen im Hintergrund: die Suche füllt sofort einen
// frischen Puffer weiter bzw. arbeitet den aktuellen Leseblock ab, während die
// Platte den vorigen wegschreibt oder den nächsten voraus liest. Gewartet wird nur,
// wenn die Platte nicht hinterherkommt. Die Sätze kommen in exakt derselben
// FIFO-Reihenfolge wieder heraus wie in der reinen RAM-Variante, das Suchverhalten
// bleibt also bitgenau identisch. Release löscht die Datei wieder.
//
// Datei-Handles werden pro Blockzugriff geöffnet und sofort wieder geschlossen
// (nur der Name bleibt gemerkt): es können hunderte Listen gleichzeitig aktiv sein
// (die Laufzug-Tiefen verteilen Pushes über viele Ziel-Tiefen), und auf Ordnern mit
// NTFS-Komprimierung arbeitet die Komprimierung erst nach dem Schließen richtig
// (Erfahrungswert aus der C#-Version, deren Handles ebenfalls bewusst zugingen).
//
// Alle Methoden gehören auf EINE Goroutine (wie bisher) - nebenläufig sind nur die
// intern gestarteten IO-Goroutinen, synchronisiert über die beiden WaitGroups.
type DepthList struct {
	recordSize int   // Satzgröße in uint16-Werten = Kistenanzahl + 1
	valueBytes int64 // Bytes je Wert in der Auslagerungsdatei (1 bei <= 256 Positionen, sonst 2)

	data     []uint16 // RAM-Schreibpuffer (ohne Auslagerung: die komplette Liste)
	dataRead int      // Leseposition im Schreibpuffer in Sätzen
	noSpill  bool     // true: Liste bleibt dauerhaft im RAM (RAM-Schwelle beim ersten Überlauf noch nicht erreicht oder Auslagerungs-Fehler)

	fileName string // Auslagerungsdatei ("" = bisher nichts ausgelagert)
	writeOff int64  // fertig geschriebene Bytes in der Datei
	readOff  int64  // bis hierhin gelesen bzw. fürs Vorauslesen reserviert (Bytes)

	readBuf []uint16 // aktueller Leseblock
	readLen int      // gültige Sätze im Leseblock
	readPos int      // bereits entnommene Sätze im Leseblock

	// --- Hintergrund-Schreiben (höchstens ein Vorgang) ---
	writeWG  sync.WaitGroup
	pending  []uint16 // Puffer des laufenden Schreibvorgangs (nil = keiner)
	writeErr error    // Ergebnis des Schreibvorgangs (gültig nach writeWG.Wait)

	// --- Hintergrund-Vorauslesen (höchstens ein Vorgang) ---
	readWG          sync.WaitGroup
	prefetch        []uint16 // Puffer des Vorauslesens
	prefetchRecords int      // Sätze des laufenden/fertigen Vorauslesens (0 = keins)
	readErr         error    // Ergebnis des Vorauslesens (gültig nach readWG.Wait)
}

// posCount = Anzahl der möglichen Positionswerte (WalkCount des Feldes): bei bis zu
// 256 begehbaren Feldern passt jeder Wert in ein Byte und die Auslagerungsdateien
// halbieren sich (reines Disk-Format, die RAM-Puffer bleiben uint16)
func NewDepthList(recordSize, posCount int) *DepthList {
	valueBytes := int64(2)
	if posCount <= 256 {
		valueBytes = 1
	}
	return &DepthList{recordSize: recordSize, valueBytes: valueBytes}
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

// reicht den vollen Schreibpuffer an eine Hintergrund-Goroutine weiter und beginnt
// einen frischen Puffer (die Datei wird erst beim ersten Überlauf angelegt); bei
// Fehlern bleibt die Liste einfach komplett im RAM. Bewusst KEIN Puffer-Recycling:
// hunderte gleichzeitig aktive Listen würden sonst je einen vollen Tausch-Puffer
// dauerhaft festhalten - der Zuwachs ab nil ist dagegen billig.
// Der dataRead-Schutz: wird bereits aus dem Schreibpuffer gelesen, darf er nicht mehr
// ausgelagert werden (die FIFO-Buchführung säße sonst schief) - im Solver/Blocker sind
// Push- und Lesephase einer Liste aber ohnehin strikt getrennt.
func (l *DepthList) spillIfFull() {
	if l.noSpill || SpillDir == "" || l.dataRead > 0 || len(l.data)*2 < SpillBufferBytes {
		return
	}
	if l.fileName == "" {
		// einmalige Entscheidung beim ersten Überlauf: solange der Prozess unter der
		// RAM-Schwelle liegt, bleibt diese Liste dauerhaft im RAM und schont die Platte -
		// erst Listen, deren Puffer bei vollem RAM überläuft, lagern wirklich aus
		if SpillRamThresholdBytes > 0 && processHeapBytes() < SpillRamThresholdBytes {
			l.noSpill = true
			return
		}
		file, err := os.CreateTemp(SpillDir, spillPattern)
		if err != nil {
			l.noSpill = true
			return
		}
		l.fileName = file.Name()
		file.Close() // Handle sofort wieder zu, geöffnet wird pro Blockzugriff
	}
	l.finishWrite() // vorigen Schreibvorgang verbuchen (meist längst fertig)
	if l.noSpill {
		return
	}

	l.pending, l.data = l.data, nil
	buf, off := l.pending, l.writeOff
	l.writeWG.Add(1)
	go func() {
		defer l.writeWG.Done()
		l.writeErr = writeSpillBlock(l.fileName, buf, off, l.valueBytes)
	}()
}

// aktueller Heap-Verbrauch des Prozesses; wird nur beim ersten Puffer-Überlauf einer
// Liste abgefragt, denn ReadMemStats ist nicht kostenlos (kurzer Stop-the-World)
func processHeapBytes() uint64 {
	var mem runtime.MemStats
	runtime.ReadMemStats(&mem)
	return mem.Alloc
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
	l.writeOff += int64(len(l.pending)) * l.valueBytes
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
			panic(fmt.Sprintf("DepthList: Lesefehler in Auslagerungsdatei %q: %v", l.fileName, l.readErr))
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
	if err := readSpillBlock(l.fileName, l.readBuf, l.readOff, l.valueBytes); err != nil {
		panic(fmt.Sprintf("DepthList: Lesefehler in Auslagerungsdatei %q: %v", l.fileName, err))
	}
	l.readOff += int64(values) * l.valueBytes
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
	l.readOff += int64(values) * l.valueBytes // Bereich gilt ab jetzt als gelesen (reserviert)
	l.readWG.Add(1)
	go func() {
		defer l.readWG.Done()
		l.readErr = readSpillBlock(l.fileName, buf, off, l.valueBytes)
	}()
}

// Sätze des nächsten Leseblocks (ein Puffer voll, am Dateiende entsprechend weniger)
func (l *DepthList) nextChunkRecords() int {
	records := SpillBufferBytes / 2 / l.recordSize
	if records < 1 {
		records = 1
	}
	if rest := int((l.writeOff - l.readOff) / l.valueBytes / int64(l.recordSize)); records > rest {
		records = rest
	}
	return records
}

// Anzahl der noch nicht entnommenen Sätze
func (l *DepthList) Count() int {
	onDisk := int((l.writeOff-l.readOff)/l.valueBytes/int64(l.recordSize)) + l.prefetchRecords
	return onDisk + l.readLen - l.readPos +
		len(l.pending)/l.recordSize + len(l.data)/l.recordSize - l.dataRead
}

// fertig in die Auslagerungsdatei geschriebene Bytes (0 = Liste liegt komplett im RAM)
func (l *DepthList) SpillBytes() int64 {
	return l.writeOff
}

// aktuell im RAM reservierte Puffer-Bytes der Liste (Schreibpuffer, laufender
// Schreibvorgang, Leseblock und Vorauslese-Block)
func (l *DepthList) RamBytes() int64 {
	return int64(cap(l.data)+cap(l.pending)+cap(l.readBuf)+cap(l.prefetch)) * 2
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
	l.data, l.prefetch = nil, nil
	l.dataRead = 0
	l.noSpill = false
	l.readBuf = nil
	l.readLen, l.readPos = 0, 0
	l.writeOff, l.readOff = 0, 0
	if l.fileName != "" {
		os.Remove(l.fileName)
		l.fileName = ""
	}
}

// schreibt einen Block in die Auslagerungsdatei (Handle nur für diesen Zugriff offen);
// bei valueBytes == 1 wandert nur das Low-Byte jedes Werts auf die Platte. Der temporäre
// Packpuffer wird bewusst je Zugriff frisch angelegt (kein Recycling, siehe spillIfFull)
// und lässt den Original-Puffer unangetastet - der Fehler-Rückzug in finishWrite hängt
// die Sätze sonst korrumpiert zurück an den RAM-Puffer
func writeSpillBlock(name string, buf []uint16, off int64, valueBytes int64) error {
	file, err := os.OpenFile(name, os.O_WRONLY, 0)
	if err != nil {
		return err
	}
	defer file.Close()
	data := u16Bytes(buf)
	if valueBytes == 1 {
		packed := make([]byte, len(buf))
		for i, v := range buf {
			packed[i] = byte(v)
		}
		data = packed
	}
	_, err = file.WriteAt(data, off)
	return err
}

// liest einen Block aus der Auslagerungsdatei (Handle nur für diesen Zugriff offen);
// bei valueBytes == 1 werden die gepackten Bytes wieder auf uint16 aufgeweitet
func readSpillBlock(name string, buf []uint16, off int64, valueBytes int64) error {
	file, err := os.Open(name)
	if err != nil {
		return err
	}
	defer file.Close()
	if valueBytes == 1 {
		packed := make([]byte, len(buf))
		if _, err = file.ReadAt(packed, off); err != nil {
			return err
		}
		for i, b := range packed {
			buf[i] = uint16(b)
		}
		return nil
	}
	_, err = file.ReadAt(u16Bytes(buf), off)
	return err
}

// interpretiert das uint16-Slice als Byte-Slice (native Byte-Reihenfolge - unkritisch,
// die Auslagerungsdateien werden nur vom selben Prozess wieder eingelesen)
func u16Bytes(data []uint16) []byte {
	if len(data) == 0 {
		return nil
	}
	return unsafe.Slice((*byte)(unsafe.Pointer(&data[0])), len(data)*2)
}
