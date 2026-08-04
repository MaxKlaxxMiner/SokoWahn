package tui

import (
	"fmt"
	"runtime"
	"time"

	"github.com/charmbracelet/bubbles/textarea"
	tea "github.com/charmbracelet/bubbletea"
)

func (m Model) Update(msg tea.Msg) (tea.Model, tea.Cmd) {
	switch msg := msg.(type) {
	case tea.WindowSizeMsg:
		m.width = msg.Width
		m.height = msg.Height
		return m, nil

	case tea.KeyMsg:
		return m.handleKey(msg)

	case tickMsg:
		return m.handleTick()
	}

	if m.mode == modeInput {
		var cmd tea.Cmd
		m.input, cmd = m.input.Update(msg)
		return m, cmd
	}
	return m, nil
}

func (m Model) handleKey(msg tea.KeyMsg) (tea.Model, tea.Cmd) {
	key := msg.String()

	// global: Strg+C beendet immer
	if key == "ctrl+c" {
		return m, tea.Quit
	}

	switch m.mode {
	case modeInput:
		switch key {
		case "ctrl+s":
			m.scan()
			return m, nil
		case "enter":
			// Enter übernimmt den Inhalt direkt, wenn er offensichtlich komplett ist
			// (einzeilig = Nummer/URL, oder mehrzeilig und als Level parsebar);
			// sonst normale neue Zeile fürs manuelle Tippen
			if m.scanOnEnter() {
				return m, nil
			}
		case "esc":
			return m, tea.Quit
		}
		var cmd tea.Cmd
		m.input, cmd = m.input.Update(msg)
		return m, cmd

	case modeBlocker:
		switch key {
		case "q":
			return m, tea.Quit
		case "s": // Ministep: eine einzelne Kombination bzw. Stellung
			if !m.blk.Next(1) {
				m.startSearch()
			}
			return m, nil
		case "b": // Bulk-Schritt
			if !m.blk.Next(m.bulkBlocker) {
				m.startSearch()
			}
			return m, nil
		case "a", " ": // Auto-Modus umschalten
			m.auto = !m.auto
			m.ramStop = false
			if m.auto {
				return m, tickCmd()
			}
			return m, nil
		case "enter": // Blockerscan beenden, mit den fertigen Stufen weitermachen
			m.auto = false
			m.blk.Abort()
			m.startSearch()
			return m, nil
		case "+":
			*m.bulkSize() *= 10
			return m, nil
		case "-":
			if *m.bulkSize() >= 10 {
				*m.bulkSize() /= 10
			}
			return m, nil
		case "i": // zurück zur Eingabe
			m.auto = false
			m.mode = modeInput
			return m, textarea.Blink
		}
		return m, nil

	case modeSearch:
		switch key {
		case "q":
			return m, tea.Quit
		case "s": // Einzelschritt: eine Stellung
			if !m.slv.Step(1) {
				m.finishSearch()
			}
			return m, nil
		case "b": // Bulk-Schritt
			if !m.slv.Step(m.bulkSearch) {
				m.finishSearch()
			}
			return m, nil
		case "a", " ":
			m.auto = !m.auto
			m.ramStop = false
			if m.auto {
				return m, tickCmd()
			}
			return m, nil
		case "+":
			*m.bulkSize() *= 10
			return m, nil
		case "-":
			if *m.bulkSize() >= 10 {
				*m.bulkSize() /= 10
			}
			return m, nil
		case "i":
			m.auto = false
			m.mode = modeInput
			return m, textarea.Blink
		}
		return m, nil

	case modeSolution:
		switch key {
		case "q":
			return m, tea.Quit
		case "left", "h":
			if m.frame > 0 {
				m.frame--
			}
			return m, nil
		case "right", "l":
			if m.frame < len(m.solution.States)-1 {
				m.frame++
			}
			return m, nil
		case "home":
			m.frame = 0
			return m, nil
		case "end":
			m.frame = len(m.solution.States) - 1
			return m, nil
		case "i": // neues Level eingeben
			m.mode = modeInput
			return m, textarea.Blink
		}
		return m, nil
	}

	return m, nil
}

// Auto-Tick: rechnet bis zum Zeitbudget weiter und plant den nächsten Tick
func (m Model) handleTick() (tea.Model, tea.Cmd) {
	if !m.auto {
		return m, nil
	}

	// RAM-Notbremse (nicht bei jedem Tick, ReadMemStats ist nicht kostenlos)
	m.ticks++
	if m.ramLimit > 0 && m.ticks%16 == 0 {
		var mem runtime.MemStats
		runtime.ReadMemStats(&mem)
		if mem.Alloc > m.ramLimit {
			m.auto = false
			m.ramStop = true
			m.status = fmt.Sprintf("RAM-Stop: %d GB überschritten (a = trotzdem weiter)", m.ramLimit>>30)
			return m, nil
		}
	}

	start := time.Now()
	deadline := start.Add(autoBudget)

	switch m.mode {
	case modeBlocker:
		for time.Now().Before(deadline) {
			if !m.blk.Next(m.bulkBlocker) {
				m.startSearch() // Stufe KistenAnzahl-1 fertig -> automatisch die echte Suche starten
				m.lastTick = time.Since(start)
				return m, tickCmd() // Auto-Modus läuft in der Suche weiter
			}
		}
	case modeSearch:
		for time.Now().Before(deadline) {
			if !m.slv.Step(m.bulkSearch) {
				m.finishSearch()
				m.lastTick = time.Since(start)
				return m, nil
			}
		}
	default:
		m.auto = false
		return m, nil
	}

	m.lastTick = time.Since(start)
	return m, tickCmd()
}
