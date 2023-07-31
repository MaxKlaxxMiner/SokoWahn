package tools

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
