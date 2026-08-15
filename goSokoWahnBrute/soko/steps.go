package soko

import "errors"

// ermittelt die Zugfolge in LURD-Notation zwischen zwei aufeinander folgenden
// Schub-Stellungen (b muss durch genau einen Kistenschub aus a hervorgehen);
// Kleinbuchstaben = Laufschritte, Großbuchstaben = Schiebeschritte
// Hinweis: der Feldzustand wird für die Berechnung verändert und nicht wiederhergestellt
func (f *Field) Steps(a, b *State) (string, error) {
	// die geschobene Kiste ermitteln: bei b steht der Spieler auf dem alten Kistenfeld
	from := b.Player
	to := f.walkEof
	for _, box := range b.Boxes {
		if !containsWpos(a.Boxes, box) {
			to = box
			break
		}
	}
	if to == f.walkEof {
		return "", errors.New("no pushed box found")
	}

	// Schub-Richtung und die Position bestimmen, von der aus geschoben wurde
	var pushStep byte
	var standPos Wpos
	switch to {
	case f.walkLeft[from]:
		pushStep, standPos = 'L', f.walkRight[from]
	case f.walkRight[from]:
		pushStep, standPos = 'R', f.walkLeft[from]
	case f.walkUp[from]:
		pushStep, standPos = 'U', f.walkDown[from]
	case f.walkDown[from]:
		pushStep, standPos = 'D', f.walkUp[from]
	default:
		return "", errors.New("pushed box is not adjacent to player")
	}
	if standPos >= f.walkEof {
		// die Schub-Position ist eine Wand: der Spieler kann dort nie gestanden haben.
		// Ohne diese Prüfung liefe die Rückverfolgung unten endlos, denn der
		// Wand-Sentinel markiert walkEof als besucht und parent[walkEof] zeigt ins
		// Leere. Trifft nur die Direkt-Kanten-Sonde der Push-Optimierung, die Steps
		// mit beliebigen Stellungs-Paaren aufruft (Haenger bei Level 25327, per
		// pprof-CPU-Profil auf die Rückverfolgungs-Schleife eingegrenzt).
		return "", errors.New("push stand position is a wall")
	}

	// Laufweg von a.Player zur Schub-Position per Breitensuche in der Kistenstellung von a
	f.SetState(a)

	parent := make([]Wpos, f.walkEof+1) // Vorgänger-Feld je erreichtem Feld
	moves := make([]byte, f.walkEof+1)  // Laufschritt, mit dem das Feld erreicht wurde
	visited := make([]bool, f.walkEof+1)
	visited[f.walkEof] = true // Sentinel: Wand gilt als besucht

	queue := make([]Wpos, 0, f.walkEof)
	queue = append(queue, a.Player)
	visited[a.Player] = true

	for qPos := 0; qPos < len(queue); qPos++ {
		pos := queue[qPos]
		if pos == standPos {
			break
		}
		for _, dir := range [4]struct {
			walk []Wpos
			step byte
		}{{f.walkLeft, 'l'}, {f.walkRight, 'r'}, {f.walkUp, 'u'}, {f.walkDown, 'd'}} {
			if p := dir.walk[pos]; !visited[p] && f.wposToBoxes[p] == f.boxCount {
				visited[p] = true
				parent[p] = pos
				moves[p] = dir.step
				queue = append(queue, p)
			}
		}
	}

	if !visited[standPos] {
		return "", errors.New("push position is not reachable")
	}

	// Laufweg rückwärts einsammeln und umdrehen
	steps := []byte{pushStep}
	for pos := standPos; pos != a.Player; pos = parent[pos] {
		steps = append(steps, moves[pos])
	}
	for i, j := 0, len(steps)-1; i < j; i, j = i+1, j-1 {
		steps[i], steps[j] = steps[j], steps[i]
	}

	return string(steps), nil
}

// prüft, ob die (aufsteigend sortierten) Kistenpositionen den Wert enthalten
func containsWpos(boxes []Wpos, value Wpos) bool {
	for _, box := range boxes {
		if box == value {
			return true
		}
		if box > value {
			return false
		}
	}
	return false
}
