package tui

import (
	"fmt"
	"strings"

	"github.com/charmbracelet/lipgloss"

	"goSokoWahnBrute/blocker"
	"goSokoWahnBrute/tools"
)

var (
	styleTitle  = lipgloss.NewStyle().Bold(true).Foreground(lipgloss.Color("12"))
	styleField  = lipgloss.NewStyle().Border(lipgloss.RoundedBorder()).Padding(0, 1)
	stylePanel  = lipgloss.NewStyle().Border(lipgloss.RoundedBorder()).Padding(0, 1)
	styleStatus = lipgloss.NewStyle().Foreground(lipgloss.Color("11"))
	styleHelp   = lipgloss.NewStyle().Faint(true)
	styleError  = lipgloss.NewStyle().Foreground(lipgloss.Color("9"))
	styleMark   = lipgloss.NewStyle().Foreground(lipgloss.Color("10")).Bold(true)
)

func (m Model) View() string {
	var body string

	switch m.mode {
	case modeInput:
		body = m.viewInput()
	case modeBlocker:
		body = m.viewBlocker()
	case modeSearch:
		body = m.viewSearch()
	case modeSolution:
		body = m.viewSolution()
	}

	title := styleTitle.Render("SokoWahn Brute") + "  " + styleHelp.Render(m.modeName())
	status := styleStatus.Render(m.status)
	help := styleHelp.Render(m.helpLine())

	return title + "\n" + body + "\n" + status + "\n" + help + "\n"
}

func (m Model) modeName() string {
	switch m.mode {
	case modeInput:
		return "- Level-Eingabe"
	case modeBlocker:
		return "- Blockerscan"
	case modeSearch:
		return "- Lösungssuche"
	case modeSolution:
		return "- Lösung"
	}
	return ""
}

func (m Model) helpLine() string {
	switch m.mode {
	case modeInput:
		return "Enter = Übernehmen (bei komplettem Level/Nummer/URL) | Strg+S = Scannen | Esc = Beenden (leer = Vanilla)"
	case modeBlocker:
		return "s = Ministep | b = Bulk | a/Leer = Auto | +/- = Bulkgröße | Enter = Blocker beenden -> Suche | i = Eingabe | q = Beenden"
	case modeSearch:
		return "s = Einzelschritt | b = Bulk | a/Leer = Auto | +/- = Bulkgröße | i = Eingabe | q = Beenden"
	case modeSolution:
		return "Pfeile/h/l = Blättern | Home/End = Anfang/Ende | i = neues Level | q = Beenden"
	}
	return ""
}

func (m Model) viewInput() string {
	body := m.input.View()
	if m.inputErr != "" {
		body += "\n" + styleError.Render("Fehler: "+m.inputErr)
	}
	return body
}

func (m Model) viewBlocker() string {
	left := styleField.Render(strings.TrimRight(m.field.String(), "\n"))

	stats := m.blk.GetStats()
	var sb strings.Builder
	sb.WriteString("Blocker-Stufen (Ziel: " + fmt.Sprint(stats.MaxBoxes-1) + "-Steiner):\n\n")
	sb.WriteString(m.blk.String())
	if m.auto {
		sb.WriteString("\n" + styleMark.Render("Auto läuft ...") + fmt.Sprintf("  (Tick: %d ms)", m.lastTick.Milliseconds()))
	}
	right := stylePanel.Render(strings.TrimRight(sb.String(), "\n"))

	return lipgloss.JoinHorizontal(lipgloss.Top, left, " ", right) + "\n" + m.workLine()
}

func (m Model) viewSearch() string {
	left := styleField.Render(strings.TrimRight(m.field.String(), "\n"))

	stats := m.slv.GetStats()
	var sb strings.Builder
	if stats.FoundMoves >= 0 {
		sb.WriteString(styleMark.Render(fmt.Sprintf("Gefunden: %d Züge", stats.FoundMoves)) + "\n\n")
	} else {
		sb.WriteString(fmt.Sprintf("Tiefe: %d + %d\n\n", stats.ForwardDepth, stats.BackwardDepth))
	}

	forward := depthColumn("vorwärts", stats.ForwardOpen, stats.ForwardDepth)
	backward := depthColumn("rückwärts", stats.BackwardOpen, stats.BackwardDepth)
	sb.WriteString(lipgloss.JoinHorizontal(lipgloss.Top, forward, "   ", backward))

	if m.auto {
		sb.WriteString("\n" + styleMark.Render("Auto läuft ...") + fmt.Sprintf("  (Tick: %d ms)", m.lastTick.Milliseconds()))
	}
	right := stylePanel.Render(strings.TrimRight(sb.String(), "\n"))

	return lipgloss.JoinHorizontal(lipgloss.Top, left, " ", right) + "\n" + m.workLine()
}

func (m Model) viewSolution() string {
	state := &m.solution.States[m.frame]
	left := styleField.Render(strings.TrimRight(state.Debug(m.field), "\n"))

	var sb strings.Builder
	fmt.Fprintf(&sb, "Lösung: %d Züge, %d Schub-Stellungen\n", len(m.solution.Moves), len(m.solution.States))
	fmt.Fprintf(&sb, "Stellung %d / %d\n\n", m.frame+1, len(m.solution.States))
	sb.WriteString("Zugfolge (LURD):\n")
	sb.WriteString(wrapText(m.solution.Moves, 60))
	right := stylePanel.Render(strings.TrimRight(sb.String(), "\n"))

	return lipgloss.JoinHorizontal(lipgloss.Top, left, " ", right)
}

// Statuszeile mit den Kennzahlen der laufenden Suche (Pendant zum Fenstertitel der alten GUI)
func (m Model) workLine() string {
	switch m.mode {
	case modeBlocker:
		stats := m.blk.GetStats()
		if stats.Status == blocker.StatusCreatePatterns {
			return styleHelp.Render(fmt.Sprintf("Stufe %d/%d | übrig: %s | Muster: %s | Bulk: %s",
				stats.CurrentBoxCount, stats.MaxBoxes-1, tools.FormatInt(stats.BadStates), tools.FormatInt(stats.FoundPatterns), tools.FormatInt(*m.bulkSize())))
		}
		return styleHelp.Render(fmt.Sprintf("Stufe %d/%d | offen: %s | bekannt: %s | Bulk: %s",
			stats.CurrentBoxCount, stats.MaxBoxes-1, tools.FormatInt(stats.OpenStates), tools.FormatInt(stats.KnownStates), tools.FormatInt(*m.bulkSize())))
	case modeSearch:
		return styleHelp.Render(fmt.Sprintf("Knoten: %s | Rest: %s | Tiefe: %d | Bulk: %s",
			tools.FormatInt(m.slv.NodeCount()), tools.FormatInt(m.slv.OpenCount()), m.slv.SearchDepth(), tools.FormatInt(*m.bulkSize())))
	}
	return ""
}

// eine Spalte der Tiefenstatistik: eine Zeile je Zugtiefe, aktuelle Tiefe markiert
func depthColumn(title string, open []int, current int) string {
	const maxRows = 22

	var sb strings.Builder
	sb.WriteString(title + "\n")

	// Fenster um die aktuelle Tiefe legen
	from := current - 2
	if from < 0 {
		from = 0
	}
	shown := 0
	for i := from; i < len(open) && shown < maxRows; i++ {
		if open[i] == 0 && i < current {
			continue // bereits abgearbeitete Tiefen ohne Inhalt überspringen
		}
		marker := "  "
		if i == current {
			marker = "->"
		}
		fmt.Fprintf(&sb, "%s[%3d] %s\n", marker, i, tools.FormatInt(open[i]))
		shown++
	}
	if len(open) > from+maxRows {
		sb.WriteString("  ...\n")
	}
	if shown == 0 {
		sb.WriteString("  (leer)\n")
	}
	return sb.String()
}

// bricht einen String hart nach width Zeichen um
func wrapText(text string, width int) string {
	var sb strings.Builder
	for len(text) > width {
		sb.WriteString(text[:width])
		sb.WriteByte('\n')
		text = text[width:]
	}
	sb.WriteString(text)
	return sb.String()
}
