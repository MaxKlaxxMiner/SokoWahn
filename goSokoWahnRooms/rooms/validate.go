package rooms

import (
	"fmt"
	"sort"
)

// prüft die Konsistenz aller Räume, Portale und Verlinkungen
// (C#-Vorbild: RoomNetwork.Validate); checkVariants prüft zusätzlich alle
// Zustände und Varianten (kann bei großen Netzwerken länger dauern)
func (n *Network) Validate(checkVariants bool) error {
	eof := n.Field.WalkEof()

	// --- Räume prüfen: Indizes, Feld-Zuordnung, Portal-Grundlagen, Startraum ---
	posToRoom := make([]*Room, eof)
	startRoom := -1
	fieldCount := 0
	for idx, room := range n.Rooms {
		if room == nil {
			return fmt.Errorf("room %d is nil", idx)
		}
		if int(room.Index) != idx {
			return fmt.Errorf("room index mismatch: %d != %d", room.Index, idx)
		}
		if len(room.Incoming) != len(room.Outgoing) {
			return fmt.Errorf("incoming/outgoing count mismatch: %v", room)
		}
		for _, pos := range room.Fields {
			if pos >= eof {
				return fmt.Errorf("invalid field %d: %v", pos, room)
			}
			if posToRoom[pos] != nil {
				return fmt.Errorf("field %d used by two rooms: %v and %v", pos, posToRoom[pos], room)
			}
			posToRoom[pos] = room
			fieldCount++
		}
		for i, p := range room.Incoming {
			if p == nil {
				return fmt.Errorf("incoming portal nil [%d]: %v", i, room)
			}
			if room.Outgoing[i] == nil {
				return fmt.Errorf("outgoing portal nil [%d]: %v", i, room)
			}
		}
		if room.StartVariantCount > 0 {
			if startRoom >= 0 {
				return fmt.Errorf("duplicate start rooms: %d and %d", startRoom, idx)
			}
			startRoom = idx
		}
	}
	if startRoom < 0 {
		return fmt.Errorf("no start room found")
	}
	if fieldCount != int(eof) {
		return fmt.Errorf("not all walkable fields covered: %d of %d", fieldCount, eof)
	}

	// --- eingehende Portale prüfen ---
	seenIncoming := map[*Portal]bool{}
	for _, room := range n.Rooms {
		for i, ip := range room.Incoming {
			if int(ip.Index) != i {
				return fmt.Errorf("portal index mismatch %d != %d: %v", ip.Index, i, ip)
			}
			if seenIncoming[ip] {
				return fmt.Errorf("portal used twice: %v", ip)
			}
			seenIncoming[ip] = true
			if ip.ToRoom != room {
				return fmt.Errorf("incoming portal [%d] does not link its own room: %v", i, room)
			}
			if int(ip.FromRoom.Index) >= len(n.Rooms) || n.Rooms[ip.FromRoom.Index] != ip.FromRoom {
				return fmt.Errorf("incoming portal [%d] has unknown source room: %v", i, room)
			}
			if posToRoom[ip.From] != ip.FromRoom {
				return fmt.Errorf("portal from-pos does not match from-room: %v", ip)
			}
			if posToRoom[ip.To] != ip.ToRoom {
				return fmt.Errorf("portal to-pos does not match to-room: %v", ip)
			}
		}
	}

	// --- ausgehende Portale inkl. Rückverweise prüfen ---
	seenOutgoing := map[*Portal]bool{}
	for _, room := range n.Rooms {
		for i, op := range room.Outgoing {
			if !seenIncoming[op] {
				return fmt.Errorf("outgoing portal not found in incoming portals: %v", op)
			}
			if seenOutgoing[op] {
				return fmt.Errorf("outgoing portal used twice: %v", op)
			}
			seenOutgoing[op] = true
			if op.Opposite != room.Incoming[i] {
				return fmt.Errorf("opposite portal mismatch: %v", op)
			}
			if op.Opposite.Opposite != op {
				return fmt.Errorf("double opposite portal mismatch: %v", op)
			}
		}
	}

	if !checkVariants {
		return nil
	}

	// --- Zustände und Varianten aller Räume prüfen ---
	for _, room := range n.Rooms {
		if err := validateRoomVariants(room); err != nil {
			return fmt.Errorf("room %d: %w", room.Index, err)
		}
	}

	return nil
}

// prüft Zustände, Varianten und deren Verzeichnisse eines einzelnen Raumes
func validateRoomVariants(room *Room) error {
	stateCount := room.States.Count()
	variantCount := room.Variants.Count()
	if stateCount < 1 {
		return fmt.Errorf("no states")
	}
	if room.StartState >= stateCount {
		return fmt.Errorf("invalid start state %d", room.StartState)
	}

	usedStates := make([]bool, stateCount)
	usedVariants := make([]bool, variantCount)
	usedStates[0] = true               // Endzustand zählt immer als benutzt
	usedStates[room.StartState] = true // Startzustand ebenfalls

	checkVariantData := func(id uint64) error {
		v := room.Variants.Get(id)
		if v.OldState >= stateCount || v.NewState >= stateCount {
			return fmt.Errorf("variant %d references invalid state", id)
		}
		usedStates[v.OldState] = true
		usedStates[v.NewState] = true
		if v.PlayerPortal != NoPortal && int(v.PlayerPortal) >= len(room.Outgoing) {
			return fmt.Errorf("variant %d references invalid player portal", id)
		}
		for _, bp := range v.BoxPortals {
			if int(bp) >= len(room.Outgoing) {
				return fmt.Errorf("variant %d references invalid box portal", id)
			}
		}
		if uint64(len(v.Path)) != uint64(v.Moves) {
			return fmt.Errorf("variant %d path length %d != moves %d", id, len(v.Path), v.Moves)
		}
		return nil
	}

	// --- Startvarianten: belegen die IDs 0..StartVariantCount-1, erst Moves, dann Pushes ---
	if room.StartVariantCount > variantCount {
		return fmt.Errorf("start variant count out of range")
	}
	pushSeen := false
	for id := uint64(0); id < room.StartVariantCount; id++ {
		usedVariants[id] = true
		if err := checkVariantData(id); err != nil {
			return err
		}
		if room.Variants.Get(id).Pushes == 0 {
			if pushSeen {
				return fmt.Errorf("start variants: push before move variant")
			}
		} else {
			pushSeen = true
		}
	}

	// --- Portal-Varianten: je (Portal, Zustand) lückenlos, insgesamt fortlaufend ---
	type spanInfo struct {
		state uint64
		span  Span
	}
	var spans []spanInfo
	for _, ip := range room.Incoming {
		for state, span := range ip.VariantSpans {
			if state >= stateCount {
				return fmt.Errorf("variant span for invalid state %d", state)
			}
			usedStates[state] = true
			spans = append(spans, spanInfo{state, span})

			pushSeen := false
			for id := span.Start; id < span.Start+span.Count; id++ {
				if id >= variantCount {
					return fmt.Errorf("variant span out of range: %d", id)
				}
				if usedVariants[id] {
					return fmt.Errorf("variant %d used twice", id)
				}
				usedVariants[id] = true
				if err := checkVariantData(id); err != nil {
					return err
				}
				v := room.Variants.Get(id)
				if v.OldState != state {
					return fmt.Errorf("variant %d old state %d != span state %d", id, v.OldState, state)
				}
				if v.Pushes == 0 {
					if pushSeen {
						return fmt.Errorf("portal variants: push before move variant")
					}
				} else {
					pushSeen = true
				}
			}
		}
	}
	sort.Slice(spans, func(i, j int) bool { return spans[i].span.Start < spans[j].span.Start })
	expected := room.StartVariantCount
	for _, si := range spans {
		if si.span.Start != expected {
			return fmt.Errorf("variant spans not contiguous: expected %d, got %d", expected, si.span.Start)
		}
		expected += si.span.Count
	}
	if expected != variantCount {
		return fmt.Errorf("not all variants covered by spans: %d of %d", expected, variantCount)
	}

	// --- BoxSwaps prüfen ---
	for _, ip := range room.Incoming {
		for oldState, newState := range ip.BoxSwap {
			if oldState >= stateCount || newState >= stateCount {
				return fmt.Errorf("box swap references invalid state")
			}
			if oldState == newState {
				return fmt.Errorf("useless box swap %d -> %d", oldState, newState)
			}
			diff := room.States.BoxCount(newState) - room.States.BoxCount(oldState)
			if diff != 1 && diff != -1 {
				return fmt.Errorf("box swap %d -> %d changes box count by %d", oldState, newState, diff)
			}
			usedStates[oldState] = true
			// newState wird bewusst nicht markiert (wie im C#: der Ziel-Zustand kann
			// aus Sicht der eigenen Varianten unerreichbar sein)
		}
	}

	// --- alles benutzt? ---
	for id, used := range usedStates {
		if !used {
			return fmt.Errorf("unused state %d", id)
		}
	}
	for id, used := range usedVariants {
		if !used {
			return fmt.Errorf("unused variant %d", id)
		}
	}

	return nil
}
