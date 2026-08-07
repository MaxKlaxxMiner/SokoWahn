package rooms

import (
	"math/big"
	"testing"

	"goSokoWahnRooms/maps"
	"goSokoWahnRooms/soko"
)

// Mini-Level: ein einzelner Schub nach rechts auf das Zielfeld löst das Level
const mapMini = `
#####
#@$.#
#####
`

// Zwei-Schub-Level: Kiste muss zweimal nach rechts geschoben werden
const mapTwoPush = `
######
#@$ .#
######
`

func buildNetwork(t *testing.T, sokoMap string) *Network {
	t.Helper()
	field, err := soko.Parse(sokoMap)
	if err != nil {
		t.Fatal("parse:", err)
	}
	network, err := NewNetwork(field)
	if err != nil {
		t.Fatal("network:", err)
	}
	return network
}

// prüft die Kennzahlen aller Räume eines Netzwerks (Zustände und Varianten je Raum)
func checkRooms(t *testing.T, n *Network, wantStates, wantVariants []uint64) {
	t.Helper()
	if len(n.Rooms) != len(wantStates) {
		t.Fatalf("rooms: got %d, want %d", len(n.Rooms), len(wantStates))
	}
	for i, room := range n.Rooms {
		if room.States.Count() != wantStates[i] {
			t.Errorf("room %d states: got %d, want %d", i, room.States.Count(), wantStates[i])
		}
		if room.Variants.Count() != wantVariants[i] {
			t.Errorf("room %d variants: got %d, want %d", i, room.Variants.Count(), wantVariants[i])
		}
	}
}

// Handverifikation Mini-Level (Wpos: 0='@', 1='$', 2='.'):
// Raum 0: nur Zustand "leer" (Ecke, nie Kiste), 1 Startvariante nach rechts.
// Raum 1: Zustände leer/Kiste; Varianten: durchlaufen (von links), Push zurück nach
//         links, End-Variante (Kiste auf Ziel, Spieler bleibt), durchlaufen (von rechts).
// Raum 2: Zustände Kiste-auf-Ziel/leer, keine Varianten (Sackgassen-Ziel, nur BoxSwap).
func TestNetworkMini(t *testing.T) {
	n := buildNetwork(t, mapMini)
	checkRooms(t, n, []uint64{1, 2, 2}, []uint64{1, 4, 0})

	if n.Rooms[0].StartVariantCount != 1 {
		t.Errorf("start variants: got %d, want 1", n.Rooms[0].StartVariantCount)
	}

	// Raum 1, Portal von links: Zustand 1 (Kiste) muss die End-Variante enthalten
	endFound := false
	for _, ip := range n.Rooms[1].Incoming {
		span := ip.GetVariantSpan(1)
		for id := span.Start; id < span.Start+span.Count; id++ {
			v := n.Rooms[1].Variants.Get(id)
			if v.PlayerPortal == NoPortal && v.Pushes == 1 && v.NewState == 0 {
				endFound = true
			}
		}
	}
	if !endFound {
		t.Error("end variant (box pushed to goal, player stays) not found in room 1")
	}

	// Raum 2: die reingeschobene Kiste löst den Zustandswechsel leer -> Kiste-auf-Ziel aus
	swapFound := false
	for _, ip := range n.Rooms[2].Incoming {
		if ip.GetBoxSwap(1) == 0 {
			swapFound = true
		}
	}
	if !swapFound {
		t.Error("box swap 1 -> 0 not found in room 2")
	}

	if got := n.Effort().Uint64(); got != 4 {
		t.Errorf("effort: got %d, want 4", got)
	}
}

// Handverifikation Zwei-Schub-Level (Wpos: 0='@', 1='$', 2=' ', 3='.'):
// Raum 1 und 2 tragen je 4 Varianten, das Sackgassen-Ziel (Raum 3) keine.
func TestNetworkTwoPush(t *testing.T) {
	n := buildNetwork(t, mapTwoPush)
	checkRooms(t, n, []uint64{1, 2, 2, 2}, []uint64{1, 4, 4, 0})

	if got := n.Effort().Uint64(); got != 16 {
		t.Errorf("effort: got %d, want 16", got)
	}
}

// Integrationstest: das Vanilla-Level (Startlevel des C#-Originals) muss sauber
// durch Aufbau und Validate laufen; die Kennzahlen sind der eingefrorene Ist-Stand
// vom 2026-08-07 (Schutz gegen stille Verhaltensänderungen). Zum Vergleich:
// das C#-Original lag nach Init + SokoBoxScanner bei 5,076e40 (PlanFertig.txt, Stufe d)
func TestNetworkVanilla(t *testing.T) {
	n := buildNetwork(t, maps.MapVanilla)

	states, variants := uint64(0), uint64(0)
	for _, room := range n.Rooms {
		states += room.States.Count()
		variants += room.Variants.Count()
	}
	t.Logf("vanilla: rooms=%d states=%d variants=%d effort=%s", len(n.Rooms), states, variants, n.EffortString())

	if len(n.Rooms) != 56 {
		t.Errorf("rooms: got %d, want 56", len(n.Rooms))
	}
	if states != 89 {
		t.Errorf("states: got %d, want 89", states)
	}
	if variants != 428 {
		t.Errorf("variants: got %d, want 428", variants)
	}
	if got := n.EffortString(); got != "6,508e40 (65.083.256.447.008.724.441.860.096.917.504.000.000.000)" {
		t.Errorf("effort: got %s", got)
	}
}

func TestFormatBig(t *testing.T) {
	tests := []struct {
		value string
		want  string
	}{
		{"0", "0"},
		{"999", "999"},
		{"1234567", "1.234.567"},
		{"999999999999", "999.999.999.999"},
		{"1234567890123", "1,235e12 (1.234.567.890.123)"}, // 5. Stelle rundet auf
		{"9999500000000", "1,0000e12 (9.999.500.000.000)"}, // Rundungs-Überlauf 9999 -> 10000 (wie C#-Original)
	}
	for _, tt := range tests {
		val, _ := new(big.Int).SetString(tt.value, 10)
		if got := FormatBig(val); got != tt.want {
			t.Errorf("FormatBig(%s): got %q, want %q", tt.value, got, tt.want)
		}
	}
}
