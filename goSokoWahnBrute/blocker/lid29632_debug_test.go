package blocker

// Regressionstest für den Bx-Hinterland-Bug (Level 29632, game-sokoban.com):
// Die alte, unbedingte Muster-Anwendung verwarf eine Stellung der optimalen
// 304-Züge-Lösung als Deadlock (4er-Hinterland-Muster, Spieler stand nach dem
// Schub einer FREMDEN Kiste in der Muster-Pose) - der Solver fand nur 306 Züge.
// Der Fehler steckte genauso im C#-Original (SokowahnBlockerBx).
//
// Dieser Test baut die Blocker-Stufen 1-4 frisch auf (Stufe 4 erzeugte das
// schuldige Muster), spielt die bekannte optimale Lösung Zug für Zug ab und
// prüft jede Nach-Schub-Stellung gegen CheckAllowed. Mit der bedingten
// Kill-Regel darf keine einzige Stellung mehr geblockt werden. Vorwärts- und
// Rückwärtssuche filtern mit demselben CheckAllowed auf denselben
// Nach-Schub-Stellungen, ein Durchlauf deckt also beide Richtungen ab.
//
// Schlägt der Test fehl, zeigt er Stufe, Musterfelder und ein ASCII-Bild der
// geblockten Stellung samt Teilspiel-Gegenprobe (lösbar = falsch-positiv).
//
// Der Test überspringt sich selbst, wenn die Lösungsdatei fehlt
// (solution-29632.txt im Repo-Root, verifizierte Bestmoves-Lösung von Max).

import (
	"os"
	"path/filepath"
	"sort"
	"strings"
	"testing"

	"goSokoWahnBrute/soko"
)

const lid29632Level = `########
#@    .##
#    #. #
# $$ #. #
#    #.$#
###$##$ #####
# .     $   #
#..# $ $$   #
#..#### ### #
# .    $    #
#  ##########
####`

// Blocker-Stufen, die der Test frisch aufbaut (Stufe 4 erzeugte das schuldige Muster)
const lid29632Stages = 4

func TestLid29632SolutionAgainstBlocker(t *testing.T) {
	if testing.Short() {
		t.Skip("Stufenbau bis 4-Steiner dauert einige Sekunden (übersprungen mit -short)")
	}

	moves, field := loadLid29632(t)

	// Blocker-Stufen 1 bis lid29632Stages frisch berechnen (ohne Cache)
	blk := New(field, "")
	for blk.Next(1000000000) {
		if len(blk.GetStats().Stages) >= lid29632Stages {
			blk.Abort()
			break
		}
	}
	logLid29632Stages(t, blk)
	replayLid29632Solution(t, blk, field, moves)
}

// Zusatz-Check gegen den echten Blocker-Cache im Repo-temp (beliebig viele Stufen):
// prüft, ob die bekannte optimale 304er-Lösung mit dem aktuell gebauten Cache-Stand
// durchkommt - damit lässt sich das Ergebnis eines laufenden Suchlaufs vorhersagen.
// Überspringt sich, wenn kein lesbarer v3-Cache vorliegt.
func TestLid29632SolutionAgainstTempCache(t *testing.T) {
	moves, field := loadLid29632(t)

	// Cache-Datei kopieren (die TUI könnte parallel schreiben)
	cacheSrc := filepath.Join("..", "..", "temp", CacheName(field))
	cacheData, err := os.ReadFile(cacheSrc)
	if err != nil {
		t.Skipf("Blocker-Cache nicht gefunden (%v) - Test übersprungen", err)
	}
	cachePath := filepath.Join(t.TempDir(), CacheName(field))
	if err := os.WriteFile(cachePath, cacheData, 0644); err != nil {
		t.Fatal(err)
	}

	blk := New(field, cachePath)
	if len(blk.stages) == 0 {
		t.Skipf("Cache %s nicht lesbar (alte Version oder unvollständig geschrieben) - Test übersprungen", cacheSrc)
	}
	blk.Abort()
	logLid29632Stages(t, blk)
	replayLid29632Solution(t, blk, field, moves)
}

// lädt Lösungsdatei und Spielfeld für die lid29632-Tests (skippt ohne Lösungsdatei)
func loadLid29632(t *testing.T) (moves string, field *soko.Field) {
	t.Helper()
	solutionPath := filepath.Join("..", "..", "solution-29632.txt")
	solutionData, err := os.ReadFile(solutionPath)
	if err != nil {
		t.Skipf("Lösungsdatei nicht gefunden (%v) - Test übersprungen", err)
	}
	field, err = soko.Parse(lid29632Level)
	if err != nil {
		t.Fatalf("Level lässt sich nicht parsen: %v", err)
	}
	return strings.TrimSpace(string(solutionData)), field
}

// gibt die Stufen-Übersicht eines Blockers aus
func logLid29632Stages(t *testing.T, blk *Blocker) {
	t.Helper()
	totalPatterns := 0
	for _, st := range blk.stages {
		count := 0
		for _, pat := range st.patterns {
			count += len(pat) / st.boxCount
		}
		totalPatterns += count
		t.Logf("Stufe %d: %d Muster (geprüft: %d)", st.boxCount, count, st.checkedStates)
	}
	t.Logf("gesamt: %d Stufen, %d Muster", len(blk.stages), totalPatterns)
}

// spielt die 304er-Lösung ab und prüft jede Nach-Schub-Stellung gegen den Blocker
func replayLid29632Solution(t *testing.T, blk *Blocker, field *soko.Field, moves string) {
	t.Helper()

	// --- eigenes Raster aufbauen und Wpos-Zuordnung des Parsers nachbilden ---
	lines := strings.Split(strings.TrimRight(soko.NormalizeLevel(lid29632Level), "\n"), "\n")
	width := 0
	for _, line := range lines {
		if len(line) > width {
			width = len(line)
		}
	}
	height := len(lines)
	grid := make([]byte, width*height)
	for i := range grid {
		grid[i] = ' '
	}
	playerPos := -1
	boxSet := map[int]bool{}
	goalSet := map[int]bool{}
	for y, line := range lines {
		for x := 0; x < len(line); x++ {
			pos := y*width + x
			switch line[x] {
			case '#':
				grid[pos] = '#'
			case '@':
				playerPos = pos
			case '+':
				playerPos = pos
				goalSet[pos] = true
			case '$':
				boxSet[pos] = true
			case '*':
				boxSet[pos] = true
				goalSet[pos] = true
			case '.':
				goalSet[pos] = true
			}
		}
	}
	if playerPos < 0 {
		t.Fatal("kein Spieler im Level gefunden")
	}

	// begehbare Felder per Flood-Fill vom Spieler aus (wie soko.Parse)
	walkable := make([]bool, width*height)
	stack := []int{playerPos}
	for len(stack) > 0 {
		pos := stack[len(stack)-1]
		stack = stack[:len(stack)-1]
		if walkable[pos] || grid[pos] == '#' {
			continue
		}
		walkable[pos] = true
		x, y := pos%width, pos/width
		if x > 0 {
			stack = append(stack, pos-1)
		}
		if x < width-1 {
			stack = append(stack, pos+1)
		}
		if y > 0 {
			stack = append(stack, pos-width)
		}
		if y < height-1 {
			stack = append(stack, pos+width)
		}
	}

	// Wpos-Nummern in Lesereihenfolge vergeben (wie soko.Parse)
	posToW := make([]int, width*height)
	walkCount := 0
	for pos := range grid {
		if walkable[pos] {
			posToW[pos] = walkCount
			walkCount++
		} else {
			posToW[pos] = -1
		}
	}

	// --- Zuordnung gegen das echte Feld validieren ---
	if walkCount != field.WalkCount() {
		t.Fatalf("WalkCount stimmt nicht: eigenes Raster %d, Parser %d", walkCount, field.WalkCount())
	}
	checkList := func(name string, mine map[int]bool, ref []soko.Wpos) {
		var mineW []int
		for pos := range mine {
			mineW = append(mineW, posToW[pos])
		}
		sort.Ints(mineW)
		if len(mineW) != len(ref) {
			t.Fatalf("%s: Anzahl stimmt nicht (%d vs %d)", name, len(mineW), len(ref))
		}
		for i := range ref {
			if mineW[i] != int(ref[i]) {
				t.Fatalf("%s[%d]: Wpos stimmt nicht (%d vs %d)", name, i, mineW[i], ref[i])
			}
		}
	}
	checkList("initBoxes", boxSet, field.InitBoxes())
	checkList("goals", goalSet, field.Goals())
	startState := soko.State{}
	field.GetState(&startState)
	if int(startState.Player) != posToW[playerPos] {
		t.Fatalf("Spieler-Wpos stimmt nicht (%d vs %d)", posToW[playerPos], startState.Player)
	}

	// --- Lösung abspielen und jede Nach-Schub-Stellung gegen den Blocker prüfen ---
	maskWords := (walkCount + 64) / 64
	blockedCount := 0
	pushCount := 0

	drawState := func(player int, patternFields map[int]bool) string {
		var sb strings.Builder
		for y := 0; y < height; y++ {
			for x := 0; x < width; x++ {
				pos := y*width + x
				c := byte(' ')
				switch {
				case grid[pos] == '#':
					c = '#'
				case patternFields[pos] && boxSet[pos]:
					c = 'X' // Musterfeld mit Kiste
				case boxSet[pos] && goalSet[pos]:
					c = '*'
				case boxSet[pos]:
					c = '$'
				case pos == player && goalSet[pos]:
					c = '+'
				case pos == player:
					c = '@'
				case goalSet[pos]:
					c = '.'
				}
				sb.WriteByte(c)
			}
			sb.WriteByte('\n')
		}
		return sb.String()
	}

	for i := 0; i < len(moves); i++ {
		c := moves[i]
		delta := 0
		switch c {
		case 'l', 'L':
			delta = -1
		case 'r', 'R':
			delta = 1
		case 'u', 'U':
			delta = -width
		case 'd', 'D':
			delta = width
		default:
			t.Fatalf("Zug %d: ungültiges Zeichen %q", i+1, c)
		}
		push := c >= 'A' && c <= 'Z'
		np := playerPos + delta

		if posToW[np] < 0 {
			t.Fatalf("Zug %d (%c): Zielfeld nicht begehbar", i+1, c)
		}
		if push {
			if !boxSet[np] {
				t.Fatalf("Zug %d (%c): keine Kiste zum Schieben auf dem Zielfeld", i+1, c)
			}
			bnp := np + delta
			if posToW[bnp] < 0 || boxSet[bnp] {
				t.Fatalf("Zug %d (%c): Kiste kann nicht geschoben werden", i+1, c)
			}
			delete(boxSet, np)
			boxSet[bnp] = true
			pushCount++
		} else if boxSet[np] {
			t.Fatalf("Zug %d (%c): Laufzug gegen eine Kiste", i+1, c)
		}
		playerPos = np

		if !push {
			continue
		}

		// Nach-Schub-Stellung gegen die Blocker-Muster prüfen
		playerW := soko.Wpos(posToW[playerPos])
		boxBits := make([]uint64, maskWords)
		for pos := range boxSet {
			w := posToW[pos]
			boxBits[w>>6] |= 1 << (uint(w) & 63)
		}

		if !blk.CheckAllowed(playerW, boxBits) {
			blockedCount++
			t.Errorf("GEBLOCKT: Zug %d (Schub %d) - diese Stellung der optimalen Lösung wird als Deadlock verworfen", i+1, pushCount)

			// alle zutreffenden Muster heraussuchen und anzeigen
			for _, st := range blk.stages {
				pat := st.patterns[playerW]
				for p := 0; p < len(pat); p += st.boxCount {
					match := true
					fields := map[int]bool{}
					for j := 0; j < st.boxCount; j++ {
						w := int(pat[p+j])
						if boxBits[w>>6]&(1<<(uint(w)&63)) == 0 {
							match = false
							break
						}
						// Wpos zurück in Raster-Position übersetzen
						for pos, pw := range posToW {
							if pw == w {
								fields[pos] = true
								break
							}
						}
					}
					if match {
						t.Logf("zutreffendes Muster: Stufe %d, Wpos %v", st.boxCount, pat[p:p+st.boxCount])
						t.Logf("Stellung (X = Musterfeld mit Kiste):\n%s", drawState(playerPos, fields))

						// Gegenprobe: das Teilspiel mit NUR den Muster-Kisten ab genau dieser
						// Stellung lösen (Bx-Kriterium: alle k Kisten auf irgendwelchen Zielfeldern).
						// Ist es lösbar, ist das Muster als falsch-positiv bewiesen.
						if depth, nodes, solvable := solveSubgame(field, playerW, pat[p:p+st.boxCount]); solvable {
							t.Logf("Gegenprobe: das %d-Kisten-Teilspiel ab dieser Stellung IST lösbar (%d Schübe, %d Knoten) -> Muster ist falsch-positiv", st.boxCount, depth, nodes)
						} else {
							t.Logf("Gegenprobe: das %d-Kisten-Teilspiel ist wirklich unlösbar (%d Knoten)", st.boxCount, nodes)
						}
					}
				}
			}
		}
	}

	// --- Endkontrolle: Lösung muss wirklich lösen ---
	for pos := range boxSet {
		if !goalSet[pos] {
			t.Fatalf("Lösung endet nicht gelöst: Kiste auf Position %d steht nicht auf einem Ziel", pos)
		}
	}
	t.Logf("Lösung abgespielt: %d Züge, %d Schübe, Endstellung gelöst", len(moves), pushCount)

	if blockedCount > 0 {
		t.Errorf("%d von %d Nach-Schub-Stellungen der optimalen Lösung werden geblockt", blockedCount, pushCount)
	}
}

// löst das Teilspiel mit nur k Kisten (auf den angegebenen Feldern) ab der angegebenen
// Spielerposition per einfacher Vorwärts-BFS ohne jeden Filter; "gelöst" nach Bx-Kriterium:
// alle k Kisten stehen auf (irgendwelchen) Zielfeldern. Liefert Schub-Tiefe, Knotenzahl
// und ob eine Lösung existiert.
func solveSubgame(field *soko.Field, player soko.Wpos, boxes []soko.Wpos) (depth int, nodes int, solvable bool) {
	sub := field.CloneWithBoxCount(len(boxes))

	goalSet := map[soko.Wpos]bool{}
	for _, g := range field.Goals() {
		goalSet[g] = true
	}
	isSolved := func(s *soko.State) bool {
		for _, b := range s.Boxes {
			if !goalSet[b] {
				return false
			}
		}
		return true
	}

	start := soko.State{Player: player, Boxes: append([]soko.Wpos(nil), boxes...)}
	start.UpdateCrc()
	if isSolved(&start) {
		return 0, 1, true
	}

	known := map[uint64]bool{uint64(start.Crc): true}
	current := []soko.State{start}
	buf := sub.MakeStateBuffer(256)[:0]

	for depth = 1; len(current) > 0; depth++ {
		var next []soko.State
		for i := range current {
			sub.SetState(&current[i])
			buf = sub.SearchVariantsForward(buf[:0])
			for j := range buf {
				v := &buf[j]
				if known[uint64(v.Crc)] {
					continue
				}
				known[uint64(v.Crc)] = true
				if isSolved(v) {
					return depth, len(known), true
				}
				next = append(next, soko.State{Player: v.Player, Boxes: append([]soko.Wpos(nil), v.Boxes...), Crc: v.Crc})
			}
		}
		current = next
	}
	return 0, len(known), false
}
