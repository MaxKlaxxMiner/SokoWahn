package rooms

// Path: LURD-Zugfolge 2-Bit-kodiert, 4 Züge je Byte (Max' Idee, 2026-08-20):
// der allererste 2-Bit-Slot enthält das Padding (0-3), also wie viele Slots
// im letzten Byte ungenutzt sind - damit braucht es keine separate
// Längenangabe. Danach folgen die Züge als 2-Bit-Codes (l=0, u=1, r=2, d=3),
// innerhalb eines Bytes von den niederwertigen Bits aufwärts. Die leere
// Zugfolge ist das leere/nil-Slice. Ein Path ist nach dem Erstellen
// unveränderlich (Concat teilt bei leerer Gegenseite das Original).
type Path []byte

// Zeichen -> 2-Bit-Code; die Reihenfolge "lurd" macht codeLURD zur Umkehrung
const codeLURD = "lurd"

var lurdCode = [256]byte{'l': 0, 'u': 1, 'r': 2, 'd': 3}

// liest den 2-Bit-Slot i (Slot 0 = Padding, Züge ab Slot 1)
func (p Path) slot(i int) byte {
	return p[i>>2] >> ((i & 3) * 2) & 3
}

// setzt den 2-Bit-Slot i (nur auf genullten Bytes, wie make sie liefert)
func (p Path) setSlot(i int, code byte) {
	p[i>>2] |= code << ((i & 3) * 2)
}

// Anzahl der Züge
func (p Path) Len() int {
	if len(p) == 0 {
		return 0
	}
	return len(p)*4 - 1 - int(p[0]&3)
}

// LURD gibt die Zugfolge als Klartext zurück (für GUI und Debug-Ausgaben)
func (p Path) LURD() string {
	moves := p.Len()
	raw := make([]byte, moves)
	for i := range raw {
		raw[i] = codeLURD[p.slot(i+1)]
	}
	return string(raw)
}

// PathFromLURD packt eine Klartext-Zugfolge (nur 'l','u','r','d')
func PathFromLURD(s string) Path {
	if len(s) == 0 {
		return nil
	}
	slots := len(s) + 1 // Slot 0 = Padding
	p := make(Path, (slots+3)/4)
	p.setSlot(0, byte(len(p)*4-slots))
	for i := 0; i < len(s); i++ {
		p.setSlot(i+1, lurdCode[s[i]])
	}
	return p
}

// Concat hängt q an p und liefert die kombinierte Zugfolge; ist eine Seite
// leer, wird die andere unverändert geteilt (Paths sind unveränderlich)
func (p Path) Concat(q Path) Path {
	np, nq := p.Len(), q.Len()
	if nq == 0 {
		return p
	}
	if np == 0 {
		return q
	}
	slots := np + nq + 1
	r := make(Path, (slots+3)/4)
	r.setSlot(0, byte(len(r)*4-slots))
	for i := 0; i < np; i++ {
		r.setSlot(i+1, p.slot(i+1))
	}
	for i := 0; i < nq; i++ {
		r.setSlot(np+i+1, q.slot(i+1))
	}
	return r
}
