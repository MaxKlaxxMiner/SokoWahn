package rooms

// ProgressFunc meldet Arbeitsschritte langer Rechnungen an die Oberfläche:
// text beschreibt den Schritt, rooms sind die gerade bearbeiteten Räume
// (die GUI markiert deren Felder gelb, wie der alte C#-FormDebugger).
// Rückgabe false bricht die Arbeit ab; was dabei mit bereits Berechnetem
// passiert, dokumentiert die jeweilige Funktion (Merge/Scan: Raum bleibt
// unverändert; Dominanzsuche: bewiesene Streichungen werden angewandt).
type ProgressFunc func(text string, rooms []*Room) bool
