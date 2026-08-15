package tui

import (
	"strings"
	"testing"

	"github.com/charmbracelet/lipgloss"
	"github.com/muesli/termenv"

	"goSokoWahnBrute/soko"
)

// die Farb-Markierungen des Spielfeldes: Spieler grün, zuletzt geschobene Kiste rot
func TestColorField(t *testing.T) {
	lipgloss.SetColorProfile(termenv.ANSI256) // ohne Terminal wäre das Profil sonst farblos

	field := `#####
#@$.#
#####`
	out := colorField(field, 2, 1) // die Kiste in Zeile 1, Spalte 2

	if !strings.Contains(out, stylePlayer.Render("@")) {
		t.Errorf("Spieler nicht grün markiert: %q", out)
	}
	if !strings.Contains(out, stylePushed.Render("$")) {
		t.Errorf("geschobene Kiste nicht rot markiert: %q", out)
	}
	if got := len(strings.Split(out, "\n")); got != 3 {
		t.Errorf("Zeilenzahl verändert: %d statt 3", got)
	}

	// ohne Markierung bleibt die Kiste unangetastet
	plain := colorField(field, -1, -1)
	if strings.Contains(plain, stylePushed.Render("$")) {
		t.Errorf("Kiste ohne Markierung eingefärbt: %q", plain)
	}
}

// die geschobene Kiste ist die Position, die vorher noch frei war
func TestPushedBox(t *testing.T) {
	prev := &soko.State{Boxes: []soko.Wpos{3, 7, 11}}
	cur := &soko.State{Boxes: []soko.Wpos{7, 12, 3}} // 11 -> 12 geschoben, Reihenfolge egal

	box, ok := pushedBox(prev, cur)
	if !ok || box != 12 {
		t.Errorf("geschobene Kiste: %d (ok = %v), erwartet 12", box, ok)
	}

	// Startstellung mit sich selbst verglichen: nichts bewegt
	if _, ok := pushedBox(prev, prev); ok {
		t.Error("unveränderte Stellung liefert eine geschobene Kiste")
	}
}

// lange Zugfolgen werden auf ein mitlaufendes Fenster beschnitten
func TestWrapMovesWindow(t *testing.T) {
	moves := strings.Repeat("u", 1000) // 100 Zeilen zu 10 Zeichen

	// Anfang: kein "..." oben, dafür unten abgeschnitten
	head := strings.Split(wrapMoves(moves, 10, 0, 8), "\n")
	if len(head) != 9 || head[0] == "..." || head[len(head)-1] != "..." {
		t.Errorf("Fenster am Anfang falsch (%d Zeilen): %q", len(head), head)
	}

	// Mitte: beidseitig abgeschnitten, aktuelle Zeile enthalten
	mid := strings.Split(wrapMoves(moves, 10, 500, 8), "\n")
	if len(mid) != 10 || mid[0] != "..." || mid[len(mid)-1] != "..." {
		t.Errorf("Fenster in der Mitte falsch (%d Zeilen): %q", len(mid), mid)
	}
	if !strings.Contains(mid[4], "\x1b[") { // die zuletzt vollständig ausgeführte Zeile
		t.Errorf("aktuelle Zeile nicht im Fenster: %q", mid)
	}

	// Ende: kein "..." unten, das letzte Zeichen bleibt sichtbar
	tail := strings.Split(wrapMoves(moves, 10, 1000, 8), "\n")
	if len(tail) != 9 || tail[0] != "..." || tail[len(tail)-1] == "..." {
		t.Errorf("Fenster am Ende falsch (%d Zeilen): %q", len(tail), tail)
	}

	// kurze Zugfolge bleibt unbeschnitten
	short := wrapMoves("uuuuuuuuuulll", 10, 0, 8)
	if strings.Contains(short, "...") {
		t.Errorf("kurze Zugfolge beschnitten: %q", short)
	}
}

// die gelbe Füllstands-Markierung greift genau ab der angezeigten 99,9 %
func TestFillNearFull(t *testing.T) {
	cases := []struct {
		fill float64
		want bool
	}{
		{-1, false},     // unbekannter Füllstand
		{0.5, false},    // 50,0 %
		{0.9984, false}, // 99,8 %
		{0.9985, true},  // rundet auf 99,9 %
		{0.999, true},   // 99,9 %
		{1.0, true},     // Resize steht an
		{1.2, true},     // MaxMem-Modus, 100 % überschritten
	}
	for _, c := range cases {
		if got := fillNearFull(c.fill); got != c.want {
			t.Errorf("fillNearFull(%v) = %v, erwartet %v", c.fill, got, c.want)
		}
	}
}
