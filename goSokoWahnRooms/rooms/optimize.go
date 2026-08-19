package rooms

import (
	"math"

	"goSokoWahnRooms/soko"
)

// Markierung für entfernte Zustände/Varianten in den Mapping-Tabellen
const droppedID = uint64(math.MaxUint64)

// entfernt nicht mehr benutzte Kisten-Zustände eines Raumes und räumt dabei
// Varianten auf, deren Zielzustand wegfällt. Läuft als Fixpunkt-Iteration:
// jede Runde kann neue Waisen erzeugen (C#-Vorbild: OptimizeTools.RemoveUnusedStates).
func removeUnusedStates(room *Room) {
	room.invalidateMinMoves()
	for removeUnusedStatesOnce(room) {
	}
}

// eine Runde: benutzte Zustände markieren, bei Waisen einmal umbauen.
// Bewusst wie im Original: der Zielzustand (NewState) einer Variante zählt NICHT
// als Benutzung - ein Zustand lebt nur, wenn von ihm aus etwas passieren kann
// (Varianten-Verzeichnis eines Portals) oder er End-/Start-/Ausgangszustand ist.
// Varianten, deren Zielzustand stirbt, sterben mit (Sackgassen-Ausdünnung).
func removeUnusedStatesOnce(room *Room) bool {
	stateCount := room.States.Count()
	used := make([]bool, stateCount)
	used[0] = true // Endzustand bleibt immer
	used[room.StartState] = true

	// Ausgangszustände der Startvarianten
	for id := uint64(0); id < room.StartVariantCount; id++ {
		used[room.Variants.Get(id).OldState] = true
	}

	goals := make(map[soko.Wpos]bool, len(room.Goals))
	for _, g := range room.Goals {
		goals[g] = true
	}
	allOnGoals := func(state uint64) bool {
		for _, pos := range room.States.Get(state) {
			if !goals[pos] {
				return false
			}
		}
		return true
	}

	for _, ip := range room.Incoming {
		// Zustände mit Anschluss-Varianten an diesem Portal
		for state, span := range ip.VariantSpans {
			if span.Count > 0 {
				used[state] = true
			}
		}
		// BoxSwap-Ausgangszustände: nur wenn der Zielzustand eine Zukunft hat
		// (Anschluss-Varianten am selben Portal oder alle Kisten auf Zielen)
		for from, to := range ip.BoxSwap {
			if ip.GetVariantSpan(to).Count == 0 && !allOnGoals(to) {
				continue
			}
			used[from] = true
		}
	}

	for _, u := range used {
		if !u {
			renewStates(room, used)
			return true
		}
	}
	return false
}

// entfernt alle nicht markierten Varianten eines Raumes und baut die
// Span-Verzeichnisse der Portale neu auf; Zustände und BoxSwaps bleiben
// unangetastet (dafür danach removeUnusedStates aufrufen).
// C#-Vorbild: OptimizeTools.RenewVariants
func renewVariants(room *Room, used []bool) {
	room.invalidateMinMoves()
	oldVariants := room.Variants
	variantMap := make([]uint64, oldVariants.Count())
	newVariants := NewVariantList()
	newStartCount := uint64(0)
	for id := uint64(0); id < oldVariants.Count(); id++ {
		if !used[id] {
			variantMap[id] = droppedID
			continue
		}
		variantMap[id] = newVariants.Add(*oldVariants.Get(id))
		if id < room.StartVariantCount {
			newStartCount++
		}
	}
	room.Variants = newVariants
	room.StartVariantCount = newStartCount

	for _, ip := range room.Incoming {
		newSpans := make(map[uint64]Span, len(ip.VariantSpans))
		for state, span := range ip.VariantSpans {
			// überlebende Varianten eines Spans bleiben lückenlos (globale
			// Neunummerierung erhält die Reihenfolge, alter Span war zusammenhängend)
			newSpan := Span{}
			for id := span.Start; id < span.Start+span.Count; id++ {
				nid := variantMap[id]
				if nid == droppedID {
					continue
				}
				if newSpan.Count == 0 {
					newSpan.Start = nid
				} else if newSpan.Start+newSpan.Count != nid {
					panic("renewVariants: span not contiguous")
				}
				newSpan.Count++
			}
			if newSpan.Count > 0 {
				newSpans[state] = newSpan
			}
		}
		ip.VariantSpans = newSpans
	}
}

// baut Zustände, Varianten, BoxSwaps und Span-Verzeichnisse eines Raumes mit den
// markierten Zuständen neu auf (C#-Vorbild: OptimizeTools.RenewStates)
func renewStates(room *Room, used []bool) {
	// Zustands-Mapping alt -> neu
	stateMap := make([]uint64, len(used))
	next := uint64(0)
	for id, u := range used {
		if u {
			stateMap[id] = next
			next++
		} else {
			stateMap[id] = droppedID
		}
	}

	// Zustandsliste gefiltert übernehmen
	oldStates := room.States
	newStates := NewStateList()
	for id := uint64(0); id < oldStates.Count(); id++ {
		if stateMap[id] != droppedID {
			newStates.Add(oldStates.Get(id))
		}
	}
	room.States = newStates
	room.StartState = stateMap[room.StartState]

	// Varianten filtern: fällt der Zielzustand weg, fällt die Variante mit weg.
	// Der Ausgangszustand ist per Markierung immer benutzt (Invarianten-Wächter).
	oldVariants := room.Variants
	variantMap := make([]uint64, oldVariants.Count())
	newVariants := NewVariantList()
	newStartCount := uint64(0)
	for id := uint64(0); id < oldVariants.Count(); id++ {
		v := oldVariants.Get(id)
		if stateMap[v.OldState] == droppedID {
			panic("renewStates: old state of a variant dropped")
		}
		if stateMap[v.NewState] == droppedID {
			variantMap[id] = droppedID
			continue
		}
		variantMap[id] = newVariants.Add(VariantData{
			OldState:     stateMap[v.OldState],
			NewState:     stateMap[v.NewState],
			Moves:        v.Moves,
			Pushes:       v.Pushes,
			BoxPortals:   v.BoxPortals,
			PlayerPortal: v.PlayerPortal,
			Path:         v.Path,
		})
		if id < room.StartVariantCount {
			newStartCount++
		}
	}
	room.Variants = newVariants
	// Abweichung vom C#-Original: das zählte startVariantCount hier nicht neu -
	// fällt eine Start-Variante weg, stimmte dort die Zählung nicht mehr
	room.StartVariantCount = newStartCount

	// Portale: BoxSwaps und Span-Verzeichnisse mit den neuen IDs neu aufbauen
	for _, ip := range room.Incoming {
		newSwap := make(map[uint64]uint64, len(ip.BoxSwap))
		for from, to := range ip.BoxSwap {
			if stateMap[from] == droppedID || stateMap[to] == droppedID {
				continue
			}
			newSwap[stateMap[from]] = stateMap[to]
		}
		ip.BoxSwap = newSwap

		newSpans := make(map[uint64]Span, len(ip.VariantSpans))
		for state, span := range ip.VariantSpans {
			if stateMap[state] == droppedID {
				panic("renewStates: span state dropped") // Span-Zustände sind immer markiert
			}
			// überlebende Varianten eines Spans bleiben ein lückenloser Bereich:
			// die globale Neunummerierung erhält die Reihenfolge, und der alte
			// Span war bereits zusammenhängend
			newSpan := Span{}
			for id := span.Start; id < span.Start+span.Count; id++ {
				nid := variantMap[id]
				if nid == droppedID {
					continue
				}
				if newSpan.Count == 0 {
					newSpan.Start = nid
				} else if newSpan.Start+newSpan.Count != nid {
					panic("renewStates: span not contiguous")
				}
				newSpan.Count++
			}
			if newSpan.Count > 0 {
				newSpans[stateMap[state]] = newSpan
			}
		}
		ip.VariantSpans = newSpans
	}
}
