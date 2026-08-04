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

## Solver (bidirektionale Suche)

- Vorwärts ab Startstellung, rückwärts ab allen Zielstellungen (`SearchGoalStates`:
  alle Kisten auf Zielen, Spieler auf pull-fähigem Nachbarfeld).
- **Natürliche Tiefen**: beide Richtungen zählen 0,1,2,... als uint16; 65535 = unbekannt
  (`DepthUnknown`). Gesamtlösung beim Treffen = vorwärts + rückwärts. Kein 60000er-Offset
  wie im Original - die Zahlen sind trotzdem 1:1 vergleichbar.
- Je Richtung: Hashtabelle (`PosTable`) + Suchlisten je Tiefe
  (`DepthList`, flache uint16-Sätze: Spieler + Kisten; Tiefe steckt im Listenindex).
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
  Zwei Implementierungen: **xsync** (lock-frei, Standard, schnellste) und **ShardDirect**
  (64 Shards CompactTable mit Mutexen, ca. 9% langsamer, aber ca. 3x speicherschonender -
  Umschalten per SetDirectTableFactory, nil = alter Serial-Merge-Pfad).
  Messwerte lid349 bis 4-Steiner (Worker / Serial-Merge / DW-Shard / DW-xsync):
  4: 6,3 / 5,7 / 5,9 s | 8: 4,5 / 3,7 / 3,8 s | 14: 3,8 / 2,9 / 2,9 s |
  128: 3,0 / 2,5 / **2,2 s**. Gesamt seit Baseline: 13,8 s -> 2,2 s (Faktor 6+). Die CompactTable gewinnt, weil die
  Crc64-Schlüssel bereits Hashes sind (Identity-Hashing, kein Re-Hash, keine Metadaten).
  Wichtige Lehre: synthetische Micro-Benchmarks übertreiben die Unterschiede stark
  (Hash-Zugriffe sind nur ~20-25% des Workloads) - neue Kandidaten immer als Adapter
  ins Paket `tables` hängen und unter Realbedingungen messen.
- `Step(limit)` verarbeitet bis zu limit Sätze der aktuellen Tiefe einer Richtung;
  Richtungswahl pro Suchtiefe: kleinere Tabelle zuerst (wie Original Z. 519-523).
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

- `CheckAllowed(player, wposToBoxes)`: Muster trifft zu, wenn ALLE Muster-Felder Kisten tragen
  (Subset-Match). `emptyBoxNumber` = Leer-Marker des abfragenden Feldes (Stufenbau: k,
  Hauptsuche: KistenAnzahl) - das Pendant zu kistenNummerLeer/Abbruch() im Original.
- Der Solver filtert vorwärts UND rückwärts mit den Blockern (rückwärts wie
  `GetVariantenRückwärtsTeilRun2` der List2-Variante; bringt z.B. bei Vanilla
  1,57 Mio statt 2,06 Mio Knoten).
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
  gebunden (Hash-Lookups und Flood-Fills über grosse Arrays), deshalb hilft deutliche
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
| Vanilla Blocker-Stufen 1-5 | Muster/geprüft | 17/92, 216/2.251, 239/26.848, 1.024/208.306, 2.835/1.056.514 |
| Vanilla mit Blockern | Knoten am Ende | 1.568.540 (Regressionswert) |
| Level 201 Blocker-Stufen 1-3 | Muster/geprüft | 80/214, 35/8.019, 781/232.082 |
| small.txt | optimale Züge | 16 |

Alle Blocker-Stufenwerte sind bitgenau gleich `SokowahnBlockerBx` (refcli-Modus `blockerbx`).
