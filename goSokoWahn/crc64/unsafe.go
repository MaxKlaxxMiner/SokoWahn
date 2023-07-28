package crc64

import (
	"reflect"
	"unsafe"
)

func UnsafeBytesToString(val []byte) string {
	return *(*string)(unsafe.Pointer(&val))
}

func UnsafeStringToBytes(val string) []byte {
	strHeader := (*reflect.StringHeader)(unsafe.Pointer(&val))
	valHeader := reflect.SliceHeader{Data: strHeader.Data, Len: strHeader.Len, Cap: strHeader.Len}
	return *(*[]byte)(unsafe.Pointer(&valHeader))
}

func updateUnsafeByteSliceAsUInt32Slice(crc Value, data []byte) Value {
	byteHeader := (*reflect.SliceHeader)(unsafe.Pointer(&data))
	uintHeader := reflect.SliceHeader{Data: byteHeader.Data, Len: byteHeader.Len >> 2, Cap: byteHeader.Len >> 2}
	uint32Slice := *(*[]uint32)(unsafe.Pointer(&uintHeader))

	result := crc
	for _, val := range uint32Slice {
		result = result.UpdateUInt32ByteOrdered(val)
	}
	return result
}
