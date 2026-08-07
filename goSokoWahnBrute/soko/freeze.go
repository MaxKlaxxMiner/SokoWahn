package soko

// Ersetzt eingefrorene Kisten auf Zielfeldern durch Wände (JSoko-Verhalten,
// dort "transform frozen boxes on goals to walls", per Default aktiv):
// eine Kiste, die nie mehr bewegt werden kann und ihr Ziel bereits bedient,
// ist von einer Wand physisch nicht zu unterscheiden - Kiste und Ziel entfallen
// ersatzlos. Läuft beim Parsen direkt auf dem Zeichen-Raster, bevor die
// Walk-Strukturen entstehen.
//
// Erkennung (klassische Freeze-Analyse): eine Kiste ist auf einer Achse
// blockiert, wenn auf einer Seite eine Wand steht oder eine Zielfeld-Kiste,
// die selbst eingefroren ist (rekursiv; im Prüfpfad besuchte Kisten zählen als
// Wand, damit gegenseitige Blockaden wie 2x2-Blöcke erkannt werden). Eine Kiste
// ist eingefroren, wenn beide Achsen blockiert sind. Kisten abseits der Ziele
// ('$') zählen bewusst nicht als Blockade (konservativ - eine eingefrorene
// $-Kiste wäre ohnehin ein unlösbares Level). Felder außerhalb des Rasters
// gelten nicht als Wand (Void bei halb offenen Web-Levels ist auf Zeichenebene
// nicht von Innenraum unterscheidbar - verpasste Ersetzung ist unschädlich).
//
// Rückgabe: Anzahl der ersetzten Kisten.
func freezeGoalBoxesToWalls(raw []byte, width, height int) int {
	replaced := 0
	for { // Fixpunkt: neue Wände können weitere Kisten einfrieren
		changed := false
		for pos, c := range raw {
			if c != '*' {
				continue
			}
			if frozenBox(raw, width, height, pos%width, pos/width, map[int]bool{}) {
				raw[pos] = '#'
				replaced++
				changed = true
			}
		}
		if !changed {
			return replaced
		}
	}
}

// prüft, ob die Zielfeld-Kiste auf (x,y) eingefroren ist; treatAsWall enthält
// die im aktuellen Prüfpfad besuchten Kisten (Positionen als x + y*width)
func frozenBox(raw []byte, width, height, x, y int, treatAsWall map[int]bool) bool {
	treatAsWall[x+y*width] = true
	return frozenAxis(raw, width, height, x, y, 1, 0, treatAsWall) &&
		frozenAxis(raw, width, height, x, y, 0, 1, treatAsWall)
}

// prüft, ob die Kiste auf (x,y) entlang einer Achse blockiert ist
func frozenAxis(raw []byte, width, height, x, y, dx, dy int, treatAsWall map[int]bool) bool {
	wallLike := func(nx, ny int) bool {
		if nx < 0 || nx >= width || ny < 0 || ny >= height {
			return false // außerhalb: konservativ nicht als Wand werten
		}
		return raw[nx+ny*width] == '#' || treatAsWall[nx+ny*width]
	}
	if wallLike(x-dx, y-dy) || wallLike(x+dx, y+dy) {
		return true
	}

	// Nachbar-Kiste auf Ziel, die ihrerseits eingefroren ist?
	// (Set pro Zweig kopieren, damit gescheiterte Prüfpfade nicht als Wand nachwirken)
	for _, side := range [2][2]int{{x - dx, y - dy}, {x + dx, y + dy}} {
		nx, ny := side[0], side[1]
		if nx < 0 || nx >= width || ny < 0 || ny >= height || raw[nx+ny*width] != '*' {
			continue
		}
		branch := make(map[int]bool, len(treatAsWall)+1)
		for k := range treatAsWall {
			branch[k] = true
		}
		if frozenBox(raw, width, height, nx, ny, branch) {
			return true
		}
	}
	return false
}
