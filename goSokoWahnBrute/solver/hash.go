package solver

import "goSokoWahnBrute/crc64"

// Sentinel-Wert: Zugtiefe unbekannt bzw. Stellung nicht in der Tabelle vorhanden
const DepthUnknown = uint16(65535)

// Hashtabelle für bekannte Stellungen: Crc64 der Stellung -> Zugtiefe
type PosTable interface {
	Get(crc crc64.Value) uint16          // liefert die Zugtiefe oder DepthUnknown
	Add(crc crc64.Value, depth uint16)   // trägt eine neue Stellung ein
	Update(crc crc64.Value, depth uint16) // aktualisiert die Zugtiefe einer bekannten Stellung
	Len() int64                          // Anzahl der Einträge
}

// einfache map-basierte Implementierung
// (Ausbaustufe: kompakte Open-Addressing-Tabelle mit 8 Byte pro Eintrag als Drop-in)
type mapTable struct {
	data map[crc64.Value]uint16
}

func NewMapTable() PosTable {
	return &mapTable{data: make(map[crc64.Value]uint16, 1024)}
}

func (t *mapTable) Get(crc crc64.Value) uint16 {
	if depth, ok := t.data[crc]; ok {
		return depth
	}
	return DepthUnknown
}

func (t *mapTable) Add(crc crc64.Value, depth uint16) {
	t.data[crc] = depth
}

func (t *mapTable) Update(crc crc64.Value, depth uint16) {
	t.data[crc] = depth
}

func (t *mapTable) Len() int64 {
	return int64(len(t.data))
}
