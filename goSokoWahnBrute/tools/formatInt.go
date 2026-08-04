package tools

// alle Ganzzahl-Typen (inklusive benannter Typen auf Ganzzahl-Basis)
type Integer interface {
	~int | ~int8 | ~int16 | ~int32 | ~int64 | ~uint | ~uint8 | ~uint16 | ~uint32 | ~uint64 | ~uintptr
}

// formatiert eine Ganzzahl mit Tausender-Punkten zur besseren Lesbarkeit
// (z.B. 1234567 -> "1.234.567")
func FormatInt[T Integer](value T) string {
	if value < 0 {
		// Betrag über die Zweierkomplement-Bits bilden (überlaufsicher auch für MinInt64)
		return "-" + formatUint(-uint64(value))
	}
	return formatUint(uint64(value))
}

// formatiert eine vorzeichenlose Zahl mit Tausender-Punkten
func formatUint(value uint64) string {
	var buf [26]byte // 20 Ziffern + 6 Punkte reichen für uint64
	pos := len(buf)
	digits := 0

	for {
		if digits > 0 && digits%3 == 0 {
			pos--
			buf[pos] = '.'
		}
		pos--
		buf[pos] = byte('0' + value%10)
		digits++
		value /= 10
		if value == 0 {
			break
		}
	}

	return string(buf[pos:])
}
