package tui

import (
	"testing"

	tea "github.com/charmbracelet/bubbletea"
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

// Enter übernimmt ein gepastetes Level direkt; beim unvollständigen Level bleibt Enter eine neue Zeile
func TestModelScanOnEnter(t *testing.T) {
	t.Chdir(t.TempDir())

	// komplettes Level + Enter -> Scan in den Blocker-Modus
	m := NewModel("", 0)
	m.input.SetValue(testLevel)
	m = press(t, m, tea.KeyMsg{Type: tea.KeyEnter})
	if m.mode != modeBlocker {
		t.Fatalf("Enter müsste das komplette Level übernehmen (Fehler: %q)", m.inputErr)
	}

	// unvollständiges Level (Kiste ohne Zielfeld -> Parse-Fehler) + Enter
	// -> bleibt in der Eingabe (normale neue Zeile)
	m = NewModel("", 0)
	m.input.SetValue("#####\n#@$ #")
	m = press(t, m, tea.KeyMsg{Type: tea.KeyEnter})
	if m.mode != modeInput {
		t.Fatal("Enter darf ein unvollständiges Level nicht übernehmen")
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
