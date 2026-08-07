package rooms

import (
	"math/big"
	"strings"
)

// theoretischer Rechenaufwand: Produkt der Variantenzahlen aller Räume
// (Räume mit 0 Varianten werden wie im C#-Original übersprungen)
func (n *Network) Effort() *big.Int {
	result := big.NewInt(1)
	tmp := new(big.Int)
	for _, room := range n.Rooms {
		count := room.Variants.Count()
		if count == 0 {
			continue
		}
		result.Mul(result, tmp.SetUint64(count))
	}
	return result
}

// Rechenaufwand als lesbare Zeichenkette, z.B. "4,700e43 (47.004...000)"
func (n *Network) EffortString() string {
	return FormatBig(n.Effort())
}

// formatiert eine große Zahl: ab 13 Stellen mit Exponent-Vorsatz,
// der volle Wert folgt mit Tausender-Punkten in Klammern
func FormatBig(value *big.Int) string {
	txt := value.String()
	grouped := groupDigits(txt)
	if len(txt) <= 12 {
		return grouped
	}

	// auf 4 Stellen runden (wie das C#-Original: 5. Stelle >= 5 rundet auf)
	lead := txt[:4]
	if txt[4] >= '5' {
		n := 0
		for _, c := range lead {
			n = n*10 + int(c-'0')
		}
		n++
		lead = big.NewInt(int64(n)).String() // kann auf 5 Stellen überlaufen (9999 -> 10000)
	}
	exp := len(txt) - 1
	return lead[:1] + "," + lead[1:] + "e" + big.NewInt(int64(exp)).String() + " (" + grouped + ")"
}

// fügt Tausender-Punkte in eine Ziffernfolge ein
func groupDigits(txt string) string {
	var sb strings.Builder
	lead := len(txt) % 3
	if lead == 0 {
		lead = 3
	}
	sb.WriteString(txt[:lead])
	for pos := lead; pos < len(txt); pos += 3 {
		sb.WriteByte('.')
		sb.WriteString(txt[pos : pos+3])
	}
	return sb.String()
}
