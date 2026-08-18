package solver

import (
	"strings"
	"testing"

	"goSokoWahnBrute/soko"
)

// ReplayLurd: die eigene Lösung der Suche muss sich abspielen lassen und
// exakt die Schub-Stellungen der Rekonstruktion liefern (gleiche Crcs/Tiefen)
func TestReplayLurd(t *testing.T) {
	s, plain := solveLevel(t, archiveTestLevel, 16)
	defer s.Close()

	states, err := s.base.ReplayLurd(plain.Moves)
	if err != nil {
		t.Fatal(err)
	}
	if len(states) != len(plain.States) {
		t.Fatalf("Replay liefert %d Stellungen, Rekonstruktion hat %d", len(states), len(plain.States))
	}
	for i := range states {
		if states[i].Crc != plain.States[i].Crc {
			t.Errorf("Stellung %d: Crc %x statt %x", i, states[i].Crc, plain.States[i].Crc)
		}
		// kumulierte Zugzahl: die Referenz sind die MoveOffsets der Lösung
		// (die MoveDepth-Werte der Rekonstruktions-States sind Relativwerte der Puffer)
		if int(states[i].MoveDepth) != plain.MoveOffsets[i] {
			t.Errorf("Stellung %d: MoveDepth %d statt %d", i, states[i].MoveDepth, plain.MoveOffsets[i])
		}
	}

	// ungültige Eingaben müssen sauber abgelehnt werden
	if _, err := s.base.ReplayLurd("x"); err == nil {
		t.Error("ungültiges Zeichen muss einen Fehler melden")
	}
	if _, err := s.base.ReplayLurd("uuuuuuuuuu"); err == nil {
		t.Error("Lauf in die Wand muss einen Fehler melden")
	}
	if _, err := s.base.ReplayLurd(strings.ToUpper(plain.Moves)); err == nil {
		t.Error("Schub ohne Kiste muss einen Fehler melden")
	}
}

// CheckSolution: die eigene Lösung der Suche ist per Konstruktion vollständig in
// den Tabellen repräsentiert (Präfix vorwärts-exakt, Suffix rückwärts-exakt,
// Verbindungs-Stellung ist Anker) - der Report muss den Pfad als begehbar melden
func TestCheckSolutionOwnPath(t *testing.T) {
	s, plain := solveLevel(t, archiveTestLevel, 16)
	defer s.Close()

	if _, err := s.GetSolutionBestPushes(); err != nil { // füllt die PushOptStats des Reports
		t.Fatal(err)
	}
	report, err := s.CheckSolution(plain.Moves)
	if err != nil {
		t.Fatal(err)
	}
	if !strings.Contains(report, "komplett begehbar") {
		t.Errorf("die eigene Lösung muss als begehbar erkannt werden, Report:\n%s", report)
	}
	if !strings.Contains(report, "Suche fand 16 Züge") {
		t.Errorf("Report muss die gefundene Zugzahl nennen:\n%s", report)
	}

	// abweichende Zuglängen müssen als Warnung erscheinen (Referenz künstlich verlängert:
	// erster Laufzug hin und zurück, danach die Original-Lösung - Endstellung bleibt gelöst)
	longer := "lr" + plain.Moves
	if _, err := s.base.ReplayLurd(longer); err != nil {
		t.Skipf("verlängerte Referenz nicht abspielbar (%v) - Warnungs-Check übersprungen", err)
	}
	report, err = s.CheckSolution(longer)
	if err != nil {
		t.Fatal(err)
	}
	if !strings.Contains(report, "ACHTUNG") {
		t.Errorf("abweichende Zuglänge muss eine Warnung auslösen:\n%s", report)
	}
}

// die PushOptStats müssen die Kennzahlen des Laufs widerspiegeln
func TestPushOptStats(t *testing.T) {
	s, plain := solveLevel(t, archiveTestLevel, 16)
	defer s.Close()

	best, err := s.GetSolutionBestPushes()
	if err != nil {
		t.Fatal(err)
	}
	st := s.PushOptStats()
	if !st.Ran {
		t.Error("das DP muss bei diesem Level gelaufen sein")
	}
	if st.Anchors != len(s.meetAnchors) {
		t.Errorf("Anchors %d, erwartet %d", st.Anchors, len(s.meetAnchors))
	}
	if st.PlainPushes != CountPushes(plain.Moves) {
		t.Errorf("PlainPushes %d, erwartet %d", st.PlainPushes, CountPushes(plain.Moves))
	}
	if st.BestPushes != CountPushes(best.Moves) {
		t.Errorf("BestPushes %d, erwartet %d", st.BestPushes, CountPushes(best.Moves))
	}
	if st.Overflow || st.AnchorCap {
		t.Errorf("Mini-Level darf keine Deckel reißen: %+v", st)
	}
	if st.DPNodes <= 0 {
		t.Errorf("DPNodes muss positiv sein: %+v", st)
	}
}

// fester Anker der Push-Optimierung auf dem Archiv-Testlevel (Gleichstands-
// Stellungen nach dem ersten Fund werden immer behalten - Hintergrund: Level 361;
// Referenzwerte gemessen 08/2026, Vorgeschichte in docs/history.md)
func TestPushOptAnchorRegression(t *testing.T) {
	field, err := soko.Parse(archiveTestLevel)
	if err != nil {
		t.Fatal(err)
	}
	s := New(field)
	defer s.Close()
	for s.Step(1000000000) {
	}
	best, err := s.GetSolutionBestPushes()
	if err != nil {
		t.Fatal(err)
	}

	if pushes := CountPushes(best.Moves); pushes != 7 {
		t.Errorf("erwartete 7 Schübe, erhalten: %d", pushes)
	}
	if anchors := s.PushOptStats().Anchors; anchors != 1 {
		t.Errorf("erwartete 1 Anker, erhalten: %d", anchors)
	}
	if nodes := s.NodeCount(); nodes != 918 {
		t.Errorf("erwartete 918 Knoten, erhalten: %d", nodes)
	}
}

// Wächter gegen versehentliche soko-Umbauten: ReplayLurd muss auf einem frisch
// geparsten Feld arbeiten können (base der Suche bleibt auf der Startstellung)
func TestReplayLurdFreshField(t *testing.T) {
	field, err := soko.Parse(archiveTestLevel)
	if err != nil {
		t.Fatal(err)
	}
	_, plain := solveLevel(t, archiveTestLevel, 16)
	states, err := field.ReplayLurd(plain.Moves)
	if err != nil {
		t.Fatal(err)
	}
	if len(states) == 0 || int(states[len(states)-1].MoveDepth) != 16 {
		t.Fatalf("Replay auf frischem Feld fehlgeschlagen: %d Stellungen", len(states))
	}
}
