package solver

import (
	"math"

	"goSokoWahnBrute/tools"
)

// Momentaufnahme des Suchfortschritts (Datenbasis für Anzeige und Vergleiche)
type Stats struct {
	ForwardDepth  int   // aktuell bearbeitete Vorwärtstiefe
	BackwardDepth int   // aktuell bearbeitete Rückwärtstiefe
	ForwardNodes  int64 // Anzahl bekannter Stellungen der Vorwärtssuche
	BackwardNodes int64 // Anzahl bekannter Stellungen der Rückwärtssuche
	ForwardOpen   []int // noch zu prüfende Sätze je Vorwärtstiefe
	BackwardOpen  []int // noch zu prüfende Sätze je Rückwärtstiefe

	ForwardSpilled  []bool // je Vorwärtstiefe: Liste hat auf die Festplatte ausgelagert
	BackwardSpilled []bool // je Rückwärtstiefe: Liste hat auf die Festplatte ausgelagert
	FoundMoves    int   // beste gefundene Lösungslänge in Zügen, -1 = noch keine
	FoundForward  int   // Vorwärtstiefe der Verbindungs-Stellung (nur gültig wenn FoundMoves >= 0)
	Done          bool  // gibt an, ob die Suche abgeschlossen ist

	CollisionRejects int64 // verworfene Schein-Verbindungen (64-Bit-Hash-Kollisionen)
}

func (s *Solver) GetStats() Stats {
	stats := Stats{
		ForwardDepth:  s.forwardDepth,
		BackwardDepth: s.backwardDepth,
		ForwardNodes:  s.forwardKnown.Len(),
		BackwardNodes: s.backwardKnown.Len(),
		ForwardOpen:   make([]int, len(s.forwardLists)),
		BackwardOpen:  make([]int, len(s.backwardLists)),

		ForwardSpilled:  make([]bool, len(s.forwardLists)),
		BackwardSpilled: make([]bool, len(s.backwardLists)),
		FoundMoves:    s.foundTotal,
		FoundForward:  s.foundForwardDepth,
		Done:          s.done,

		CollisionRejects: s.collisionRejects,
	}
	for i, list := range s.forwardLists {
		stats.ForwardOpen[i] = list.Count()
		stats.ForwardSpilled[i] = list.SpillBytes() > 0
	}
	for i, list := range s.backwardLists {
		stats.BackwardOpen[i] = list.Count()
		stats.BackwardSpilled[i] = list.SpillBytes() > 0
	}
	return stats
}

// Gesamtzahl der noch zu prüfenden Stellungen (Pendant zu KnotenRest im Original)
func (s *Solver) OpenCount() int64 {
	var sum int64
	for _, list := range s.forwardLists {
		sum += int64(list.Count())
	}
	for _, list := range s.backwardLists {
		sum += int64(list.Count())
	}
	return sum
}

// auf die Festplatte ausgelagerte Bytes aller Suchlisten (0 = alles im RAM)
func (s *Solver) SpillBytes() int64 {
	var sum int64
	for _, list := range s.forwardLists {
		sum += list.SpillBytes()
	}
	for _, list := range s.backwardLists {
		sum += list.SpillBytes()
	}
	return sum
}

// im RAM reservierte Bytes der Suche: die beiden Hashtabellen plus die Puffer
// aller Suchlisten (Gegenstück zu SpillBytes für die Anzeige)
func (s *Solver) RamBytes() int64 {
	sum := s.forwardKnown.Bytes() + s.backwardKnown.Bytes()
	for _, list := range s.forwardLists {
		sum += list.RamBytes()
	}
	for _, list := range s.backwardLists {
		sum += list.RamBytes()
	}
	return sum
}

// Speicher-Kennzahlen einer Stellungs-Tabelle (Datenbasis der Hash-Statuszeile;
// wird beim Ausbau der Tabellen-Varianten um deren Spezial-Infos erweitert)
type TableInfo struct {
	Bytes   int64   // reservierter Speicher in Bytes
	Fill    float64 // Füllstand relativ zur Resize-/Merge-Schwelle (1.0 = steht an), -1 = unbekannt
	Archive bool    // true = Tabelle läuft im Archiv-Format (Fill bezieht sich auf das Delta)
}

func tableInfo(t PosTable) TableInfo {
	info := TableInfo{Bytes: t.Bytes(), Fill: -1}
	if f, ok := t.(FillTable); ok {
		info.Fill = f.Fill()
	}
	_, info.Archive = t.(*ArchiveTable)
	return info
}

// Kennzahlen der beiden Stellungs-Tabellen (vorwärts/rückwärts) für die Anzeige
func (s *Solver) TableInfos() (forward, backward TableInfo) {
	return tableInfo(s.forwardKnown), tableInfo(s.backwardKnown)
}

// verdichtet per Taste h die Stellungs-Tabelle, deren schneller CompactTable-Teil
// aktuell die meisten Einträge hält: eine reine CompactTable wird komplett ins
// Archiv-Format konvertiert (7 statt 13,3 Byte je Eintrag), bei einer bereits
// konvertierten Tabelle wird der Delta-Merge vorgezogen. Der nächste Tastendruck
// trifft damit automatisch die jeweils andere Richtung. Liefert die Beschreibung
// für die Statuszeile. Nur zwischen zwei Steps aufrufen (das TUI garantiert das:
// Tasten und Ticks laufen seriell im selben Event-Loop).
func (s *Solver) ArchiveLargerTable() string {
	table, name := &s.forwardKnown, "Vorwärts-Hash"
	if fastPartLen(s.backwardKnown) > fastPartLen(s.forwardKnown) {
		table, name = &s.backwardKnown, "Rückwärts-Hash"
	}

	switch t := (*table).(type) {
	case *CompactTable:
		before := t.Bytes()
		converted := NewArchiveTableFrom(t)
		*table = converted
		return name + ": ins Archiv-Format konvertiert (" + formatMBStatus(before) +
			" -> " + formatMBStatus(converted.Bytes()) + ")"
	case *ArchiveTable:
		if t.delta.count == 0 {
			return name + ": Delta ist leer, nichts zu verdichten"
		}
		before := t.Bytes()
		t.merge()
		return name + ": Delta-Merge vorgezogen (" + formatMBStatus(before) +
			" -> " + formatMBStatus(t.Bytes()) + ")"
	}
	return name + ": Tabellen-Typ unterstützt keine Archiv-Konvertierung"
}

// prüft vor einem Arbeitsschritt, ob bei einer Stellungs-Tabelle eine Verdopplung
// ansteht, deren Umkopier-Spitze (alte + neue Arrays gleichzeitig, also
// ram + 2*Tabellengröße) den berechneten Verbrauch über die RAM-Notbremse
// (RamLimitBytes) heben würde - statt zu verdoppeln wandert die Tabelle dann ins
// Archiv-Format, bei einer bereits konvertierten Tabelle wird der Delta-Merge
// vorgezogen. So bleiben die großen Verdopplungs-Spitzen aus und die Suche läuft
// dicht an der Grenze weiter: erst weichen nacheinander beide Tabellen ins Archiv
// aus, parallel lagern die Suchlisten auf die Platte aus (SpillRamThresholdBytes
// liegt darunter), und erst wenn der berechnete Verbrauch das Limit wirklich
// überschreitet, greift der RAM-Stop im TUI.
// ram = frisch berechneter Verbrauch (RamBytes); höchstens eine Konvertierung je
// Aufruf, denn danach stimmt ram nicht mehr - der nächste Step prüft erneut.
// Wie ArchiveLargerTable nur zwischen zwei Arbeitsphasen aufrufen (Step-Anfang).
func (s *Solver) autoArchive(ram int64) {
	if RamLimitBytes <= 0 {
		return
	}
	if s.autoArchiveTable(&s.forwardKnown, "Vorwärts-Hash", ram) {
		return
	}
	s.autoArchiveTable(&s.backwardKnown, "Rückwärts-Hash", ram)
}

// prüft eine einzelne Tabelle (true = konvertiert bzw. gemerged, Meldung liegt in
// archiveNote). "Verdopplung in Reichweite" heißt ab 90% der Wachstums-Schwelle:
// früh genug, dass ein Bulk-Step die Schwelle nicht überspringt, und spät genug,
// dass die Tabelle bis dahin mit vollem CompactTable-Tempo läuft
func (s *Solver) autoArchiveTable(table *PosTable, name string, ram int64) bool {
	switch t := (*table).(type) {
	case *CompactTable:
		// Kriterium ist die Umkopier-Spitze der Verdopplung: während des Grows leben
		// alte und neue Arrays gleichzeitig (ram + 2*Bytes). Der frühere Vergleich
		// mit dem Dauerzustand danach (ram + Bytes) löste zu spät aus: auf dem
		// 640-GB-Server riss die 80-GB-Vorwärts-Tabelle mit ihrer 160-GB-Spitze
		// das physische RAM (OOM-Kill bei 427 GB berechnetem Verbrauch), obwohl
		// der Wert nach der Verdopplung unter der Notbremse gelegen hätte.
		// Die Konvertierungs-Spitze selbst ist deutlich kleiner (~0,6x Bytes:
		// Archiv-Records + Index entstehen neben der alten Tabelle) und hat am
		// Auslösepunkt per Konstruktion noch mindestens 2*Bytes Luft bis zur Grenze.
		if t.count < t.growLimit()/10*9 || ram+2*t.Bytes() <= RamLimitBytes {
			return false
		}
		before := t.Bytes()
		converted := NewArchiveTableFrom(t)
		*table = converted
		s.archiveNote = name + ": statt Verdopplung ins Archiv-Format konvertiert (" +
			formatMBStatus(before) + " -> " + formatMBStatus(converted.Bytes()) + ")"
		return true
	case *ArchiveTable:
		// dasselbe für das Delta einer bereits konvertierten Tabelle: der vorgezogene
		// Merge ersetzt die Delta-Verdopplung (Kriterium wie oben die Umkopier-Spitze
		// ram + 2*Bytes). Mini-Deltas (unter ArchiveDeltaMin) wachsen weiter normal -
		// ihre Verdopplung ist billig, ständiges Mergen würde dagegen nur den
		// Bucket-Index immer wieder neu bauen
		if t.delta.count < ArchiveDeltaMin || t.delta.count < t.delta.growLimit()/10*9 ||
			ram+2*t.delta.Bytes() <= RamLimitBytes {
			return false
		}
		before := t.Bytes()
		t.merge()
		s.archiveNote = name + ": Delta-Merge statt Verdopplung vorgezogen (" +
			formatMBStatus(before) + " -> " + formatMBStatus(t.Bytes()) + ")"
		return true
	}
	return false
}

// holt die Meldung der letzten automatischen Archiv-Konvertierung ab (einmalig,
// "" = nichts passiert; das TUI zeigt sie nach dem Arbeitsschritt in der Statuszeile)
func (s *Solver) TakeArchiveNote() string {
	note := s.archiveNote
	s.archiveNote = ""
	return note
}

// Einträge im schnellen CompactTable-Teil einer Tabelle (Auswahl-Kriterium der Taste h)
func fastPartLen(t PosTable) int64 {
	switch table := t.(type) {
	case *CompactTable:
		return table.count
	case *ArchiveTable:
		return table.delta.count
	}
	return 0
}

// formatiert Bytes als ganze Megabytes für Status-Meldungen
func formatMBStatus(bytes int64) string {
	return tools.FormatInt(bytes>>20) + " MB"
}

// Gesamtzahl der bisher verarbeiteten Sätze aus den Suchlisten (läuft anders als
// NodeCount auch in der Beweis-Endphase weiter, wenn kaum noch Neues dazukommt)
func (s *Solver) ProcessedCount() int64 {
	return s.processed
}

// Gesamtzahl der bekannten Stellungen (Pendant zu KnotenAnzahl im Original)
func (s *Solver) NodeCount() int64 {
	return s.forwardKnown.Len() + s.backwardKnown.Len()
}

// aktuelle Suchtiefe (Summe beider Richtungen, Pendant zu SuchTiefe im Original)
func (s *Solver) SearchDepth() int {
	return s.forwardDepth + s.backwardDepth
}

// schätzt anhand des Hash-Wachstums der letzten 20 Suchtiefen, bis zu welcher Suchtiefe
// die Hashtabellen mit 100 Mio, 1 Mrd bzw. 3 Mrd Einträgen reichen (wie das List2-Original:
// mittlerer Anstieg der letzten 10 Stufen, Wachstumsfaktor aus dem Vergleich zu den 10 davor);
// ok = false, wenn noch zu wenige Messpunkte vorliegen oder die Nutzung außerhalb 1M..3G liegt
func (s *Solver) EstimateMaxDepths() (depth100M, depth1G, depth3G int, ok bool) {
	n := len(s.hashUsage)
	if n <= 20 {
		return 0, 0, 0, false
	}
	last := s.hashUsage[n-1]
	if last <= 1000000 || last >= 3000000000 {
		return 0, 0, 0, false
	}

	riseLast := float64(s.hashUsage[n-1] - s.hashUsage[n-11])
	riseBefore := float64(s.hashUsage[n-11] - s.hashUsage[n-21])
	mulPerDepth := 1.0
	if riseBefore > 0 && riseLast > riseBefore {
		mulPerDepth = math.Pow(riseLast/riseBefore, 1.0/10)
	}

	depth100M, depth1G, depth3G = s.SearchDepth(), s.SearchDepth(), s.SearchDepth()
	expect := float64(last)
	rise := riseLast * 0.1
	for expect < 3000000000 && depth3G < 9999 {
		if expect < 100000000 {
			depth100M++
		}
		if expect < 1000000000 {
			depth1G++
		}
		depth3G++
		expect += rise
		rise *= mulPerDepth
	}
	return depth100M, depth1G, depth3G, true
}
