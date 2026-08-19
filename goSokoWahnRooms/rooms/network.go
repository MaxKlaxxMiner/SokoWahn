package rooms

import (
	"fmt"

	"goSokoWahnRooms/soko"
)

// auf Räumen basierendes Netzwerk eines Spielfeldes
type Network struct {
	Field *soko.Field
	Rooms []*Room
	Scan  *BoxScan // Single-Box-Scan (bleibt für Debug/Anzeige erhalten)
}

// erstellt ein neues Netzwerk aus 1-Feld-Räumen (C#-Vorbild: RoomNetwork-Konstruktor)
func NewNetwork(f *soko.Field) (*Network, error) {
	if len(f.Goals()) != len(f.InitBoxes()) {
		return nil, fmt.Errorf("goal count != box count (%d != %d)", len(f.Goals()), len(f.InitBoxes()))
	}

	scan := ScanSingleBoxPushes(f)
	eof := f.WalkEof()
	n := &Network{Field: f, Scan: scan}

	// --- Räume erstellen: jedes begehbare Feld wird ein eigener Raum ---
	maxBoxes := uint32(f.BoxCount())
	n.Rooms = make([]*Room, eof)
	for pos := soko.Wpos(0); pos < eof; pos++ {
		room := &Room{
			Index:    uint32(pos),
			Fields:   []soko.Wpos{pos},
			MaxBoxes: maxBoxes,
			States:   NewStateList(),
			Variants: NewVariantList(),
		}
		if f.IsGoal(pos) {
			room.Goals = room.Fields
		}
		for _, box := range f.InitBoxes() {
			if box == pos {
				room.StartBoxes = room.Fields
			}
		}
		n.Rooms[pos] = room
	}

	// --- eingehende Portale erstellen (Reihenfolge: von links, rechts, oben, unten) ---
	for pos := soko.Wpos(0); pos < eof; pos++ {
		room := n.Rooms[pos]
		for _, side := range soko.Dirs {
			from := f.Neighbor(pos, side)
			if from >= eof {
				continue // Wand/Void: kein Portal
			}
			dirIn := soko.OppositeDir(side) // Portal zeigt vom Nachbarn zu uns

			// durchgeschobene Kiste kommt nicht an bzw. steckt danach fest?
			farther := f.Neighbor(pos, dirIn) // Feld hinter uns in Schieberichtung
			blockedBox := !scan.HasPush(from, pos) || !scan.HasPush(pos, farther)

			room.Incoming = append(room.Incoming, &Portal{
				From:         from,
				To:           pos,
				FromRoom:     n.Rooms[from],
				ToRoom:       room,
				Index:        uint32(len(room.Incoming)),
				Dir:          dirIn,
				BlockedBox:   blockedBox,
				BoxSwap:      map[uint64]uint64{},
				VariantSpans: map[uint64]Span{},
			})
		}
	}

	// --- ausgehende Portale setzen und Gegenportale verlinken ---
	for _, room := range n.Rooms {
		room.Outgoing = make([]*Portal, len(room.Incoming))
		for i, ip := range room.Incoming {
			for _, p := range ip.FromRoom.Incoming {
				if p.From == ip.To {
					room.Outgoing[i] = p
					ip.Opposite = p
					break
				}
			}
			if room.Outgoing[i] == nil {
				return nil, fmt.Errorf("opposite portal not found: %v", ip)
			}
		}
	}

	// --- Zustände und Varianten erstellen ---
	for _, room := range n.Rooms {
		if err := room.initStates(f, scan); err != nil {
			return nil, err
		}
	}
	for _, room := range n.Rooms {
		room.initVariants(f, scan)
	}

	// --- zum Schluss alles prüfen ---
	if err := n.Validate(true); err != nil {
		return nil, err
	}

	n.warmMinMoves() // Caches vorwärmen (lesende API-Zugriffe bleiben race-frei)
	return n, nil
}
