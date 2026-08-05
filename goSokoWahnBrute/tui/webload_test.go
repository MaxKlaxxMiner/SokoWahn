package tui

import (
	"os"
	"strings"
	"testing"

	"goSokoWahnBrute/soko"
	"goSokoWahnBrute/solver"
)

// WICHTIG: Die Tests in dieser Datei arbeiten ausschließlich offline (eingebettetes
// Seiten-Fragment und testdata-Dateien) - die echte Webseite wird bewusst NICHT abgefragt,
// damit die Testläufe game-sokoban.com nicht zuspammen (der Level-Cache soll die Aufrufe
// ja sogar reduzieren). Einzige Ausnahme: der Opt-in-Live-Test ganz unten.

// echtes Fragment der game-sokoban.com-Seite von Level 200 (neues Format, Stand 2026)
const webPageSample = `<div id="startLevel" class="helper">
	<l id="200" number="1" total_levels="50" name="soko 01" cid="4" cname=" aenigma" solution="Lruu"  author="Brian Kent" best="aleksandar,78,37,263,!" best_page="1" creation_date="1223361635" tmark="a17c0d4c51e3ac476309c25fb5872627" status="0" comments="9" sol_arch=""><r>19v</r><r>19v</r><r>8v,3w,8v</r><r>8v,w,a,w,8v</r><r>4v,5w,a,5w,4v</r><r>3v,2w,9f,2w,3v</r><r>2v,2w,2f,w,f,w,f,w,f,w,2f,2w,2v</r><r>2v,w,2f,2w,5f,2w,2f,w,2v</r><r>2v,w,f,2w,2f,w,f,w,2f,2w,f,w,2v</r><r>2v,w,13f,w,2v</r><r>2v,4w,2f,3w,2f,4w,2v</r><r>5v,4w,v,4w,5v</r><r>19v</r><r>19v</r><r>19v</r><r>19v</r><r>19v</r><am>66,85</am><b>179,181</b><mv>180</mv></l></div>`

func TestParseWebLevel(t *testing.T) {
	level, info, err := parseWebLevel(webPageSample)
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

	// das Level muss kompakt sein: keine Leerzeilen/Einrückung/Zeilenend-Leerzeichen
	if level != soko.NormalizeLevel(level) {
		t.Errorf("Level ist nicht normalisiert:\n%q", level)
	}
	if strings.HasPrefix(level, "\n") || strings.Contains(level, " \n") {
		t.Errorf("Level enthält Leerraum drumherum:\n%q", level)
	}

	// Metadaten aus den Attributen des Level-Blocks
	if info.Catalog != "aenigma" || info.Name != "soko 01" || info.Number != "1/50" {
		t.Errorf("unerwartete Metadaten: %+v", info)
	}
	if info.BestMoves != 78 {
		t.Errorf("erwartet 78 Bestmoves, erhalten: %d", info.BestMoves)
	}
}

// Cache-Roundtrip: Speichern und Laden eines Web-Levels samt Metadaten
func TestLevelCacheRoundtrip(t *testing.T) {
	t.Chdir(t.TempDir())

	level, info, err := parseWebLevel(webPageSample)
	if err != nil {
		t.Fatal(err)
	}
	info.ID = "200"
	info.URL = "http://www.game-sokoban.com/index.php?mode=level&lid=200"
	writeLevelCache("200", level, info)

	cachedLevel, cachedInfo, err := readLevelCache("200")
	if err != nil {
		t.Fatal("Cache-Datei nicht lesbar:", err)
	}
	if cachedLevel != level {
		t.Errorf("Level aus dem Cache weicht ab:\n%q\n%q", cachedLevel, level)
	}
	if !cachedInfo.Cached {
		t.Error("Cached-Flag fehlt")
	}
	if cachedInfo.URL != info.URL || cachedInfo.Catalog != "aenigma" || cachedInfo.Name != "soko 01" ||
		cachedInfo.Number != "1/50" || cachedInfo.BestMoves != 78 {
		t.Errorf("Metadaten aus dem Cache weichen ab: %+v", cachedInfo)
	}

	// die Cache-Datei muss auch direkt als Level-Datei parsebar sein (Metadaten werden ignoriert)
	data, err := os.ReadFile("levelcache/200.txt")
	if err != nil {
		t.Fatal(err)
	}
	if _, err := soko.Parse(string(data)); err != nil {
		t.Errorf("Cache-Datei parst nicht als Level: %v", err)
	}
}

// die lid-Erkennung aus URLs und Nummern
func TestExtractLevelID(t *testing.T) {
	cases := map[string]string{
		"http://www.game-sokoban.com/index.php?mode=level&lid=29622": "29622",
		"http://www.game-sokoban.com/index.php?lid=7&mode=level":     "7",
		"http://www.game-sokoban.com/index.php?mode=level":           "",
	}
	for url, want := range cases {
		if got := extractLevelID(url); got != want {
			t.Errorf("extractLevelID(%q) = %q, erwartet %q", url, got, want)
		}
	}
}

func TestParseWebLevelOldFormat(t *testing.T) {
	// altes Format: <xml id="startLevel"> statt <div id="startLevel"><l ...>
	oldPage := strings.Replace(webPageSample, `<div id="startLevel" class="helper">`, `<xml id="startLevel">`, 1)
	oldPage = strings.Replace(oldPage, `</l></div>`, `</l></xml>`, 1)

	level, _, err := parseWebLevel(oldPage)
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
	level, _, err := parseWebLevel(string(page))
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

// einziger Test mit echtem Webseiten-Zugriff - läuft NUR auf ausdrücklichen Wunsch:
//
//	SOKO_WEB_TEST=1 go test -run TestWebLevelLive ./tui/
//
// (prüft Download, Metadaten und dass der zweite Aufruf aus dem Level-Cache kommt)
func TestWebLevelLive(t *testing.T) {
	if os.Getenv("SOKO_WEB_TEST") == "" {
		t.Skip("Live-Test nur mit SOKO_WEB_TEST=1 (Webseite nicht unnötig abfragen)")
	}
	t.Chdir(t.TempDir())

	level, info, err := loadWebLevel("29622")
	if err != nil {
		t.Fatal(err)
	}
	if info.Cached {
		t.Error("erster Aufruf darf nicht aus dem Cache kommen")
	}
	if info.Catalog != "A.K.K. Informatika" || info.Name != "level 07" || info.Number != "7/32" {
		t.Errorf("unerwartete Metadaten: %+v", info)
	}
	if info.BestMoves <= 0 {
		t.Errorf("Bestmoves fehlt: %d", info.BestMoves)
	}
	if _, err := soko.Parse(level); err != nil {
		t.Errorf("geladenes Level parst nicht: %v", err)
	}

	level2, info2, err := loadWebLevel("29622")
	if err != nil {
		t.Fatal(err)
	}
	if !info2.Cached || level2 != level {
		t.Error("zweiter Aufruf müsste identisch aus dem Level-Cache kommen")
	}
}

// halb offenes Level 353: begehbare Felder reichen bis an die Rasterkante (keine
// umschließende Wand). Die Rasterkante wirkt im Parser als Wand, Void-Zellen neben
// begehbaren Feldern werden beim Laden zu Wänden - das Level muss damit exakt den
// Bestwert der Seite erreichen (93 Züge; die alte C#-Version fand hier keine Lösung).
func TestWebLevelHalfOpen(t *testing.T) {
	page, err := os.ReadFile("testdata/lid353.html")
	if err != nil {
		t.Fatal(err)
	}
	level, info, err := parseWebLevel(string(page))
	if err != nil {
		t.Fatal(err)
	}
	if info.BestMoves != 93 {
		t.Errorf("erwartet 93 Bestmoves, erhalten: %d", info.BestMoves)
	}

	field, err := soko.Parse(level)
	if err != nil {
		t.Fatalf("geparstes Level ist ungültig: %v\n%s", err, level)
	}
	if field.BoxCount() != 7 {
		t.Errorf("erwartet 7 Kisten, erhalten: %d", field.BoxCount())
	}

	s := solver.New(field)
	for s.Step(1000000000) {
	}
	if got := s.GetStats().FoundMoves; got != 93 {
		t.Errorf("erwartet 93 Züge (Bestwert der Seite), erhalten: %d", got)
	}
}
