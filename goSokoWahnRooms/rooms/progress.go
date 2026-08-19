package rooms

import "time"

// ProgressFunc meldet Arbeitsschritte langer Rechnungen an die Oberfläche:
// text beschreibt den Schritt, rooms sind die gerade bearbeiteten Räume
// (die GUI markiert deren Felder gelb, wie der alte C#-FormDebugger).
// Rückgabe false bricht die Arbeit ab; was dabei mit bereits Berechnetem
// passiert, dokumentiert die jeweilige Funktion (Merge/Scan: Raum bleibt
// unverändert; Dominanzsuche: bewiesene Streichungen werden angewandt).
type ProgressFunc func(text string, rooms []*Room) bool

// Meldungs-Intervall der heißen Schleifen: Fortschritt (und Stop-Check)
// zeitgesteuert statt in festen Blöcken - feste Block-Größen passen nie
// (kleine Räume melden zu oft, riesige Räume können zwischen zwei Blöcken
// Millionen Varianten erzeugen)
const progressInterval = 100 * time.Millisecond

// progressThrottle drosselt Meldungen auf das Intervall; die erste Meldung
// kommt sofort (Null-Wert), danach frühestens alle progressInterval.
// Zum Overhead: time.Now() kostet gemessen ~2 ns (Go liest die monotone Uhr
// ohne Syscall) und due() läuft pro Stellung/Gruppen-Test, deren Bearbeitung
// um Größenordnungen teurer ist - eine grobe Uhr per Ticker-Goroutine
// (atomarer Millis-Zähler, Max' Idee 2026-08-19) lohnt erst, falls due()
// je in eine echte Pro-Variante-Schleife wandert.
type progressThrottle struct {
	last time.Time
}

func (t *progressThrottle) due() bool {
	now := time.Now()
	if now.Sub(t.last) < progressInterval {
		return false
	}
	t.last = now
	return true
}
