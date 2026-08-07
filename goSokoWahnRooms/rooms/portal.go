package rooms

import (
	"fmt"

	"goSokoWahnRooms/soko"
)

// zusammenhängender Varianten-Bereich (IDs Start..Start+Count-1)
type Span struct {
	Start uint64
	Count uint64
}

// gerichtete Verbindung zweier Nachbarfelder in verschiedenen Räumen;
// Portale existieren immer paarweise (Opposite)
type Portal struct {
	From soko.Wpos // Feld im Quell-Raum
	To   soko.Wpos // Feld im Ziel-Raum (dem das Portal gehört)

	FromRoom *Room
	ToRoom   *Room

	Index uint32 // Position in ToRoom.Incoming
	Dir   byte   // Richtung des Portals ('l','r','u','d'), From -> To

	Opposite *Portal

	// durchgeschobene Kiste steckt fest bzw. kann nicht weitergeschoben werden:
	// der Spieler darf dann nicht mehr hinterher (aus dem Single-Box-Scan abgeleitet)
	BlockedBox bool

	// Zustandswechsel des Zielraums, wenn eine Kiste durch das Portal geschoben wird
	BoxSwap map[uint64]uint64

	// Verzeichnis: Raum-Zustand -> Varianten, wenn der Spieler hier reinkommt
	VariantSpans map[uint64]Span
}

// trägt einen Zustandswechsel für eine reingeschobene Kiste ein
func (p *Portal) AddBoxSwap(oldState, newState uint64) {
	if oldState == newState {
		panic("useless box swap")
	}
	if _, ok := p.BoxSwap[oldState]; ok {
		panic("duplicate box swap")
	}
	p.BoxSwap[oldState] = newState
}

// gibt den Zustandswechsel nach einer reingeschobenen Kiste zurück
// (gleiche ID, wenn der Raum keine Kiste aufnehmen kann)
func (p *Portal) GetBoxSwap(state uint64) uint64 {
	if next, ok := p.BoxSwap[state]; ok {
		return next
	}
	return state
}

// trägt eine Variante für einen Raum-Zustand ein; die Varianten je (Portal, Zustand)
// müssen lückenlos aufsteigend vergeben werden (Span-Invariante)
func (p *Portal) AddVariant(state uint64, variantID uint64) {
	sp, ok := p.VariantSpans[state]
	if !ok {
		p.VariantSpans[state] = Span{Start: variantID, Count: 1}
		return
	}
	if sp.Start+sp.Count != variantID {
		panic(fmt.Sprintf("variant span not contiguous: state %d, expected %d, got %d", state, sp.Start+sp.Count, variantID))
	}
	sp.Count++
	p.VariantSpans[state] = sp
}

// gibt den Varianten-Bereich für einen Raum-Zustand zurück (Count 0, wenn keiner existiert)
func (p *Portal) GetVariantSpan(state uint64) Span {
	return p.VariantSpans[state]
}

func (p *Portal) String() string {
	return fmt.Sprintf("%c: %d -> %d", p.Dir, p.From, p.To)
}
