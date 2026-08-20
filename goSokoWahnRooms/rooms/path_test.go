package rooms

import "testing"

// Pack/Unpack-Roundtrip über alle Padding-Fälle plus Concat-Kombinationen
func TestPathRoundtrip(t *testing.T) {
	cases := []string{
		"", "l", "u", "r", "d", // 1 Zug: Padding 2
		"lu", "rd", // Padding 1
		"lur", // Padding 0
		"lurd", // 2 Bytes, Padding 3
		"drrrllurr", // TwoBox-Optimum (Path kennt nur Kleinbuchstaben)
		"lurdlurdlurdlurdlurdl",
	}
	for _, want := range cases {
		p := PathFromLURD(want)
		if got := p.LURD(); got != want {
			t.Errorf("roundtrip %q: got %q", want, got)
		}
		if got := p.Len(); got != len(want) {
			t.Errorf("len %q: got %d, want %d", want, got, len(want))
		}
	}

	for _, a := range cases {
		for _, b := range cases {
			got := PathFromLURD(a).Concat(PathFromLURD(b)).LURD()
			if got != a+b {
				t.Errorf("concat %q+%q: got %q", a, b, got)
			}
		}
	}
}

// die Packung muss wirklich packen: 4 Züge je Byte (plus der Padding-Slot)
func TestPathSize(t *testing.T) {
	if got := len(PathFromLURD("lur")); got != 1 {
		t.Errorf("3 züge: %d bytes, want 1", got)
	}
	if got := len(PathFromLURD("lurdlurdlur")); got != 3 {
		t.Errorf("11 züge: %d bytes, want 3", got)
	}
	if PathFromLURD("") != nil {
		t.Error("leere zugfolge muss nil sein")
	}
}
