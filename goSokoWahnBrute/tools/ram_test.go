package tools

import (
	"runtime"
	"testing"
)

// auf den unterstützten Plattformen (Windows, Linux) muss die RAM-Erkennung
// einen plausiblen Wert liefern - sonst fiele der -ram-Default still auf 100 GB zurück
func TestTotalRAMBytes(t *testing.T) {
	total := TotalRAMBytes()
	if runtime.GOOS != "windows" && runtime.GOOS != "linux" {
		t.Skipf("Plattform %s ohne RAM-Erkennung (liefert %d)", runtime.GOOS, total)
	}
	if total < 1<<30 {
		t.Fatalf("TotalRAMBytes = %d, erwartet mindestens 1 GB", total)
	}
}
