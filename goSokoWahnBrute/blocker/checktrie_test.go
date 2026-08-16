package blocker

// Äquivalenz-Absicherung des Set-Trie-Checks: CheckAllowed (Trie-Tiefensuche)
// muss für jede Stellung exakt dasselbe Ergebnis liefern wie der naive lineare
// Scan über alle Muster aller Stufen (inklusive der bedingten Kill-Regel).

import (
	"math/rand"
	"testing"

	"goSokoWahnBrute/maps"
	"goSokoWahnBrute/soko"
)

// naive Referenz-Implementierung: linearer Scan über alle Muster aller Stufen,
// Subset-Test per Feldvergleich, gleiche bedingte Kill-Regel wie CheckAllowed
func naiveCheckAllowed(b *Blocker, player soko.Wpos, boxBits []uint64) bool {
	var candidates [4]soko.Wpos
	candCount := 0
	covered, allCovered := 0, 0

	for s := range b.stages {
		k := b.stages[s].boxCount
		pat := b.stages[s].patterns[player]
		for p := 0; p < len(pat); p += k {
			match := true
			for i := 0; i < k; i++ {
				f := pat[p+i]
				if boxBits[f>>6]&(1<<(f&63)) == 0 {
					match = false
					break
				}
			}
			if !match {
				continue
			}
			if allCovered == 0 {
				candidates, candCount = b.base.PushPoseCandidates(player, boxBits)
				if candCount == 0 {
					return true
				}
				allCovered = 1<<candCount - 1
			}
			for c := 0; c < candCount; c++ {
				for i := 0; i < k; i++ {
					if pat[p+i] == candidates[c] {
						covered |= 1 << c
						break
					}
				}
			}
			if covered == allCovered {
				return false
			}
		}
	}
	return true
}

// vergleicht Trie und Referenz auf einer Stellung und meldet Abweichungen
func compareCheck(t *testing.T, b *Blocker, player soko.Wpos, boxBits []uint64) {
	t.Helper()
	if got, want := b.CheckAllowed(player, boxBits), naiveCheckAllowed(b, player, boxBits); got != want {
		t.Fatalf("CheckAllowed weicht ab: player=%d boxBits=%v trie=%v naiv=%v", player, boxBits, got, want)
	}
}

func TestCheckTrieMatchesNaive(t *testing.T) {
	field, err := soko.Parse(maps.MapVanilla)
	if err != nil {
		t.Fatal(err)
	}
	blk := buildBlocker(t, field, "")
	walkCount := blk.walkCount
	words := (walkCount + 64) / 64

	// 1. gezielte Treffer: für jedes Muster exakt die Muster-Kisten setzen
	// (plus Varianten mit einer Extra-Kiste) - deckt die Treffer-Pfade samt
	// Kandidaten-Abdeckung ab, die Zufallsstellungen nur selten erreichen
	rng := rand.New(rand.NewSource(25523))
	for s := range blk.stages {
		k := blk.stages[s].boxCount
		for player := 0; player < walkCount; player++ {
			pat := blk.stages[s].patterns[player]
			for p := 0; p < len(pat); p += k {
				boxBits := make([]uint64, words)
				for i := 0; i < k; i++ {
					f := pat[p+i]
					boxBits[f>>6] |= 1 << (f & 63)
				}
				if boxBits[player>>6]&(1<<(player&63)) != 0 {
					continue // Spieler stünde auf einer Kiste - im Spiel unmöglich
				}
				compareCheck(t, blk, soko.Wpos(player), boxBits)

				// eine zufällige Extra-Kiste dazu (macht Ober-Stellungen und
				// zusätzliche Schub-Pose-Kandidaten sichtbar)
				extra := soko.Wpos(rng.Intn(walkCount))
				if extra != soko.Wpos(player) {
					boxBits[extra>>6] |= 1 << (extra & 63)
					compareCheck(t, blk, soko.Wpos(player), boxBits)
				}
			}
		}
	}

	// 2. Zufallsstellungen (fester Seed): breite Abdeckung inklusive Nicht-Treffern
	for round := 0; round < 20000; round++ {
		boxBits := make([]uint64, words)
		boxCount := 1 + rng.Intn(8)
		for i := 0; i < boxCount; i++ {
			f := rng.Intn(walkCount)
			boxBits[f>>6] |= 1 << (f & 63)
		}
		player := soko.Wpos(rng.Intn(walkCount))
		if boxBits[player>>6]&(1<<(player&63)) != 0 {
			continue
		}
		compareCheck(t, blk, player, boxBits)
	}
}
