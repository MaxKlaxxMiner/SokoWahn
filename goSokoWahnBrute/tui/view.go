package tui

import (
	"fmt"
	"strings"

	"github.com/charmbracelet/lipgloss"

	"goSokoWahnBrute/blocker"
	"goSokoWahnBrute/solver"
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
		return "Enter = Übernehmen (bei Nummer/URL) | Strg+S = Level scannen | Esc = Beenden (leer = Vanilla)"
	case modeBlocker:
		return "s = Ministep | b = Bulk | a/Leer = Auto | +/- = Bulkgröße | Enter = Blocker beenden -> Suche | i = Eingabe | q = Beenden"
	case modeSearch:
		return "s = Einzelschritt | b = Bulk | a/Leer = Auto | 1/2/3 = Richtung | +/- = Bulkgröße | *,/ = Worker | i = Eingabe | q = Beenden"
	case modeSolution:
		return "Pfeile/h/l = Blättern | Home/End = Anfang/Ende | c = Zugfolge kopieren | i = neues Level | q = Beenden"
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
		// wie das Original: Treffpunkt relativ zur Vorwärtstiefe und die Rest-Beweislücke
		// (die Suche läuft weiter, bis vorwärts + rückwärts die gefundene Tiefe erreicht)
		meet := stats.FoundForward - stats.ForwardDepth
		rest := stats.FoundMoves - stats.ForwardDepth - stats.BackwardDepth
		sb.WriteString(styleMark.Render(fmt.Sprintf("Gefunden: %d Züge", stats.FoundMoves)) +
			fmt.Sprintf("  (Treffpunkt: %+d / Rest: %d)\n\n", meet, rest))
	} else {
		// wie das Original: addierte Zugtiefe, Anzahl der Tiefenlisten und die geschätzte
		// Endtiefe (gewichtete Median-Tiefe der offenen Sätze beider Richtungen)
		estimate := medianDepth(stats.ForwardOpen) + medianDepth(stats.BackwardOpen)
		sb.WriteString(fmt.Sprintf("Tiefe: %d (%d + %d) - Listen: %d - geschätzt: %s\n",
			stats.ForwardDepth+stats.BackwardDepth, stats.ForwardDepth, stats.BackwardDepth,
			len(stats.ForwardOpen)+len(stats.BackwardOpen), formatDepth(estimate)))
		// Hochrechnung des Originals: erreichbare Suchtiefe je Hashtable-Budget
		if d100M, d1G, d3G, ok := m.slv.EstimateMaxDepths(); ok {
			sb.WriteString(fmt.Sprintf("max: %s / %s / %s (100M, 1G, 3G)\n",
				tools.FormatInt(d100M), tools.FormatInt(d1G), tools.FormatInt(d3G)))
		}
		sb.WriteByte('\n')
	}

	// manuell erzwungene Suchrichtung (Tasten 1/2) in der Spaltenüberschrift markieren
	forwardTitle, backwardTitle := "vorwärts", "rückwärts"
	switch m.slv.DirMode() {
	case solver.DirForward:
		forwardTitle = styleMark.Render("vorwärts [fix]")
	case solver.DirBackward:
		backwardTitle = styleMark.Render("rückwärts [fix]")
	}
	forward := depthColumn(forwardTitle, stats.ForwardOpen, stats.ForwardDepth)
	backward := depthColumn(backwardTitle, stats.BackwardOpen, stats.BackwardDepth)
	sb.WriteString(lipgloss.JoinHorizontal(lipgloss.Top, forward, "   ", backward))

	if m.auto {
		sb.WriteString("\n" + styleMark.Render("Auto läuft ...") + fmt.Sprintf("  (%s Stellungen/s)", tools.FormatInt(m.statesPerSec)))
	}
	right := stylePanel.Render(strings.TrimRight(sb.String(), "\n"))

	return lipgloss.JoinHorizontal(lipgloss.Top, left, " ", right) + "\n" + m.workLine()
}

func (m Model) viewSolution() string {
	state := &m.solution.States[m.frame]
	left := styleField.Render(strings.TrimRight(state.Debug(m.field), "\n"))

	done := m.solution.MoveOffsets[m.frame]
	var sb strings.Builder
	fmt.Fprintf(&sb, "Lösung: %d Züge, %d Schub-Stellungen\n", len(m.solution.Moves), len(m.solution.States))
	fmt.Fprintf(&sb, "Stellung %d / %d - Zug %d / %d\n\n", m.frame+1, len(m.solution.States), done, len(m.solution.Moves))
	sb.WriteString("Zugfolge (LURD, ausgeführte Züge markiert):\n")
	sb.WriteString(wrapMoves(m.solution.Moves, 60, done))
	right := stylePanel.Render(strings.TrimRight(sb.String(), "\n"))

	return lipgloss.JoinHorizontal(lipgloss.Top, left, " ", right)
}

// Statuszeile mit den Kennzahlen der laufenden Suche (Pendant zum Fenstertitel der alten GUI)
func (m Model) workLine() string {
	switch m.mode {
	case modeBlocker:
		stats := m.blk.GetStats()
		disk := diskInfo(m.blk.SpillBytes())
		if stats.Status == blocker.StatusCreatePatterns {
			return styleHelp.Render(fmt.Sprintf("Stufe %d/%d | übrig: %s | Muster: %s%s | Bulk: %s",
				stats.CurrentBoxCount, stats.MaxBoxes-1, tools.FormatInt(stats.BadStates), tools.FormatInt(stats.FoundPatterns), disk, tools.FormatInt(*m.bulkSize())))
		}
		if stats.Status == blocker.StatusMergeGoals {
			return styleHelp.Render(fmt.Sprintf("Stufe %d/%d | offen: %s | bekannt: %s | Rest: %s%s | Bulk: %s",
				stats.CurrentBoxCount, stats.MaxBoxes-1, tools.FormatInt(stats.OpenStates), tools.FormatInt(stats.KnownStates), tools.FormatInt(stats.MergeRest), disk, tools.FormatInt(*m.bulkSize())))
		}
		return styleHelp.Render(fmt.Sprintf("Stufe %d/%d | offen: %s | bekannt: %s%s | Bulk: %s",
			stats.CurrentBoxCount, stats.MaxBoxes-1, tools.FormatInt(stats.OpenStates), tools.FormatInt(stats.KnownStates), disk, tools.FormatInt(*m.bulkSize())))
	case modeSearch:
		return styleHelp.Render(fmt.Sprintf("Knoten: %s | Rest: %s%s | RAM: %s MB | Tiefe: %d | Worker: %d | Bulk: %s",
			tools.FormatInt(m.slv.NodeCount()), tools.FormatInt(m.slv.OpenCount()), diskInfo(m.slv.SpillBytes()), formatMB(m.slv.RamBytes()), m.slv.SearchDepth(), m.slv.Workers(), tools.FormatInt(*m.bulkSize())))
	}
	return ""
}

// formatiert Bytes als Megabytes mit zwei Nachkommastellen (z.B. "1.234,56")
func formatMB(bytes int64) string {
	hundredths := bytes * 100 >> 20
	return fmt.Sprintf("%s,%02d", tools.FormatInt(hundredths/100), hundredths%100)
}

// Anhang der Statuszeile mit den auf die Festplatte ausgelagerten Suchlisten-Bytes
// ("" solange alles im RAM liegt)
func diskInfo(bytes int64) string {
	if bytes <= 0 {
		return ""
	}
	return " | Disk: " + tools.FormatInt(bytes>>20) + " MB"
}

// gewichtete Median-Tiefe der offenen Sätze einer Richtung: die Tiefe, unter der die
// Hälfte der restlichen Arbeit liegt (Schätzwert des Originals für die Endtiefe)
func medianDepth(open []int) float64 {
	var sum int64
	for _, n := range open {
		sum += int64(n)
	}
	if sum == 0 {
		return 0
	}
	half := sum / 2
	for i, n := range open {
		if int64(n) > half {
			return float64(i) + float64(half)/float64(n)
		}
		half -= int64(n)
	}
	return float64(len(open))
}

// formatiert eine Tiefen-Schätzung mit zwei Nachkommastellen (deutsches Komma)
func formatDepth(v float64) string {
	return strings.ReplaceAll(fmt.Sprintf("%.2f", v), ".", ",")
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

// bricht die Zugfolge hart nach width Zeichen um und hebt die ersten done Züge farblich hervor
// (zeigt beim Blättern, wo man sich in der Zugfolge gerade befindet)
func wrapMoves(text string, width, done int) string {
	var sb strings.Builder
	for pos := 0; pos < len(text); pos += width {
		if pos > 0 {
			sb.WriteByte('\n')
		}
		end := pos + width
		if end > len(text) {
			end = len(text)
		}
		line := text[pos:end]
		switch {
		case done >= end:
			sb.WriteString(styleMark.Render(line))
		case done > pos:
			sb.WriteString(styleMark.Render(line[:done-pos]))
			sb.WriteString(line[done-pos:])
		default:
			sb.WriteString(line)
		}
	}
	return sb.String()
}
