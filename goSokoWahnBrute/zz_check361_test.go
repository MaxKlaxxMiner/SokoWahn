package main

// Temporärer Diagnose-Test für Level 361: spielt die von Max/JSOKO gefundene
// 315-Züge/108-Schübe-Lösung Schub für Schub ab und prüft für jede Kante,
// ob die Varianten-Generierung sie liefert - ungefiltert, nur mit Blocker
// (6-Steiner-Cache aus temp/), nur mit Regeln und mit beidem, jeweils
// vorwärts (CheckPush/CheckAllowed) und rückwärts (CheckPull/CheckAllowed).
// Fällt eine Kante nur mit Filter weg, filtert der Filter unsound.
// Nicht einchecken.

import (
	"os"
	"path/filepath"
	"sort"
	"testing"

	"goSokoWahnBrute/blocker"
	"goSokoWahnBrute/soko"
)

const repoRoot = `E:\prog\spiele\sokowahn\SokoWahn`

// spielt die LURD-Folge auf dem Gitter ab und liefert die Schub-Stellungen
// (Start + eine Stellung je Großbuchstabe, MoveDepth = kumulierte Züge)
func replaySolution(t *testing.T, field *soko.Field, lurd string) []soko.State {
	t.Helper()

	// (x,y) -> Wpos über die exportierte Rückrichtung aufbauen
	type xy struct{ x, y int }
	toWpos := map[xy]soko.Wpos{}
	for pos := 0; pos < field.WalkCount(); pos++ {
		x, y := field.FieldXY(soko.Wpos(pos))
		toWpos[xy{x, y}] = soko.Wpos(pos)
	}

	var start soko.State
	field.GetState(&start)

	px, py := field.FieldXY(start.Player)
	boxes := map[xy]bool{}
	for _, b := range start.Boxes {
		x, y := field.FieldXY(b)
		boxes[xy{x, y}] = true
	}

	collect := func(moveDepth int) soko.State {
		st := soko.State{Player: toWpos[xy{px, py}], MoveDepth: int32(moveDepth)}
		for b := range boxes {
			st.Boxes = append(st.Boxes, toWpos[b])
		}
		sort.Slice(st.Boxes, func(i, j int) bool { return st.Boxes[i] < st.Boxes[j] })
		st.UpdateCrc()
		return st
	}

	states := []soko.State{collect(0)}
	for i := 0; i < len(lurd); i++ {
		dx, dy := 0, 0
		switch lurd[i] {
		case 'l', 'L':
			dx = -1
		case 'r', 'R':
			dx = 1
		case 'u', 'U':
			dy = -1
		case 'd', 'D':
			dy = 1
		default:
			t.Fatalf("ungültiges LURD-Zeichen %q an Position %d", lurd[i], i)
		}
		nx, ny := px+dx, py+dy
		if _, ok := toWpos[xy{nx, ny}]; !ok {
			t.Fatalf("Zug %d (%c): Zielfeld (%d,%d) nicht begehbar", i, lurd[i], nx, ny)
		}
		push := lurd[i] >= 'A' && lurd[i] <= 'Z'
		if push {
			bx, by := nx+dx, ny+dy
			if !boxes[xy{nx, ny}] {
				t.Fatalf("Zug %d (%c): keine Kiste auf (%d,%d)", i, lurd[i], nx, ny)
			}
			if _, ok := toWpos[xy{bx, by}]; !ok || boxes[xy{bx, by}] {
				t.Fatalf("Zug %d (%c): Kistenziel (%d,%d) blockiert", i, lurd[i], bx, by)
			}
			delete(boxes, xy{nx, ny})
			boxes[xy{bx, by}] = true
		} else if boxes[xy{nx, ny}] {
			t.Fatalf("Zug %d (%c): Laufzug in Kiste auf (%d,%d)", i, lurd[i], nx, ny)
		}
		px, py = nx, ny
		if push {
			states = append(states, collect(i+1))
		}
	}
	return states
}

func TestCheck361Solution(t *testing.T) {
	levelData, err := os.ReadFile(filepath.Join(repoRoot, "levelcache", "361.txt"))
	if err != nil {
		t.Fatal(err)
	}
	lurdData, err := os.ReadFile(filepath.Join(repoRoot, "solution-id361-315.108.txt"))
	if err != nil {
		t.Fatal(err)
	}
	lurd := string(lurdData)
	for len(lurd) > 0 && (lurd[len(lurd)-1] == '\n' || lurd[len(lurd)-1] == '\r') {
		lurd = lurd[:len(lurd)-1]
	}

	baseField, err := soko.Parse(string(levelData))
	if err != nil {
		t.Fatal(err)
	}

	states := replaySolution(t, baseField, lurd)
	t.Logf("Lösung abgespielt: %d Züge, %d Schub-Stellungen (inkl. Start)", len(lurd), len(states))

	// Endstellung muss gelöst sein
	goals := baseField.Goals()
	last := states[len(states)-1]
	for i, b := range last.Boxes {
		if b != goals[i] {
			t.Fatalf("Endstellung nicht gelöst: Kiste %d auf %d statt %d", i, b, goals[i])
		}
	}

	// Blocker aus dem 6-Steiner-Cache laden (nur lesen, nicht weiterbauen)
	cachePath := filepath.Join(repoRoot, "temp", blocker.CacheName(baseField))
	if _, err := os.Stat(cachePath); err != nil {
		t.Fatalf("Blocker-Cache fehlt: %s (%v)", cachePath, err)
	}
	blk := blocker.New(baseField.Clone(), cachePath)
	blk.Abort()
	t.Logf("Blocker geladen:\n%s", blk)

	configs := []struct {
		name       string
		useBlocker bool
		useRules   bool
	}{
		{"ohne Filter", false, false},
		{"nur Blocker", true, false},
		{"nur Regeln", false, true},
		{"Blocker+Regeln", true, true},
	}

	for _, cfg := range configs {
		field, err := soko.Parse(string(levelData))
		if err != nil {
			t.Fatal(err)
		}
		if cfg.useRules {
			rules := soko.NewRules(field)
			field.SetRules(rules)
			field.SetRulesBackward(rules)
		}
		if cfg.useBlocker {
			field.SetBlocker(blk)
			field.SetBlockerBackward(blk)
		}

		varBuf := field.MakeStateBuffer(256)[:0]
		fwdMiss, bwdMiss := 0, 0

		// vorwärts: von jeder Stellung muss die nächste als Schub-Variante fallen
		for i := 0; i+1 < len(states); i++ {
			cur := states[i]
			field.SetState(&cur)
			varBuf = field.SearchVariantsForward(varBuf[:0])
			found := false
			for j := range varBuf {
				if varBuf[j].Crc == states[i+1].Crc {
					found = true
					if varBuf[j].MoveDepth != states[i+1].MoveDepth {
						t.Errorf("[%s] vorwärts Schub %d: Zugzahl %d statt %d",
							cfg.name, i+1, varBuf[j].MoveDepth, states[i+1].MoveDepth)
					}
					break
				}
			}
			if !found {
				fwdMiss++
				t.Errorf("[%s] vorwärts Schub %d (Zug %d): Nachfolger fehlt in den Varianten",
					cfg.name, i+1, states[i+1].MoveDepth)
			}
		}

		// rückwärts: von jeder Stellung muss die vorige als Pull-Variante fallen
		// (i=0 entfällt: die Startstellung ist keine Schub-Stellung)
		for i := 1; i+1 < len(states); i++ {
			next := cloneStateLocal(&states[i+1])
			next.MoveDepth = 0
			field.SetState(&next)
			varBuf = field.SearchVariantsBackward(varBuf[:0])
			found := false
			wantDepth := states[i+1].MoveDepth - states[i].MoveDepth
			for j := range varBuf {
				if varBuf[j].Crc == states[i].Crc {
					found = true
					if varBuf[j].MoveDepth != wantDepth {
						t.Errorf("[%s] rückwärts Schub %d: Zugzahl %d statt %d",
							cfg.name, i+1, varBuf[j].MoveDepth, wantDepth)
					}
					break
				}
			}
			if !found {
				bwdMiss++
				t.Errorf("[%s] rückwärts Schub %d (Zug %d): Vorgänger fehlt in den Pull-Varianten",
					cfg.name, i+1, states[i+1].MoveDepth)
			}
		}

		t.Logf("[%s] vorwärts: %d/%d Kanten fehlen, rückwärts: %d/%d Kanten fehlen",
			cfg.name, fwdMiss, len(states)-1, bwdMiss, len(states)-2)
	}
}

func cloneStateLocal(v *soko.State) soko.State {
	boxes := make([]soko.Wpos, len(v.Boxes))
	copy(boxes, v.Boxes)
	return soko.State{Player: v.Player, Boxes: boxes, MoveDepth: v.MoveDepth, Crc: v.Crc}
}
