package main

import (
	"flag"
	"fmt"
	"net/http"
	_ "net/http/pprof" // Diagnose-Endpunkte für -debugport (Goroutine-Stacks, Heap-Profil)
	"os"
	"path/filepath"
	"runtime/debug"
	"strings"
	"time"

	"goSokoWahnBrute/blocker"
	"goSokoWahnBrute/maps"
	"goSokoWahnBrute/soko"
	"goSokoWahnBrute/solver"
	"goSokoWahnBrute/tools"
	"goSokoWahnBrute/tui"
)

func main() {
	// echte Läufe starten mit groß vorbelegten Stellungs-Tabellen (2x 2,68 GB) -
	// Tests und Bibliotheks-Nutzung behalten den kleinen Default der Factory
	solver.TableFactory = solver.NewCompactTableLarge

	// Defaults der beiden RAM-Grenzen anteilig am installierten RAM; nur wenn die
	// Erkennung fehlschlägt, bleiben die alten Festwerte der 64-GB-Maschine.
	// Beide Grenzen messen den berechneten Verbrauch der Suche (RamBytes, siehe
	// solver.RamLimitBytes - bewusst kein ReadMemStats). Notbremse 85% (15% Reserve
	// für OS und Fremdprozesse), Auslagerungs-Schwelle 70% (entspricht dem erprobten
	// 44-GB-Wert bei 64 GB). Eskalations-Reihenfolge bei Speicherdruck: ab 70%
	// lagern die Suchlisten auf die Platte aus, Tabellen-Verdopplungen, deren
	// Umkopier-Spitze (Verbrauch + 2x Tabellengröße) die 85% rechnerisch reißen
	// würde, weichen ins Archiv-Format aus (solver.autoArchive), und erst wenn
	// der berechnete Verbrauch die 85% wirklich überschreitet, stoppt der Auto-Modus
	defaultRAMLimitGB := 100
	defaultSpillRAMGB := 44
	if total := tools.TotalRAMBytes(); total > 0 {
		defaultRAMLimitGB = int(total * 85 / 100 >> 30)
		defaultSpillRAMGB = int(total * 70 / 100 >> 30)
	}

	cliMode := flag.Bool("cli", false, "Kommandozeilen-Modus ohne TUI (für Skripte und Referenz-Läufe); Blocker und Regel-Filter laufen wie in der TUI immer mit")
	blockerStages := flag.Int("stages", 0, "CLI: nur die Blocker-Stufen bis N berechnen und ausgeben (ohne Suche, ohne Cache)")
	ramLimitGB := flag.Int("ram", defaultRAMLimitGB, "RAM-Notbremse in GB für den berechneten Verbrauch (0 = aus; Standard: 85% des installierten RAM; Tabellen weichen vorher ins Archiv-Format aus, das TUI stoppt den Auto-Modus)")
	spillRAMGB := flag.Int("spillram", defaultSpillRAMGB, "RAM-Schwelle in GB, ab der Suchlisten auf die Platte auslagern (0 = sofort auslagern; Standard: 70% des installierten RAM)")
	workers := flag.Int("workers", 0, "Anzahl der Worker für Blocker und Suche (0 = automatisch, 1 = seriell)")
	debugPort := flag.Int("debugport", 0, "Diagnose: pprof-HTTP-Server auf localhost:PORT (0 = aus); bei einem Hänger liefert curl localhost:PORT/debug/pprof/goroutine?debug=2 alle Stacks und .../heap?debug=1 die Speicher-Verteilung, ohne den Prozess zu beenden")
	checkSolFile := flag.String("checksol", "", "Diagnose: LURD-Datei einer bekannten zugoptimalen Lösung - nach Abschluss der Suche wird geprüft, wie der Pfad in den Hashtabellen/Ankern der Push-Optimierung repräsentiert ist (TUI: Report als <datei>.report.txt, CLI: Ausgabe am Ende)")
	gcPercent := flag.Int("gc", 5, "GC-Reserve in Prozent des Live-Heaps (Go GOGC): 5 = sparsam (Standard, kaum Overhead bei den Riesen-Slices der Suche), 100 = Go-Default mit weniger GC-Läufen - auf Servern mit reichlich RAM eine Option")
	flag.Parse()

	// GC-Headroom drosseln: der Heap besteht fast nur aus wenigen Riesen-Slices
	// (Hashtabellen, Suchlisten-Puffer) mit kaum Pointern - der Go-Default (100 =
	// Ziel 2x Live-Heap) verdoppelt sonst nur nutzlos den RAM-Verbrauch, 5% Reserve
	// reichen für das Kleinzeug locker. Per -gc übersteuerbar (z.B. -gc 100 auf
	// Servern, wo der Speicher egal ist und seltenere GC-Läufe Tempo sparen)
	debug.SetGCPercent(*gcPercent)

	// Diagnose-Server (nur localhost): läuft in eigener Goroutine und funktioniert
	// auch dann noch, wenn der Haupt-Loop hängt - genau dafür ist er da
	if *debugPort > 0 {
		go func() {
			if err := http.ListenAndServe(fmt.Sprintf("localhost:%d", *debugPort), nil); err != nil {
				fmt.Fprintf(os.Stderr, "debugport %d nicht verfügbar: %v\n", *debugPort, err)
			}
		}()
	}

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
	solver.SpillRamThresholdBytes = int64(*spillRAMGB) << 30
	solver.RamLimitBytes = int64(*ramLimitGB) << 30

	// optionales Level aus Datei laden
	levelData := ""
	if flag.NArg() >= 1 {
		fileData, err := os.ReadFile(flag.Arg(0))
		if err != nil {
			panic(err)
		}
		levelData = string(fileData)
	}

	// optionale Referenz-Lösung für die Diagnose laden - Fehler sofort melden,
	// nicht erst nach einem stundenlangen Suchlauf (Datei fehlt, keine LURD-Zeichen)
	checkSol := ""
	if *checkSolFile != "" {
		fileData, err := os.ReadFile(*checkSolFile)
		if err != nil {
			panic(err)
		}
		checkSol = strings.TrimSpace(string(fileData))
		for i := 0; i < len(checkSol); i++ {
			switch checkSol[i] {
			case 'l', 'u', 'r', 'd', 'L', 'U', 'R', 'D':
			default:
				panic(fmt.Sprintf("-checksol %s: ungültiges LURD-Zeichen %q an Position %d", *checkSolFile, checkSol[i], i))
			}
		}
		if checkSol == "" {
			panic(fmt.Sprintf("-checksol %s: Datei enthält keine LURD-Zugfolge", *checkSolFile))
		}
	}

	if *blockerStages > 0 {
		runBlockerOnly(levelData, *blockerStages, *workers)
		return
	}

	if !*cliMode {
		if err := tui.Run(levelData, *ramLimitGB, checkSol, *checkSolFile); err != nil {
			panic(err)
		}
		return
	}

	runCli(levelData, *workers, checkSol)
}

// berechnet nur die Blocker-Stufen bis einschließlich maxStages und gibt sie aus
// (ohne Suche und ohne Cache-Datei, für schnelle Stufen-Diffs zwischen Go-Ständen)
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
// (deterministische Ausgaben mit fester Filter-Konfiguration - Blocker inkl.
// temp/-Cache und Regel-Filter immer an, identisch zur TUI; Tiefenzeilen sind
// damit zwischen Go-Ständen byte-gleich diffbar)
func runCli(levelData string, workers int, checkSol string) {
	if levelData == "" {
		levelData = maps.MapVanilla
	}

	field, err := soko.Parse(levelData)
	if err != nil {
		panic(err)
	}

	fmt.Println(field)

	rules := soko.NewRules(field)
	field.SetRules(rules)
	field.SetRulesBackward(rules)

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

	s := solver.New(field)
	defer s.Close() // Auslagerungsdateien der Suchlisten löschen
	if workers > 0 {
		s.SetWorkers(workers)
	}
	startTime := time.Now()
	lastDepth := -1

	// ganze Tiefenstufen pro Schritt
	for s.Step(1000000000) {
		if depth := s.SearchDepth(); depth != lastDepth {
			lastDepth = depth
			fmt.Printf("Tiefe %4d: Knoten=%s Rest=%s\n", depth, tools.FormatInt(s.NodeCount()), tools.FormatInt(s.OpenCount()))
		}
	}

	stats := s.GetStats()
	fmt.Printf("\nFertig nach %s: SuchTiefe=%d Knoten=%s\n", time.Since(startTime).Round(time.Millisecond), s.SearchDepth(), tools.FormatInt(s.NodeCount()))

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

	// Diagnose der Push-Optimierung gegen eine Referenz-Lösung (Flag -checksol);
	// hängt nur zusätzliche Ausgaben an - die Zeilen oben bleiben unverändert
	if checkSol != "" {
		best, err := s.GetSolutionBestPushes()
		if err != nil {
			fmt.Printf("Push-Optimierung fehlgeschlagen: %v\n", err)
		} else {
			fmt.Printf("\nPush-optimiert: %d Züge, %d Schübe\n", len(best.Moves), solver.CountPushes(best.Moves))
		}
		report, err := s.CheckSolution(checkSol)
		if err != nil {
			fmt.Printf("Checksol fehlgeschlagen: %v\n", err)
			return
		}
		fmt.Print(report)
	}
}
