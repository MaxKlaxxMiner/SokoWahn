package soko

import (
	"encoding/binary"
	"math/bits"
)

// Ziel-Matching-Deadlock-Erkennung (Regel-Stufe 2, JSoko-Vorbild "frozen boxes
// on goals block access to other goals"): eingefrorene Kisten auf Zielfeldern
// können sich für den Rest der Partie nie mehr bewegen und wirken damit wie
// Wände. Jede noch bewegliche Kiste muss mit diesen Zusatz-Wänden weiterhin ein
// FREIES Ziel (keines der dauerhaft besetzten) erreichen können, und alle
// beweglichen Kisten zusammen brauchen eine überschneidungsfreie Zuordnung auf
// die freien Ziele (bipartites Matching). Scheitert eine der beiden Prüfungen,
// ist die Stellung bewiesen unlösbar.
//
// Konservativ und damit korrekt: die Erreichbarkeit wird je Kiste einzeln
// berechnet (allmächtiger Spieler, andere bewegliche Kisten ausgeblendet, nur
// die eingefrorenen als Wände) - eine Obermenge der echten Möglichkeiten.
// Findet selbst diese Relaxation kein vollständiges Matching, kann auch die
// echte Partie keines finden. Zielfelder besetzt eingefrorener Kisten sind
// aus dem Matching ausgenommen; da eingefrorene Kisten ihre Ziele bereits
// bedienen, geht die Zählung bewegliche Kisten = freie Ziele exakt auf.
//
// Die Erreichbarkeits-Masken hängen nur von der eingefrorenen Kistenmenge ab
// und werden je Rules-Instanz gecacht. Schlüssel ist die exakte Maske (kein
// Hash: eine Kollision würde fremde Erreichbarkeiten anwenden und könnte
// lösbare Stellungen verwerfen).

// Obergrenze des Erreichbarkeits-Caches je Rules-Instanz; bei Überlauf wird
// komplett geleert (die Einträge sind reine Ableitungen und jederzeit neu
// berechenbar, eine Verdrängungs-Logik lohnt nicht)
const matchCacheLimit = 512

// prüft, ob alle beweglichen Kisten mit den eingefrorenen Ziel-Kisten als
// Wänden noch überschneidungsfrei auf freie Ziele verteilt werden können.
// frozen ist das Fixpunkt-Ergebnis aus checkFreeze (alle Kisten darin stehen
// auf Zielen). false = beweisbarer Deadlock.
func (r *Rules) checkGoalMatch(frozen, boxBits []uint64) bool {
	sh := r.shared
	gw := sh.goalWords
	r.ensureMatchScratch()
	reach := r.reachWithFrozen(frozen)

	// bewegliche Kisten einsammeln (Sentinel-Bit fällt mit frozen nicht weg,
	// liegt aber >= walkEof und wird über die Positionsgrenze ausgefiltert)
	boxes := r.matchBoxes[:0]
	for w, word := range boxBits {
		word &^= frozen[w]
		for word != 0 {
			pos := Wpos(w<<6 + bits.TrailingZeros64(word))
			word &= word - 1
			if pos < sh.walkEof {
				boxes = append(boxes, pos)
			}
		}
	}
	r.matchBoxes = boxes

	// billige Vorstufe: jede Kiste braucht mindestens ein erreichbares freies Ziel
	for _, pos := range boxes {
		adj := reach[int(pos)*gw : int(pos)*gw+gw]
		any := false
		for _, a := range adj {
			if a != 0 {
				any = true
				break
			}
		}
		if !any {
			return false // Kiste erreicht hinter den eingefrorenen Wänden kein freies Ziel mehr
		}
	}

	// bipartites Matching per Kuhn-Augmentierung über die Bitmasken-Adjazenz;
	// owner[g] = Index (in boxes) der Kiste, der Ziel g aktuell zugeordnet ist.
	// Wichtig für die Korrektheit: ein Ziel wird erst beim tatsächlichen
	// Ausprobieren als besucht markiert (klassisches Kuhn-Schema) - ein
	// unvollständiger Suchlauf würde sonst lösbare Stellungen verwerfen.
	owner := r.matchOwner
	for i := range owner {
		owner[i] = -1
	}
	seen := r.matchSeen
	var augment func(bi int) bool
	augment = func(bi int) bool {
		base := int(boxes[bi]) * gw
		for w := 0; w < gw; w++ {
			for {
				cand := reach[base+w] &^ seen[w]
				if cand == 0 {
					break
				}
				g := w<<6 + bits.TrailingZeros64(cand)
				seen[w] |= 1 << (g & 63)
				if owner[g] < 0 || augment(int(owner[g])) {
					owner[g] = int16(bi)
					return true
				}
			}
		}
		return false
	}
	for bi := range boxes {
		for w := range seen {
			seen[w] = 0
		}
		if !augment(bi) {
			return false // kein vollständiges Matching -> Deadlock
		}
	}
	return true
}

// legt die wiederverwendbaren Puffer des Matchings beim ersten Gebrauch an
// (Klone starten ohne, damit ungenutzte Instanzen nichts kosten)
func (r *Rules) ensureMatchScratch() {
	if r.matchOwner != nil {
		return
	}
	sh := r.shared
	r.matchBoxes = make([]Wpos, 0, len(sh.goals))
	r.matchOwner = make([]int16, len(sh.goals))
	r.matchSeen = make([]uint64, sh.goalWords)
	r.reachSeen = make([]bool, sh.walkEof)
	r.matchKey = make([]byte, len(sh.goalBits)*8)
}

// liefert die Erreichbarkeits-Masken für eine eingefrorene Kistenmenge:
// je begehbarem Feld die Bitmaske der von dort per Schieben erreichbaren
// freien Ziele (Bit-Index = Position in shared.goals). Ergebnisse werden
// je Rules-Instanz gecacht - die Masken hängen nur von frozen ab.
func (r *Rules) reachWithFrozen(frozen []uint64) []uint64 {
	sh := r.shared
	key := r.matchKey
	for i, w := range frozen {
		binary.LittleEndian.PutUint64(key[i*8:], w)
	}
	if reach, hit := r.matchCache[string(key)]; hit {
		return reach
	}

	gw := sh.goalWords
	reach := make([]uint64, int(sh.walkEof)*gw)
	seen := r.reachSeen
	queue := r.reachQueue[:0]
	frozenAt := func(p Wpos) bool { return frozen[p>>6]&(1<<(p&63)) != 0 }

	// je freiem Ziel eine Rückwärts-BFS über Schub-Züge (gleiche Mechanik wie
	// die deadBits-Vorberechnung in NewRules): Kiste auf q kann auf das bereits
	// markierte Feld a geschoben werden, wenn der Spieler hinter q Platz hat;
	// eingefrorene Kisten blockieren Kisten- UND Spielerfeld
	mark := func(q, pp Wpos) {
		if q < sh.walkEof && pp < sh.walkEof && !seen[q] && !frozenAt(q) && !frozenAt(pp) {
			seen[q] = true
			queue = append(queue, q)
		}
	}
	for gi, g := range sh.goals {
		if frozenAt(g) {
			continue // dauerhaft besetztes Ziel: steht für keine bewegliche Kiste bereit
		}
		for i := range seen {
			seen[i] = false
		}
		queue = queue[:0]
		seen[g] = true
		queue = append(queue, g)
		wordIdx, bit := gi>>6, uint64(1)<<(gi&63)
		for len(queue) > 0 {
			a := queue[len(queue)-1]
			queue = queue[:len(queue)-1]
			reach[int(a)*gw+wordIdx] |= bit
			if q := sh.walkRight[a]; q < sh.walkEof {
				mark(q, sh.walkRight[q]) // Schub nach links auf a
			}
			if q := sh.walkLeft[a]; q < sh.walkEof {
				mark(q, sh.walkLeft[q]) // Schub nach rechts auf a
			}
			if q := sh.walkDown[a]; q < sh.walkEof {
				mark(q, sh.walkDown[q]) // Schub nach oben auf a
			}
			if q := sh.walkUp[a]; q < sh.walkEof {
				mark(q, sh.walkUp[q]) // Schub nach unten auf a
			}
		}
	}
	r.reachQueue = queue[:0]

	if len(r.matchCache) >= matchCacheLimit {
		r.matchCache = nil
	}
	if r.matchCache == nil {
		r.matchCache = make(map[string][]uint64, 64)
	}
	r.matchCache[string(key)] = reach
	return reach
}
