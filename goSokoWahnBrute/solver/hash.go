package solver

import "goSokoWahnBrute/crc64"

// Sentinel-Wert: Zugtiefe unbekannt bzw. Stellung nicht in der Tabelle vorhanden
const DepthUnknown = uint16(65535)

// Hashtabelle für bekannte Stellungen: Crc64 der Stellung -> Zugtiefe
// (Implementierung: CompactTable, siehe hashCompact.go; ein Hashtable-Shootout
// gegen diverse Bibliotheken ist in docs/architektur.md dokumentiert)
type PosTable interface {
	Get(crc crc64.Value) uint16           // liefert die Zugtiefe oder DepthUnknown
	Add(crc crc64.Value, depth uint16)    // trägt eine neue Stellung ein
	Update(crc crc64.Value, depth uint16) // aktualisiert die Zugtiefe einer bekannten Stellung
	Len() int64                           // Anzahl der Einträge
	Bytes() int64                         // reservierter Speicher in Bytes (für die RAM-Anzeige)
}

// Fabrik für die beiden Stellungs-Tabellen des Solvers (vorwärts + rückwärts) -
// austauschbar, um Tabellen-Varianten unter Realbedingungen zu vergleichen
var TableFactory func() PosTable = NewCompactTable

//var TableFactory func() PosTable = NewSegmentTable
