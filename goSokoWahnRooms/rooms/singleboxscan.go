package rooms

import (
	"goSokoWahnRooms/crc64"
	"goSokoWahnRooms/soko"
)

// Ergebnis des Single-Box-Scans: alle Kistenschübe (From -> To), die auf dem Weg
// einer einzelnen Kiste von einer Startposition zu irgendeinem Ziel liegen können.
// Nachbau von SokoBoxScanner.ScanSingleBoxPushes aus dem C#-Original:
// Rückwärts-Pull-Suche von allen Zielstellungen, dann Vorwärts-Push-Suche ab den
// Startpositionen; nur Stellungen, die in beiden Suchen vorkommen, liefern Schübe.
type BoxScan struct {
	pushes map[uint64]bool // Schlüssel: From<<32 | To
	onPath []bool          // Feld kommt als From oder To eines gültigen Schubs vor
}

func pushKey(from, to soko.Wpos) uint64 {
	return uint64(from)<<32 | uint64(to)
}

// gibt an, ob der Kistenschub From -> To grundsätzlich möglich ist
func (b *BoxScan) HasPush(from, to soko.Wpos) bool {
	return b.pushes[pushKey(from, to)]
}

// gibt an, ob auf dem Feld theoretisch eine Kiste stehen kann
// (Feld kommt als From oder To eines gültigen Schubs vor)
func (b *BoxScan) OnBoxPath(p soko.Wpos) bool {
	return b.onPath[p]
}

// scannt das Spielfeld mit einer einzelnen Kiste
func ScanSingleBoxPushes(f *soko.Field) *BoxScan {
	f1 := f.CloneWithBoxCount(1)
	buf := f1.MakeStateBuffer(16)

	// --- Rückwärts-Pull-Suche von allen Zielstellungen (Kiste auf Ziel, Spieler daneben) ---
	backward := map[crc64.Value]bool{}
	var stack []soko.State
	pushState := func(stack []soko.State, s *soko.State) []soko.State {
		boxes := make([]soko.Wpos, len(s.Boxes))
		copy(boxes, s.Boxes)
		return append(stack, soko.State{Player: s.Player, Boxes: boxes, Crc: s.Crc})
	}

	// Zielstellungen im Push-Ergebnis-Format: Kiste auf dem Ziel, Spieler direkt
	// dahinter (dort stand die Kiste vorher), das Feld hinter dem Spieler begehbar
	// (dort stand er beim Schieben). Anders als soko.SearchGoalStates werden auch
	// Stellungen ohne weitere Rückwärts-Vorgänger aufgenommen - sonst gingen
	// 1-Schub-Enden verloren (forwardOnly-Sonderfall im Brute-Solver).
	for _, goal := range f.Goals() {
		for _, dir := range soko.Dirs {
			player := f.Neighbor(goal, dir)
			if player >= f.WalkEof() {
				continue
			}
			if f.Neighbor(player, dir) >= f.WalkEof() {
				continue // Spieler hätte beim letzten Schub in der Wand gestanden
			}
			start := soko.State{Player: player, Boxes: []soko.Wpos{goal}}
			start.UpdateCrc()
			stack = pushState(stack, &start)
		}
	}

	for len(stack) > 0 {
		check := stack[len(stack)-1]
		stack = stack[:len(stack)-1]
		if backward[check.Crc] {
			continue
		}
		backward[check.Crc] = true
		f1.SetState(&check)
		buf = f1.SearchVariantsBackward(buf[:0])
		for i := range buf {
			if !backward[buf[i].Crc] {
				stack = pushState(stack, &buf[i])
			}
		}
	}

	// --- Vorwärts-Push-Suche ab den Start-Kistenpositionen ---
	scan := &BoxScan{
		pushes: map[uint64]bool{},
		onPath: make([]bool, f.WalkEof()),
	}
	forward := map[crc64.Value]bool{}
	for _, box := range f.InitBoxes() {
		start := soko.State{Player: f.InitPlayer(), Boxes: []soko.Wpos{box}}
		start.UpdateCrc()
		stack = pushState(stack, &start)
	}

	for len(stack) > 0 {
		check := stack[len(stack)-1]
		stack = stack[:len(stack)-1]
		if forward[check.Crc] {
			continue
		}
		forward[check.Crc] = true
		boxFrom := check.Boxes[0]
		f1.SetState(&check)
		buf = f1.SearchVariantsForward(buf[:0])
		for i := range buf {
			if !backward[buf[i].Crc] {
				continue // Stellung kann nie mehr ein Ziel erreichen
			}
			boxTo := buf[i].Boxes[0]
			key := pushKey(boxFrom, boxTo)
			if !scan.pushes[key] {
				scan.pushes[key] = true
				scan.onPath[boxFrom] = true
				scan.onPath[boxTo] = true
			}
			if !forward[buf[i].Crc] {
				stack = pushState(stack, &buf[i])
			}
		}
	}

	return scan
}
