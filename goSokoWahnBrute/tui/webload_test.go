package tui

import (
	"os"
	"strings"
	"testing"

	"goSokoWahnBrute/soko"
)

// echtes Fragment der game-sokoban.com-Seite von Level 200 (neues Format, Stand 2026)
const webPageSample = `<div id="startLevel" class="helper">
	<l id="200" number="1" total_levels="50" name="soko 01" cid="4" cname=" aenigma" solution="Lruu"  author="Brian Kent" best="aleksandar,78,37,263,!" best_page="1" creation_date="1223361635" tmark="a17c0d4c51e3ac476309c25fb5872627" status="0" comments="9" sol_arch=""><r>19v</r><r>19v</r><r>8v,3w,8v</r><r>8v,w,a,w,8v</r><r>4v,5w,a,5w,4v</r><r>3v,2w,9f,2w,3v</r><r>2v,2w,2f,w,f,w,f,w,f,w,2f,2w,2v</r><r>2v,w,2f,2w,5f,2w,2f,w,2v</r><r>2v,w,f,2w,2f,w,f,w,2f,2w,f,w,2v</r><r>2v,w,13f,w,2v</r><r>2v,4w,2f,3w,2f,4w,2v</r><r>5v,4w,v,4w,5v</r><r>19v</r><r>19v</r><r>19v</r><r>19v</r><r>19v</r><am>66,85</am><b>179,181</b><mv>180</mv></l></div>`

func TestParseWebLevel(t *testing.T) {
	level, err := parseWebLevel(webPageSample)
	if err != nil {
		t.Fatal(err)
	}

	// das Ergebnis muss ein gültiges Sokoban-Level mit 2 Kisten sein
	field, err := soko.Parse(level)
	if err != nil {
		t.Fatalf("geparstes Level ist ungültig: %v\n%s", err, level)
	}
	if field.BoxCount() != 2 {
		t.Errorf("erwartet 2 Kisten, erhalten: %d", field.BoxCount())
	}

	// Stichprobe: die Kisten stehen nebeneinander mit dem Spieler dazwischen ($@$)
	if !strings.Contains(level, "$@$") {
		t.Errorf("erwartetes Muster '$@$' fehlt:\n%s", level)
	}
}

func TestParseWebLevelOldFormat(t *testing.T) {
	// altes Format: <xml id="startLevel"> statt <div id="startLevel"><l ...>
	oldPage := strings.Replace(webPageSample, `<div id="startLevel" class="helper">`, `<xml id="startLevel">`, 1)
	oldPage = strings.Replace(oldPage, `</l></div>`, `</l></xml>`, 1)

	level, err := parseWebLevel(oldPage)
	if err != nil {
		t.Fatal(err)
	}
	if _, err := soko.Parse(level); err != nil {
		t.Fatalf("geparstes Level ist ungültig: %v", err)
	}
}

// Level 37988 startet bereits gelöst (alle 70 Kisten stehen auf Zielfeldern):
// das ist KEINE 0-Züge-Lösung - das Spiel prüft den Zielzustand erst nach einem Zug,
// die Suche muss also normal anlaufen (Kiste herausschieben und wiederherstellen).
// Die Original-Seite liegt als Testdatei bei, damit auch der Loader real geprüft wird.
func TestWebLevelSolvedStart(t *testing.T) {
	page, err := os.ReadFile("testdata/lid37988.html")
	if err != nil {
		t.Fatal(err)
	}
	level, err := parseWebLevel(string(page))
	if err != nil {
		t.Fatal(err)
	}

	field, err := soko.Parse(level)
	if err != nil {
		t.Fatalf("geparstes Level ist ungültig: %v\n%s", err, level)
	}
	if field.BoxCount() != 70 {
		t.Errorf("erwartet 70 Kisten, erhalten: %d", field.BoxCount())
	}
	if !field.IsSolved() {
		t.Fatal("Level 37988 müsste bereits gelöst starten")
	}

	t.Chdir(t.TempDir()) // Blocker-Cache nicht im Repo ablegen
	m := NewModel("", 0)
	m.input.SetValue(level)
	m.scan()
	if m.inputErr != "" {
		t.Fatalf("scan meldet einen Fehler: %s", m.inputErr)
	}
	m.blk.Abort()
	m.startSearch()

	// die Suche darf nicht sofort mit einer 0-Züge-Lösung enden; ein paar Schritte
	// rechnen (komplett lösen wäre für einen Test zu teuer - 70 Kisten)
	if !m.slv.Step(1000) {
		t.Fatal("Suche endet sofort - gelöster Start wurde fälschlich als Lösung gewertet")
	}
	for i := 0; i < 5; i++ {
		m.slv.Step(1000)
	}
	if found := m.slv.GetStats().FoundMoves; found == 0 {
		t.Fatal("0-Züge-Lösung gemeldet - das Spiel verlangt mindestens einen Zug")
	}
}
