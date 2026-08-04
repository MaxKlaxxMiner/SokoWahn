package crc64

import "unsafe"

// verarbeitet die Bytes in 4er-Schritten als uint32-Werte (Rest-Bytes muss der Aufrufer behandeln)
func updateUnsafeByteSliceAsUInt32Slice(crc Value, data []byte) Value {
	uint32Slice := unsafe.Slice((*uint32)(unsafe.Pointer(unsafe.SliceData(data))), len(data)>>2)

	result := crc
	for _, val := range uint32Slice {
		result = result.UpdateUInt32ByteOrdered(val)
	}
	return result
}
