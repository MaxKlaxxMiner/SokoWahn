package rooms

import (
	"fmt"
	"slices"

	"goSokoWahnRooms/soko"
)

// ResetRooms setzt gemergte Räume auf ihre 1-Feld-Start-Räume zurück, wie
// frisch initialisiert (Entf-Taste in der GUI): für Fehlgriffe beim Mergen,
// ohne das ganze Level neu laden zu müssen. Möglich, weil Portale immer
// paarweise je Feldkante existieren und Merges sie erhalten - die Nachbarn
// samt ihrer Optimierungen bleiben unberührt (Dominanz/Deadlock-Beweise sind
// raum-lokal, Budget-Streichungen sind Aussagen über das Level selbst und
// bleiben auch nach dem Zurücksetzen wahr). 1-Feld-Räume der Auswahl werden
// übersprungen. Liefert die Anzahl der zurückgesetzten Räume; bei Fehlern
// vor dem Umbau bleibt das Netzwerk unverändert.
func (n *Network) ResetRooms(indices []uint32, info ProgressFunc) (int, error) {
	f, scan := n.Field, n.Scan
	eof := f.WalkEof()

	// Auswahl deduplizieren; nur Mehr-Feld-Räume sind zurückzusetzen
	resetRooms := map[*Room]bool{}
	var active []*Room // für die Fortschritts-Markierung (deterministische Reihenfolge)
	for _, idx := range indices {
		if int(idx) >= len(n.Rooms) {
			return 0, fmt.Errorf("reset: invalid room index %d", idx)
		}
		room := n.Rooms[idx]
		if len(room.Fields) > 1 && !resetRooms[room] {
			resetRooms[room] = true
			active = append(active, room)
		}
	}
	if len(resetRooms) == 0 {
		return 0, nil
	}

	// alle betroffenen Felder aufsteigend - gleiche Reihenfolge wie NewNetwork,
	// damit die neuen Räume identisch zur Erst-Initialisierung entstehen
	var fields []soko.Wpos
	for _, room := range active {
		fields = append(fields, room.Fields...)
	}
	slices.Sort(fields)
	if info != nil {
		info(fmt.Sprintf("reset: %d räume -> %d ein-feld-räume", len(active), len(fields)), active)
	}

	// --- neue 1-Feld-Räume anlegen (wie NewNetwork) ---
	maxBoxes := uint32(f.BoxCount())
	newRoomOf := make(map[soko.Wpos]*Room, len(fields))
	for _, pos := range fields {
		room := &Room{
			Fields:   []soko.Wpos{pos},
			MaxBoxes: maxBoxes,
			States:   NewStateList(),
			Variants: NewVariantList(),
			Paths:    NewPathStore(),
		}
		if f.IsGoal(pos) {
			room.Goals = room.Fields
		}
		for _, box := range f.InitBoxes() {
			if box == pos {
				room.StartBoxes = room.Fields
			}
		}
		newRoomOf[pos] = room
	}

	// Feld -> bestehender Raum (für die Nachbar-Zuordnung an den Außenkanten)
	roomOf := make([]*Room, eof)
	for _, room := range n.Rooms {
		for _, p := range room.Fields {
			roomOf[p] = room
		}
	}

	// --- eingehende Portale (Reihenfolge wie NewNetwork: links, rechts, oben, unten) ---
	for _, pos := range fields {
		room := newRoomOf[pos]
		for _, side := range soko.Dirs {
			from := f.Neighbor(pos, side)
			if from >= eof {
				continue // Wand/Void: kein Portal
			}
			dirIn := soko.OppositeDir(side)
			farther := f.Neighbor(pos, dirIn)
			fromRoom := newRoomOf[from]
			if fromRoom == nil {
				fromRoom = roomOf[from]
			}
			room.Incoming = append(room.Incoming, &Portal{
				From:         from,
				To:           pos,
				FromRoom:     fromRoom,
				ToRoom:       room,
				Index:        uint32(len(room.Incoming)),
				Dir:          dirIn,
				BlockedBox:   !scan.HasPush(from, pos) || !scan.HasPush(pos, farther),
				BoxSwap:      map[uint64]uint64{},
				VariantSpans: map[uint64]Span{},
			})
		}
	}

	// --- Outgoing/Opposite verlinken: innere Kanten unter den neuen Räumen,
	// Außenkanten übernehmen das bestehende Portal des Nachbarn (dessen
	// Umverdrahtung passiert erst im Commit) ---
	type boundary struct {
		ip *Portal // neues eingehendes Portal (Nachbarfeld -> neues Feld)
		op *Portal // bestehendes eingehendes Portal des Nachbarn (neues Feld -> Nachbarfeld)
	}
	var bounds []boundary
	for _, pos := range fields {
		room := newRoomOf[pos]
		room.Outgoing = make([]*Portal, len(room.Incoming))
		for i, ip := range room.Incoming {
			if twin := newRoomOf[ip.From]; twin != nil {
				for _, p := range twin.Incoming {
					if p.From == pos {
						room.Outgoing[i] = p
						ip.Opposite = p
						break
					}
				}
			} else {
				// ein gemergter Nachbar kann an mehreren Kanten angrenzen -
				// das Gegenportal ist über das Feld-Paar eindeutig
				for _, p := range roomOf[ip.From].Incoming {
					if p.From == pos && p.To == ip.From {
						room.Outgoing[i] = p
						ip.Opposite = p
						bounds = append(bounds, boundary{ip: ip, op: p})
						break
					}
				}
			}
			if room.Outgoing[i] == nil {
				return 0, fmt.Errorf("reset: opposite portal not found: %v", ip)
			}
		}
	}

	// --- Zustände und Varianten (identisch zur Erst-Initialisierung; bis hier
	// ist das Netzwerk unangetastet, ein Fehler lässt alles unverändert) ---
	for _, pos := range fields {
		if err := newRoomOf[pos].initStates(f, scan); err != nil {
			return 0, err
		}
	}
	for _, pos := range fields {
		newRoomOf[pos].initVariants(f, scan)
	}

	// --- Commit: Nachbar-Portale umhängen und die Raum-Liste ersetzen ---
	for _, b := range bounds {
		nb := b.op.ToRoom
		b.op.FromRoom = b.ip.ToRoom
		b.op.Opposite = b.ip
		nb.Outgoing[b.op.Index] = b.ip
	}
	newList := make([]*Room, 0, len(n.Rooms)+len(fields)-len(resetRooms))
	for _, room := range n.Rooms {
		if resetRooms[room] {
			for _, p := range room.Fields {
				newList = append(newList, newRoomOf[p])
			}
		} else {
			newList = append(newList, room)
		}
	}
	for i, room := range newList {
		room.Index = uint32(i)
	}
	n.Rooms = newList

	// Struktur komplett, Varianten nur von den neuen 1-Feld-Räumen
	// (die übrigen Räume sind unangetastet, siehe ValidateRooms)
	created := make([]*Room, len(fields))
	for i, pos := range fields {
		created[i] = newRoomOf[pos]
	}
	if err := n.ValidateRooms(created...); err != nil {
		return 0, fmt.Errorf("validate after reset: %w", err)
	}
	n.warmMinMoves() // Caches vorwärmen (lesende API-Zugriffe bleiben race-frei)
	return len(resetRooms), nil
}
