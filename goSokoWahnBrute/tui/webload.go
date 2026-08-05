package tui

import (
	"encoding/xml"
	"errors"
	"fmt"
	"io"
	"net/http"
	"os"
	"path/filepath"
	"strconv"
	"strings"
	"time"

	"goSokoWahnBrute/soko"
)

// Metadaten eines von game-sokoban.com geladenen Levels
type WebLevelInfo struct {
	ID        string // Level-Nummer (lid) der Webseite
	URL       string // Quell-URL
	Catalog   string // Katalog-Name
	Name      string // Level-Name innerhalb des Katalogs
	Number    string // Nummer im Katalog, z.B. "7/32"
	BestMoves int    // Rekord mit den wenigsten Zügen (0 = unbekannt)
	Cached    bool   // true = aus dem lokalen Level-Cache geladen
}

// Ordner für lokal zwischengespeicherte Web-Levels (reduziert die Webseiten-Aufrufe)
const levelCacheDir = "levelcache"

// lädt ein Level von game-sokoban.com (Eingabe: Level-Nummer oder komplette URL)
// und liefert es in Standard-Levelnotation zurück; einmal geladene Levels werden
// unter levelcache/<id>.txt samt Metadaten abgelegt und von dort wiederverwendet
func loadWebLevel(input string) (string, *WebLevelInfo, error) {
	url := strings.TrimSpace(input)
	if _, err := strconv.Atoi(url); err == nil {
		url = "http://www.game-sokoban.com/index.php?mode=level&lid=" + url
	}
	if !strings.Contains(url, "game-sokoban.com/") {
		return "", nil, errors.New("keine Levelnotation, Level-Nummer oder game-sokoban.com-URL erkannt")
	}
	// die Seite liefert nur über http zuverlässig (das https-Zertifikat ist abgelaufen)
	url = strings.Replace(url, "https://", "http://", 1)
	id := extractLevelID(url)

	// zuerst den lokalen Cache versuchen
	if id != "" {
		if level, info, err := readLevelCache(id); err == nil {
			return level, info, nil
		}
	}

	client := http.Client{Timeout: 10 * time.Second}
	resp, err := client.Get(url)
	if err != nil {
		return "", nil, err
	}
	defer resp.Body.Close()
	if resp.StatusCode != http.StatusOK {
		return "", nil, fmt.Errorf("Download fehlgeschlagen: HTTP %d", resp.StatusCode)
	}
	page, err := io.ReadAll(io.LimitReader(resp.Body, 4<<20))
	if err != nil {
		return "", nil, err
	}

	level, info, err := parseWebLevel(string(page))
	if err != nil {
		return "", nil, err
	}
	info.ID = id
	info.URL = url
	if id != "" {
		writeLevelCache(id, level, info) // Fehler ignorieren: Cache ist nur eine Beschleunigung
	}
	return level, info, nil
}

// zieht die Level-Nummer (lid=...) aus einer game-sokoban.com-URL ("" = nicht gefunden)
func extractLevelID(url string) string {
	pos := strings.Index(url, "lid=")
	if pos < 0 {
		return ""
	}
	id := url[pos+4:]
	for i := 0; i < len(id); i++ {
		if id[i] < '0' || id[i] > '9' {
			id = id[:i]
			break
		}
	}
	return id
}

// speichert ein geladenes Level samt Metadaten im Level-Cache
func writeLevelCache(id, level string, info *WebLevelInfo) {
	if err := os.MkdirAll(levelCacheDir, 0755); err != nil {
		return
	}
	var sb strings.Builder
	sb.WriteString(strings.TrimRight(level, "\n"))
	sb.WriteString("\n\n")
	sb.WriteString("url: " + info.URL + "\n")
	sb.WriteString("catalog: " + info.Catalog + "\n")
	sb.WriteString("name: " + info.Name + "\n")
	sb.WriteString("number: " + info.Number + "\n")
	if info.BestMoves > 0 {
		sb.WriteString("bestmoves: " + strconv.Itoa(info.BestMoves) + "\n")
	}
	_ = os.WriteFile(filepath.Join(levelCacheDir, id+".txt"), []byte(sb.String()), 0644)
}

// liest ein Level samt Metadaten aus dem Level-Cache
// (das Level steht am Dateianfang, die Metadaten folgen als "key: value"-Zeilen)
func readLevelCache(id string) (string, *WebLevelInfo, error) {
	data, err := os.ReadFile(filepath.Join(levelCacheDir, id+".txt"))
	if err != nil {
		return "", nil, err
	}

	text := strings.ReplaceAll(string(data), "\r", "")
	info := &WebLevelInfo{ID: id, Cached: true}
	level := text
	if pos := strings.Index(text, "\nurl:"); pos >= 0 {
		level = text[:pos]
		for _, line := range strings.Split(text[pos+1:], "\n") {
			key, value, ok := strings.Cut(line, ":")
			if !ok {
				continue
			}
			value = strings.TrimSpace(value)
			switch key {
			case "url":
				info.URL = value
			case "catalog":
				info.Catalog = value
			case "name":
				info.Name = value
			case "number":
				info.Number = value
			case "bestmoves":
				info.BestMoves, _ = strconv.Atoi(value)
			}
		}
	}
	// auch ältere, noch ungetrimmte Cache-Dateien kompakt zurückgeben
	return soko.NormalizeLevel(level), info, nil
}

// XML-Struktur des Level-Blocks (<l ...><r>...</r><b>...</b><mv>...</mv></l>)
type webLevel struct {
	Rows        []string `xml:"r"`
	Boxes       string   `xml:"b"`
	Player      string   `xml:"mv"`
	Name        string   `xml:"name,attr"`
	Catalog     string   `xml:"cname,attr"`
	Number      string   `xml:"number,attr"`
	TotalLevels string   `xml:"total_levels,attr"`
	Best        string   `xml:"best,attr"`
}

// extrahiert das Level samt Metadaten aus der game-sokoban.com-Seite;
// unterstützt das alte Format (<xml id="startLevel">) und das neue (<div id="startLevel"><l ...>)
func parseWebLevel(page string) (string, *WebLevelInfo, error) {
	pos := strings.Index(page, "id=\"startLevel\"")
	if pos < 0 {
		return "", nil, errors.New("kein Level in der Seite gefunden (id=\"startLevel\" fehlt)")
	}
	page = page[pos:]
	pos = strings.Index(page, "<l ")
	if pos < 0 {
		return "", nil, errors.New("kein Level-Block in der Seite gefunden (<l ...> fehlt)")
	}
	page = page[pos:]
	end := strings.Index(page, "</l>")
	if end < 0 {
		return "", nil, errors.New("Level-Block ist unvollständig (</l> fehlt)")
	}

	var level webLevel
	if err := xml.Unmarshal([]byte(page[:end+4]), &level); err != nil {
		return "", nil, err
	}

	// Zeilen aus der Lauflängen-Kodierung dekodieren (12v = 12x 'v'; v/f = frei, w = Wand, a = Ziel)
	grid := make([][]byte, 0, len(level.Rows))
	width := 0
	for _, row := range level.Rows {
		var line []byte
		for _, token := range strings.Split(row, ",") {
			if token == "" {
				continue
			}
			letter := token[len(token)-1]
			count := 1
			if len(token) > 1 {
				n, err := strconv.Atoi(token[:len(token)-1])
				if err != nil {
					return "", nil, fmt.Errorf("ungültiger Zeilen-Token: %q", token)
				}
				count = n
			}
			var c byte
			switch letter {
			case 'v', 'f':
				c = ' '
			case 'w':
				c = '#'
			case 'a':
				c = '.'
			default:
				return "", nil, fmt.Errorf("unbekanntes Zeichen in Zeile: %q", letter)
			}
			for i := 0; i < count; i++ {
				line = append(line, c)
			}
		}
		grid = append(grid, line)
		if len(line) > width {
			width = len(line)
		}
	}
	if width == 0 || len(grid) == 0 {
		return "", nil, errors.New("leeres Level")
	}
	for i := range grid { // kurze Zeilen auffüllen
		for len(grid[i]) < width {
			grid[i] = append(grid[i], ' ')
		}
	}

	// Kisten und Spieler setzen (Index = Position im Raster ohne Zeilenumbrüche)
	setCell := func(index int, box bool) error {
		if index < 0 || index >= width*len(grid) {
			return fmt.Errorf("Position %d liegt außerhalb des Feldes", index)
		}
		c := &grid[index/width][index%width]
		switch {
		case box && *c == '.':
			*c = '*'
		case box:
			*c = '$'
		case *c == '.':
			*c = '+'
		default:
			*c = '@'
		}
		return nil
	}

	for _, token := range strings.Split(level.Boxes, ",") {
		if token = strings.TrimSpace(token); token == "" {
			continue
		}
		index, err := strconv.Atoi(token)
		if err != nil {
			return "", nil, fmt.Errorf("ungültige Kistenposition: %q", token)
		}
		if err := setCell(index, true); err != nil {
			return "", nil, err
		}
	}
	playerIndex, err := strconv.Atoi(strings.TrimSpace(level.Player))
	if err != nil {
		return "", nil, fmt.Errorf("ungültige Spielerposition: %q", level.Player)
	}
	if err := setCell(playerIndex, false); err != nil {
		return "", nil, err
	}

	var sb strings.Builder
	for _, line := range grid {
		sb.Write(line)
		sb.WriteByte('\n')
	}
	// das Seiten-Raster hat viel Leerraum drumherum -> auf die kompakte Form bringen
	// (Leerzeilen, Zeilenenden und gemeinsame Einrückung weg, wie beim Parsen)
	levelText := soko.NormalizeLevel(sb.String())

	info := &WebLevelInfo{
		Catalog:   strings.TrimSpace(level.Catalog),
		Name:      strings.TrimSpace(level.Name),
		BestMoves: bestMovesFromList(level.Best),
	}
	if level.Number != "" && level.TotalLevels != "" {
		info.Number = level.Number + "/" + level.TotalLevels
	}
	return levelText, info, nil
}

// zieht aus der Rekordliste der Seite (Format "spieler,zuege,...!spieler,zuege,...!...")
// den Rekord mit den wenigsten Zügen (0 = keine verwertbaren Einträge)
func bestMovesFromList(best string) int {
	bestMoves := 0
	for _, entry := range strings.Split(best, "!") {
		fields := strings.Split(entry, ",")
		if len(fields) < 2 {
			continue
		}
		moves, err := strconv.Atoi(strings.TrimSpace(fields[1]))
		if err != nil || moves <= 0 {
			continue
		}
		if bestMoves == 0 || moves < bestMoves {
			bestMoves = moves
		}
	}
	return bestMoves
}
