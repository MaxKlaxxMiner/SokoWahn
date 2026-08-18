package rooms

import (
	"slices"
	"sort"
	"testing"
)

// Dominanz-Labor (Vorarbeit für M4b, siehe docs/konzept.md): enumeriert alle
// terminalen Nutzungen der 202er-Kammer aus Außenwelt-Sicht und kürt je
// Außen-Signatur die kostenoptimalen Ketten (moves, dann pushes).
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

// kostenoptimale Ketten einer Außen-Signatur
type labSigInfo struct {
	moves  uint32
	pushes uint32
	chains [][]uint64
}

// enumeriert alle terminalen Nutzungen eines Ein-Portal-Raums bis maxEvents
// sichtbare Ereignisse; allowed (optional) schränkt die nutzbaren Varianten ein
func enumerateUsages(room *Room, maxEvents int, allowed func(id uint64) bool) map[string]*labSigInfo {
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

	best := map[string]*labSigInfo{}
	record := func(c chain, terminal string) {
		sig := c.sig + terminal
		info := best[sig]
		if info == nil || c.moves < info.moves || (c.moves == info.moves && c.pushes < info.pushes) {
			best[sig] = &labSigInfo{moves: c.moves, pushes: c.pushes, chains: [][]uint64{slices.Clone(c.vars)}}
			return
		}
		if c.moves == info.moves && c.pushes == info.pushes {
			info.chains = append(info.chains, slices.Clone(c.vars))
		}
	}

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
			if allowed != nil && !allowed(id) {
				continue
			}
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
	return best
}

// sortiert Signaturen nach Länge, dann alphabetisch
func sortedSigs(best map[string]*labSigInfo) []string {
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
	return sigs
}

const labHorizon = 11

// Voll-Enumeration: teilt die Varianten in essenziell (irgendeine Signatur
// braucht sie in ALLEN Optimal-Ketten), austauschbar (nur in Gleichstands-
// Alternativen) und nie-optimal (Kandidaten für die Dominanzsuche)
func TestDominanceLab202(t *testing.T) {
	_, room := merge202Chamber(t)
	best := enumerateUsages(room, labHorizon, nil)

	used := map[uint64]bool{}
	essential := map[uint64]string{}
	for _, sig := range sortedSigs(best) {
		info := best[sig]
		if len(sig) <= 6 {
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
				if !slices.Contains(ch, id) {
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
	t.Logf("essenziell: %v", essentials)
	for _, id := range essentials {
		t.Logf("  essenziell: variant %d (zwingend in signatur %s)", id, essential[id])
	}
	t.Logf("austauschbar: %v", replaceable)
	t.Logf("nie in einer optimalen Kette: %v", unused)
}

// Minimal-These (Max, 2026-08-18): die Kammer braucht nur 7 Varianten -
// "ohne parken" (direkt ins Ziel + raus/END, Garage vor der Lieferung) und
// "mit parken" (immer v3 wegen des einmaligen 2-Züge-Tricks, dann beliebig
// viele Garagen als Loop, Abschluss raus/END). Der Test prüft: die auf diese
// Varianten eingeschränkte Enumeration erreicht für JEDE Signatur exakt
// dieselben Optimal-Kosten wie die Voll-Enumeration.
var lab202MinimalSet = map[uint64]bool{
	1:  true, // {36}: Garagen-Kiste wieder raus (vor der Ziel-Lieferung)
	3:  true, // {36}: parken auf 26 (der Trick, Teil 1)
	5:  true, // {36}: direkt ins Ziel + raus
	6:  true, // {36}: direkt ins Ziel + Spielende
	17: true, // {26}: geparkte ins Ziel + Spielende
	19: true, // {26,36}: Garagen-Kiste raus + geparkte ins Ziel (der Trick, Teil 2)
	20: true, // {26,36}: Garagen-Kiste raus, geparkte bleibt (der Garagen-Loop)
}

func TestDominanceLab202Minimal(t *testing.T) {
	_, room := merge202Chamber(t)
	minimalSet := lab202MinimalSet

	full := enumerateUsages(room, labHorizon, nil)
	reduced := enumerateUsages(room, labHorizon, func(id uint64) bool { return minimalSet[id] })

	for _, sig := range sortedSigs(full) {
		f := full[sig]
		r, exists := reduced[sig]
		if !exists {
			t.Errorf("signatur %s: mit Minimal-Menge nicht mehr bedienbar", sig)
			continue
		}
		if r.moves != f.moves || r.pushes != f.pushes {
			t.Errorf("signatur %s: Minimal-Menge teurer (%d/%d statt %d/%d)",
				sig, r.moves, r.pushes, f.moves, f.pushes)
		}
	}
	for _, sig := range sortedSigs(reduced) {
		if _, exists := full[sig]; !exists {
			t.Errorf("signatur %s: nur mit Minimal-Menge erreichbar (kann nicht sein)", sig)
		}
	}
	t.Logf("alle %d Signaturen mit 7 von %d Varianten optimal bedient", len(full), room.Variants.Count())

	// welche Zustände die Minimal-Menge noch braucht
	states := map[uint64]bool{0: true, room.StartState: true}
	for id := range minimalSet {
		v := room.Variants.Get(id)
		states[v.OldState] = true
		states[v.NewState] = true
	}
	t.Logf("benötigte Zustände: %d von %d", len(states), room.States.Count())
}
