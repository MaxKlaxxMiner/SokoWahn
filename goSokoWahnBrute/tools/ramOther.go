//go:build !windows && !linux

package tools

// Rückfall für sonstige Plattformen: RAM-Größe unbekannt (Aufrufer nutzen dann ihren eigenen Default)
func TotalRAMBytes() uint64 {
	return 0
}
