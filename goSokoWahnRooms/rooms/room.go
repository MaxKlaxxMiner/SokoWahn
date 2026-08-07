package rooms

import (
	"fmt"

	"goSokoWahnRooms/soko"
)

// Raum: Menge begehbarer Felder mit eigenen Zuständen und Varianten (Teil des Network)
type Room struct {
	Index uint32

	Fields     []soko.Wpos // Felder des Raumes, aufsteigend sortiert (Fields[0] = stabile Kennung)
	Goals      []soko.Wpos // Zielfelder des Raumes
	StartBoxes []soko.Wpos // Kisten, die am Spielstart im Raum stehen
	MaxBoxes   uint32      // maximale Kistenzahl, die theoretisch im Raum sein darf

	Incoming []*Portal // eingehende Portale (gehören diesem Raum)
	Outgoing []*Portal // ausgehende Portale = eingehende der Nachbarn (gleiche Reihenfolge)

	States     *StateList
	StartState uint64 // Zustand des Raumes am Spielstart

	Variants          *VariantList
	StartVariantCount uint64 // Startvarianten (nur > 0, wenn der Spieler in diesem Raum startet)
}

func (r *Room) String() string {
	return fmt.Sprintf("room[%d] fields=%v states=%d variants=%d", r.Index, r.Fields, r.States.Count(), r.Variants.Count())
}

// erstellt die Zustände eines 1-Feld-Raumes.
// Feldklassifikation über (Startkiste, Zielfeld): (false,false)=' ', (false,true)='.',
// (true,false)='$', (true,true)='*' - Spieler auf dem Feld ändert nichts an den Zuständen.
func (r *Room) initStates(f *soko.Field, scan *BoxScan) error {
	if len(r.Fields) != 1 {
		panic("initStates: only single-field rooms")
	}
	pos := r.Fields[0]
	isGoal := f.IsGoal(pos)
	hasBox := len(r.StartBoxes) == 1
	boxPossible := scan.OnBoxPath(pos) // darf hier theoretisch eine Kiste stehen?

	switch {
	case !hasBox && !isGoal: // leeres Feld (ggf. mit Spieler)
		r.States.Add(nil) // Zustand 0: leer = Endzustand
		r.StartState = 0
		if boxPossible {
			r.States.Add(r.Fields) // Zustand 1: Kiste
		}

	case !hasBox && isGoal: // leeres Zielfeld
		r.States.Add(r.Fields) // Zustand 0: Kiste auf Ziel = Endzustand
		r.States.Add(nil)      // Zustand 1: leeres Zielfeld
		r.StartState = 1

	case hasBox && !isGoal: // Kiste auf normalem Feld
		if f.IsCorner(pos) {
			return fmt.Errorf("invalid box in corner at %d,%d", f.FieldPos(pos)%f.Width(), f.FieldPos(pos)/f.Width())
		}
		r.States.Add(nil)      // Zustand 0: leer = Endzustand
		r.States.Add(r.Fields) // Zustand 1: Kiste
		r.StartState = 1

	default: // Kiste auf Zielfeld
		r.States.Add(r.Fields) // Zustand 0: Kiste auf Ziel = Endzustand
		r.StartState = 0
		if !f.IsCorner(pos) {
			r.States.Add(nil) // Zustand 1: leer (nur wenn die Kiste je rausgeschoben werden kann)
		}
	}

	return nil
}

// erstellt die Varianten eines 1-Feld-Raumes samt BoxSwaps und Portal-Verzeichnissen
func (r *Room) initVariants(f *soko.Field, scan *BoxScan) {
	if len(r.Fields) != 1 {
		panic("initVariants: only single-field rooms")
	}
	pos := r.Fields[0]
	isGoal := f.IsGoal(pos)
	eof := f.WalkEof()

	// --- Startvarianten (nur wenn der Spieler in diesem Raum startet) ---
	if pos == f.InitPlayer() {
		state := uint64(0)
		if isGoal {
			state = 1 // Spieler auf leerem Zielfeld: Startzustand ist 1 (leer)
		}
		for i := range r.Incoming {
			r.Variants.Add(VariantData{
				OldState:     state,
				NewState:     state,
				Moves:        1,
				Pushes:       0,
				PlayerPortal: uint32(i),
				Path:         string(rune(r.Outgoing[i].Dir)),
			})
			r.StartVariantCount++
		}
	}

	// --- BoxSwaps: Zustandswechsel, wenn eine Kiste reingeschoben wird ---
	// stateEmpty/stateBox: Zustand ohne/mit Kiste suchen (bei 1-Feld-Räumen 0 oder 1)
	stateEmpty, stateBox := int64(-1), int64(-1)
	for id := uint64(0); id < r.States.Count(); id++ {
		if r.States.BoxCount(id) == 0 {
			stateEmpty = int64(id)
		} else {
			stateBox = int64(id)
		}
	}
	for _, ip := range r.Incoming {
		if stateEmpty < 0 || stateBox < 0 {
			continue // Zustandswechsel mit neuer Kiste nicht möglich
		}
		if f.IsCorner(ip.From) {
			continue // Kiste könnte vorher nie auf dem Quellfeld stehen
		}
		if f.Neighbor(ip.From, soko.OppositeDir(ip.Dir)) >= eof {
			continue // Spieler stünde beim Schieben in der Wand
		}
		if !scan.HasPush(ip.From, ip.To) {
			continue // Schub laut Single-Box-Scan nicht möglich/sinnvoll
		}
		ip.AddBoxSwap(uint64(stateEmpty), uint64(stateBox))
	}

	// --- Portal-Varianten ---
	for ipIdx := range r.Incoming {
		ip := r.Incoming[ipIdx]

		// ausgehendes Portal für die gleichzeitig rausgeschobene Kiste suchen
		// (Spieler betritt den Raum und schiebt die Kiste in gleicher Richtung weiter)
		boxPortal := -1
		for oIdx, o := range r.Outgoing {
			if o.Dir != ip.Dir {
				continue
			}
			if f.IsCorner(o.To) && !f.IsGoal(o.To) {
				continue // Kiste würde in einer toten Ecke landen
			}
			if !scan.HasPush(pos, o.To) {
				continue // Schub laut Single-Box-Scan nicht möglich/sinnvoll
			}
			boxPortal = oIdx
		}

		if !isGoal {
			// Zustände: 0 = leer (Endzustand), 1 = Kiste (falls vorhanden)

			// durchlaufen (Zustand 0: leer)
			for oIdx := range r.Outgoing {
				if oIdx == ipIdx {
					continue // nicht zum gleichen Portal zurücklaufen
				}
				id := r.Variants.Add(VariantData{
					OldState:     0,
					NewState:     0,
					Moves:        1,
					Pushes:       0,
					PlayerPortal: uint32(oIdx),
					Path:         string(rune(r.Outgoing[oIdx].Dir)),
				})
				ip.AddVariant(0, id)
			}

			// Kiste beim Eintritt weiterschieben, dann durch ein beliebiges Portal austreten
			// (Zustand 1: Kiste; nur wenn der Zustand existiert - Abweichung vom C#,
			// das hier auch nicht existierende Zustände referenzieren konnte)
			if r.States.Count() > 1 && boxPortal >= 0 && !f.IsCorner(pos) {
				for oIdx, o := range r.Outgoing {
					// folgt der Spieler der Kiste, schiebt er sie beim Austritt zwangsläufig
					// weiter - würde sie dabei in einer toten Ecke (oder Wand) landen, ist
					// die Variante sinnlos
					if oIdx == boxPortal {
						checkPos := f.Neighbor(o.To, o.Dir)
						if f.IsCorner(checkPos) && !f.IsGoal(checkPos) {
							continue
						}
					}
					id := r.Variants.Add(VariantData{
						OldState:     1,
						NewState:     0,
						Moves:        1,
						Pushes:       1,
						BoxPortals:   []uint32{uint32(boxPortal)},
						PlayerPortal: uint32(oIdx),
						Path:         string(rune(r.Outgoing[oIdx].Dir)),
					})
					ip.AddVariant(1, id)
				}
			}

			// Kiste beim Eintritt auf ein Zielfeld im Nachbarraum schieben und stehen
			// bleiben = potenziell letzter Zug des Spiels
			if r.States.Count() > 1 && boxPortal >= 0 && !f.IsCorner(pos) && f.IsGoal(r.Outgoing[boxPortal].To) {
				id := r.Variants.Add(VariantData{
					OldState:     1,
					NewState:     0,
					Moves:        0,
					Pushes:       1,
					BoxPortals:   []uint32{uint32(boxPortal)},
					PlayerPortal: NoPortal,
					Path:         "",
				})
				ip.AddVariant(1, id)
			}
		} else {
			// Zustände: 0 = Kiste auf Ziel (Endzustand), 1 = leer (falls vorhanden)

			// Kiste beim Eintritt vom Zielfeld runterschieben (Zustand 0 -> 1)
			if r.States.Count() > 1 && boxPortal >= 0 && !f.IsCorner(pos) {
				for oIdx, o := range r.Outgoing {
					if oIdx == boxPortal {
						checkPos := f.Neighbor(o.To, o.Dir)
						if f.IsCorner(checkPos) && !f.IsGoal(checkPos) {
							continue
						}
					}
					id := r.Variants.Add(VariantData{
						OldState:     0,
						NewState:     1,
						Moves:        1,
						Pushes:       1,
						BoxPortals:   []uint32{uint32(boxPortal)},
						PlayerPortal: uint32(oIdx),
						Path:         string(rune(r.Outgoing[oIdx].Dir)),
					})
					ip.AddVariant(0, id)
				}
			}

			// durchlaufen (Zustand 1: leer; nur wenn der Zustand existiert -
			// bei '*' in einer Ecke kann der Spieler nie über das Feld laufen)
			if r.States.Count() > 1 {
				for oIdx := range r.Outgoing {
					if oIdx == ipIdx {
						continue
					}
					id := r.Variants.Add(VariantData{
						OldState:     1,
						NewState:     1,
						Moves:        1,
						Pushes:       0,
						PlayerPortal: uint32(oIdx),
						Path:         string(rune(r.Outgoing[oIdx].Dir)),
					})
					ip.AddVariant(1, id)
				}
			}
		}
	}
}
