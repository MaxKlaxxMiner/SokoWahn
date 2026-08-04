package tui

import tea "github.com/charmbracelet/bubbletea"

// startet die Terminal-Oberfläche (initialLevel wird vorab ins Eingabefeld gelegt)
func Run(initialLevel string, ramLimitGB int) error {
	program := tea.NewProgram(NewModel(initialLevel, ramLimitGB), tea.WithAltScreen())
	_, err := program.Run()
	return err
}
