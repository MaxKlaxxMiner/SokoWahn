package tui

import tea "github.com/charmbracelet/bubbletea"

// startet die Terminal-Oberfläche (initialLevel wird vorab ins Eingabefeld gelegt)
func Run(initialLevel string, ramLimitGB int) error {
	program := tea.NewProgram(NewModel(initialLevel, ramLimitGB), tea.WithAltScreen())
	finalModel, err := program.Run()
	if m, ok := finalModel.(Model); ok {
		m.closeWork() // Auslagerungsdateien der Suchlisten löschen
	}
	return err
}
