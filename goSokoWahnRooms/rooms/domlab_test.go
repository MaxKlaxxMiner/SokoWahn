package rooms

import (
	"slices"
	"sort"
	"testing"
)

// Dominanz-Labor (Vorarbeit für M4b, siehe docs/konzept.md): enumeriert alle
// terminalen Nutzungen der 202er-Kammer aus Außenwelt-Sicht und kürt je
// Außen-Signatur den Pareto-Sieger (moves, dann pushes).
//
// Modell: die Kammer hat ein Portal; eine Nutzung ist eine Folge von Ereignissen,
// von denen die Außenwelt nur sieht:
//   E = Einschub (Außenwelt schiebt eine Kiste rein, nur wenn der BoxSwap existiert)
//   X = Besuch mit Kisten-Export
//   ! = der letzte Besuch war eine End-Variante (Spieler bleibt drin, Spielende)
// Exportlose Besuche (B) sind außenweltlich TRANSPARENT und tauchen in der
// Signatur nicht auf (Max' Argument, 2026-08-18): jedes Außen-Ereignis findet
// am Portal statt, der Spieler steht dort also ohnehin - einen exportlosen
// Besuch kann er direkt davor oder danach kostenlos einschieben (er endet, wo
// er beginnt, und verändert draußen nichts). Nutzungen mit verschieden vielen
// B-Besuchen konkurrieren daher in derselben Signatur-Gruppe.
// Eine Nutzung endet, wenn die Kammer im Endzustand 0 hinterlassen wird (bzw.
// mit "!"). Als einzige Reduktion ist die bewiesene Selbes-Portal-Regel
// eingebaut: nach einem exportlosen Besuch darf nicht direkt der nächste
// Besuch folgen (fusionierbar = dominiert); nach Export oder Einschub schon.
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

	// je Signatur alle kostenoptimalen Ketten (Sieger + Gleichstände)
	type sigInfo struct {
		moves  uint32
		pushes uint32
		chains [][]uint64
	}
	best := map[string]*sigInfo{}
	record := func(c chain, terminal string) {
		sig := c.sig + terminal
		info := best[sig]
		if info == nil || c.moves < info.moves || (c.moves == info.moves && c.pushes < info.pushes) {
			best[sig] = &sigInfo{moves: c.moves, pushes: c.pushes, chains: [][]uint64{slices.Clone(c.vars)}}
			return
		}
		if c.moves == info.moves && c.pushes == info.pushes {
			info.chains = append(info.chains, slices.Clone(c.vars))
		}
	}

	const maxEvents = 11
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
				nc.sig += "X" // exportlose Besuche bleiben außen unsichtbar
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

	// --- Auswertung: je Variante das Urteil über alle Signaturen ---
	sigs := make([]string, 0, len(best))
	for sig := range best {
		sigs = append(sigs, sig)
	}
	sort.Slice(sigs, func(i, j int) bool {
		if len(sigs[i]) != len(sigs[j]) {
			return len(sigs[i]) < len(sigs[j])
		}
		return sigs[i] < sigs[j]
	})

	contains := func(chain []uint64, id uint64) bool { return slices.Contains(chain, id) }
	used := map[uint64]bool{}      // kommt in mindestens einer optimalen Kette vor
	essential := map[uint64]string{} // in mindestens einer Signatur enthalten ALLE optimalen Ketten die Variante
	for _, sig := range sigs {
		info := best[sig]
		if len(sig) <= 8 {
			t.Logf("signatur %-8s optimal: moves=%3d pushes=%2d ketten=%d, z.B. %v",
				sig, info.moves, info.pushes, len(info.chains), info.chains[0])
		}
		inAll := map[uint64]bool{}
		for _, id := range info.chains[0] {
			inAll[id] = true
		}
		for _, ch := range info.chains {
			for _, id := range ch {
				used[id] = true
			}
			for id := range inAll {
				if !contains(ch, id) {
					delete(inAll, id)
				}
			}
		}
		for id := range inAll {
			if _, done := essential[id]; !done {
				essential[id] = sig
			}
		}
	}

	var essentials, replaceable, unused []uint64
	for id := uint64(0); id < room.Variants.Count(); id++ {
		switch {
		case essential[id] != "":
			essentials = append(essentials, id)
		case used[id]:
			replaceable = append(replaceable, id)
		default:
			unused = append(unused, id)
		}
	}
	t.Logf("varianten gesamt: %d", room.Variants.Count())
	t.Logf("essenziell (eine Signatur braucht sie zwingend): %v", essentials)
	for _, id := range essentials {
		t.Logf("  essenziell: variant %d (zwingend in signatur %s)", id, essential[id])
	}
	t.Logf("austauschbar (nur in Gleichstands-Alternativen): %v", replaceable)
	t.Logf("nie in einer optimalen Kette: %v", unused)
}
