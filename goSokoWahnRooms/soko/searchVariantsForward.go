package soko

import "goSokoWahnRooms/tools"

// Interface für den Deadlock-Filter (Blocker): erlaubt das Verwerfen von Stellungen
// direkt im Zuggenerator, bevor sie kopiert und gehasht werden.
// boxBits ist die Kisten-Belegung als Bitmaske über die begehbaren Felder.
type BlockerCheck interface {
	CheckAllowed(player Wpos, boxBits []uint64) bool
}

// setzt den optionalen Deadlock-Filter für die Vorwärtssuche (nil = kein Filter)
func (f *Field) SetBlocker(blocker BlockerCheck) {
	f.blocker = blocker
}

// sucht alle Stellungen, welche durch einen einzelnen Kistenschub erreichbar sind
// (Spieler flutet alle erreichbaren Felder, an jeder Kiste wird der Schub geprüft)
func (f *Field) SearchVariantsForward(result []State) []State {
	checkFrom := 0
	checkTo := 0

	tools.ClearBools(f.tmpCheckDone[:len(f.tmpCheckDone)-1])

	startPos := f.player
	startDepth := f.moveDepth

	// erste Spielerposition hinzufügen
	f.tmpCheckDone[startPos] = true
	f.tmpCheckPos[checkTo] = startPos
	f.tmpCheckDepth[checkTo] = startDepth
	checkTo++

	// alle erreichbaren Spielerpositionen abarbeiten
	for checkFrom < checkTo {
		pos := f.tmpCheckPos[checkFrom]
		pDepth := f.tmpCheckDepth[checkFrom] + 1

		// --- links ---
		if p := f.walkLeft[pos]; !f.tmpCheckDone[p] {
			if box := f.wposToBoxes[p]; box < f.boxCount {
				if p2 := f.walkLeft[p]; p2 < f.walkEof && f.wposToBoxes[p2] == f.boxCount {
					result = f.pushVariantHorizontal(result, p, p2, box, pDepth) // Kiste nach links schieben
				}
			} else {
				f.tmpCheckDone[p] = true
				f.tmpCheckPos[checkTo] = p
				f.tmpCheckDepth[checkTo] = pDepth
				checkTo++
			}
		}

		// --- rechts ---
		if p := f.walkRight[pos]; !f.tmpCheckDone[p] {
			if box := f.wposToBoxes[p]; box < f.boxCount {
				if p2 := f.walkRight[p]; p2 < f.walkEof && f.wposToBoxes[p2] == f.boxCount {
					result = f.pushVariantHorizontal(result, p, p2, box, pDepth) // Kiste nach rechts schieben
				}
			} else {
				f.tmpCheckDone[p] = true
				f.tmpCheckPos[checkTo] = p
				f.tmpCheckDepth[checkTo] = pDepth
				checkTo++
			}
		}

		// --- oben ---
		if p := f.walkUp[pos]; !f.tmpCheckDone[p] {
			if box := f.wposToBoxes[p]; box < f.boxCount {
				if p2 := f.walkUp[p]; p2 < f.walkEof && f.wposToBoxes[p2] == f.boxCount {
					result = f.pushVariantVertical(result, p, p2, box, pDepth, true) // Kiste nach oben schieben
				}
			} else {
				f.tmpCheckDone[p] = true
				f.tmpCheckPos[checkTo] = p
				f.tmpCheckDepth[checkTo] = pDepth
				checkTo++
			}
		}

		// --- unten ---
		if p := f.walkDown[pos]; !f.tmpCheckDone[p] {
			if box := f.wposToBoxes[p]; box < f.boxCount {
				if p2 := f.walkDown[p]; p2 < f.walkEof && f.wposToBoxes[p2] == f.boxCount {
					result = f.pushVariantVertical(result, p, p2, box, pDepth, false) // Kiste nach unten schieben
				}
			} else {
				f.tmpCheckDone[p] = true
				f.tmpCheckPos[checkTo] = p
				f.tmpCheckDepth[checkTo] = pDepth
				checkTo++
			}
		}

		checkFrom++
	}

	// Ursprungszustand wiederherstellen
	f.player = startPos
	f.moveDepth = startDepth

	return result
}

// führt einen horizontalen Kistenschub aus, sammelt die Stellung ein und macht den Schub rückgängig
// (bei links/rechts bleibt die Kisten-Sortierung erhalten, da sich der Index nur um 1 auf ein freies Feld ändert)
func (f *Field) pushVariantHorizontal(result []State, p, p2 Wpos, box uint32, pDepth int32) []State {
	f.player = p                                            // Spieler auf das alte Kistenfeld setzen
	f.moveDepth = pDepth                                    // Zugtiefe des Schubs
	f.wposToBoxes[p2], f.wposToBoxes[p] = box, f.boxCount   // Kiste auf das Zielfeld schieben
	f.boxes[box] = p2                                       // neue Kistenposition merken
	f.boxBitClear(p)
	f.boxBitSet(p2)
	if f.blocker == nil || f.blocker.CheckAllowed(f.player, f.boxBits) {
		result = f.AppendGetState(result)                   // Stellung einsammeln
	}
	f.wposToBoxes[p], f.wposToBoxes[p2] = box, f.boxCount   // Kiste wieder zurück schieben
	f.boxes[box] = p                                        // alte Kistenposition wiederherstellen
	f.boxBitClear(p2)
	f.boxBitSet(p)
	return result
}

// führt einen vertikalen Kistenschub aus, sammelt die Stellung ein und macht den Schub rückgängig
// (bei oben/unten muss die Kisten-Sortierung angepasst werden, da sich die Index-Reihenfolge ändern kann)
func (f *Field) pushVariantVertical(result []State, p, p2 Wpos, box uint32, pDepth int32, up bool) []State {
	f.player = p                                            // Spieler auf das alte Kistenfeld setzen
	f.moveDepth = pDepth                                    // Zugtiefe des Schubs
	f.wposToBoxes[p2], f.wposToBoxes[p] = box, f.boxCount   // Kiste auf das Zielfeld schieben
	f.boxes[box] = p2                                       // neue Kistenposition merken
	f.boxBitClear(p)
	f.boxBitSet(p2)
	if up {
		f.sortBoxesUp(box)                                  // Kisten sortieren (Index ist kleiner geworden)
	} else {
		f.sortBoxesDown(box)                                // Kisten sortieren (Index ist größer geworden)
	}
	if f.blocker == nil || f.blocker.CheckAllowed(f.player, f.boxBits) {
		result = f.AppendGetState(result)                   // Stellung einsammeln
	}
	box = f.wposToBoxes[p2]                                 // Kisten-Nummer erneut abfragen (kann sich durch Sortierung geändert haben)
	f.wposToBoxes[p], f.wposToBoxes[p2] = box, f.boxCount   // Kiste wieder zurück schieben
	f.boxes[box] = p                                        // alte Kistenposition wiederherstellen
	f.boxBitClear(p2)
	f.boxBitSet(p)
	if up {
		f.sortBoxesDown(box)                                // Sortierung rückgängig machen
	} else {
		f.sortBoxesUp(box)                                  // Sortierung rückgängig machen
	}
	return result
}
