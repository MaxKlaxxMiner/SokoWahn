package rooms

import (
	"fmt"
	"slices"

	"goSokoWahnRooms/tools"
)

// Deadlock-Scan (M4): entfernt Varianten eines Raumes, die von der Startsituation
// aus nie erreichbar sind (Vorwärts-Scan) oder nie zu einem lokalen Spielende
// führen können (Rückwärts-Scan), samt dabei verwaisender Zustände.
// C#-Vorbild: RoomDeadlockScanner + RoomReverse. Bewusste Abweichungen:
//   - Varianten werden einzeln statt Span-weise markiert (das C# konnte beim
//     Masken-Zweig bereits markierte Varianten doppelt zählen).
//   - der Ziele-Check für End-Varianten gilt auch für End-Startvarianten.
//   - Aufgaben werden dedupliziert (das C# erzeugte je markierter Variante eine).
//
// Die Selbes-Portal-Regel des Originals (Wiedereintritt durchs Austritts-Portal
// nur nach rausgeschobener Kiste, dort noch mit "todo: bug?" markiert) ist
// übernommen - sie ist beweisbar korrekt und die Hauptquelle der Ausdünnung,
// Begründung siehe Kommentar im Vorwärts-Scan.
//
// Beide Scans sind Über-Approximationen der echten Erreichbarkeit (die Außenwelt
// wird nur über die BoxSwap-Masken angenähert) - entfernt wird nur, was selbst
// unter diesen großzügigen Annahmen nie vorkommen kann bzw. immer von einer
// kürzeren Spielweise dominiert wird (Zugoptimalität bleibt erhalten).
//
// info (optional) bekommt Fortschritts-Meldungen; Rückgabe false bricht ab,
// der Raum bleibt dann unverändert (die Umbauten passieren erst ganz am Ende).
func (n *Network) DeadlockScan(room *Room, info ProgressFunc) (removed uint64, ok bool) {
	variantCount := room.Variants.Count()
	if variantCount == 0 {
		return 0, true
	}
	stateCount := room.States.Count()
	usedForward := make([]bool, variantCount)
	usedBackward := make([]bool, variantCount)

	// Fortschritt/Stop der heißen Schleifen: bei Monster-Räumen (zig Millionen
	// Varianten) liefen beide Scan-Richtungen minutenlang stumm und ohne
	// Abbruch-Möglichkeit (Max' Befund 2026-08-21). Abbruch lässt den Raum
	// unverändert (die Umbauten passieren erst ganz am Ende).
	throttle := &progressThrottle{}
	aborted := false
	tick := func(phase string, done uint64) bool {
		if aborted {
			return false
		}
		if info == nil || !throttle.due() {
			return true
		}
		if !info(fmt.Sprintf("deadlock scan room %d: %s %s", room.Index, phase, tools.FormatInt(done)), []*Room{room}) {
			aborted = true
		}
		return !aborted
	}

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
		if info != nil && !info(fmt.Sprintf("deadlock scan room %d: forward", room.Index), []*Room{room}) {
			return 0, false
		}
		// Zustände, die durch von außen reingeschobene Kisten entstehen können
		// (ohne Identität - der unveränderte Zustand läuft über den Regel-Zweig)
		maskStates := buildMaskStates(len(room.Incoming), stateCount, false, func(portal int, state uint64) uint64 {
			return room.Incoming[portal].GetBoxSwap(state)
		}, func(done uint64) bool { return tick("forward masken", done) })
		if maskStates == nil {
			return 0, false
		}

		// Austritts-Situation: der Spieler hat den Raum über exitPortal verlassen
		// (NoPortal = er war noch nie drin); blockSame sperrt den Wiedereintritt
		// durchs Austritts-Portal (siehe Selbes-Portal-Regel unten)
		type fwdTask struct {
			exitPortal uint32
			blockSame  bool
			state      uint64
		}
		// Aufgaben-Dedup dicht statt als Map: der Schlüsselraum
		// Zustand x Portal-Slot (inkl. NoPortal) x blockSame ist kompakt
		seen := make([]bool, stateCount*uint64(len(room.Incoming)+1)*2)
		taskSlot := func(t fwdTask) uint64 {
			slot := uint64(0)
			if t.exitPortal != NoPortal {
				slot = uint64(t.exitPortal) + 1
			}
			slot = (slot*stateCount + t.state) * 2
			if t.blockSame {
				slot++
			}
			return slot
		}
		var stack []fwdTask
		visit := func(t fwdTask) {
			if slot := taskSlot(t); !seen[slot] {
				seen[slot] = true
				stack = append(stack, t)
			}
		}

		// markiert eine Variante als erreichbar und erzeugt ihre Austritts-Aufgabe;
		// enterPortal = Portal, über das der Besuch hereinkam (NoPortal = Startvariante)
		mark := func(id uint64, enterPortal uint32) {
			if usedForward[id] {
				return
			}
			v := room.Variants.Get(id)
			if v.PlayerPortal == NoPortal {
				if endValid(v) {
					usedForward[id] = true
				}
				return
			}
			usedForward[id] = true
			visit(fwdTask{
				exitPortal: v.PlayerPortal,
				blockSame:  enterPortal == v.PlayerPortal && len(v.BoxPortals) == 0,
				state:      v.NewState,
			})
		}

		// verarbeitet einen Varianten-Span in Tick-Blöcken: der Tick-Check
		// läuft je Block statt je Variante und bleibt damit aus der Hotloop
		// draußen (Max' Hinweis 2026-08-21); false = Nutzer-Stop
		var steps uint64 // besuchte Varianten (nur Fortschritts-Anzeige)
		nextTick := uint64(deadlockTickStep)
		markSpan := func(span Span, entry uint32) bool {
			for id, end := span.Start, span.Start+span.Count; id < end; {
				chunk := end - id
				if remain := nextTick - steps; chunk > remain {
					chunk = remain
				}
				for stop := id + chunk; id < stop; id++ {
					mark(id, entry)
				}
				steps += chunk
				if steps >= nextTick {
					nextTick += deadlockTickStep
					if !tick("forward", steps) {
						return false
					}
				}
			}
			return true
		}

		if room.StartVariantCount > 0 {
			for id := uint64(0); id < room.StartVariantCount; id++ {
				mark(id, NoPortal)
			}
		} else {
			visit(fwdTask{exitPortal: NoPortal, state: room.StartState})
		}

		for len(stack) > 0 {
			task := stack[len(stack)-1]
			stack = stack[:len(stack)-1]

			// Wiedereintritt bei unverändertem Zustand: alle Portale, außer der
			// Selbes-Portal-Regel. Die gilt NUR, wenn der Besuch durch dasselbe
			// Portal herein- UND hinauskam und nichts exportiert hat: dann war das
			// Portal-Außenfeld beim Eintritt frei (der Spieler stand darauf), beim
			// Austritt also auch (draußen bewegt sich nichts, solange er drin ist) -
			// der Besuch hat die Außenwelt komplett unverändert gelassen, und statt
			// raus- und wieder reinzulaufen kann der Spieler auf dem Portalfeld
			// stehen bleiben und dieselbe Fortsetzung 2 Züge billiger direkt spielen
			// (Außen-Aktionen sind vorziehbar). Bei Eintritt über ein ANDERES Portal
			// gilt das nicht: vor dem Austritts-Portal kann von früher eine Kiste
			// liegen, die der Austritts-Schritt schiebt (wird beim Nachbarraum
			// verbucht, hier unsichtbar) - die pauschale C#-Regel hatte hier ein
			// Loch und hätte zwingend nötige Wiedereintritte wegwerfen können.
			// Gleiches gilt für Startvarianten (Spieler startet im Raum): beim
			// ersten Austritt kann draußen noch die Start-Aufstellung liegen -
			// Startvarianten setzen blockSame daher nie (mark mit NoPortal).
			for _, ip := range room.Incoming {
				if ip.Index == task.exitPortal && task.blockSame {
					continue
				}
				if !markSpan(ip.GetVariantSpan(task.state), ip.Index) {
					return 0, false
				}
			}

			// Wiedereintritt nach reingeschobenen Kisten: die Außenwelt hat den Raum
			// verändert, hier ist jedes Portal erlaubt
			for _, s := range maskStates[task.state] {
				for _, ip := range room.Incoming {
					if !markSpan(ip.GetVariantSpan(s), ip.Index) {
						return 0, false
					}
				}
			}
		}
	}

	// ---------- Rückwärts-Scan: Varianten, die zum lokalen Ende führen können ----------
	{
		if info != nil && !info(fmt.Sprintf("deadlock scan room %d: backward", room.Index), []*Room{room}) {
			return 0, false
		}
		// Pull-Swaps: Umkehrung der BoxSwaps je Portal (Kiste wieder rausziehen)
		var steps uint64 // besuchte Einträge/Varianten (nur Fortschritts-Anzeige)
		pullSwaps := make([]map[uint64]uint64, len(room.Incoming))
		for i, ip := range room.Incoming {
			pull := make(map[uint64]uint64, len(ip.BoxSwap))
			for from, to := range ip.BoxSwap {
				if steps++; steps%deadlockTickStep == 0 && !tick("backward swaps", steps) {
					return 0, false
				}
				pull[to] = from
			}
			pullSwaps[i] = pull
		}
		maskStates := buildMaskStates(len(room.Incoming), stateCount, true, func(portal int, state uint64) uint64 {
			if next, exists := pullSwaps[portal][state]; exists {
				return next
			}
			return state
		}, func(done uint64) bool { return tick("backward masken", done) })
		if maskStates == nil {
			return 0, false
		}

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
		// Tick je Block statt je Variante (wie markSpan im Vorwärts-Scan);
		// steps ist durch die Swap-Schleife oben schon vorbelastet
		nextTick := steps + deadlockTickStep
		for _, ip := range room.Incoming {
			for _, span := range ip.VariantSpans {
				for id, end := span.Start, span.Start+span.Count; id < end; {
					chunk := end - id
					if remain := nextTick - steps; chunk > remain {
						chunk = remain
					}
					for stop := id + chunk; id < stop; id++ {
						v := room.Variants.Get(id)
						slot := revSlot(v.PlayerPortal, v.NewState)
						revMap[slot] = append(revMap[slot], revVariant{id: id, entry: ip.Index})
					}
					steps += chunk
					if steps >= nextTick {
						nextTick += deadlockTickStep
						if !tick("backward verzeichnis", steps) {
							return 0, false
						}
					}
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
					entries := revMap[revSlot(uint32(exit), s)]
					// Tick je Slot statt je Eintrag (hält die Hotloop frei)
					if steps += uint64(len(entries)); steps >= nextTick {
						for nextTick <= steps {
							nextTick += deadlockTickStep
						}
						if !tick("backward", steps) {
							return 0, false
						}
					}
					for _, rv := range entries {
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
	if info != nil && !info(fmt.Sprintf("deadlock scan room %d: remove %s variants", room.Index, tools.FormatInt(removed)), []*Room{room}) {
		return 0, false
	}
	renewVariants(room, used)
	removeUnusedStates(room)
	return removed, true
}

// Tick-Schrittweite der heißen Deadlock-Schleifen (Fortschritt/Stop-Check;
// die Zeitdrosselung übernimmt der tick-Callback selbst)
const deadlockTickStep = 1 << 16

// buildMaskStates liefert je Ausgangszustand alle Zustände, die durch eine
// beliebige Portal-Teilmenge von Kisten-Zustandswechseln entstehen können;
// includeIdentity nimmt die leere Teilmenge (= Zustand selbst) mit auf.
// Teilmengen werden wie im C#-Original in fester Portal-Reihenfolge angewendet -
// für Kisten-Mengen ist die Reihenfolge egal, nur bei Lücken in den
// Zwischen-Zuständen könnte eine andere Reihenfolge theoretisch mehr finden
// (Parität zum Original). swap liefert den Folgezustand (unverändert = Wechsel
// nicht möglich). tick (optional) meldet den Fortschritt über die Zustände;
// false = Nutzer-Stop, Ergebnis nil.
func buildMaskStates(portalCount int, stateCount uint64, includeIdentity bool, swap func(portal int, state uint64) uint64, tick func(done uint64) bool) [][]uint64 {
	result := make([][]uint64, stateCount)
	maskEnd := uint64(1) << portalCount
	maskStart := uint64(1)
	if includeIdentity {
		maskStart = 0
	}
	for state := uint64(0); state < stateCount; state++ {
		if tick != nil && state%deadlockTickStep == 0 && !tick(state) {
			return nil
		}
		var list []uint64
		for mask := maskStart; mask < maskEnd; mask++ {
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
			if !valid || slices.Contains(list, s) {
				continue // Dedup linear - die Liste hat höchstens 2^Portale Einträge
			}
			list = append(list, s)
		}
		result[state] = list
	}
	return result
}

// OptimizeRooms führt den Deadlock-Scan (M4) und die Dominanzsuche (M4b) auf
// einer Raum-Auswahl aus und validiert danach das Netzwerk; liefert die Zahl
// der entfernten Varianten. Reihenfolge: erst der billige Scan (räumt
// Unerreichbares weg), dann die Dominanz auf dem Rest.
// maxMoves > 0 ist eine VERIFIZIERTE obere Schranke der Gesamtlösung (z.B.
// die Länge einer bekannten Lösung): über die bewiesenen Pflicht-Minima
// aller Räume (Room.MinMoves) entsteht ein Slack, und jeder Raum darf dann
// höchstens Minimum + Slack kosten - teurere Nutzungen kappt die Dominanz.
// ACHTUNG: ein zu kleines maxMoves würde die Optimallösung wegwerfen; die
// Verantwortung für die Schranke liegt beim Aufrufer.
func (n *Network) OptimizeRooms(indices []uint32, maxMoves uint64, info ProgressFunc) (removed uint64, err error) {
	// Budget-Schnellscan über ALLE Räume (auch Mehr-Portal/Startvarianten,
	// die die Dominanz noch nicht abdeckt): streicht Varianten, deren
	// billigste denkbare Nutzung das Raum-Budget überschreitet
	if maxMoves > 0 {
		count, ok, scanErr := n.BudgetScan(maxMoves, info)
		removed += count
		if scanErr != nil || !ok {
			if removed > 0 {
				// die Scan-Diagnose ("Schranke bewiesen zu klein") hat
				// Vorrang - dann DARF das Netz unlösbar zurückbleiben
				if verr := n.Validate(true); verr != nil && scanErr == nil {
					scanErr = fmt.Errorf("validate after budget scan: %w", verr)
				}
				n.warmMinMoves()
			}
			return removed, scanErr
		}
	}

	// Budget-Zerlegung: Slack = Schranke minus Summe aller Raum-Minima
	slack := int64(0)
	if maxMoves > 0 {
		total := uint64(0)
		for _, room := range n.Rooms {
			total += room.MinMoves()
		}
		if total > maxMoves {
			return 0, fmt.Errorf("max moves %s liegt unter dem bewiesenen Minimum %s - Schranke unerreichbar", tools.FormatInt(maxMoves), tools.FormatInt(total))
		}
		slack = int64(maxMoves - total)
		if info != nil && !info(fmt.Sprintf("move budget: minimum %s, slack %s", tools.FormatInt(total), tools.FormatInt(slack)), nil) {
			return 0, nil
		}
	}

	for _, idx := range indices {
		if int(idx) >= len(n.Rooms) {
			return removed, fmt.Errorf("optimize: invalid room index %d", idx)
		}
		count, scanOK := n.DeadlockScan(n.Rooms[idx], info)
		removed += count
		if scanOK {
			moveLimit := int64(0)
			if maxMoves > 0 {
				moveLimit = int64(n.Rooms[idx].MinMoves()) + slack
			}
			count, scanOK = n.DominanceReduce(n.Rooms[idx], moveLimit, info)
			removed += count
		}
		if !scanOK {
			break // abgebrochen: restliche Räume bleiben unangetastet
		}
	}
	// auch nach einem Abbruch validieren - bereits angewandte Streichungen
	// (Dominanz wendet Bewiesenes trotz Stop an) müssen konsistent sein.
	// Ohne Budget-Scan sind nachweislich nur die Auswahl-Räume angefasst
	// (gezielte Prüfung); der Scan dagegen ändert potenziell ALLE Räume
	if maxMoves > 0 {
		if err := n.Validate(true); err != nil {
			return removed, fmt.Errorf("validate after optimize: %w", err)
		}
	} else {
		touched := make([]*Room, 0, len(indices))
		for _, idx := range indices {
			touched = append(touched, n.Rooms[idx])
		}
		if err := n.ValidateRooms(touched...); err != nil {
			return removed, fmt.Errorf("validate after optimize: %w", err)
		}
	}
	n.warmMinMoves() // Caches vorwärmen (lesende API-Zugriffe bleiben race-frei)
	return removed, nil
}
