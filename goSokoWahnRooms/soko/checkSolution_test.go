package soko

import (
	"strings"
	"testing"
)

// Zwei-Kisten-Level mit bekanntem Optimum dRRRlluRR (9 Züge, siehe rooms-Tests)
const checkTwoBox = `
#######
#@ $ .#
# $  .#
#######
`

func TestCheckSolution(t *testing.T) {
	f, err := Parse(checkTwoBox)
	if err != nil {
		t.Fatal(err)
	}

	if err := f.CheckSolution("dRRRlluRR"); err != nil {
		t.Errorf("optimum abgelehnt: %v", err)
	}
	// Groß-/Kleinschreibung ist egal
	if err := f.CheckSolution("drrrllurr"); err != nil {
		t.Errorf("kleingeschriebenes optimum abgelehnt: %v", err)
	}

	cases := []struct{ lurd, wantErr string }{
		{"dRRRlluR", "nicht gelöst"}, // eine Kiste bleibt liegen
		{"u", "Wand"},                // Spieler läuft in die Wand
		{"rrRR", "schieben"},         // Kiste vom Ziel weiter in die Wand gedrückt
		{"dRRRlluRRx", "Zeichen"},    // ungültiges Zeichen
	}
	for _, tc := range cases {
		err := f.CheckSolution(tc.lurd)
		if err == nil || !strings.Contains(err.Error(), tc.wantErr) {
			t.Errorf("%q: got %v, want Fehler mit %q", tc.lurd, err, tc.wantErr)
		}
	}
}
