package rooms

import (
	"fmt"
	"slices"
	"sort"
	"testing"
)

// Dominanz-Labor (Vorarbeit für M4b, siehe docs/konzept.md): enumeriert alle
// terminalen Nutzungen der 202er-Kammer aus Außenwelt-Sicht und kürt je
// Außen-Signatur den Pareto-Sieger (moves, dann pushes).
//
// Modell: die Kammer hat ein Portal; eine Nutzung ist eine Folge von Ereignissen
//   E = Einschub (Außenwelt schiebt eine Kiste rein, nur wenn der BoxSwap existiert)
//   B = Besuch ohne Kisten-Export
//   X = Besuch mit Kisten-Export
//   ! = der letzte Besuch war eine End-Variante (Spieler bleibt drin, Spielende)
// und endet, wenn die Kammer im Endzustand 0 hinterlassen wird (bzw. mit "!").
// Als einzige Reduktion ist die bewiesene Selbes-Portal-Regel eingebaut: nach
// einem exportlosen Besuch darf nicht direkt der nächste Besuch folgen
// (fusionierbar = dominiert); nach einem Export oder einem Einschub schon.
//
// Varianten, die in KEINEM Signatur-Sieger vorkommen, sind Kandidaten für die
// Dominanzsuche - Varianten, die nur in "exotischen" Signaturen siegen, zeigen,
// wo lokale Signatur-Gleichheit allein nicht reicht und signatur-übergreifende
// Argumente (Außenwelt-Annahmen) nötig wären.
func TestDominanceLab202(t *testing.T) {
	_, room := merge202Chamber(t)
	ip := room.Incoming[0]

	type chain struct {
		state        uint64
		sig          string
		moves        uint32
		pushes       uint32
		vars         []uint64
		lastVisit    bool // letztes Ereignis war ein Besuch
		lastExported bool // ... und der hat eine Kiste exportiert
	}

	winners := map[string]chain{}
	record := func(c chain, terminal string) {
		sig := c.sig + terminal
		best, exists := winners[sig]
		if !exists || c.moves < best.moves || (c.moves == best.moves && c.pushes < best.pushes) {
			c.sig = sig
			winners[sig] = c
		}
	}

	const maxEvents = 7
	var walk func(c chain)
	walk = func(c chain) {
		if c.state == 0 && len(c.vars) > 0 {
			record(c, "") // Nutzung kann hier enden: Endzustand 0 hinterlassen
		}
		if len(c.sig) >= maxEvents {
			return
		}

		// Einschub durch die Außenwelt
		if next, exists := ip.BoxSwap[c.state]; exists {
			nc := c
			nc.state = next
			nc.sig += "E"
			nc.lastVisit = false
			walk(nc)
		}

		// Besuche (Selbes-Portal-Regel: nach exportlosem Besuch erst wieder
		// nach einem Einschub - sonst fusionierbar und damit dominiert)
		if c.lastVisit && !c.lastExported {
			return
		}
		span := ip.GetVariantSpan(c.state)
		for id := span.Start; id < span.Start+span.Count; id++ {
			v := room.Variants.Get(id)
			nc := c
			nc.state = v.NewState
			nc.moves += v.Moves
			nc.pushes += v.Pushes
			nc.vars = append(slices.Clone(c.vars), id)
			exported := len(v.BoxPortals) > 0
			if exported {
				nc.sig += "X"
			} else {
				nc.sig += "B"
			}
			nc.lastVisit, nc.lastExported = true, exported
			if v.PlayerPortal == NoPortal {
				if nc.state == 0 {
					record(nc, "!")
				}
				continue
			}
			walk(nc)
		}
	}
	walk(chain{state: room.StartState})

	// --- Auswertung: Sieger je Signatur + Varianten-Nutzung ---
	sigs := make([]string, 0, len(winners))
	for sig := range winners {
		sigs = append(sigs, sig)
	}
	sort.Slice(sigs, func(i, j int) bool {
		if len(sigs[i]) != len(sigs[j]) {
			return len(sigs[i]) < len(sigs[j])
		}
		return sigs[i] < sigs[j]
	})

	used := map[uint64]bool{}
	for _, sig := range sigs {
		w := winners[sig]
		t.Logf("signatur %-8s sieger: moves=%3d pushes=%2d varianten=%v", sig, w.moves, w.pushes, w.vars)
		for _, id := range w.vars {
			used[id] = true
		}
	}

	var unused []uint64
	for id := uint64(0); id < room.Variants.Count(); id++ {
		if !used[id] {
			unused = append(unused, id)
		}
	}
	t.Logf("varianten gesamt: %d, in siegern: %d, nie in einem sieger: %v",
		room.Variants.Count(), len(used), unused)
	for _, id := range unused {
		v := room.Variants.Get(id)
		t.Logf("  unbenutzt: variant %d (old=%d new=%d moves=%d pushes=%d exports=%d)",
			id, v.OldState, v.NewState, v.Moves, v.Pushes, len(v.BoxPortals))
	}
	if fmt.Sprint(unused) == "" {
		t.Log("(keine)")
	}
}
