//go:build windows

package tools

import (
	"syscall"
	"unsafe"
)

// Ergebnis-Struktur von GlobalMemoryStatusEx (kernel32), Layout laut Win32-API
type memoryStatusEx struct {
	length               uint32
	memoryLoad           uint32
	totalPhys            uint64
	availPhys            uint64
	totalPageFile        uint64
	availPageFile        uint64
	totalVirtual         uint64
	availVirtual         uint64
	availExtendedVirtual uint64
}

var procGlobalMemoryStatusEx = syscall.NewLazyDLL("kernel32.dll").NewProc("GlobalMemoryStatusEx")

// liefert den insgesamt installierten physischen RAM in Bytes (0 = nicht ermittelbar)
func TotalRAMBytes() uint64 {
	var status memoryStatusEx
	status.length = uint32(unsafe.Sizeof(status))
	ret, _, _ := procGlobalMemoryStatusEx.Call(uintptr(unsafe.Pointer(&status)))
	if ret == 0 {
		return 0
	}
	return status.totalPhys
}
