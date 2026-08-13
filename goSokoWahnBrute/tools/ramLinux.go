//go:build linux

package tools

import (
	"bufio"
	"os"
	"strconv"
	"strings"
)

// liefert den insgesamt installierten physischen RAM in Bytes (0 = nicht ermittelbar).
// Quelle ist die MemTotal-Zeile aus /proc/meminfo ("MemTotal:  65834372 kB").
func TotalRAMBytes() uint64 {
	file, err := os.Open("/proc/meminfo")
	if err != nil {
		return 0
	}
	defer file.Close()

	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		line := scanner.Text()
		if !strings.HasPrefix(line, "MemTotal:") {
			continue
		}
		fields := strings.Fields(line)
		if len(fields) < 2 {
			return 0
		}
		kb, err := strconv.ParseUint(fields[1], 10, 64)
		if err != nil {
			return 0
		}
		return kb << 10
	}
	return 0
}
