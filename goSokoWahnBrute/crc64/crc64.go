// Paket crc64 liefert das FNV-1a-64-Hashing der Stellungs-Schlüssel
// (trotz Namens kein echtes CRC). Übrig aus einer universellen Fluent-API
// ist genau der eine Schritt, den die Suche braucht: uint32-Werte einmischen
// (soko.State.UpdateCrc hasht Spielerposition und Kistenpositionen).
package crc64

type Value uint64

const (
	Start Value = 0xcbf29ce484222325
	Mul   Value = 0x100000001b3
)

func (crc Value) UpdateUInt32(value uint32) Value {
	return (crc ^ Value(value)) * Mul
}
