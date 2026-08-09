package tui

import (
	"slices"
	"strings"
	"testing"

	tea "github.com/charmbracelet/bubbletea"

	"goSokoWahnBrute/soko"
)

// kleines Mehrkisten-Level (Referenz: 16 Züge optimal)
const testLevel = `#######
#.@ # #
#$* $ #
#   $ #
# ..  #
#  *  #
#######
`

// simuliert einen Tastendruck auf dem Model
func press(t *testing.T, m Model, key tea.KeyMsg) Model {
	t.Helper()
	updated, _ := m.handleKey(key)
	return updated.(Model)
}

// Worker-Stufenfolge: verdoppelnd bis zur Kernzahl, darüber Kernzahl-Vielfache bis *8
func TestWorkerSteps(t *testing.T) {
	if got, want := workerStepsFor(12), []int{1, 2, 4, 8, 12, 24, 48, 96}; !slices.Equal(got, want) {
		t.Errorf("workerStepsFor(12) = %v, erwartet %v", got, want)
	}
	if got, want := workerStepsFor(16), []int{1, 2, 4, 8, 16, 32, 64, 128}; !slices.Equal(got, want) {
		t.Errorf("workerStepsFor(16) = %v, erwartet %v", got, want)
	}
	if got, want := workerStepsFor(6), []int{1, 2, 4, 6, 12, 24, 48}; !slices.Equal(got, want) {
		t.Errorf("workerStepsFor(6) = %v, erwartet %v", got, want)
	}
}

// Worker-Umschaltung per * und /: eine Stufe hoch bzw. runter, an den Enden bleibt es stehen
func TestChangeWorkers(t *testing.T) {
	t.Chdir(t.TempDir())

	m := NewModel("", 0)
	m.input.SetValue(testLevel)
	m.scan()
	m.blk.Abort()
	m.startSearch()

	// unabhängig von der Kernzahl: von 1 geht es immer auf 2 hoch und zurück auf 1
	m.slv.SetWorkers(1)
	m = press(t, m, tea.KeyMsg{Type: tea.KeyRunes, Runes: []rune("*")})
	if got := m.slv.Workers(); got != 2 {
		t.Errorf("nach * werden 2 Worker erwartet, erhalten: %d", got)
	}
	m = press(t, m, tea.KeyMsg{Type: tea.KeyRunes, Runes: []rune("/")})
	if got := m.slv.Workers(); got != 1 {
		t.Errorf("nach / wird 1 Worker erwartet, erhalten: %d", got)
	}
	m = press(t, m, tea.KeyMsg{Type: tea.KeyRunes, Runes: []rune("/")})
	if got := m.slv.Workers(); got != 1 {
		t.Errorf("unter 1 Worker darf es nicht gehen, erhalten: %d", got)
	}
}

// kompletter Ablauf ohne Terminal: Scan -> Blockerscan -> Suche -> Lösung
func TestModelFlow(t *testing.T) {
	t.Chdir(t.TempDir()) // der Blocker-Cache (temp/) soll nicht im Repo landen

	m := NewModel("", 0)
	m.input.SetValue(testLevel)
	m.scan()

	if m.mode != modeBlocker {
		t.Fatalf("nach dem Scan wird der Blocker-Modus erwartet (Status: %q)", m.inputErr)
	}

	// Blockerscan per Bulk-Taste komplett durchrechnen (endet automatisch bei KistenAnzahl-1)
	keyB := tea.KeyMsg{Type: tea.KeyRunes, Runes: []rune("b")}
	for i := 0; i < 100000 && m.mode == modeBlocker; i++ {
		m = press(t, m, keyB)
	}
	if m.mode != modeSearch {
		t.Fatal("Blockerscan wurde nicht automatisch beendet")
	}

	stats := m.blk.GetStats()
	if len(stats.Stages) != stats.MaxBoxes-1 {
		t.Errorf("erwartet %d fertige Blocker-Stufen, erhalten: %d", stats.MaxBoxes-1, len(stats.Stages))
	}

	// Suche per Bulk-Taste bis zur Lösung durchrechnen
	for i := 0; i < 100000 && m.mode == modeSearch; i++ {
		m = press(t, m, keyB)
	}
	if m.mode != modeSolution {
		t.Fatalf("Suche hat keine Lösung geliefert (Status: %q)", m.status)
	}
	if len(m.solution.Moves) != 16 {
		t.Errorf("erwartete 16 Züge, erhalten: %d (%s)", len(m.solution.Moves), m.solution.Moves)
	}

	// durch die Lösung blättern
	keyRight := tea.KeyMsg{Type: tea.KeyRight}
	for i := 0; i < len(m.solution.States)+5; i++ {
		m = press(t, m, keyRight)
	}
	if m.frame != len(m.solution.States)-1 {
		t.Errorf("Blättern endet nicht bei der letzten Stellung: %d", m.frame)
	}

	// die View darf in keinem Modus leer sein oder crashen
	if m.View() == "" {
		t.Error("View liefert keine Ausgabe")
	}
}

// Enter scannt nur einzeilige Nummern/URLs direkt; Levelnotation bleibt eine normale
// neue Zeile (beim Einfügen kommen Zeilenschaltungen als Enter-Tastendrücke an)
func TestModelScanOnEnter(t *testing.T) {
	t.Chdir(t.TempDir())

	// komplettes Level + Enter -> KEIN Scan, bleibt in der Eingabe (Übernahme per Strg+S)
	m := NewModel("", 0)
	m.input.SetValue(testLevel)
	m = press(t, m, tea.KeyMsg{Type: tea.KeyEnter})
	if m.mode != modeInput {
		t.Fatal("Enter darf Levelnotation nicht übernehmen (zerstört sonst das Einfügen)")
	}

	// erste Zeile eines gerade laufenden Pastes + Enter -> ebenfalls normale neue Zeile
	m = NewModel("", 0)
	m.input.SetValue("#####")
	m = press(t, m, tea.KeyMsg{Type: tea.KeyEnter})
	if m.mode != modeInput {
		t.Fatal("Enter darf eine einzelne Levelzeile nicht übernehmen")
	}

	// Nummer/URL wird dagegen als scan-würdig erkannt (ohne Netz: nur die Erkennung prüfen)
	for _, web := range []string{"349", "http://www.game-sokoban.com/index.php?mode=level&lid=349"} {
		if !isWebInput(web) {
			t.Errorf("Eingabe %q müsste als Nummer/URL erkannt werden", web)
		}
	}
	for _, level := range []string{"", "#####", "#####\n#@$ #", "  ### mit Einrückung"} {
		if isWebInput(level) {
			t.Errorf("Eingabe %q darf nicht als Nummer/URL erkannt werden", level)
		}
	}
}

// Einfügen eines Levels mit Windows-Zeilenenden (CRLF) darf keine doppelten Zeilen erzeugen.
// Geprüft wird der Weg, den Max nutzt: Strg+V liest die Zwischenablage.
func TestModelPasteCRLF(t *testing.T) {
	t.Chdir(t.TempDir())

	level := "#######\n#.@ # #\n#$* $ #\n#   $ #\n# ..  #\n#  *  #\n#######"
	clip := strings.ReplaceAll(level, "\n", "\r\n")
	orig := readClipboard
	readClipboard = func() (string, error) { return clip, nil }
	defer func() { readClipboard = orig }()

	m := NewModel("", 0)
	m = press(t, m, tea.KeyMsg{Type: tea.KeyCtrlV})

	if got := m.input.Value(); got != level {
		t.Fatalf("eingefügtes CRLF-Level landet verändert im Eingabefeld:\n%q", got)
	}

	// zweiter Weg: bracketed paste (Terminals, die den Text als eine Tastennachricht liefern)
	m2 := NewModel("", 0)
	m2 = press(t, m2, tea.KeyMsg{Type: tea.KeyRunes, Runes: []rune(clip), Paste: true})
	if got := m2.input.Value(); got != level {
		t.Fatalf("bracketed paste mit CRLF landet verändert im Eingabefeld:\n%q", got)
	}

	m.scan()
	if m.inputErr != "" {
		t.Fatalf("eingefügtes CRLF-Level parst nicht: %s", m.inputErr)
	}
	want, err := soko.Parse(level)
	if err != nil {
		t.Fatal(err)
	}
	if m.field.FieldCrc() != want.FieldCrc() {
		t.Fatal("eingefügtes CRLF-Level ergibt eine andere Feldgeometrie")
	}
}

// scan darf den Eingabetext nicht komplett trimmen: das würde nur der ersten Zeile
// die Einrückung nehmen und sie gegen den Rest des Levels verschieben
func TestModelScanIndentedFirstLine(t *testing.T) {
	t.Chdir(t.TempDir())

	level := "   ####\n####  #\n#  @$.#\n#######"
	want, err := soko.Parse(level)
	if err != nil {
		t.Fatal(err)
	}

	m := NewModel("", 0)
	m.input.SetValue(level)
	m.scan()
	if m.inputErr != "" {
		t.Fatalf("Level mit eingerückter erster Zeile parst nicht: %s", m.inputErr)
	}
	if m.field.FieldCrc() != want.FieldCrc() {
		t.Fatal("scan verändert die Feldgeometrie (erste Zeile wurde getrimmt)")
	}
}

// Ministep im Blockerscan: ein einzelner Next(1)-Schritt darf den Modus nicht verlassen
func TestModelBlockerMinistep(t *testing.T) {
	t.Chdir(t.TempDir())

	m := NewModel("", 0)
	m.input.SetValue(testLevel)
	m.scan()

	keyS := tea.KeyMsg{Type: tea.KeyRunes, Runes: []rune("s")}
	m = press(t, m, keyS)
	if m.mode != modeBlocker {
		t.Fatal("ein Ministep darf den Blockerscan nicht beenden")
	}

	// Enter beendet den Blockerscan sofort und startet die Suche
	m = press(t, m, tea.KeyMsg{Type: tea.KeyEnter})
	if m.mode != modeSearch {
		t.Fatal("Enter muss den Blockerscan beenden und die Suche starten")
	}
}
