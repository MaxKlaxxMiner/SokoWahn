package soko

import "errors"

// ermittelt die Zugfolge in LURD-Notation zwischen zwei aufeinander folgenden
// Schub-Stellungen (b muss durch genau einen Kistenschub aus a hervorgehen);
// Kleinbuchstaben = Laufschritte, Großbuchstaben = Schiebeschritte
// Hinweis: der Feldzustand wird für die Berechnung verändert und nicht wiederhergestellt
func (f *Field) Steps(a, b *State) (string, error) {
	// die geschobene Kiste ermitteln und die Ein-Schub-Bedingung verifizieren:
	// b darf sich von a in genau einer Kistenposition unterscheiden, und die in
	// a verschwundene Kiste muss auf dem alten Kistenfeld (= b.Player) stehen.
	// Ohne diese Prüfung akzeptierte die Direkt-Kanten-Sonde der Push-Optimierung
	// (die Steps mit beliebigen Stellungspaaren aufruft) auch Stellungen mehrere
	// Schübe hinter dem Start als Schein-1-Push-Kante - die Lösung begann dann
	// mit einem unsinnigen Segment und falscher Schub-Zahl (Level 25523).
	from := b.Player
	vanished, to := f.walkEof, f.walkEof
	ia, ib := 0, 0
	for ia < len(a.Boxes) || ib < len(b.Boxes) {
		switch {
		case ib >= len(b.Boxes) || (ia < len(a.Boxes) && a.Boxes[ia] < b.Boxes[ib]):
			if vanished != f.walkEof {
				return "", errors.New("more than one box moved")
			}
			vanished = a.Boxes[ia]
			ia++
		case ia >= len(a.Boxes) || b.Boxes[ib] < a.Boxes[ia]:
			if to != f.walkEof {
				return "", errors.New("more than one box moved")
			}
			to = b.Boxes[ib]
			ib++
		default:
			ia++
			ib++
		}
	}
	if to == f.walkEof {
		return "", errors.New("no pushed box found")
	}
	if vanished != from {
		return "", errors.New("moved box did not start on the player position of b")
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
		// mit beliebigen Stellungs-Paaren aufruft (Hänger bei Level 25327, per
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
