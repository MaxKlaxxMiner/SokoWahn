package tui

import (
	"encoding/xml"
	"errors"
	"fmt"
	"io"
	"net/http"
	"strconv"
	"strings"
	"time"
)

// lädt ein Level von game-sokoban.com (Eingabe: Level-Nummer oder komplette URL)
// und liefert es in Standard-Levelnotation zurück
func loadWebLevel(input string) (string, error) {
	url := strings.TrimSpace(input)
	if _, err := strconv.Atoi(url); err == nil {
		url = "http://www.game-sokoban.com/index.php?mode=level&lid=" + url
	}
	if !strings.Contains(url, "game-sokoban.com/") {
		return "", errors.New("keine Levelnotation, Level-Nummer oder game-sokoban.com-URL erkannt")
	}
	// die Seite liefert nur über http zuverlässig (das https-Zertifikat ist abgelaufen)
	url = strings.Replace(url, "https://", "http://", 1)

	client := http.Client{Timeout: 10 * time.Second}
	resp, err := client.Get(url)
	if err != nil {
		return "", err
	}
	defer resp.Body.Close()
	if resp.StatusCode != http.StatusOK {
		return "", fmt.Errorf("Download fehlgeschlagen: HTTP %d", resp.StatusCode)
	}
	page, err := io.ReadAll(io.LimitReader(resp.Body, 4<<20))
	if err != nil {
		return "", err
	}

	return parseWebLevel(string(page))
}

// XML-Struktur des Level-Blocks (<l ...><r>...</r><b>...</b><mv>...</mv></l>)
type webLevel struct {
	Rows   []string `xml:"r"`
	Boxes  string   `xml:"b"`
	Player string   `xml:"mv"`
}

// extrahiert das Level aus der game-sokoban.com-Seite;
// unterstützt das alte Format (<xml id="startLevel">) und das neue (<div id="startLevel"><l ...>)
func parseWebLevel(page string) (string, error) {
	pos := strings.Index(page, "id=\"startLevel\"")
	if pos < 0 {
		return "", errors.New("kein Level in der Seite gefunden (id=\"startLevel\" fehlt)")
	}
	page = page[pos:]
	pos = strings.Index(page, "<l ")
	if pos < 0 {
		return "", errors.New("kein Level-Block in der Seite gefunden (<l ...> fehlt)")
	}
	page = page[pos:]
	end := strings.Index(page, "</l>")
	if end < 0 {
		return "", errors.New("Level-Block ist unvollständig (</l> fehlt)")
	}

	var level webLevel
	if err := xml.Unmarshal([]byte(page[:end+4]), &level); err != nil {
		return "", err
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
					return "", fmt.Errorf("ungültiger Zeilen-Token: %q", token)
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
				return "", fmt.Errorf("unbekanntes Zeichen in Zeile: %q", letter)
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
		return "", errors.New("leeres Level")
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
			return "", fmt.Errorf("ungültige Kistenposition: %q", token)
		}
		if err := setCell(index, true); err != nil {
			return "", err
		}
	}
	playerIndex, err := strconv.Atoi(strings.TrimSpace(level.Player))
	if err != nil {
		return "", fmt.Errorf("ungültige Spielerposition: %q", level.Player)
	}
	if err := setCell(playerIndex, false); err != nil {
		return "", err
	}

	var sb strings.Builder
	for _, line := range grid {
		sb.Write(line)
		sb.WriteByte('\n')
	}
	return sb.String(), nil
}
