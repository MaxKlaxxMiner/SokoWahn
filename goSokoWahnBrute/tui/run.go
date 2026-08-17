package tui

import tea "github.com/charmbracelet/bubbletea"

// startet die Terminal-Oberfläche (initialLevel wird vorab ins Eingabefeld gelegt;
// checkSol/checkSolPath: optionale Referenz-Lösung des Flags -checksol - nach
// Abschluss der Suche wird der Diagnose-Report neben die LURD-Datei geschrieben)
func Run(initialLevel string, ramLimitGB int, checkSol, checkSolPath string) error {
	model := NewModel(initialLevel, ramLimitGB)
	model.checkSol = checkSol
	model.checkSolPath = checkSolPath
	program := tea.NewProgram(model, tea.WithAltScreen())
	finalModel, err := program.Run()
	if m, ok := finalModel.(Model); ok {
		m.closeWork() // Auslagerungsdateien der Suchlisten löschen
	}
	return err
}
