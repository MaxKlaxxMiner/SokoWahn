package soko

import "testing"

// prüft die Kennzahlen eines geparsten Levels nach der Freeze-Ersetzung
func checkFreeze(t *testing.T, sokoMap string, wantWalk, wantBoxes, wantGoals int) {
	t.Helper()
	f, err := Parse(sokoMap)
	if err != nil {
		t.Fatal("parse:", err)
	}
	if f.WalkCount() != wantWalk {
		t.Errorf("walk count: got %d, want %d", f.WalkCount(), wantWalk)
	}
	if f.BoxCount() != wantBoxes {
		t.Errorf("box count: got %d, want %d", f.BoxCount(), wantBoxes)
	}
	if len(f.Goals()) != wantGoals {
		t.Errorf("goal count: got %d, want %d", len(f.Goals()), wantGoals)
	}
}

// Kiste auf Ziel in der Ecke: wird zur Wand, Kiste und Ziel entfallen
func TestFreezeCorner(t *testing.T) {
	checkFreeze(t, `
######
#*@$.#
######
`, 3, 1, 1)
}

// Kaskade: die linke Kiste friert an der Wand-Ecke ein, dadurch auch die rechte
func TestFreezeCascade(t *testing.T) {
	checkFreeze(t, `
#######
#**@$.#
#######
`, 3, 1, 1)
}

// freistehender 2x2-Block: nur über gegenseitige Blockade erkennbar
func TestFreezeBlock2x2(t *testing.T) {
	checkFreeze(t, `
##########
#        #
#  **    #
#  **    #
#        #
# @$   . #
##########
`, 36, 1, 1)
}

// bewegliche Kisten auf Zielen bleiben erhalten: die freie '*' in der Fläche
// und die nur vertikal blockierte '*' an der Wand sind nicht eingefroren
func TestFreezeKeepsMovable(t *testing.T) {
	checkFreeze(t, `
#######
#     #
# * @ #
# $ . #
#     #
#######
`, 20, 2, 2)

	checkFreeze(t, `
#######
#@ * .#
#  $  #
#######
`, 10, 2, 2)
}
