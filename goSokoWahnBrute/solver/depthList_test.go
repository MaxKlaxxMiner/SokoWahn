package solver

import (
	"os"
	"path/filepath"
	"testing"
	"time"

	"goSokoWahnBrute/soko"
)

// stellt die Disk-Auslagerung für einen Test scharf (eigener Temp-Ordner, kleine
// Puffergröße) und setzt die Konfiguration danach wieder zurück
func setupSpill(t *testing.T, bufferBytes int) string {
	t.Helper()
	oldDir, oldSize := SpillDir, SpillBufferBytes
	dir := t.TempDir()
	SpillDir, SpillBufferBytes = dir, bufferBytes
	t.Cleanup(func() { SpillDir, SpillBufferBytes = oldDir, oldSize })
	return dir
}

// zählt die Auslagerungsdateien in einem Ordner
func countSpillFiles(t *testing.T, dir string) int {
	t.Helper()
	matches, err := filepath.Glob(filepath.Join(dir, spillPattern))
	if err != nil {
		t.Fatal(err)
	}
	return len(matches)
}

// Auslagerung: die Sätze müssen in exakt derselben FIFO-Reihenfolge zurückkommen wie
// bei der reinen RAM-Variante - auch über Blockgrenzen hinweg und wenn nach begonnenem
// Lesen noch weitere Sätze eingeschoben werden
func TestDepthListSpillFifo(t *testing.T) {
	const recordSize = 3
	const records = 1000
	dir := setupSpill(t, 64) // 64 Bytes = 32 Werte -> gut 10 Sätze pro Block

	list := NewDepthList(recordSize)
	push := func(i int) {
		list.Push(&soko.State{Player: soko.Wpos(i), Boxes: []soko.Wpos{soko.Wpos(i + 1), soko.Wpos(i + 2)}})
	}

	pushed := records / 2
	for i := 0; i < pushed; i++ {
		push(i)
	}
	if list.SpillBytes() == 0 {
		t.Fatal("Liste hat trotz überschrittener Puffergröße nicht ausgelagert")
	}
	if n := countSpillFiles(t, dir); n != 1 {
		t.Fatalf("erwartete genau 1 Auslagerungsdatei, gefunden: %d", n)
	}

	// mit wechselnden Batchgrößen lesen, zwischendurch weitere Sätze einschieben
	next := 0 // nächster erwarteter Satz
	for batchSize := 1; list.Count() > 0; batchSize++ {
		if got := list.Count(); got != pushed-next {
			t.Fatalf("Count = %d, erwartet %d", got, pushed-next)
		}
		batch := list.PopBatch(batchSize)
		if len(batch) == 0 || len(batch)%recordSize != 0 {
			t.Fatalf("ungültige Batchgröße: %d Werte", len(batch))
		}
		for off := 0; off < len(batch); off += recordSize {
			if batch[off] != uint16(next) || batch[off+1] != uint16(next+1) || batch[off+2] != uint16(next+2) {
				t.Fatalf("Satz %d: erwartet [%d %d %d], erhalten %v", next, next, next+1, next+2, batch[off:off+recordSize])
			}
			next++
		}
		if pushed < records { // Push nach begonnenem Lesen: Reihenfolge muss halten
			push(pushed)
			pushed++
		}
	}
	if next != records {
		t.Fatalf("es kamen %d Sätze zurück, erwartet %d", next, records)
	}

	list.Release()
	if countSpillFiles(t, dir) != 0 {
		t.Fatal("Release hat die Auslagerungsdatei nicht gelöscht")
	}
}

// ohne SpillDir (Standard in Tests) darf nie eine Datei entstehen -
// die Liste verhält sich wie die alte reine RAM-Variante
func TestDepthListNoSpillDir(t *testing.T) {
	const recordSize = 2
	list := NewDepthList(recordSize)
	for i := 0; i < 10000; i++ {
		list.PushRecord([]uint16{uint16(i), uint16(i + 1)})
	}
	if list.SpillBytes() != 0 {
		t.Fatal("ohne SpillDir darf nichts ausgelagert werden")
	}
	batch := list.PopBatch(10000)
	if len(batch) != 10000*recordSize {
		t.Fatalf("erwartete alle Sätze in einem Batch, erhalten: %d Werte", len(batch))
	}
	list.Release()
}

// Aufräumen beim Programmstart: nur alte Auslagerungsdateien (über maxAge) werden
// gelöscht - frische Dateien (parallel laufende Prozesse) und fremde Dateien
// (z.B. Blocker-Caches) bleiben stehen
func TestCleanupSpillFiles(t *testing.T) {
	dir := t.TempDir()
	oldFile := filepath.Join(dir, "sokolist_123.tmp")
	newFile := filepath.Join(dir, "sokolist_456.tmp")
	foreign := filepath.Join(dir, "blocker_x0000000000000000.gz")
	for _, path := range []string{oldFile, newFile, foreign} {
		if err := os.WriteFile(path, []byte("x"), 0644); err != nil {
			t.Fatal(err)
		}
	}
	past := time.Now().Add(-25 * time.Hour)
	for _, path := range []string{oldFile, foreign} {
		if err := os.Chtimes(path, past, past); err != nil {
			t.Fatal(err)
		}
	}

	CleanupSpillFiles(dir, 24*time.Hour)

	if _, err := os.Stat(oldFile); !os.IsNotExist(err) {
		t.Error("alte Auslagerungsdatei wurde nicht gelöscht")
	}
	if _, err := os.Stat(newFile); err != nil {
		t.Error("frische Auslagerungsdatei hätte stehen bleiben müssen")
	}
	if _, err := os.Stat(foreign); err != nil {
		t.Error("fremde Datei hätte stehen bleiben müssen")
	}
}
