package soko

// Closed-Diagonal-Deadlock-Erkennung, portiert aus JSoko 2.28
// (deadlockdetection/ClosedDiagonalDeadlock.java, GPL v2, (c) Matthias Meger).
// Erkennt diagonale Ketten aus Kisten/Wänden, die sich gegenseitig einschließen:
//
//	 **               #    *$
//	 * $       *$    $ #   *.#
//	  #.$     * #   $.*     # *
//	   # *   * *    $$       * *
//	    **   **               $$
//
// Ist die Diagonale an beiden Enden geschlossen und gibt es keine rettende
// "Ziele-und-Wände-Sequenz" (über die Kisten nach innen auf Ziele geschoben
// werden könnten), ist die Stellung bewiesen unlösbar.
//
// Der Scan läuft in absoluten Feldkoordinaten (x/y), weil er auch Wandfelder
// und Felder außerhalb der begehbaren Hülle betrachten muss. Konservativ:
// außerhalb des Feldes bzw. unbegehbar-ohne-Wand beendet den Scan ohne
// Deadlock-Anspruch (JSokos "outer square"-Behandlung).

// prüft, ob die geschobene Kiste Teil eines geschlossenen Diagonal-Deadlocks ist
func (r *Rules) isDiagonalDeadlock(player, box Wpos, boxBits []uint64) bool {
	sh := r.shared
	abs := sh.wposToField[box]
	boxX, boxY := abs%sh.width, abs/sh.width

	// Die Spielerposition wird nur berücksichtigt, wenn der Spieler komplett
	// eingeschlossen ist (JSoko-Verhalten): nur in der Startstellung kann der
	// Spieler selbst in einer geschlossenen Diagonale stehen - nach einem Schub
	// nie. Steht er auf einem Diagonalfeld, ist die Diagonale kein Deadlock.
	playerX, playerY := -1, -1
	pAbs := sh.wposToField[player]
	px, py := pAbs%sh.width, pAbs/sh.width
	if sh.isBoxOrWall(px-1, py, boxBits) && sh.isBoxOrWall(px+1, py, boxBits) &&
		sh.isBoxOrWall(px, py-1, boxBits) && sh.isBoxOrWall(px, py+1, boxBits) {
		playerX, playerY = px, py
	}

	// Kistenfeld mit linkem und rechtem Nachbarn als Diagonal-Start prüfen
	return r.diagonalBlocked(boxX-1, boxY, boxX, boxY, playerX, playerY, boxBits) ||
		r.diagonalBlocked(boxX+1, boxY, boxX, boxY, playerX, playerY, boxBits)
}

// untersucht alle von (startX,startY)/(boxX,boxY) ausgehenden Diagonalen.
// true = geschlossener Diagonal-Deadlock bewiesen.
//
// Zwei Situationen (siehe JSoko-Doku):
//   - Start-Nachbar ist frei -> die Kiste liegt in der Mitte der Diagonale:
//     zwei Diagonalen (links-oben, rechts-oben) werden jeweils erst aufwärts und
//     nach Richtungswechsel abwärts verfolgt; beide Enden müssen geschlossen sein.
//   - Start-Nachbar ist Kiste/Wand -> die Startreihe ist bereits ein Ende:
//     vier Diagonalen (links/rechts x oben/unten) werden einzeln verfolgt.
//
// Eine "Ziele-und-Wände-Sequenz" (zusammenhängender Block aus Zielen und Wänden
// entlang der Diagonale) beweist, dass Kisten nach innen auf Ziele geschoben
// werden können und die Diagonale zu öffnen ist -> kein Deadlock.
func (r *Rules) diagonalBlocked(startX, startY, boxX, boxY, playerX, playerY int, mask []uint64) bool {
	sh := r.shared

	// Startfeld selbst Kiste/Wand? Dann ist die Startreihe ein Diagonal-Ende ("Situation b")
	startBlocked := sh.isBoxOrWall(startX, startY, mask)

	dirX, dirY := -1, -1 // Diagonalen-Reihenfolge: links-oben, rechts-oben, links-unten, rechts-unten

	for {
		sx, sy := dirX, dirY

		// Merker für ein Ziel-/Wandfeld AUF der Diagonale: die Suche in Gegenrichtung
		// muss dort starten, damit Ziele-und-Wände-Sequenzen korrekt bewertet werden
		goalFound := false
		var goalX, goalY int

		var cx, cy int          // aktuelles Diagonalfeld
		allGoalsWalls := false  // läuft gerade eine Ziele-und-Wände-Sequenz?

	currentDiagonal:
		for {
			if startBlocked {
				// Startreihe ist ein Ende: bei rechts-läufiger Diagonale am linkesten
				// Feld beginnen, bei links-läufiger am rechtesten
				if (sx > 0) == (startX < boxX) {
					cx, cy = startX, startY
				} else {
					cx, cy = boxX, boxY
				}
				allGoalsWalls = sh.isGoalOrWall(cx, cy) && sh.isGoalOrWall(cx+sx, cy)
				cx += sx
				cy += sy
			} else if !goalFound {
				cx, cy = startX, startY
				allGoalsWalls = false
			} else {
				// Gegenrichtungs-Suche: am gemerkten Ziel-/Wandfeld neu aufsetzen
				// (dort beginnt eine neue Ziele-und-Wände-Sequenz)
				cx, cy = goalX, goalY
				allGoalsWalls = sh.isGoalOrWall(cx+sx, cy)
				cx += sx
				cy += sy
			}

			// außerhalb der erreichbaren Fläche und keine Wand -> Scan konservativ beenden
			if sh.isOuterNotWall(cx, cy) {
				break currentDiagonal
			}

			for {
				// Spieler auf dem Diagonalfeld (nur Startstellung möglich) -> kein Deadlock
				if cx == playerX && cy == playerY {
					break currentDiagonal
				}

				// Nachbarn in der Reihe: n1 "hinter", n2 "vor" der Laufrichtung
				n1x := cx - sx
				n2x := cx + sx

				// ohne Kiste/Wand hinter dem Diagonalfeld ist die Diagonale offen
				if !sh.isBoxOrWall(n1x, cy, mask) {
					break currentDiagonal
				}

				if allGoalsWalls {
					// läuft die Sequenz über den hinteren Nachbarn weiter?
					allGoalsWalls = sh.isGoalOrWall(n1x, cy)
					// Sequenz intakt und auch das Diagonalfeld ist Ziel/Wand ->
					// die Diagonale lässt sich auf Ziele öffnen, kein Deadlock
					if allGoalsWalls && sh.isGoalOrWall(cx, cy) {
						break currentDiagonal
					}
				}

				if !sh.isBoxOrWall(cx, cy, mask) {
					// leeres Diagonalfeld: nur mit Kiste/Wand auf BEIDEN Reihen-Nachbarn
					// bleibt die Diagonale geschlossen
					if !sh.isBoxOrWall(n2x, cy, mask) {
						break currentDiagonal
					}

					if sh.isGoal(cx, cy) {
						allGoalsWalls = true // Ziel auf der Diagonale startet/verlängert eine Sequenz
						if !goalFound {
							goalFound, goalX, goalY = true, cx, cy
						}
					}
					if allGoalsWalls {
						allGoalsWalls = sh.isGoalOrWall(n2x, cy)
					}

					// weiter, sofern die Diagonale nicht durch eine Doppelwand endet:
					//   #      <- (cx, cy+sy)
					//  *d#     <- d = Diagonalfeld, '#' = n2
					if !sh.isWall(n2x, cy) || !sh.isWall(cx, cy+sy) {
						cx += sx
						cy += sy
						continue
					}

					// Doppelwand-Ende: mit intakter Sequenz kein Deadlock
					if allGoalsWalls {
						break currentDiagonal
					}

					// Startpunkt für die Gegenrichtungs-Suche merken (das Feld hinter der
					// Doppelwand zählt als Sequenz-Start, JSoko Z. 438: bewusst ohne
					// goalFound-Guard - ein späterer Fund übersteuert einen früheren)
					goalFound, goalX, goalY = true, cx+sx, cy+sy
				}

				// Diagonale ist an diesem Ende geschlossen (Kiste, Wand oder Doppelwand)
				// und keine Sequenz rettet sie
				if !startBlocked && sy < 0 {
					// Mitten-Start: erst die Gegenrichtung (abwärts) prüfen; endet die
					// Diagonale mit Wand/Ziel und wurde noch kein Ziel gefunden, startet
					// die Gegenrichtung an diesem Feld (Sequenz-Behandlung)
					if (sh.isWall(cx, cy) || sh.isGoal(cx, cy)) && !goalFound {
						goalFound, goalX, goalY = true, cx, cy
					}
					sx = -sx
					sy = -sy
					continue currentDiagonal
				}
				return true // beide Enden geschlossen -> Deadlock
			}
		}

		// nächste Diagonale: links-oben -> rechts-oben -> (nur Situation b) links-unten -> rechts-unten
		if dirY < 0 {
			if dirX < 0 {
				dirX = 1
			} else {
				if !startBlocked {
					return false // Mitten-Start: beide Diagonalen geprüft, kein Deadlock
				}
				dirX, dirY = -1, 1
			}
		} else {
			if dirX < 0 {
				dirX = 1
			} else {
				return false // alle vier Diagonalen geprüft, kein Deadlock
			}
		}
	}
}

// --- Geometrie-Helfer auf absoluten Koordinaten ---

func (sh *rulesShared) inBounds(x, y int) bool {
	return x >= 0 && x < sh.width && y >= 0 && y < sh.height
}

// echte Wand ('#')
func (sh *rulesShared) isWall(x, y int) bool {
	return sh.inBounds(x, y) && sh.fieldData[y*sh.width+x] == '#'
}

// Kiste auf dem Feld (nur begehbare Felder können Kisten tragen)
func (sh *rulesShared) isBox(x, y int, mask []uint64) bool {
	if !sh.inBounds(x, y) {
		return false
	}
	p := sh.fieldToWpos[y*sh.width+x]
	return p < sh.walkEof && mask[p>>6]&(1<<(p&63)) != 0
}

func (sh *rulesShared) isBoxOrWall(x, y int, mask []uint64) bool {
	return sh.isWall(x, y) || sh.isBox(x, y, mask)
}

func (sh *rulesShared) isGoal(x, y int) bool {
	if !sh.inBounds(x, y) {
		return false
	}
	p := sh.fieldToWpos[y*sh.width+x]
	return p < sh.walkEof && sh.goalBits[p>>6]&(1<<(p&63)) != 0
}

func (sh *rulesShared) isGoalOrWall(x, y int) bool {
	return sh.isWall(x, y) || sh.isGoal(x, y)
}

// außerhalb des Feldes bzw. unbegehbar ohne Wand (JSokos "outer square":
// dort darf kein Deadlock-Anspruch entstehen)
func (sh *rulesShared) isOuterNotWall(x, y int) bool {
	if !sh.inBounds(x, y) {
		return true
	}
	pos := y*sh.width + x
	return sh.fieldToWpos[pos] == sh.walkEof && sh.fieldData[pos] != '#'
}
