package tui

import (
	"fmt"
	"os"
	"path/filepath"
	"runtime"
	"strings"
	"time"

	"github.com/charmbracelet/bubbles/textarea"
	tea "github.com/charmbracelet/bubbletea"

	"goSokoWahnBrute/blocker"
	"goSokoWahnBrute/maps"
	"goSokoWahnBrute/soko"
	"goSokoWahnBrute/solver"
)

// Anzeige-Modus der Oberfläche
type uiMode int

const (
	modeInput    uiMode = iota // Level-Eingabe (Text, Level-Nummer oder URL)
	modeBlocker                // Blocker-Vorberechnung (Pendant zum negativen Limit der alten GUI)
	modeSearch                 // bidirektionale Lösungssuche
	modeSolution               // fertige Lösung durchblättern
)

// Zeitbudget pro Auto-Tick (wie die alte GUI: ca. 10 Anzeige-Updates pro Sekunde)
const autoBudget = 100 * time.Millisecond

// Nachricht des Auto-Timers
type tickMsg time.Time

type Model struct {
	mode uiMode

	// --- Level-Eingabe ---
	input    textarea.Model
	inputErr string

	// --- Suche ---
	field       *soko.Field
	blk         *blocker.Blocker
	slv         *solver.Solver
	auto        bool   // Auto-Modus läuft
	bulkBlocker int    // Stellungen pro Bulk-Schritt im Blockerscan (groß: füttert alle Worker)
	bulkSearch  int    // Stellungen pro Bulk-Schritt in der Suche (fein: bessere UI-Granularität)
	ramLimit    uint64 // RAM-Notbremse in Bytes für den berechneten Verbrauch (0 = aus)
	ramStop     bool
	lastTick    time.Duration // Rechenzeit des letzten Auto-Ticks
	statesPerSec int64        // geglätteter Suchdurchsatz im Auto-Modus (verarbeitete Stellungen pro Sekunde)

	// --- Lösung ---
	solution *solver.Solution
	frame    int

	width  int
	height int
	status string
}

// erstellt das Anfangsmodell (initialLevel wird vorab ins Eingabefeld gelegt)
func NewModel(initialLevel string, ramLimitGB int) Model {
	input := textarea.New()
	input.Placeholder = "Level einfügen (#-Notation), game-sokoban.com-Nummer/URL eingeben\noder leer lassen für das Vanilla-Testlevel ..."
	input.CharLimit = 0
	input.SetWidth(76)
	input.SetHeight(16)
	input.Focus()
	if initialLevel != "" {
		input.SetValue(initialLevel)
	}

	return Model{
		mode:        modeInput,
		input:       input,
		bulkBlocker: 100000, // laut Benchmarks: große Batches halten alle Worker beschäftigt (~50ms pro Next)
		bulkSearch:  10000,  // Messung von Max: durchweg schneller als 1k (parallele Suche will Futter), 100k bringt nichts mehr und ruckelt nur
		ramLimit:    uint64(ramLimitGB) << 30,
		status:      "Level eingeben, dann Strg+S zum Scannen",
	}
}

// Bulk-Größe des aktuellen Modus (als Zeiger, damit +/- direkt anpassen kann)
func (m *Model) bulkSize() *int {
	if m.mode == modeBlocker {
		return &m.bulkBlocker
	}
	return &m.bulkSearch
}

func (m Model) Init() tea.Cmd {
	return textarea.Blink
}

// startet den Auto-Timer neu
func tickCmd() tea.Cmd {
	return tea.Tick(10*time.Millisecond, func(t time.Time) tea.Msg { return tickMsg(t) })
}

// prüft bei Enter, ob der Eingabe-Inhalt eine einzeilige Level-Nummer oder URL ist,
// und scannt dann direkt (true = Enter wurde verbraucht)
func (m *Model) scanOnEnter() bool {
	if !isWebInput(m.input.Value()) {
		return false
	}
	m.scan()
	return true
}

// erkennt eine einzeilige Level-Nummer oder game-sokoban.com-URL.
// Levelnotation wird bewusst NIE per Enter übernommen: beim Einfügen eines Levels kommen
// die Zeilenschaltungen als einzelne Enter-Tastendrücke an - dort muss Enter immer
// eine normale neue Zeile bleiben (Übernahme dann per Strg+S).
func isWebInput(text string) bool {
	text = strings.TrimSpace(text)
	return text != "" && !strings.Contains(text, "\n") && !strings.HasPrefix(text, "#")
}

// liest das Level ein und wechselt in den Blocker-Modus
func (m *Model) scan() {
	// Achtung: der Text darf hier NICHT komplett getrimmt werden - das würde nur der ersten
	// Zeile die Einrückung nehmen und sie gegen den Rest des Levels verschieben
	// (Parse entfernt selbst Leerzeilen und die gemeinsame Einrückung aller Zeilen)
	text := m.input.Value()
	if strings.TrimSpace(text) == "" {
		text = maps.MapVanilla
		m.input.SetValue(strings.TrimSpace(strings.ReplaceAll(maps.MapVanilla, "\t", "")))
	}

	// keine Levelnotation -> als Level-Nummer oder game-sokoban.com-URL versuchen
	var webInfo *WebLevelInfo
	if isWebInput(text) {
		webLevel, info, err := loadWebLevel(strings.TrimSpace(text))
		if err != nil {
			m.inputErr = err.Error()
			return
		}
		text = webLevel
		webInfo = info
		m.input.SetValue(webLevel)
	}

	field, err := soko.Parse(text)
	if err != nil {
		m.inputErr = err.Error()
		return
	}
	m.inputErr = ""
	m.field = field
	// Regel-Filter (Stufe 1: Freeze + Diagonale vorwärts, Pull-Freeze rückwärts)
	// vorbereiten - Default an, reaktiviert sich also mit jedem neuen Level;
	// Umschalten per Taste 5 in der Suche. Der Blocker-Stufenbau nutzt seine
	// eigene Regel-Instanz (nur vorwärts, siehe blocker.New).
	rules := soko.NewRules(field)
	field.SetRules(rules)
	field.SetRulesBackward(rules)
	m.closeWork() // alten Arbeitsstand samt Auslagerungsdateien freigeben
	m.slv = nil
	m.solution = nil
	m.auto = false
	m.ramStop = false
	// Max-Memory-Modus gilt nur gezielt für den aktuellen Lauf (Taste m in der Suche) -
	// beim neuen Level zurück auf den schnellen Standard, wie bei den Filter-Tasten
	solver.CompactMaxMemory = false

	// Blocker mit Datei-Cache anlegen (Wiederaufnahme über Läufe hinweg)
	cachePath := ""
	if err := os.MkdirAll("temp", 0755); err == nil {
		cachePath = filepath.Join("temp", blocker.CacheName(field))
	}
	m.blk = blocker.New(field, cachePath)
	field.SetBlocker(m.blk)

	// sind bereits alle Stufen im Cache, direkt zur Suche wechseln
	if stats := m.blk.GetStats(); len(stats.Stages) >= stats.MaxBoxes-1 {
		m.blk.Abort()
		m.startSearch()
		m.status = webInfoLine(webInfo) + "Blocker komplett aus dem Cache geladen"
		return
	}

	m.mode = modeBlocker
	m.status = webInfoLine(webInfo) + "Blockerscan bereit: s = Ministep, b = Bulk, a = Auto, Enter = beenden und Suche starten"
}

// baut die Statuszeilen-Info eines Web-Levels ("" wenn kein Web-Level geladen wurde)
func webInfoLine(info *WebLevelInfo) string {
	if info == nil {
		return ""
	}
	source := "geladen"
	if info.Cached {
		source = "aus dem Level-Cache"
	}
	line := fmt.Sprintf("Level %s %s: %s - %s (%s)", info.ID, source, info.Catalog, info.Name, info.Number)
	if info.BestMoves > 0 {
		line += fmt.Sprintf(" | Bestmoves: %d", info.BestMoves)
	}
	return line + " | "
}

// Stufenfolge der Such-Worker-Anzahl: von 1 verdoppelnd bis zur Kernzahl,
// darüber Vielfache der Kernzahl bis *8 (z.B. bei 12 Kernen: 1,2,4,8,12,24,48,96)
func workerStepsFor(cpu int) []int {
	var steps []int
	for v := 1; v < cpu; v *= 2 {
		steps = append(steps, v)
	}
	for mul := 1; mul <= 8; mul *= 2 {
		steps = append(steps, cpu*mul)
	}
	return steps
}

// schaltet die Such-Worker eine Stufe hoch oder runter (Tasten * und /)
func (m *Model) changeWorkers(up bool) {
	if m.slv == nil {
		return
	}
	steps := workerStepsFor(runtime.NumCPU())
	cur := m.slv.Workers()
	if up {
		for _, v := range steps {
			if v > cur {
				m.slv.SetWorkers(v)
				break
			}
		}
	} else {
		for i := len(steps) - 1; i >= 0; i-- {
			if steps[i] < cur {
				m.slv.SetWorkers(steps[i])
				break
			}
		}
	}
	m.status = fmt.Sprintf("Such-Worker: %d", m.slv.Workers())
}

// gibt Solver und Blocker samt ihrer Auslagerungsdateien frei
// (beim Levelwechsel und beim Beenden der Oberfläche)
func (m Model) closeWork() {
	if m.slv != nil {
		m.slv.Close()
	}
	if m.blk != nil {
		m.blk.Abort() // gibt auch den Arbeitszustand der laufenden Stufe frei
	}
}

// wechselt vom Blockerscan in die Lösungssuche
func (m *Model) startSearch() {
	m.slv = solver.New(m.field)
	m.mode = modeSearch
	m.statesPerSec = 0
	m.status = "Suche bereit: s = Einzelschritt, b = Bulk, a = Auto"
}

// schließt die Suche ab (Lösung anzeigen oder Fehlschlag melden)
func (m *Model) finishSearch() {
	m.auto = false
	stats := m.slv.GetStats()
	if stats.FoundMoves < 0 {
		m.status = "Suche abgeschlossen: keine Lösung vorhanden"
		return
	}

	// unter den zugoptimalen Lösungen die mit den wenigsten Schüben nehmen
	// (Webseiten-Bewertung: erst Züge, dann Schübe; fällt intern auf die
	// einfache Rekonstruktion zurück, wenn nichts zu optimieren ist)
	solution, err := m.slv.GetSolutionBestPushes()
	if err != nil {
		m.status = "Fehler bei der Lösungs-Rekonstruktion: " + err.Error()
		return
	}
	m.solution = solution
	m.frame = 0
	m.mode = modeSolution
	m.status = "Lösung gefunden - mit Pfeiltasten blättern"
}
