package rooms

import "fmt"

// Deadlock-Scan (M4): entfernt Varianten eines Raumes, die von der Startsituation
// aus nie erreichbar sind (Vorwärts-Scan) oder nie zu einem lokalen Spielende
// führen können (Rückwärts-Scan), samt dabei verwaisender Zustände.
// C#-Vorbild: RoomDeadlockScanner + RoomReverse. Bewusste Abweichungen:
//   - die dort selbst mit "todo: bug?" markierte Regel "Rückweg durchs gleiche
//     Portal nur nach rausgeschobener Kiste" entfällt - raus- und wieder
//     reinlaufen ist physisch immer möglich, die Regel hätte erreichbare
//     Varianten wegwerfen können. Die Vorwärts-Suche läuft dadurch als simple
//     Zustands-Erreichbarkeit (Aufgaben sind Zustände statt Austritts-Tripel).
//   - Varianten werden einzeln statt Span-weise markiert (das C# konnte beim
//     Masken-Zweig bereits markierte Varianten doppelt zählen).
//   - der Ziele-Check für End-Varianten gilt auch für End-Startvarianten.
//
// Beide Scans sind Über-Approximationen der echten Erreichbarkeit (die Außenwelt
// wird nur über die BoxSwap-Masken angenähert) - entfernt wird nur, was selbst
// unter diesen großzügigen Annahmen nie vorkommen kann.
//
// info (optional) bekommt Fortschritts-Meldungen; Rückgabe false bricht ab,
// der Raum bleibt dann unverändert (die Umbauten passieren erst ganz am Ende).
func (n *Network) DeadlockScan(room *Room, info func(string) bool) (removed uint64, ok bool) {
	variantCount := room.Variants.Count()
	if variantCount == 0 {
		return 0, true
	}
	stateCount := room.States.Count()
	usedForward := make([]bool, variantCount)
	usedBackward := make([]bool, variantCount)

	// End-Varianten sind nur gültig, wenn alle dabei rausgeschobenen Kisten auf
	// Zielfeldern landen - sonst wäre das Spiel gar nicht vorbei
	endValid := func(v *VariantData) bool {
		for _, bp := range v.BoxPortals {
			if !n.Field.IsGoal(room.Outgoing[bp].To) {
				return false
			}
		}
		return true
	}

	// ---------- Vorwärts-Scan: von der Startsituation erreichbare Varianten ----------
	{
		if info != nil && !info(fmt.Sprintf("deadlock scan room %d: forward", room.Index)) {
			return 0, false
		}
		// alle Zustände, die durch von außen reingeschobene Kisten entstehen können
		maskStates := buildMaskStates(len(room.Incoming), stateCount, func(portal int, state uint64) uint64 {
			return room.Incoming[portal].GetBoxSwap(state)
		})

		seen := make([]bool, stateCount)
		var stack []uint64
		visit := func(state uint64) {
			if !seen[state] {
				seen[state] = true
				stack = append(stack, state)
			}
		}

		if room.StartVariantCount > 0 {
			for id := uint64(0); id < room.StartVariantCount; id++ {
				v := room.Variants.Get(id)
				if v.PlayerPortal == NoPortal {
					if endValid(v) {
						usedForward[id] = true
					}
					continue
				}
				usedForward[id] = true
				visit(v.NewState)
			}
		} else {
			visit(room.StartState)
		}

		for len(stack) > 0 {
			state := stack[len(stack)-1]
			stack = stack[:len(stack)-1]
			for _, s := range maskStates[state] {
				for _, ip := range room.Incoming {
					span := ip.GetVariantSpan(s)
					for id := span.Start; id < span.Start+span.Count; id++ {
						if usedForward[id] {
							continue
						}
						v := room.Variants.Get(id)
						if v.PlayerPortal == NoPortal {
							if endValid(v) {
								usedForward[id] = true
							}
							continue
						}
						usedForward[id] = true
						visit(v.NewState)
					}
				}
			}
		}
	}

	// ---------- Rückwärts-Scan: Varianten, die zum lokalen Ende führen können ----------
	{
		if info != nil && !info(fmt.Sprintf("deadlock scan room %d: backward", room.Index)) {
			return 0, false
		}
		// Pull-Swaps: Umkehrung der BoxSwaps je Portal (Kiste wieder rausziehen)
		pullSwaps := make([]map[uint64]uint64, len(room.Incoming))
		for i, ip := range room.Incoming {
			pull := make(map[uint64]uint64, len(ip.BoxSwap))
			for from, to := range ip.BoxSwap {
				pull[to] = from
			}
			pullSwaps[i] = pull
		}
		maskStates := buildMaskStates(len(room.Incoming), stateCount, func(portal int, state uint64) uint64 {
			if next, exists := pullSwaps[portal][state]; exists {
				return next
			}
			return state
		})

		// Rückwärts-Verzeichnis: (Austritts-Portal, Endzustand) -> Varianten samt
		// Eintritts-Portal; Gruppe 0 = End-Varianten (werden nicht expandiert)
		type revVariant struct {
			id    uint64
			entry uint32 // eingehendes Portal (NoPortal = Startvariante)
		}
		revSlot := func(exit uint32, state uint64) uint64 {
			slot := uint64(0)
			if exit != NoPortal {
				slot = uint64(exit) + 1
			}
			return slot*stateCount + state
		}
		revMap := make([][]revVariant, (uint64(len(room.Incoming))+1)*stateCount)
		for id := uint64(0); id < room.StartVariantCount; id++ {
			v := room.Variants.Get(id)
			slot := revSlot(v.PlayerPortal, v.NewState)
			revMap[slot] = append(revMap[slot], revVariant{id: id, entry: NoPortal})
		}
		for _, ip := range room.Incoming {
			for _, span := range ip.VariantSpans {
				for id := span.Start; id < span.Start+span.Count; id++ {
					v := room.Variants.Get(id)
					slot := revSlot(v.PlayerPortal, v.NewState)
					revMap[slot] = append(revMap[slot], revVariant{id: id, entry: ip.Index})
				}
			}
		}

		seen := make([]bool, stateCount)
		var stack []uint64
		visit := func(state uint64) {
			if !seen[state] {
				seen[state] = true
				stack = append(stack, state)
			}
		}

		// Seeds: alle Varianten, die den gelösten Raum-Zustand erreichen; vorwärts
		// unerreichbare zählen als Ende, werden aber nicht weiter zurückverfolgt
		// (sie fliegen ohnehin über die Schnittmenge raus)
		for id := uint64(0); id < variantCount; id++ {
			v := room.Variants.Get(id)
			if v.NewState != 0 {
				continue
			}
			usedBackward[id] = true
			if usedForward[id] {
				visit(v.OldState)
			}
		}
		// dazu Zustände, aus denen eine von außen reingeschobene Kiste das Ende macht
		for i := range room.Incoming {
			if from, exists := pullSwaps[i][0]; exists {
				visit(from)
			}
		}

		for len(stack) > 0 {
			state := stack[len(stack)-1]
			stack = stack[:len(stack)-1]
			for _, s := range maskStates[state] {
				for exit := range room.Incoming {
					for _, rv := range revMap[revSlot(uint32(exit), s)] {
						if usedBackward[rv.id] {
							continue
						}
						usedBackward[rv.id] = true
						if rv.entry == NoPortal {
							continue // Startvariante: hat keinen Vorgänger
						}
						visit(room.Variants.Get(rv.id).OldState)
					}
				}
			}
		}
	}

	// ---------- Schnittmenge anwenden ----------
	used := make([]bool, variantCount)
	for id := range used {
		used[id] = usedForward[id] && usedBackward[id]
		if !used[id] {
			removed++
		}
	}
	if removed == 0 {
		return 0, true
	}
	if info != nil && !info(fmt.Sprintf("deadlock scan room %d: remove %d variants", room.Index, removed)) {
		return 0, false
	}
	renewVariants(room, used)
	removeUnusedStates(room)
	return removed, true
}

// buildMaskStates liefert je Ausgangszustand alle Zustände, die durch eine
// beliebige Portal-Teilmenge von Kisten-Zustandswechseln entstehen können
// (inklusive der leeren Teilmenge = Zustand selbst). Teilmengen werden wie im
// C#-Original in fester Portal-Reihenfolge angewendet - für Kisten-Mengen ist
// die Reihenfolge egal, nur bei Lücken in den Zwischen-Zuständen könnte eine
// andere Reihenfolge theoretisch mehr finden (Parität zum Original).
// swap liefert den Folgezustand (unverändert = Wechsel nicht möglich).
func buildMaskStates(portalCount int, stateCount uint64, swap func(portal int, state uint64) uint64) [][]uint64 {
	result := make([][]uint64, stateCount)
	maskEnd := uint64(1) << portalCount
	for state := uint64(0); state < stateCount; state++ {
		seen := map[uint64]bool{}
		var list []uint64
		for mask := uint64(0); mask < maskEnd; mask++ {
			s := state
			valid := true
			for p := 0; p < portalCount; p++ {
				if mask&(uint64(1)<<p) == 0 {
					continue
				}
				next := swap(p, s)
				if next == s {
					valid = false
					break
				}
				s = next
			}
			if !valid || seen[s] {
				continue
			}
			seen[s] = true
			list = append(list, s)
		}
		result[state] = list
	}
	return result
}

// OptimizeRooms führt den Deadlock-Scan auf einer Raum-Auswahl aus (M4) und
// validiert danach das Netzwerk; liefert die Zahl der entfernten Varianten
func (n *Network) OptimizeRooms(indices []uint32, info func(string) bool) (removed uint64, err error) {
	for _, idx := range indices {
		if int(idx) >= len(n.Rooms) {
			return removed, fmt.Errorf("optimize: invalid room index %d", idx)
		}
		count, scanOK := n.DeadlockScan(n.Rooms[idx], info)
		if !scanOK {
			return removed, nil // abgebrochen, restliche Räume bleiben unangetastet
		}
		removed += count
	}
	if err := n.Validate(true); err != nil {
		return removed, fmt.Errorf("validate after optimize: %w", err)
	}
	return removed, nil
}
