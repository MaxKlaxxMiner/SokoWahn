package weblevel

import (
	"os"
	"strings"
	"testing"

	"goSokoWahnRooms/soko"
)

// WICHTIG: Die Tests in dieser Datei arbeiten ausschließlich offline (eingebettetes
// Seiten-Fragment) - die echte Webseite wird bewusst NICHT abgefragt, damit die
// Testläufe game-sokoban.com nicht zuspammen (der Level-Cache soll die Aufrufe
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

// die Eingabe-Erkennung für main: Nummer und URL ja, Dateiname und Leveltext nein
func TestIsWebInput(t *testing.T) {
	cases := map[string]bool{
		"2164": true,
		"http://www.game-sokoban.com/index.php?mode=level&lid=2164": true,
		"levelcache/2164.txt": false,
		"#####\n#@$.#\n#####": false,
	}
	for input, want := range cases {
		if got := IsWebInput(input); got != want {
			t.Errorf("IsWebInput(%q) = %v, erwartet %v", input, got, want)
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

// einziger Test mit echtem Webseiten-Zugriff - läuft NUR auf ausdrücklichen Wunsch:
//
//	SOKO_WEB_TEST=1 go test -run TestWebLevelLive ./weblevel/
//
// (prüft Download, Metadaten und dass der zweite Aufruf aus dem Level-Cache kommt)
func TestWebLevelLive(t *testing.T) {
	if os.Getenv("SOKO_WEB_TEST") == "" {
		t.Skip("Live-Test nur mit SOKO_WEB_TEST=1 (Webseite nicht unnötig abfragen)")
	}
	t.Chdir(t.TempDir())

	level, info, err := Load("29622")
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

	level2, info2, err := Load("29622")
	if err != nil {
		t.Fatal(err)
	}
	if !info2.Cached || level2 != level {
		t.Error("zweiter Aufruf müsste identisch aus dem Level-Cache kommen")
	}
}
