package rooms

import (
	"fmt"

	"goSokoWahnRooms/tools"
)

// Budget-Schnellscan (Max' Idee, 2026-08-19): entfernt in ALLEN Räumen die
// Varianten, deren billigste denkbare Nutzung das Raum-Budget überschreitet.
// Im Gegensatz zur Dominanzsuche funktioniert das für jeden Raum (auch
// Mehr-Portal und Startvarianten), weil nur der Zustands-Dijkstra gebraucht
// wird: fwd[OldState] + Varianten-Kosten + bwd[NewState] ist eine sichere
// Untergrenze jeder Nutzung, die die Variante enthält (Distanz-Korridor,
// dieselbe Idee wie Brutes Tiefenschranke). Liegt sie über
// Minimum + Slack, kann die Variante in keiner Lösung innerhalb der
// Schranke vorkommen. Unerreichbare Varianten (Distanz unendlich) fallen
// dabei gratis mit.
//
// Läuft als Fixpunkt-Iteration: Streichungen heben die Raum-Minima an,
// der Slack schrumpft, die Limits der anderen Räume werden schärfer.
// maxMoves muss eine VERIFIZIERTE obere Schranke sein (siehe OptimizeRooms).
func (n *Network) BudgetScan(maxMoves uint64, info ProgressFunc) (removed uint64, ok bool, err error) {
	for round := 1; ; round++ {
		total := uint64(0)
		for _, room := range n.Rooms {
			total += room.MinMoves()
		}
		if total > maxMoves {
			return removed, true, fmt.Errorf("max moves %s liegt unter dem bewiesenen Minimum %s - Schranke unerreichbar",
				tools.FormatInt(maxMoves), tools.FormatInt(total))
		}
		slack := maxMoves - total

		changed := false
		for _, room := range n.Rooms {
			count := room.Variants.Count()
			if count == 0 {
				continue
			}
			if info != nil && !info(fmt.Sprintf("budget scan round %d: room %d (%s variants, limit %s)",
				round, room.Index, tools.FormatInt(count), tools.FormatInt(room.MinMoves()+slack)), []*Room{room}) {
				return removed, false, nil
			}
			limit := room.MinMoves() + slack
			fwd := room.stateDistances(room.StartState, false)
			bwd := room.stateDistances(0, true)

			used := make([]bool, count)
			cut := uint64(0)
			for vid := uint64(0); vid < count; vid++ {
				v := room.Variants.Get(vid)
				f, b := fwd[v.OldState], bwd[v.NewState]
				if f == minMovesInf || b == minMovesInf {
					cut++ // im über-approximierten Graphen unerreichbar: sicher tot
					continue
				}
				// gepackte Schlüssel addieren (Pushes können nicht in die
				// Moves überlaufen: beide weit unter 2^32)
				if (f+(uint64(v.Moves)<<32|uint64(v.Pushes))+b)>>32 > limit {
					cut++
					continue
				}
				used[vid] = true
			}
			if cut == 0 {
				continue
			}
			if info != nil {
				info(fmt.Sprintf("budget scan round %d: room %d - remove %s variants",
					round, room.Index, tools.FormatInt(cut)), []*Room{room})
			}
			hadStarts := room.StartVariantCount > 0
			renewVariants(room, used)
			removeUnusedStates(room)
			removed += cut
			changed = true

			// Unlösbarkeits-Nachweis: verliert ein Raum alle nötigen
			// Varianten (Startraum ohne Startvarianten bzw. gelöster
			// Zustand unerreichbar), ist die Schranke BEWIESEN zu klein -
			// sauber melden statt später am Validate zu scheitern.
			// Achtung: die bisherigen Streichungen sind bereits angewandt,
			// unter der (falschen) Schranke war das Level ohnehin unlösbar.
			dead := hadStarts && room.StartVariantCount == 0
			if !dead && room.StartState != 0 {
				dead = room.stateDistances(room.StartState, false)[0] == minMovesInf
			}
			if dead {
				return removed, true, fmt.Errorf(
					"budget scan: Raum %d unter der Schranke unlösbar - max moves %s ist bewiesen zu klein (Level neu laden)",
					room.Index, tools.FormatInt(maxMoves))
			}
		}
		if !changed {
			return removed, true, nil
		}
	}
}
