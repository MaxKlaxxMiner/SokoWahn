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

// optionale Erweiterung einer PosTable: Füllstand relativ zur internen
// Wachstums-Schwelle (1.0 = die nächste Einfügung löst das Resize aus)
type FillTable interface {
	Fill() float64
}

// Fabrik für die beiden Stellungs-Tabellen des Solvers (vorwärts + rückwärts) -
// austauschbar, um Tabellen-Varianten unter Realbedingungen zu vergleichen.
// Default klein (Tests und Bibliotheks-Nutzung); die App setzt in main auf
// NewCompactTableLarge um (spart die Verdopplungs-Leiter, Messung: +10%)
var TableFactory func() PosTable = NewCompactTable

// RAM-Notbremsen-Grenze in Bytes (0 = aus) - main.go setzt beim Programmstart 85%
// des installierten RAM (Flag -ram). Vergleichsbasis ist überall der berechnete
// Verbrauch der Suche (RamBytes: Hashtabellen + Listen-Puffer, dieselbe Basis wie
// RAM-Anzeige und Auslagerungs-Schwelle), bewusst NICHT ReadMemStats: der echte
// Go-Heap enthält Runtime-Reserven und GC-Transienten (z.B. die Umkopier-Spitze
// einer Tabellen-Verdopplung, bei der alte und neue Arrays gleichzeitig leben)
// und hat auf einer 640-GB-Maschine die Notbremse ausgelöst, obwohl die Suche
// selbst noch weit unter dem Limit lag.
// Neben dem Stopp im TUI nutzt der Solver die Grenze, um anstehende Verdopplungen
// der Stellungs-Tabellen, die das Limit rechnerisch reißen würden, durch die
// kompaktere Archiv-Konvertierung zu ersetzen (siehe autoArchive in stats.go).
var RamLimitBytes int64

//var TableFactory func() PosTable = NewSegmentTable
