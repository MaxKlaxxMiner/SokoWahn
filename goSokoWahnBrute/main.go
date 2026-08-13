package main

import (
	"flag"
	"fmt"
	"os"
	"path/filepath"
	"runtime/debug"
	"time"

	"goSokoWahnBrute/blocker"
	"goSokoWahnBrute/maps"
	"goSokoWahnBrute/soko"
	"goSokoWahnBrute/solver"
	"goSokoWahnBrute/tools"
	"goSokoWahnBrute/tui"
)

func main() {
	// GC-Headroom drosseln: der Heap besteht fast nur aus wenigen Riesen-Slices
	// (Hashtabellen, Suchlisten-Puffer) mit kaum Pointern - der Default (100 = Ziel
	// 2x Live-Heap) verdoppelt sonst nur nutzlos den RAM-Verbrauch, 5% Reserve
	// reichen für das Kleinzeug locker
	debug.SetGCPercent(5)

	// echte Läufe starten mit groß vorbelegten Stellungs-Tabellen (2x 2,68 GB) -
	// Tests und Bibliotheks-Nutzung behalten den kleinen Default der Factory
	solver.TableFactory = solver.NewCompactTableLarge

	// Default der RAM-Notbremse: 85% des installierten RAM (15% Reserve für OS und
	// Fremdprozesse); nur wenn die Erkennung fehlschlägt, bleibt der alte Festwert
	defaultRAMLimitGB := 100
	if total := tools.TotalRAMBytes(); total > 0 {
		defaultRAMLimitGB = int(total * 85 / 100 >> 30)
	}

	cliMode := flag.Bool("cli", false, "Kommandozeilen-Modus ohne TUI (für Skripte und Orakel-Vergleiche)")
	useBlocker := flag.Bool("blocker", false, "CLI: Deadlock-Blocker vorberechnen (alle Stufen bis Kistenanzahl-1)")
	useRules := flag.Bool("rules", false, "CLI: regelbasierten Live-Deadlock-Filter aktivieren (Stufe 1+2: Freeze + Diagonale + Ziel-Matching); ändert die Knotenzahlen, für Orakel-Vergleiche weglassen")
	rulesCompare := flag.Bool("rulescompare", false, "CLI: Debug - Regeln parallel zum Blocker auswerten und die Überlappung ausgeben (impliziert -rules)")
	blockerStages := flag.Int("stages", 0, "CLI: nur die Blocker-Stufen bis N berechnen und ausgeben (ohne Suche, ohne Cache)")
	ramLimitGB := flag.Int("ram", defaultRAMLimitGB, "TUI: RAM-Notbremse in GB für den Auto-Modus (0 = aus; Standard: 85% des installierten RAM)")
	workers := flag.Int("workers", 0, "Anzahl der Worker für Blocker und Suche (0 = automatisch, 1 = seriell)")
	flag.Parse()

	// Auslagerung großer Suchlisten auf die Festplatte aktivieren und dabei
	// liegengebliebene Dateien abgestürzter Läufe aufräumen (älter als eine Woche;
	// parallele Prozesse stören sich dank der Zufallsnamen nicht). Ausgelagert wird
	// erst bei echtem Speicherdruck: unterhalb von solver.SpillRamThresholdBytes
	// bleiben die Listen komplett im RAM und schonen die Platte.
	// Existiert C:\temp\sokowahn (von Max angelegt, z.B. auf einer anderen Platte),
	// hat dieser Ordner Vorrang - sonst wie gehabt temp/ im Arbeitsverzeichnis.
	spillDir := `C:\temp\sokowahn`
	if info, err := os.Stat(spillDir); err != nil || !info.IsDir() {
		spillDir = "temp"
		if err := os.MkdirAll(spillDir, 0755); err != nil {
			spillDir = "" // kein Auslagerungs-Ordner verfügbar -> alles bleibt im RAM
		}
	}
	if spillDir != "" {
		solver.CleanupSpillFiles(spillDir, 7*24*time.Hour)
		solver.SpillDir = spillDir
	}

	// optionales Level aus Datei laden
	levelData := ""
	if flag.NArg() >= 1 {
		fileData, err := os.ReadFile(flag.Arg(0))
		if err != nil {
			panic(err)
		}
		levelData = string(fileData)
	}

	if *blockerStages > 0 {
		runBlockerOnly(levelData, *blockerStages, *workers)
		return
	}

	if !*cliMode {
		if err := tui.Run(levelData, *ramLimitGB); err != nil {
			panic(err)
		}
		return
	}

	runCli(levelData, *useBlocker, *useRules || *rulesCompare, *rulesCompare, *workers)
}

// berechnet nur die Blocker-Stufen bis einschließlich maxStages und gibt sie aus
// (ohne Suche und ohne Cache-Datei, für schnelle Orakel-Vergleiche)
func runBlockerOnly(levelData string, maxStages int, workers int) {
	if levelData == "" {
		levelData = maps.MapVanilla
	}

	field, err := soko.Parse(levelData)
	if err != nil {
		panic(err)
	}

	blk := blocker.New(field, "")
	if workers > 0 {
		blk.SetWorkers(workers)
	}
	for blk.Next(1000000000) {
		if len(blk.GetStats().Stages) >= maxStages {
			blk.Abort()
			break
		}
	}
	fmt.Print(blk)
}

// Kommandozeilen-Modus: Level lösen und Fortschritt als Text ausgeben
// (deterministische Ausgaben, direkt vergleichbar mit dem C#-Orakel refcli -
// sofern die optionalen Regel-Filter aus bleiben)
func runCli(levelData string, useBlocker, useRules, rulesCompare bool, workers int) {
	if levelData == "" {
		levelData = maps.MapVanilla
	}

	field, err := soko.Parse(levelData)
	if err != nil {
		panic(err)
	}

	fmt.Println(field)

	if useRules {
		rules := soko.NewRules(field)
		rules.CompareBlocker = rulesCompare
		field.SetRules(rules)
		field.SetRulesBackward(rules)
	}

	if useBlocker {
		if err := os.MkdirAll("temp", 0755); err != nil {
			panic(err)
		}
		blk := blocker.New(field, filepath.Join("temp", blocker.CacheName(field)))
		if workers > 0 {
			blk.SetWorkers(workers)
		}
		blockerStart := time.Now()
		for blk.Next(1000000000) {
		}
		fmt.Printf("Blocker fertig nach %s:\n%s\n", time.Since(blockerStart).Round(time.Millisecond), blk)
		field.SetBlocker(blk)
	}

	s := solver.New(field)
	defer s.Close() // Auslagerungsdateien der Suchlisten löschen
	if workers > 0 {
		s.SetWorkers(workers)
	}
	startTime := time.Now()
	lastDepth := -1

	// ganze Tiefenstufen pro Schritt (vergleichbar mit refcli-Standardaufruf)
	for s.Step(1000000000) {
		if depth := s.SearchDepth(); depth != lastDepth {
			lastDepth = depth
			fmt.Printf("Tiefe %4d: Knoten=%s Rest=%s\n", depth, tools.FormatInt(s.NodeCount()), tools.FormatInt(s.OpenCount()))
		}
	}

	stats := s.GetStats()
	fmt.Printf("\nFertig nach %s: SuchTiefe=%d Knoten=%s\n", time.Since(startTime).Round(time.Millisecond), s.SearchDepth(), tools.FormatInt(s.NodeCount()))

	if rules := field.Rules(); rules != nil {
		rst := rules.Stats()
		fmt.Printf("Regeln vorwärts: Freeze=%s Diagonale=%s Matching=%s | rückwärts: Totfeld=%s PullFreeze=%s\n",
			tools.FormatInt(rst.FreezeKills), tools.FormatInt(rst.DiagonalKills), tools.FormatInt(rst.MatchKills),
			tools.FormatInt(rst.PullDeadKills), tools.FormatInt(rst.PullFreezeKills))
		if rules.CompareBlocker {
			fmt.Printf("Vergleich: nurBlocker=%s nurRegeln=%s beide=%s\n",
				tools.FormatInt(rst.CmpBlockerOnly), tools.FormatInt(rst.CmpRulesOnly), tools.FormatInt(rst.CmpBoth))
		}
	}

	if stats.FoundMoves < 0 {
		fmt.Println("keine Lösung gefunden")
		return
	}

	solution, err := s.GetSolution()
	if err != nil {
		panic(err)
	}
	fmt.Printf("Lösung: %d Züge, %d Schub-Stellungen\n", len(solution.Moves), len(solution.States))
	fmt.Println(solution.Moves)
}
