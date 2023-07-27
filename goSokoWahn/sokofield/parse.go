package sokofield

import (
	"bytes"
	"errors"
	"fmt"
	"strings"
)

func parseFilterLines(sokoMap string) (lines [][]byte, maxlen int) {
	chars := []byte(strings.ReplaceAll(sokoMap, "\t", "    "))

	// --- ungültige Zeichen am Ende des Spielfeldes entfernen ---
	for i, c := range chars {
		switch c {
		case '#', ' ', '.', '$', '*', '@', '+', '\r', '\n':
			continue
		}
		chars = chars[:i]
		break
	}

	// --- Leerzeichen am Ende der Zeilen entfernen ---
	lines = bytes.Split(chars, []byte{'\n'})
	for i := range lines {
		lines[i] = bytes.TrimRight(lines[i], " \r")
		if len(lines[i]) > maxlen {
			maxlen = len(lines[i])
		}
	}

	// --- komplett leere Zeilen entfernen ---
	for len(lines) > 0 && len(lines[0]) == 0 {
		lines = lines[1:]
	}
	for len(lines) > 0 && len(lines[len(lines)-1]) == 0 {
		lines = lines[:len(lines)-1]
	}

	// --- Leerzeichen am Anfang der Zeilen entfernen (nur identische Leerzeichen und Tabs) ---
	for {
		found := 0
		first := lines[0][0]
		if first != ' ' {
			break
		}
		for i := range lines {
			if lines[i][0] == first {
				found++
			}
		}
		if found < len(lines) {
			break
		}
		for i := range lines {
			lines[i] = lines[i][1:]
		}
		maxlen--
	}
	return
}

func Parse(sokoMap string) (f *Field, err error) {
	f = &Field{}

	lines, maxlen := parseFilterLines(sokoMap)

	// --- Größe des Spielfeldes setzen und prüfen ---
	f.width = maxlen
	f.height = len(lines)
	if f.width*f.height == 0 {
		return nil, errors.New("no sokoban field found")
	}

	// --- Felder in raw speichern ---
	raw := make([]byte, 0, f.width*f.height)
	for _, line := range lines {
		raw = append(raw, line...)
		for fill := len(line); fill < maxlen; fill++ {
			raw = append(raw, ' ')
		}
	}
	f.fieldData = raw

	// --- leere Version von raw erstellen ---
	rawEmpty := make([]byte, len(raw))
	foundPlayer := -1
	var foundBoxes []int
	var foundGoals []int
	for i, c := range raw {
		switch c {
		case '#', ' ':
			// ignore wall & free fields
		case '.':
			foundGoals = append(foundGoals, i)
			c = ' '
		case '$':
			foundBoxes = append(foundBoxes, i)
			c = ' '
		case '*':
			foundBoxes = append(foundBoxes, i)
			foundGoals = append(foundGoals, i)
			c = ' '
		case '@':
			if foundPlayer >= 0 {
				return nil, errors.New("duplicate player found")
			}
			foundPlayer = i
			c = ' '
		case '+':
			foundGoals = append(foundGoals, i)
			if foundPlayer >= 0 {
				return nil, errors.New("duplicate player found")
			}
			foundPlayer = i
			c = ' '
		default:
			c = ' '
		}
		rawEmpty[i] = c
	}

	// --- gefundene Inhalte prüfen ---
	if foundPlayer < 0 {
		return nil, errors.New("no player found")
	}
	if len(foundBoxes) == 0 {
		return nil, errors.New("no boxes found")
	}
	if len(foundGoals) == 0 {
		return nil, errors.New("no goals found")
	}
	if len(foundBoxes) > len(foundGoals) {
		return nil, fmt.Errorf("more boxes (%d) than goals (%d) found", len(foundBoxes), len(foundGoals))
	}
	if len(foundGoals) > len(foundBoxes) {
		return nil, fmt.Errorf("more goals (%d) than boxes (%d) found", len(foundGoals), len(foundBoxes))
	}
	f.boxCount = uint32(len(foundBoxes))

	// --- begehbare Bereiche scannen ---
	doCheck := []int{foundPlayer}
	walkable := make(map[int]bool)

	for len(doCheck) > 0 {
		nextPos := doCheck[len(doCheck)-1]
		doCheck = doCheck[:len(doCheck)-1]
		if walkable[nextPos] || rawEmpty[nextPos] != ' ' {
			continue
		}
		walkable[nextPos] = true

		nx, ny := nextPos%f.width, nextPos/f.width
		if np := nx - 1 + ny*f.width; nx > 0 {
			doCheck = append(doCheck, np)
		}
		if np := nx + 1 + ny*f.width; nx < f.width-1 {
			doCheck = append(doCheck, np)
		}
		if np := nx + (ny-1)*f.width; ny > 0 {
			doCheck = append(doCheck, np)
		}
		if np := nx + (ny+1)*f.width; ny < f.height-1 {
			doCheck = append(doCheck, np)
		}
	}
	f.walkSize = wpos(len(walkable))

	// --- hin und her Map erstellen ---
	posToW := make([]wpos, len(rawEmpty))
	wToPos := make([]int, 0, f.walkSize)
	for pos := range rawEmpty {
		if walkable[pos] {
			posToW[pos] = wpos(len(wToPos))
			wToPos = append(wToPos, pos)
		} else {
			posToW[pos] = f.walkSize
		}
	}
	f.fieldToWpos = posToW
	f.wposToField = wToPos

	// --- walkable-arrays befüllen ---
	f.walkLeft = make([]wpos, f.walkSize)
	f.walkRight = make([]wpos, f.walkSize)
	f.walkUp = make([]wpos, f.walkSize)
	f.walkDown = make([]wpos, f.walkSize)
	for i := 0; i < int(f.walkSize); i++ {
		f.walkLeft[i] = f.walkSize
		f.walkRight[i] = f.walkSize
		f.walkUp[i] = f.walkSize
		f.walkDown[i] = f.walkSize
	}

	for _, pos := range wToPos {
		px, py := pos%f.width, pos/f.width
		if px > 0 {
			f.walkLeft[posToW[pos]] = posToW[pos-1]
		}
		if px < f.width-1 {
			f.walkRight[posToW[pos]] = posToW[pos+1]
		}
		if py > 0 {
			f.walkUp[posToW[pos]] = posToW[pos-f.width]
		}
		if py < f.height-1 {
			f.walkDown[posToW[pos]] = posToW[pos+f.width]
		}
	}

	// --- restliche daten befüllen ---
	f.initPlayer = posToW[foundPlayer]
	f.player = f.initPlayer

	// todo: Kisten entfernen, welche unereichbar oder blockiert sind

	f.initBoxes = make([]wpos, 0, f.boxCount)
	for _, pos := range foundBoxes {
		if posToW[pos] == f.walkSize {
			return nil, errors.New("unreachable box found")
		}
		f.initBoxes = append(f.initBoxes, posToW[pos])
	}

	f.goals = make([]wpos, 0, f.boxCount)
	for _, pos := range foundGoals {
		if posToW[pos] == f.walkSize {
			return nil, errors.New("unreachable goal found")
		}
		f.goals = append(f.goals, posToW[pos])
	}

	f.wposToBoxes = make([]uint32, f.walkSize)
	for i := range f.wposToBoxes {
		f.wposToBoxes[i] = f.boxCount
	}
	f.boxes = make([]wpos, f.boxCount)
	for i := range f.boxes {
		f.boxes[i] = posToW[foundBoxes[i]]
		f.wposToBoxes[f.boxes[i]] = uint32(i)
	}

	f.tmpCheckPos = make([]wpos, f.walkSize)
	f.tmpCheckDepth = make([]uint32, f.walkSize)
	f.tmpCheckDone = make([]bool, f.walkSize+1)
	f.tmpCheckDone[len(f.tmpCheckDone)-1] = true // letztes Feld schon auf "Fertig" setzen

	return
}
