package rooms

import "goSokoWahnRooms/soko"

// Spieler-Konnektivität der Außenwelt eines Raums (Max' Idee, 2026-08-20):
// die begehbaren Felder OHNE die Raum-Felder zerfallen in statische
// Zusammenhangskomponenten (nur Wände zählen, fremde Raum-Felder werden
// optimistisch als frei angenommen - Nichtzusammenhang ist damit BEWIESEN,
// Zusammenhang nur angenommen, also konservativ). Ein Portal-Ereignis ist
// nur legal, wenn das Portal in der Komponente der aktuellen Spieler-
// Position liegt; die Seite wechseln kann der Spieler nur durch sichtbare
// Durchgangs-Besuche. Illegale Wörter fallen aus der Anforderungsmenge der
// Dominanzsuche - reiner Filter, die Korrektheit hängt nicht an ihm
// (Kisten-Konnektivität bewusst NICHT einbezogen: fremde Kisten,
// zustandsabhängige Flüsse - notierte Ausbaustufe).
type usageEnv struct {
	portalComp []int // Außen-Komponente je Portal-Slot (Außenfeld Incoming.From)
	startComp  int   // Komponente des Spieler-Startfelds (-1: Spieler startet im Raum)
}

// Komponente der Spieler-Position (Portal-Slot oder usagePosStart)
func (e *usageEnv) compAt(pos int) int {
	if pos == usagePosStart {
		return e.startComp
	}
	return e.portalComp[pos]
}

// berechnet die Außen-Komponenten eines Raums über eine Flutung des
// Komplements (BFS über die begehbaren Felder ohne die Raum-Felder)
func (n *Network) usageEnv(room *Room) *usageEnv {
	eof := int(n.Field.WalkEof())
	comp := make([]int, eof)
	for i := range comp {
		comp[i] = -1
	}
	for _, pos := range room.Fields {
		comp[pos] = -2 // Raum-Feld: gehört keiner Außen-Komponente an
	}
	next := 0
	var queue []soko.Wpos
	for p := 0; p < eof; p++ {
		if comp[p] != -1 {
			continue
		}
		comp[p] = next
		queue = append(queue[:0], soko.Wpos(p))
		for len(queue) > 0 {
			cur := queue[0]
			queue = queue[1:]
			for _, dir := range []byte{'l', 'r', 'u', 'd'} {
				nb := n.Field.Neighbor(cur, dir)
				if int(nb) < eof && comp[nb] == -1 {
					comp[nb] = next
					queue = append(queue, nb)
				}
			}
		}
		next++
	}

	env := &usageEnv{portalComp: make([]int, len(room.Incoming)), startComp: -1}
	for i, ip := range room.Incoming {
		env.portalComp[i] = comp[ip.From]
	}
	if start := n.Field.InitPlayer(); comp[start] >= 0 {
		env.startComp = comp[start]
	}
	return env
}
