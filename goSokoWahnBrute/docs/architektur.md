# Architektur goSokoWahnBrute

Go-Nachbau des C#-Solvers `SokoWahn_4th_generation` (bidirektionale Breitensuche über Moves)
mit Blocker-Deadlock-Vorberechnung nach `SokowahnBlockerBx`-Semantik und Bubble-Tea-TUI.

## Pakete

| Paket | Inhalt |
|---|---|
| `soko` | Spielfeld, Parsen, Zuggeneratoren (vorwärts/rückwärts), Zielstellungen, LURD-Ableitung |
| `solver` | bidirektionaler Such-Treiber, Hashtabellen-Interface, Tiefenlisten, Lösungs-Rekonstruktion |
| `blocker` | Deadlock-Vorberechnung (k-Steiner-Muster) mit Step-API und gzip-Cache |
| `tui` | Terminal-Oberfläche (Bubble Tea) inkl. game-sokoban.com-Loader |
| `crc64` | FNV-1a-64-Hashing (Fluent-API), trotz Namens kein echtes CRC |
| `tools` | Kleinkram: ClearBools, UnsafeString, FormatInt (Tausender-Punkte, generisch) |
| `maps` | Test-Levels |

## Kern-Datenstrukturen (Paket soko)

- Nur begehbare Felder werden zu kompakten Indizes verdichtet: `Wpos` (uint32), vergeben in
  Lesereihenfolge. `walkLeft/Right/Up/Down []Wpos` sind die Nachbar-Tabellen.
- Sentinel-Muster: `walkEof` = "Wand/nicht begehbar", `boxCount` = "keine Kiste".
  `wposToBoxes` hat Länge walkEof+1, `tmpCheckDone` ebenfalls (letztes Element dauerhaft true) -
  spart Randabfragen in den inneren Schleifen.
- Doppel-Indizierung `boxes[i] <-> wposToBoxes[pos]` erlaubt O(1)-Push und O(1)-Undo.
- **Kanonik**: `boxes` ist immer aufsteigend sortiert. Nur vertikale Schübe können die Ordnung
  verletzen -> `sortBoxesUp/Down` (einzelne Insertion-Schritte), beim Undo rückwärts.
- Stellungs-Hash: FNV-1a über Spielerposition + sortierte Kistenpositionen (`State.UpdateCrc`).
- Zuggenerator = Flood-Fill des Spielers, an jeder erreichten Kiste wird der Schub (bzw.
  rückwärts der Zug) geprüft. Wichtig: Kistenfelder werden im Flood-Fill NICHT als "fertig"
  markiert, damit jede Richtung ihren eigenen Schub probiert.
- Optionale Filter am Feld: `SetBlocker` (vorwärts) und `SetBlockerBackward` (rückwärts,
  nur Blocker-Stufenbau und Solver-Rückwärtssuche).
- **Freeze-Filter beim Parsen** (`freeze.go`, JSoko-Verhalten, immer aktiv): eingefrorene
  Kisten auf Zielfeldern werden durch Wände ersetzt, Kiste und Ziel entfallen ersatzlos
  (kaskadierend bis zum Fixpunkt, erkennt auch gegenseitig blockierte 2x2-Blöcke;
  Kisten abseits der Ziele zählen konservativ nie als Blockade). Das C#-Orakel wendet
  denselben Filter an (FreezeGoalBoxesToWalls in refcli/Program.cs) - Diff-Vergleiche
  bleiben auch bei betroffenen Levels byte-gleich (verifiziert per refcli-Diff auf einem
  Level mit einfrierender *-Spalte; Transformation als Tests in soko/freeze_test.go verankert).
  Die verankerten Referenz-Levels (Vanilla, small, lid201, SolvedStart) enthalten nur
  bewegliche *-Kisten und sind unverändert. Gleicher Filter auch in goSokoWahnRooms.

## Solver (bidirektionale Suche)

- Vorwärts ab Startstellung, rückwärts ab allen Zielstellungen (`SearchGoalStates`:
  alle Kisten auf Zielen, Spieler auf pull-fähigem Nachbarfeld).
- **Natürliche Tiefen**: beide Richtungen zählen 0,1,2,... als uint16; 65535 = unbekannt
  (`DepthUnknown`). Gesamtlösung beim Treffen = vorwärts + rückwärts. Kein 60000er-Offset
  wie im Original - die Zahlen sind trotzdem 1:1 vergleichbar.
- Je Richtung: Hashtabelle (`PosTable`) + Suchlisten je Tiefe
  (`DepthList`, flache uint16-Sätze: Spieler + Kisten; Tiefe steckt im Listenindex).
- **Disk-Auslagerung der Suchlisten** (Muster von SokowahnLinearList2): wächst eine Liste
  über `solver.SpillBufferBytes` (16 MB wie das C#-Original; 64 MB summierten sich bei
  hunderten aktiven Listen auf zweistellige GB RAM), wandert der Schreibpuffer blockweise in eine
  Temp-Datei (`sokolist_*.tmp`, Zufallsname via os.CreateTemp - mehrere Prozesse
  stören sich nicht); gelesen wird sequenziell über einen gleich großen Lesepuffer.
  Ordner-Wahl beim Programmstart: `C:\temp\sokowahn` falls vorhanden (bewusst von Hand
  anzulegen, z.B. auf einer anderen Platte), sonst `temp/` im Arbeitsverzeichnis.
  Datei-Handles werden pro Blockzugriff geöffnet und sofort wieder geschlossen: es
  können hunderte Listen gleichzeitig aktiv sein (Laufzug-Tiefen verteilen Pushes über
  viele Ziel-Tiefen), und NTFS-Komprimierung arbeitet erst nach dem Schließen richtig
  (Erfahrungswert aus der C#-Version). Aus demselben Grund gibt es kein Puffer-
  Recycling - jede aktive Liste hält höchstens einen Schreibpuffer.
  Schreiben und Vorauslesen laufen als Hintergrund-Goroutinen mit Doppel-Pufferung
  (je Liste höchstens ein Schreib- und ein Lesevorgang gleichzeitig) - die Suche
  arbeitet währenddessen weiter und wartet nur, wenn die Platte nicht hinterherkommt.
  Die FIFO-Reihenfolge bleibt exakt erhalten -> Suchverhalten bitgenau wie die reine
  RAM-Variante (Tests: TestSolveSpillDeterminism, TestSolveVanillaSpillOracle). Auch die
  vier Blocker-Stufenlisten lagern so aus. `Release`/`Solver.Close`/`Blocker.Abort`
  löschen die Dateien; beim Programmstart räumt `CleanupSpillFiles` Reste abgestürzter
  Läufe weg (älter als eine Woche - üppige Reserve, denn länger als ein paar Stunden
  läuft keine Suche, die Hashtabellen füllen 128 GB RAM in 3-4 Stunden). Ohne
  gesetztes `solver.SpillDir` (z.B. in Tests) bleibt alles im RAM.
- `PosTable`-Implementierung ist die `CompactTable`: offene Adressierung mit linearem
  Sondieren, 10 Byte pro Slot (voller 64-Bit-Schlüssel + uint16-Tiefe, verlustfrei),
  crc==0 als Frei-Marker (Sondieren berührt nur das Schlüssel-Array), Verdopplung bei
  75% Füllstand. Die map-Variante bleibt als Vergleichs-Referenz erhalten.
- Hashtable-Shootout unter **Realbedingungen** (echter Blocker-Workload lid349 bis
  4-Steiner, 4 Worker, je 2 Läufe, historisches Ergebnis vom 05.08.2026):
  CompactTable 6,3 s, builtin map 6,8 s, cockroachdb/swiss 6,9 s, brentp/intintmap 6,9 s,
  dolthub/swiss 7,4 s, tidwall/hashmap 7,8 s, puzpuzpuz/xsync 8,0 s (einzige Concurrent-Map
  im Limit); alphadose/haxmap und cornelk/hashmap DNF (>12 s, brechen unter Masseninserts ein).
  Die Verlierer-Adapter (Paket tables) wurden danach entfernt - für neue Kandidaten
  einfach wieder einen kleinen PosTable-Adapter schreiben und über SetTableFactory bzw.
  SetDirectTableFactory unter Realbedingungen messen.
  Anmerkung zu xsync: im Serial-Merge-Design zahlt sie Atomic-Kosten ohne Nutzen -
  ihr Potenzial zeigt sich erst im Direct-Write-Modus (siehe unten).
- **Direct-Write-Modus** (Standard, `blocker/direct.go`): die Worker beanspruchen ihre
  Funde atomar selbst (ClaimPending per first-wins, MergeTransition für die monotonen
  Marker-Übergänge unbekannt -> pending -> good), der serielle Merge entfällt komplett;
  danach werden nur noch die fertigen Record-Buffer blockweise an die Listen gehängt.
  Ergebnis-Sets beweisbar identisch (per Test gegen den Standard-Pfad abgesichert).
  Zwei Implementierungen: **ShardDirect** (Standard: 64 Shards CompactTable mit Mutexen,
  10 Byte/Slot und damit ca. 3x speicherschonender) und **xsync** (lock-frei, ca. 9%
  schneller, aber deutlich mehr RAM pro Eintrag - Umschalten per SetDirectTableFactory,
  nil = alter Serial-Merge-Pfad). Speicher schlug ab hohen Steiner-Zahlen spürbar zu
  Buche, daher ist die sparsame Variante Standard.
  Messwerte lid349 bis 4-Steiner (Worker / Serial-Merge / DW-Shard / DW-xsync):
  4: 6,3 / 5,7 / 5,9 s | 8: 4,5 / 3,7 / 3,8 s | 14: 3,8 / 2,9 / 2,9 s |
  128: 3,0 / 2,5 / **2,2 s**. Gesamt seit Baseline: 13,8 s -> 2,2 s (Faktor 6+). Die CompactTable gewinnt, weil die
  Crc64-Schlüssel bereits Hashes sind (Identity-Hashing, kein Re-Hash, keine Metadaten).
  Wichtige Lehre: synthetische Micro-Benchmarks übertreiben die Unterschiede stark
  (Hash-Zugriffe sind nur ~20-25% des Workloads) - neue Kandidaten immer als Adapter
  ins Paket `tables` hängen und unter Realbedingungen messen.
- `Step(limit)` verarbeitet bis zu limit Sätze der aktuellen Tiefe einer Richtung;
  Richtungswahl pro Suchtiefe: kleinere Tabelle zuerst (wie Original Z. 519-523).
- **Parallele Suche** (`parallel.go`, Default NumCPU*4 Worker, `SetWorkers(1)` = seriell):
  Batches ab `parallelMinRecords` Sätzen werden in statische, zusammenhängende Bereiche
  geteilt; die Worker generieren Varianten und filtern gegen die (während der Generierung
  eingefrorene) Hashtabelle vor, der serielle Merge läuft in Bereichs-Reihenfolge = exakter
  FIFO-Reihenfolge mit der Originallogik -> bitgenau identisch zur seriellen Suche, egal
  wie viele Worker (Tests: TestSolveParallelDeterminism, Vanilla-Orakel läuft im Default
  parallel). Wichtig fürs Verständnis: die dynamischen Chunks des Blockers wären hier
  falsch, weil foundTotal-Pruning und Tiefen-Updates reihenfolge-abhängig sind; und der
  eingefrorene Vorfilter ist äquivalent zum Live-Zugriff, weil Add/Update innerhalb eines
  Tiefen-Batches nur Tiefen > Listentiefe schreiben und Tabellen-Tiefen nur sinken.
  Der forwardOnly-Sonderfall (Mini-Levels ohne Zielstellungen) bleibt komplett seriell.
  Benchmark lid4208 (8 Kisten, 132 Züge, 1,55M Knoten, Blocker 7 Stufen aus dem Cache,
  6 Kerne / 12 Threads mit SMT, 09.08.2026): Worker 1/2/4/8/12/24/48/96 -> 16,9 / 10,0 /
  6,2 / 4,7 / 4,4 / 4,2 / **3,8** / 3,8 s (Faktor 4,4; daher Default NumCPU*4).
  Warum Überbelegung hilft: bei den STATISCHEN Bereichen ist es vor allem Lastausgleich -
  viele kleine Häppchen glätten ungleich teure Bereiche, am wg.Wait warten sonst alle
  auf den langsamsten (Cache-Misses und Page Faults sind für den Go-Scheduler unsichtbar,
  der schaltet nur bei Runtime-sichtbarem Warten um: Locks, Channels, Syscalls).
  Achtung beim Vergleichen: der Blocker-Bestwert NumCPU*8 wurde auf einem ANDEREN
  Rechner gemessen (Ultra 9, 16 Kerne ohne SMT, davon 2 langsame Effizienz-Kerne,
  die unter Rechenlast kaum ausgelastet werden) - dort greift SMT als Erklärung
  gar nicht, dafür hat der Blocker echte Mutex-Waits auf den ShardDirect-Shards,
  die der Scheduler mit Ersatz-Goroutinen überbrücken kann. Heterogene Kerne
  verstärken zudem das Lastausgleichs-Argument: bei statischen Bereichen ohne
  Überbelegung würde der Bereich eines Effizienz-Kerns zum Nachzügler, auf den
  am wg.Wait alle warten. Die Bulk-Größe ist bei 48 Workern praktisch
  egal (1.000 bis 1e9: alle ~3,9-4,0 s; der alte C#-Erfahrungswert "Bulk ~200 optimal"
  gilt für das Fan-out-Design nicht mehr - kleine Batches kosten dort nur Overhead).
  Alle Sweep-Läufe byte-gleich über sämtliche 132 Tiefenzeilen (Knoten, Rest, Lösung).
- Sonderfall `forwardOnly`: Levels ohne Zielstellungen (z.B. 1-Schub-Level) laufen als reine
  Vorwärtssuche mit direkter Gelöst-Prüfung - das konnte das C#-Original nicht.
- Lösungs-Rekonstruktion über beide Tabellen (Vorgänger/Nachfolger mit exakt passender Tiefe),
  LURD-Zugfolge per BFS-Laufweg zwischen den Schub-Stellungen (`soko.Steps`).

## Blocker (Bx-Semantik)

Pro Kistenzahl k = 1, 2, ... (automatisches Ende nach Stufe KistenAnzahl-1):

1. **CollectStart**: alle C(n,k)-Kombinationen der Start-Kistenfelder, deren Schub-Varianten
   werden als "Prüfung ausstehend" (Marker 12345) registriert. Generischer Odometer statt
   handentrolltem switch - keine 8-Steiner-Grenze mehr.
2. **CollectGoals**: Kombinationen der Zielfelder; freie Kisten-Nachbarn = "gute" Stellungen
   (Marker 60000), zusätzlich vorwärts weiterverfolgt.
3. **SearchVariants**: Vorwärts-BFS über alles Erreichbare (bereits fertige Stufen filtern mit).
4. **MergeGoals**: Rückwärtssuche ab den guten Stellungen markiert alles Gute.
   **Bx-Unterschied**: rückwärts erreichte, vorwärts nie gesehene Stellungen werden als
   Blocker-Kandidaten registriert (Ziel-Hinterland, im echten Spiel unerreichbar); die
   Rückwärtssuche filtert selbst mit den Blockern (vermeidet redundante Muster).
5. **CreatePatterns**: alles was noch 12345 ist -> Deadlock-Muster, abgelegt pro Spielerposition.

- `CheckAllowed(player, boxBits)`: Muster trifft zu, wenn ALLE Muster-Felder Kisten tragen
  (Subset-Match). **Bedingte Kill-Regel** (seit 08/2026, Cache-Version 3): ein zutreffendes
  Muster allein blockt noch nicht - verworfen wird erst, wenn JEDER Schub-Pose-Kandidat
  (jede Kiste neben dem Spieler, die als "zuletzt geschobene" infrage kommt:
  Kiste auf Nachbarfeld, Gegenfeld frei) von einem zutreffenden Muster abgedeckt ist.
  Hintergrund: Die Bx-Hinterland-Muster ("rückwärts erreichbar, vorwärts nie gesehen")
  beweisen nur, dass die Stellung nicht durch den Schub einer MUSTER-Kiste entstanden
  sein kann - steht der Spieler nach dem Schub einer fremden Kiste in der Muster-Pose,
  ist die Stellung trotzdem legal. Die unbedingte Anwendung (so auch im C#-Original
  SokowahnBlockerBx) verwarf so bei Level 29632 eine Stellung der optimalen
  304-Züge-Lösung, der Solver fand nur 306 (Regressionstest:
  blocker/lid29632_debug_test.go, braucht solution-29632.txt im Repo-Root).
  Der Fix wurde ins C# zurückportiert (SokowahnBlockerBx.CheckErlaubt, Cache-Version
  107 -> 108, alte Caches werden ignoriert statt Exception) - gen4-plain (SokowahnBlocker),
  SokowahnBlockerB und gen5 (SokowahnBlockerB2) hatten den Bug nie: sie registrieren
  kein Ziel-Hinterland (B2s Blocker sind per Konstruktion echte Teilspiel-Deadlocks).
  Beweisskizze der bedingten Regel: jedes Muster ist entweder (a) in der Start-Hülle
  des k-Spiels und dann per vollständiger Rückwärtswelle beweisbar tot (Kill immer
  korrekt) oder (b) nicht in der Start-Hülle, dann ist jede Entstehung als
  Nach-Schub-Stellung einer Muster-Kiste widerlegt (Projektions-Argument: die
  k-Projektion jeder legalen Partie landet nach dem Schub einer Teilmengen-Kiste in
  der k-Start-Hülle). Sind alle Kandidaten abgedeckt, ist jede mögliche Entstehung
  widerlegt oder tot. Kosten: Vanilla-Suche nur ca. 1,7% mehr Knoten (s.u.).
  Die Blocker-Stufenwerte ändern sich durch die bedingte Regel (der Stufenbau
  filtert sich selbst damit: größere Hüllen, längere Rückwärtswellen, teils deutlich
  mehr Hinterland-Muster, z.B. lid201 Stufe 2: 2288 statt 35; nur Stufe 1 bleibt gleich) -
  dank Rückport bleiben sie **bitgenau vergleichbar mit dem gefixten C#-refcli**
  (verifiziert: vanilla blockerbx 5 und lid201 blockerbx 3 exakt gleich).
  Alte v2-Caches werden verworfen und neu gerechnet.
  Zwei Beschleunigungen gegenüber dem naiven Feld-für-Feld-Vergleich:
  1. **Bitmasken**: das Field pflegt die Kistenbelegung als Bitmaske über die begehbaren
     Felder (`boxBits`, 2 Bit-Operationen pro Schub/Undo); jedes Muster liegt ebenfalls als
     Maske vor, der Match ist ein branchloser Subset-Test (`pattern &^ state == 0` je Wort).
  2. **Anker-Index**: je Spielerposition sind die Muster aller Stufen nach ihrem Ankerfeld
     (kleinstes Muster-Feld) gebucketet; geprüft werden nur Buckets, deren Ankerfeld
     tatsächlich eine Kiste trägt - alle anderen Muster können nicht zutreffen.
  Messung unter Realbedingungen (lid46084, 5-Steiner-Cache mit 190.708 Mustern, Suche bis
  Tiefe 266, als Speedcheck-Test verankert): naiv 7,8 s -> nur Bitmasken 2,8 s ->
  Bitmasken + Anker-Index **1,0 s** (Faktor 7,7; Knotenzahl bitgenau gleich). Der
  Stufenbau selbst (lid349, wenige Muster) bleibt unverändert bei 2,44 s.
  Der frühere `emptyBoxNumber`-Mechanismus (kistenNummerLeer-Pendant) entfällt: die
  Bitmaske kennt nur "Kiste ja/nein" und ist damit unabhängig von der Kistenanzahl
  des abfragenden Feldes.
- Der Solver filtert vorwärts UND rückwärts mit den Blockern (rückwärts wie
  `GetVariantenRückwärtsTeilRun2` der List2-Variante; bringt z.B. bei Vanilla
  1,60 Mio statt 2,06 Mio Knoten).
- Cache: gzip, versioniert (aktuell v2), benannt per FNV über die Feldgeometrie,
  gespeichert nach jeder fertigen Stufe -> Wiederaufnahme jederzeit.
- **Parallelisierung**: die beiden teuren Phasen (SearchVariants, MergeGoals) verteilen
  jeden Batch per Atomic-Zähler in Chunks (Standard 512 Sätze, `SetChunkSize`) auf Worker.
  Jeder Worker hat einen eigenen Field-Clone und generiert/vorfiltert nur (Lese-Zugriffe
  auf die Hashtabelle); das Einsortieren läuft danach seriell in fester Worker-Reihenfolge.
  Die Ergebnis-Sets sind dadurch beweisbar identisch zum seriellen Lauf (Fixpunkt der
  Vorwärts-Hülle ist unabhängig von der Wellen-Reihenfolge) - abgesichert über die
  Orakel-Referenzwerte im Benchmark.
- **Worker-Anzahl**: Standard ist 8x NumCPU (`SetWorkers`). Die Arbeit ist memory-latency-
  gebunden (Hash-Lookups und Flood-Fills über große Arrays), deshalb hilft deutliche
  Überbelegung: die Goroutinen verstecken gegenseitig ihre DRAM-Wartezeiten
  (GOMAXPROCS bleibt bei NumCPU OS-Threads). Sweep-Messungen lid349 bis 4-Steiner
  (Intel Ultra 9 285H, 6P+8E+2LPE): seriell+map 13,8 s -> seriell+CompactTable 12,0 s ->
  14 Worker 3,8 s (= 16, LP-E-Kerne irrelevant) -> 128 Worker 3,1 s -> Plateau ~3,0 s ab 128.
  Chunk-Größe: 20 und 20000 sind messbar schlechter, 200-2000 gleichwertig -> 512.

## Referenzwerte (als Tests verankert)

| Level | Messung | Wert |
|---|---|---|
| Vanilla (lid214) | optimale Züge | 230 |
| Vanilla ohne Blocker | Knoten am Ende | 8.710.434 (bitgenau = refcli) |
| Vanilla Blocker-Stufen 1-5 | Muster/geprüft | 17/92, 218/2.257, 496/27.219, 1.173/210.093, 2.652/1.071.408 |
| Vanilla mit Blockern | Knoten am Ende | 1.595.042 (Regressionswert) |
| Level 201 Blocker-Stufen 1-3 | Muster/geprüft | 80/214, 2.288/10.272, 1.819/233.120 |
| small.txt | optimale Züge | 16 |
| Level 29632 | 304er-Lösung passiert Stufen 1-4 | Regressionstest (Bx-Hinterland-Fix) |

Die Suche ohne Blocker bleibt bitgenau vergleichbar mit dem C#-Orakel. Die
Blocker-Stufenwerte gelten seit der bedingten Kill-Regel (Bx-Hinterland-Fix) und
sind bitgenau gleich dem **gefixten** C#-refcli (`blockerbx`, Cache-Version 108);
die alten Werte der unbedingten Bx-Semantik zum Vergleich: 216/2.251, 239/26.848,
1.024/208.306, 2.835/1.056.514 bzw. lid201 35/8.019, 781/232.082, Vanilla-Suche
1.568.540 Knoten.
