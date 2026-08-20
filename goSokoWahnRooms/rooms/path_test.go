package rooms

import "testing"

// Pack/Unpack-Roundtrip über alle Rest-Fälle plus Concat-Kombinationen
func TestPathStoreRoundtrip(t *testing.T) {
	cases := []string{
		"", "l", "u", "r", "d",
		"lu", "rd",
		"lur",
		"lurd",                  // Blatt über 4 Züge = 2 leaf-Bytes
		"drrrllurr",             // TwoBox-Optimum
		"lurdlurdlurdlurdlurdl", // 21 Züge
	}
	ps := NewPathStore()
	for _, want := range cases {
		id := ps.AddLURD(want)
		if got := ps.LURD(id); got != want {
			t.Errorf("roundtrip %q: got %q", want, got)
		}
		if got := ps.Len(id); got != len(want) {
			t.Errorf("len %q: got %d, want %d", want, got, len(want))
		}
	}

	// Concat: alle Paare, auch verschachtelt (Concat auf Concat-Ergebnis)
	for _, a := range cases {
		for _, b := range cases {
			ab := ps.Concat(ps.AddLURD(a), ps.AddLURD(b))
			if got := ps.LURD(ab); got != a+b {
				t.Errorf("concat %q+%q: got %q", a, b, got)
			}
			aba := ps.Concat(ab, ps.AddLURD(a))
			if got := ps.LURD(aba); got != a+b+a {
				t.Errorf("concat %q+%q+%q: got %q", a, b, a, got)
			}
			if got := ps.Len(aba); got != 2*len(a)+len(b) {
				t.Errorf("concat len %q+%q+%q: got %d", a, b, a, got)
			}
		}
	}
}

// Ein-Zug-Pfade sind vordefinierte IDs (1-4), in jedem Store identisch
func TestPathStoreDirs(t *testing.T) {
	ps := NewPathStore()
	for _, dir := range []byte("lurd") {
		id := PathOfDir(dir)
		if id == EmptyPath || id > 4 {
			t.Errorf("dir %c: id %d außerhalb der Vorbelegung", dir, id)
		}
		if got := ps.LURD(id); got != string(dir) {
			t.Errorf("dir %c: got %q", dir, got)
		}
	}
	if before := len(ps.nodes); ps.AddLURD("") != EmptyPath || len(ps.nodes) != before {
		t.Error("leere Zugfolge muss EmptyPath ohne neuen Knoten liefern")
	}
}

// CopyFrom überträgt Ketten zwischen Stores und erhält das Sharing
func TestPathStoreCopyFrom(t *testing.T) {
	src := NewPathStore()
	prefix := src.AddLURD("lurdlur")
	a := src.Concat(prefix, PathOfDir('l'))
	b := src.Concat(prefix, PathOfDir('d'))

	dst := NewPathStore()
	memo := map[PathID]PathID{}
	na := dst.CopyFrom(src, a, memo)
	nodesAfterA := len(dst.nodes)
	nb := dst.CopyFrom(src, b, memo)
	if got := dst.LURD(na); got != "lurdlurl" {
		t.Errorf("copy a: got %q", got)
	}
	if got := dst.LURD(nb); got != "lurdlurd" {
		t.Errorf("copy b: got %q", got)
	}
	// b teilt den Präfix mit a: nur der eine neue Concat-Knoten darf dazukommen
	if got := len(dst.nodes) - nodesAfterA; got != 1 {
		t.Errorf("sharing verloren: %d neue Knoten für b, want 1", got)
	}
}

// ExportPacked/AddPacked: das dichte 2-Bit-Format des Snapshot-Speicherns
func TestPathStorePacked(t *testing.T) {
	ps := NewPathStore()
	id := ps.Concat(ps.AddLURD("drrrl"), ps.AddLURD("lurr"))
	data, moves := ps.ExportPacked(id)
	if moves != 9 || len(data) != 3 {
		t.Fatalf("export: %d züge in %d bytes, want 9 in 3", moves, len(data))
	}
	other := NewPathStore()
	if got := other.LURD(other.AddPacked(data, moves)); got != "drrrllurr" {
		t.Errorf("packed roundtrip: got %q", got)
	}
}
