package tui

import (
	"fmt"
	"strings"
	"time"

	"github.com/atotto/clipboard"
	"github.com/charmbracelet/bubbles/textarea"
	tea "github.com/charmbracelet/bubbletea"

	"goSokoWahnBrute/solver"
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
	msg.Runes = []rune(normalizeNewlines(string(msg.Runes)))
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
		case "ctrl+v":
			// Einfügen bewusst selbst erledigen: das Textfeld würde die Zwischenablage
			// zwar auch lesen, dabei aber aus '\r' UND '\n' je eine neue Zeile machen
			// (siehe normalizeNewlines) - Windows-Levels bekämen so lauter Leerzeilen
			text, err := readClipboard()
			if err != nil {
				m.inputErr = "Zwischenablage nicht lesbar: " + err.Error()
				return m, nil
			}
			m.input.InsertString(normalizeNewlines(text))
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
		case "c": // Spielfeld (Level-Notation) in die Zwischenablage kopieren
			return m.copyField()
		case "i": // neues Level eingeben
			return m.enterInput()
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
			if note := m.slv.TakeArchiveNote(); note != "" {
				m.status = note
			}
			return m, nil
		case "b": // Bulk-Schritt
			if !m.slv.Step(m.bulkSearch) {
				m.finishSearch()
			}
			if note := m.slv.TakeArchiveNote(); note != "" {
				m.status = note
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
		case "1": // Richtung erzwingen: nur vorwärts suchen
			m.slv.SetDirMode(solver.DirForward)
			m.status = "Richtung: nur vorwärts (3 = wieder automatisch)"
			return m, nil
		case "2": // Richtung erzwingen: nur rückwärts suchen
			m.slv.SetDirMode(solver.DirBackward)
			m.status = "Richtung: nur rückwärts (3 = wieder automatisch)"
			return m, nil
		case "3": // Richtungswahl wieder der Automatik überlassen
			m.slv.SetDirMode(solver.DirAuto)
			m.status = "Richtung: automatisch (kleinere Hashtabelle zuerst)"
			return m, nil
		case "4": // Blocker-Filter an/aus (Default: an, reaktiviert sich bei neuem Level)
			on := !m.slv.BlockerEnabled()
			m.slv.SetBlockerEnabled(on)
			m.status = "Blocker-Filter: " + onOff(on)
			return m, nil
		case "5": // Regel-Filter an/aus (Freeze + Diagonale + Matching)
			on := !m.slv.RulesEnabled()
			m.slv.SetRulesEnabled(on)
			m.status = "Regel-Filter (Freeze+Diagonale+Matching): " + onOff(on)
			return m, nil
		case "6": // Ziel-Matching (Regel-Stufe 2) einzeln an/aus (teuerster Regel-Teil)
			on := !m.slv.MatchEnabled()
			m.slv.SetMatchEnabled(on)
			m.status = "Ziel-Matching (Regel-Stufe 2): " + onOff(on)
			return m, nil
		case "h": // Hashing: die Tabelle mit dem vollsten CompactTable-Teil ins Archiv-Format verdichten
			m.status = m.slv.ArchiveLargerTable()
			return m, nil
		case "m": // Max-Memory-Modus: Hash-Resize erst bei 93,75% statt 75% Füllstand
			on := !solver.CompactMaxMemory
			solver.CompactMaxMemory = on
			m.status = "Max-Memory-Modus (Hash-Resize erst bei 125 % Anzeige-Füllstand): " + onOff(on)
			return m, nil
		case "*": // Such-Worker eine Stufe hoch
			m.changeWorkers(true)
			return m, nil
		case "/": // Such-Worker eine Stufe runter
			m.changeWorkers(false)
			return m, nil
		case "c": // Spielfeld (Level-Notation) in die Zwischenablage kopieren
			return m.copyField()
		case "i": // neues Level eingeben
			return m.enterInput()
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
		case "c": // Zugfolge (LURD) in die Zwischenablage kopieren
			if err := writeClipboard(m.solution.Moves); err != nil {
				m.status = "Zwischenablage nicht beschreibbar: " + err.Error()
				return m, nil
			}
			m.status = fmt.Sprintf("Zugfolge kopiert (%d Züge)", len(m.solution.Moves))
			return m, nil
		case "i": // neues Level eingeben
			return m.enterInput()
		}
		return m, nil
	}

	return m, nil
}

// formatiert einen Schalter-Zustand für die Statuszeile
func onOff(on bool) string {
	if on {
		return "an"
	}
	return "aus"
}

// liest bzw. schreibt die Zwischenablage (als Variablen, damit Tests sie ersetzen können)
var readClipboard = clipboard.ReadAll
var writeClipboard = clipboard.WriteAll

// vereinheitlicht Zeilenenden auf '\n' (Windows CRLF und alte Mac-CR).
// Hintergrund: der Sanitizer des Textfeldes (bubbles/runeutil) behandelt '\r' und '\n'
// unabhängig voneinander und macht aus BEIDEN je eine neue Zeile - ein CRLF-Level
// bekäme also nach jeder Zeile eine zusätzliche Leerzeile. Einzeln getippte
// Zeilenschaltungen sind nicht betroffen (die kommen als Enter-Taste ohne Runen an).
func normalizeNewlines(text string) string {
	if !strings.ContainsRune(text, '\r') {
		return text
	}
	return strings.ReplaceAll(strings.ReplaceAll(text, "\r\n", "\n"), "\r", "\n")
}

// kopiert das Spielfeld (Level-Notation) in die Zwischenablage
// (Taste c im Blocker- und Such-Modus, bewusst ohne Eintrag in der Hilfezeile)
func (m Model) copyField() (tea.Model, tea.Cmd) {
	if err := writeClipboard(m.field.String()); err != nil {
		m.status = "Zwischenablage nicht beschreibbar: " + err.Error()
		return m, nil
	}
	m.status = "Spielfeld kopiert"
	return m, nil
}

// wechselt zur Level-Eingabe für ein neues Level (Eingabefeld wird geleert,
// da scan() dort z.B. bei URL-Eingaben das geladene Level abgelegt hat)
func (m Model) enterInput() (tea.Model, tea.Cmd) {
	m.auto = false
	m.input.SetValue("")
	m.inputErr = ""
	m.mode = modeInput
	m.status = "Level eingeben, dann Strg+S zum Scannen"
	return m, textarea.Blink
}

// Auto-Tick: rechnet bis zum Zeitbudget weiter und plant den nächsten Tick
func (m Model) handleTick() (tea.Model, tea.Cmd) {
	if !m.auto {
		return m, nil
	}

	// RAM-Notbremse: Vergleichsbasis ist der berechnete Verbrauch (RamBytes,
	// dieselbe Basis wie RAM-Anzeige und Auslagerungs-Schwelle) - bewusst nicht
	// ReadMemStats: der echte Go-Heap enthält Runtime-Reserven und GC-Transienten
	// und hat auf einer 640-GB-Maschine gestoppt, obwohl die Suche selbst noch weit
	// unter dem Limit lag (Details bei solver.RamLimitBytes)
	if m.ramLimit > 0 {
		var used int64
		switch m.mode {
		case modeBlocker:
			used = m.blk.RamBytes()
		case modeSearch:
			used = m.slv.RamBytes()
		}
		if used > int64(m.ramLimit) {
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
		startProcessed := m.slv.ProcessedCount()
		for time.Now().Before(deadline) {
			if !m.slv.Step(m.bulkSearch) {
				m.finishSearch()
				m.lastTick = time.Since(start)
				return m, nil
			}
		}
		// Suchdurchsatz messen und exponentiell glätten (ruhige Anzeige beim Worker-Tuning);
		// gezählt werden verarbeitete Sätze, nicht neue Knoten - die Anzeige bleibt damit
		// auch in der Beweis-Endphase aussagekräftig, wenn kaum noch Neues dazukommt
		if elapsed := time.Since(start).Seconds(); elapsed > 0 {
			rate := float64(m.slv.ProcessedCount()-startProcessed) / elapsed
			m.statesPerSec = (m.statesPerSec*7 + int64(rate)*3) / 10
		}
		// automatische Archiv-Konvertierung sichtbar machen (seltenes Ereignis)
		if note := m.slv.TakeArchiveNote(); note != "" {
			m.status = note
		}
	default:
		m.auto = false
		return m, nil
	}

	m.lastTick = time.Since(start)
	return m, tickCmd()
}
