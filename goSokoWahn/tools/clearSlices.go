package tools

import (
	"reflect"
	"unsafe"
)

func ClearBools(bools []bool) {
	pos := 0

	if len(bools) >= 16 {
		boolsHeader := (*reflect.SliceHeader)(unsafe.Pointer(&bools))
		uint64Header := reflect.SliceHeader{Data: boolsHeader.Data, Len: boolsHeader.Len >> 3, Cap: boolsHeader.Len >> 3}
		uint64Slice := *(*[]uint64)(unsafe.Pointer(&uint64Header))
		u64clear := uint64(0x0000000000000000)
		for i := range uint64Slice {
			uint64Slice[i] = u64clear
		}
		pos += len(uint64Slice) << 3
	}

	for ; pos < len(bools); pos++ {
		bools[pos] = false
	}
}
