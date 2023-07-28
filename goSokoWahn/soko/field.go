package soko

type Field struct {
	// --- statische Daten (bleiben dauerhaft erhalten) ---
	fieldData   []byte // Grunddaten des gesamten Spielfeldes als Standard ASCII-Zeichen, Größe: width * height
	width       int    // Breite des gesamten Spielfeldes
	height      int    // Höhe des gesamten Spielfeldes
	walkEof     Wpos   // Anzahl der (theoretisch) begehbaren Felder
	walkLeft    []Wpos // Index zum linken begehbaren Feld, walkEof wenn nicht begehbar
	walkRight   []Wpos // Index zum rechten begehbaren Feld, walkEof wenn nicht begehbar
	walkUp      []Wpos // Index zum oberen begehbaren Feld, walkEof wenn nicht begehbar
	walkDown    []Wpos // Index zum unteren begehbaren Feld, walkEof wenn nicht begehbar
	fieldToWpos []Wpos // Map zu den Index-Werten, walkEof wenn nicht begehbar
	wposToField []int  // Map vom Index wieder zu den absoluten Positionen
	boxCount    uint32 // Anzahl der Kisten auf dem Spielfeld
	initPlayer  Wpos   // Startposition des Spielers, Index zu begehbaren Feldern
	initBoxes   []Wpos // Startpositionen aller Kisten, Index zu begehbaren Feldern
	goals       []Wpos // Endpositionen aller Kisten = die Zielfelder, Index zu begehbaren Feldern

	// --- dynamische Daten (werden durch Spielzüge geändert) ---
	player      Wpos     // Aktuelle Spielerposition, Index zu begehbaren Feldern
	wposToBoxes []uint32 // Index von begehbaren Feldern zu Kisten, boxCount wenn keine Kiste auf dem Feld
	boxes       []Wpos   // Positionen der Kisten, jeweils Index zu begehbaren Feldern oder walkEof, wenn Kiste nicht gesetzt
	moveDepth   int32    // Aktuelle Zugtiefe

	// --- temporäre Buffer für die Suchfunktion ---
	tmpCheckPos   []Wpos   // Temporäre Positionen zur Prüfung
	tmpCheckDepth []uint32 // Temporäre Zugtiefen zur Prüfung
	tmpCheckDone  []bool   // Felder, die bereits geprüft wurden
}
