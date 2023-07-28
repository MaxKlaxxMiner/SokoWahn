package soko

func (f *Field) SearchVariantsBackward(result []State) []State {
	posStart := f.player
	posLeft := f.walkLeft[posStart]
	posRight := f.walkRight[posStart]
	posUp := f.walkUp[posStart]
	posDown := f.walkDown[posStart]

	// --- Links-Vermutung: Kiste wurde das letztes mal nach links geschoben ---
	if f.wposToBoxes[posLeft] < f.boxCount && posRight < f.walkEof && f.wposToBoxes[posRight] == f.boxCount {
		f.player = posRight                                               // Spieler rückwärts nach rechts bewegen
		box := f.wposToBoxes[posLeft]                                     // Kisten-Nummer abfragen
		f.wposToBoxes[posStart], f.wposToBoxes[posLeft] = box, f.boxCount // Kiste auf den Platz schieben, wo vorher der Spieler stand
		f.boxes[box] = posStart                                           // neue Kistenposition merken

		//  foreach (var variante in GetVariantenRückwärtsTeil()) yield return variante;

		f.wposToBoxes[posLeft], f.wposToBoxes[posStart] = box, f.boxCount // Kiste wieder auf das alte Feld schieben
		f.boxes[box] = posLeft                                            // neue Kistenposition merken
		f.player = posStart                                               // Spieler zurück setzen
	}

	// --- Rechts-Vermutung: Kiste wurde das letztes mal nach rechts geschoben ---
	if f.wposToBoxes[posRight] < f.boxCount && posLeft < f.walkEof && f.wposToBoxes[posLeft] == f.boxCount {
		f.player = posRight                                                // Spieler rückwärts nach links bewegen
		box := f.wposToBoxes[posRight]                                     // Kisten-Nummer abfragen
		f.wposToBoxes[posStart], f.wposToBoxes[posRight] = box, f.boxCount // Kiste auf den Platz schieben, wo vorher der Spieler stand
		f.boxes[box] = posStart                                            // neue Kistenposition merken

		//  foreach (var variante in GetVariantenRückwärtsTeil()) yield return variante;

		f.wposToBoxes[posRight], f.wposToBoxes[posStart] = box, f.boxCount // Kiste wieder auf das alte Feld schieben
		f.boxes[box] = posRight                                            // neue Kistenposition merken
		f.player = posStart                                                // Spieler zurück setzen
	}

	// --- Oben-Vermutung: Kiste wurde das letztes mal nach oben geschoben ---
	if f.wposToBoxes[posUp] < f.boxCount && posDown < f.walkEof && f.wposToBoxes[posDown] == f.boxCount {
		f.player = posDown                                              // Spieler rückwärts nach unten bewegen
		box := f.wposToBoxes[posUp]                                     // Kisten-Nummer abfragen
		f.wposToBoxes[posStart], f.wposToBoxes[posUp] = box, f.boxCount // Kiste auf den Platz schieben, wo vorher der Spieler stand
		f.boxes[box] = posStart                                         // neue Kistenposition merken
		f.sortBoxesUp(box)                                              // Kisten sortieren, da sich die Index-Reihenfolge kann

		//  foreach (var variante in GetVariantenRückwärtsTeil()) yield return variante;

		box = f.wposToBoxes[posStart]                                   // Kisten-Nummer erneut abfragen
		f.wposToBoxes[posUp], f.wposToBoxes[posStart] = box, f.boxCount // Kiste wieder auf das alte Feld schieben
		f.boxes[box] = posUp                                            // neue Kistenposition merken
		f.sortBoxesDown(box)                                            // Kisten sortieren, da sich die Index-Reihenfolge kann
		f.player = posStart                                             // Spieler zurück setzen
	}

	// --- Unten-Vermutung: Kiste wurde das letztes mal nach unten geschoben ---
	if f.wposToBoxes[posDown] < f.boxCount && posUp < f.walkEof && f.wposToBoxes[posUp] == f.boxCount {
		f.player = posUp                                                  // Spieler rückwärts nach oben bewegen
		box := f.wposToBoxes[posDown]                                     // Kisten-Nummer abfragen
		f.wposToBoxes[posStart], f.wposToBoxes[posDown] = box, f.boxCount // Kiste auf den Platz schieben, wo vorher der Spieler stand
		f.boxes[box] = posStart                                           // neue Kistenposition merken
		f.sortBoxesDown(box)                                              // Kisten sortieren, da sich die Index-Reihenfolge kann

		//  foreach (var variante in GetVariantenRückwärtsTeil()) yield return variante;

		box = f.wposToBoxes[posStart]                                     // Kisten-Nummer erneut abfragen
		f.wposToBoxes[posDown], f.wposToBoxes[posStart] = box, f.boxCount // Kiste wieder auf das alte Feld schieben
		f.boxes[box] = posDown                                            // neue Kistenposition merken
		f.sortBoxesUp(box)                                                // Kisten sortieren, da sich die Index-Reihenfolge kann
		f.player = posStart                                               // Spieler zurück setzen
	}

	return result
}
