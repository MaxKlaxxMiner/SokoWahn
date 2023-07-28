//go:build ppc64 || ppc64le || s390x || mips || mips64

package crc64

import "math/bits"

func (crc Value) UpdateUInt32ByteOrdered(value uint32) Value {
	return (crc ^ Value(bits.ReverseBytes32(value))) * Mul
}

func (crc Value) UpdateUInt64ByteOrdered(value uint64) Value {
	return crc.UpdateUInt32ByteOrdered(uint32(value >> 32)).UpdateUInt32ByteOrdered(uint32(value))
}
