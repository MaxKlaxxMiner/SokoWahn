package rooms

import (
	"bytes"
	"testing"
)

// Snapshot-Roundtrip: Speichern und Laden muss ein Raum-für-Raum identisches
// Netzwerk ergeben (inklusive gemergtem Zwischenstand mit echten Portal-
// Verzeichnissen); der Header trägt Crc und Effort für die Snapshot-Liste
func TestSnapshotRoundtrip(t *testing.T) {
	n := buildNetwork(t, mapTwoBox)
	if _, err := n.MergeSelection([]uint32{0, 1, 2}, 0, nil); err != nil {
		t.Fatal("merge:", err)
	}

	var buf bytes.Buffer
	if err := n.WriteSnapshot(&buf, nil); err != nil {
		t.Fatal("write:", err)
	}

	crc, effort, err := ReadSnapshotHeader(bytes.NewReader(buf.Bytes()))
	if err != nil {
		t.Fatal("header:", err)
	}
	if crc != n.Field.FieldCrc() || effort != n.EffortString() {
		t.Errorf("header: got crc %016x/effort %q, want %016x/%q", crc, effort, n.Field.FieldCrc(), n.EffortString())
	}

	loaded, err := ReadSnapshot(n.Field, n.Scan, bytes.NewReader(buf.Bytes()), nil)
	if err != nil {
		t.Fatal("read:", err)
	}
	checkFreshEqual(t, loaded, n)
	if loaded.EffortString() != n.EffortString() {
		t.Errorf("effort: got %q, want %q", loaded.EffortString(), n.EffortString())
	}

	// geladener Stand muss normal weiterrechenbar sein (Voll-Merge-Anker)
	minMoves, _ := checkFullMerge(t, fullMerge(t, loaded))
	if minMoves != 9 {
		t.Errorf("min moves nach load+merge: got %d, want 9", minMoves)
	}
}

// ein Snapshot eines anderen Levels darf nicht ladbar sein (Crc-Bindung)
func TestSnapshotWrongLevel(t *testing.T) {
	n := buildNetwork(t, mapTwoBox)
	var buf bytes.Buffer
	if err := n.WriteSnapshot(&buf, nil); err != nil {
		t.Fatal("write:", err)
	}
	other := buildNetwork(t, mapMini)
	if _, err := ReadSnapshot(other.Field, other.Scan, bytes.NewReader(buf.Bytes()), nil); err == nil {
		t.Fatal("snapshot eines anderen Levels wurde geladen")
	}
}
