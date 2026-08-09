package tools

import "unsafe"

// wandelt ein Byte-Slice ohne Kopie in einen String um (der Slice darf danach nicht mehr verändert werden)
func UnsafeBytesToString(val []byte) string {
	return unsafe.String(unsafe.SliceData(val), len(val))
}

// wandelt einen String ohne Kopie in ein Byte-Slice um (das Slice darf nicht verändert werden)
func UnsafeStringToBytes(val string) []byte {
	return unsafe.Slice(unsafe.StringData(val), len(val))
}
