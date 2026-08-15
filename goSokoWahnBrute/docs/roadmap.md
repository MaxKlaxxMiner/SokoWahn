# Roadmap / nächste Ideen

Lose Sammlung der nächsten Ausbaustufen, grob nach Nutzen sortiert.
Grundregel bleibt: jede Änderung am Suchverhalten gegen das C#-Orakel bzw.
die verankerten Referenzwerte absichern.

## Performance-Kern

- ERLEDIGT: **Kompakte Hashtabelle** (`CompactTable`, 10 Byte/Slot verlustfrei, crc==0 als
  Frei-Marker).
- ERLEDIGT: **SegmentTable** (8 Byte/Slot und trotzdem verlustfrei - die Top-16-Bits
  stecken implizit im Segment-Index, die Tiefe invertiert in den freien 16 Bit, Slot 0 =
  frei; Sondieren strikt im Segment, Grow parallel je Segment). Per solver.TableFactory
  schaltbar, bitgenau (Vanilla-Orakel-Test). Messung Level 38044: ~20% weniger
  Tabellen-RAM, ~10% langsamer -> Wahl je nach RAM-Druck.
- ERLEDIGT: **ArchiveTable ("SlowCompactArchiveTable")**: zweistufig wie
  SokowahnHash_Index24Multi im Original, gebaut aber als Bucket-Archiv statt der hier
  ursprünglich skizzierten Sortierung mit Interpolationssuche: 8-Byte-Records (48 Bit
  Rest-Schlüssel + 16 Bit Tiefe) nach Bucket gruppiert, uint32-Offset-Index mit
  adaptiven Bucket-Bits (26..32), 256 Shards, paralleler Merge, CompactTable als
  Delta. TUI-Taste h, effektiv 8,25-10 Byte/Eintrag inkl. Index statt 13,3
  (CompactTable); Details und Messwerte in docs/architektur.md.
- **Archiv-Ausbau 1: 6-Byte-Records** (Analyse 13.08.2026): bei Index-Floor bits=26
  sind nur 64-26 = 38 Rest-Bits nötig (Record speichert key>>26 statt key>>16, Rest
  bleibt wie heute gegen höhere Bucket-Bits redundanz-tolerant); mit 10 Tiefen-Bits
  (Tiefen bis 1023, DepthUnknown wird nie gespeichert) sind das exakt 48 Bit = 6 Byte
  je Record - byte-aligned, kein Bit-Cursor, Goal-2-Leiter und Miss-Verhalten bleiben.
  Ersparnis 37-40% Archiv-RAM bis ~8G Einträge (1G: 10,0 -> 6,25 GB; 8G: 80 -> 48 GB),
  darüber noch 27-33%. Bei 12 Tiefen-Bits derselbe Trick mit bits=28 (36+12 = 48;
  Preis: 1-GB-Index-Floor je Tabelle). Tiefen-Bits beim Merge aus maxTiefe+Reserve
  ableiten; läuft ein in-place-Update über, erzwungener Merge mit Umpacken auf 7 Byte.
  Erwartete Lookup-Kosten: ähnlich dem alten 7-Byte-Format aus der Git-Historie
  (Mikrobench damals 12-17% langsamer als volle uint64 - der Preis kommt zurück).
- **Archiv-Ausbau 2: echte slowHashMap (volle Bitpackung)**: Record = (64-bits)
  Rest-Bits + v Tiefen-Bits als Bit-Stream, Bucket-Bits frei nach Gesamtverbrauch
  optimiert. Der Trade ist vorab exakt rechenbar: +1 Bucket-Bit kostet 4*2^bits Byte
  Index und spart N/8 Byte Daten -> Index wachsen lassen, solange 2^bits < N/32,
  Speicher-Optimum bei 16-32 Einträgen je Bucket (statt Goal 2 - daher "slow").
  Ersparnis 33-41% gegenüber heute (5,5-6,7 B/Eintrag inkl. Index); gegenüber
  Ausbau 1 lohnt sie erst ab ~16G Einträgen (64G: 352 vs 384 GB) - Server-Revier.
  Kosten: Bit-Extraktion je Record (Lookup geschätzt 2-4x langsamer als heute),
  Read-Modify-Write über Wortgrenzen bei in-place-Updates, Komplett-Umpacken bei
  v-Wachstum (nur eine Handvoll Mal, an den Merge koppeln). Zum Einordnen: das
  informationstheoretische Minimum (N zufällige 64-Bit-Schlüssel als Menge) liegt
  bei 8G um ~5,3 B/Eintrag - viel Luft bleibt danach nicht mehr.
- ERLEDIGT: **Parallelisierung der Blocker-Phasen** (SearchVariants + MergeGoals,
  Worker-Pool mit Atomic-Chunks, seriell-identische Ergebnisse; lid349/4-Steiner:
  13,8 s -> 3,8 s bei 16 Kernen). Noch offen:
  - ERLEDIGT: **Solver-Suche parallelisieren** (solver/parallel.go): Batch-Fan-out mit
    STATISCHEN zusammenhängenden Satz-Bereichen + serieller Merge in Bereichs-Reihenfolge
    = exakte FIFO-Reihenfolge -> bitgenau zur seriellen Suche (die dynamischen Chunks des
    Blockers wären hier reihenfolge-abhängig: foundTotal-Pruning und Tiefen-Updates).
    Vorfilter der Worker liest die eingefrorene Tabelle (äquivalent, da Batch-Updates nur
    Tiefen > Listentiefe schreiben), Merge prüft mit Live-Tabelle und Originallogik.
    -workers wirkt jetzt auch auf die Suche, Default NumCPU*4, forwardOnly bleibt seriell.
    Benchmark-Sweep lid4208 erledigt (Ergebnisse in docs/architektur.md): Faktor 4,4 bei
    12 Kernen, Bulk-Größe im Fan-out-Design praktisch egal (der alte C#-Erfahrungswert
    "Bulk ~200" gilt nicht mehr). Offen: Direct-Write-Idee auf die Suche übertragen
    (Tiefen-Buchführung braucht Compare-and-Swap statt monotoner Marker) - könnte den
    seriellen Merge-Anteil eliminieren, der die Skalierung aktuell begrenzt.
  - ERLEDIGT: Direct-Write statt seriellem Merge (xsync als Standard, ShardDirect als
    speicherschonende Alternative; lid349/4-Steiner: 3,0 s -> 2,2 s bei 128 Workern).
    Offen: dieselbe Direct-Write-Idee auf die Solver-Suche übertragen (dort ist die
    Tiefen-Buchführung komplexer: Add/Update mit Tiefenvergleich statt monotoner Marker).
  - CollectStart/CollectGoals parallelisieren (Kombinationen unabhängig; lohnt erst
    bei hohen Steiner-Zahlen mit vielen Kombinationen).
- ERLEDIGT: **Disk-Auslagerung der Tiefenlisten** in der DepthList selbst (List2-Muster,
  vereinfacht: 16-MB-Puffer als Blockgröße, sequenzielle Temp-Datei statt Slot-Recycling -
  die Listen werden strikt erst geschrieben und dann gelesen, Wiederverwendung lohnt nicht).
  Zufalls-Dateinamen (os.CreateTemp) für parallele Prozesse, Aufräumen beim Start
  (Dateien älter als eine Woche), Handles nur pro Blockzugriff offen (NTFS-Komprimierung),
  bitgenau identisches Suchverhalten (Tests: TestSolveSpillDeterminism + Vanilla-Orakel).
  - ERLEDIGT: **RAM-Schwelle vor dem Auslagern** (SpillRamThresholdBytes): unterhalb
    bleiben die Listen komplett im RAM und schonen die Platte; geprüft wird je Liste beim
    ersten Puffer-Überlauf und nach jeweils 16 MB weiterem Zuwachs - bei Speicherdruck
    lagert auch eine schon gewachsene Liste ihren kompletten Puffer aus
    (Test: TestDepthListRamThreshold). Die erste Fassung entschied einmalig je Liste -
    zu statisch: einzelne Zugtiefen wuchsen nach ihrer Entscheidung noch um mehrere GB.
    Vergleichsbasis ist der berechnete Verbrauch der Anzeige (SetSpillRamUsage aus
    Step/Next, kein ReadMemStats); ausgelagerte Puffer werden sofort abgezogen, damit
    beim Schwellen-Übertritt nicht alle Listen gleichzeitig auslagern.
  - ERLEDIGT: **Byte-Packung des Disk-Formats** bei WalkCount <= 256: 1 Byte je Wert
    statt uint16, halbes IO-Volumen (Test: TestDepthListSpillBytePacked).
- **Byte-Modus-Äquivalent im RAM**: uint8-Sätze wenn walkEof < 255 (halber Listen-Speicher;
  das Disk-Format packt bereits auf Bytes, siehe oben).
  Eventuell generisch über den Satztyp statt zwei Codepfade.
- **Blocker.CheckAllowed beschleunigen** (pprof-Befund 15.08.2026, Level 25327 auf dem
  640-GB-Server, 20-s-CPU-Profil während der Suche via -debugport): CheckAllowed frisst
  48% der gesamten Rechenzeit (fast alles flat in der Muster-Prüfschleife), die
  Regel-Filter weitere ~34% (davon Ziel-Matching 23%) - Hashtabellen-Lookups nur ~5%.
  Bei Muster-reichen Levels ist "Nein sagen" also der Hauptkostenpunkt. Ansätze:
  Muster-Reihenfolge nach Treffer-Häufigkeit (Early-Exit), kompakteres
  Bitmasken-Layout, ggf. die rekursive Kisten-Abfrage der SokowahnBlockerB-Idee
  (siehe Blocker-Ausbau unten). Vorher/nachher mit demselben CPU-Profil messen.

## Solver-Feinheiten aus der List2-Variante (noch nicht portiert)

- Adaptive Richtungswahl: ab Tiefe > 10 den Hash-Zuwachs der letzten 10 Ebenen vergleichen
  statt nur der Tabellen-Größen (hashVorwärtsNutzung/hashRückwärtsNutzung im Original).
- Push-Anzahl als Sekundärkriterium: bei gleicher Move-Zahl die Lösung mit weniger
  Schüben bevorzugen (CountPushes + OrderBy in der Rekonstruktion).
- Refresh/RAM-Rückgabe zur Laufzeit (Listen-Puffer verkleinern).

## Regel-Filter (Live-Deadlock-Erkennung)

Recherche-Basis: Festival 3.1, JSoko 2.28 und YASC 1.689 in den `*_src`-Ordnern eine
Ebene über dem Repo (Analyse vom 11.08.2026). Grundsätze: nur beweisbare Deadlocks
(keine Dominanz-Prunings, Zugoptimalität bleibt), Knoten- statt Zeitbudgets
(Determinismus), nur die Vorwärtssuche filtern.

- ERLEDIGT: **Stufe 1 - Freeze + Diagonale** (soko/rules.go): Frozen-Boxes-Fixpunkt
  nach Festival-Vorbild plus Closed-Diagonal-Port aus JSoko, mit toten Feldern per
  Pull-BFS, Statistik-Zählern und Debug-Vergleichsmodus gegen den Blocker
  (-rulescompare). Vanilla: Faktor 3 weniger Knoten ohne Blocker; Details und
  Referenzwerte in docs/architektur.md.
- ERLEDIGT: **Stufe 2 - Ziel-Matching mit eingefrorenen Ziel-Kisten als Wänden**
  (soko/rulesMatch.go, 12.08.2026): eingefrorene Ziel-Kisten wirken wie Wände;
  jede bewegliche Kiste muss noch ein freies Ziel erreichen (billige Vorstufe)
  und alle zusammen brauchen ein bipartites Matching auf die freien Ziele
  (Kuhn-Augmentierung über Bitmasken). Erreichbarkeits-Masken hängen nur von der
  eingefrorenen Menge ab -> Cache je Rules-Instanz mit der exakten Maske als
  Schlüssel (JSoko-Idee, Board.java:3231). Eigener Schalter (MatchEnabled,
  TUI-Taste 6), wirkt auch im adaptiven Blocker-Stufenbau (Cache-Version 7).
  Vanilla: 0 Treffer, Knotenzahlen unverändert, ~5% Laufzeit-Overhead (der
  Fixpunkt-Early-Exit entfällt, sobald eine Kiste auf einem Ziel steht) -
  das Revier sind Levels, deren Ziel-Kisten während der Suche zu Sperr-Riegeln
  einfrieren; Messungen auf den Monster-Leveln macht Max. Nicht übernommen:
  Distanz-Matching per Auktionsalgorithmus (JSoko BipartiteMatchings.java) -
  Erreichbarkeit statt Distanzen reicht für den reinen Deadlock-Beweis.
  Praxis-Befund (Max, 12.08.2026): Ausbeute hängt daran, WIE FRÜH die
  Vorwärtssuche Ziel-Kisten einfriert - 2164 (Zielkammern neben den Starts)
  über 1 Mio Treffer, 5003 (Zielraum im Mittelspiel) wenige, 2135 (Korridor-
  Packing als Endspiel) null: das Endspiel deckt die Rückwärtssuche ab, die
  per Konstruktion deadlock-frei ist; 201 strukturell ungeeignet (4 Zugänge,
  5-Steiner subsumiert fast alles, 8 Treffer bei 130 Mio Hash). Paradebeispiel
  29628 (14 Kisten, dichtes Ziel-Kreuz mitten im Feld): trotz 6-Steiner-Blocker
  feuert regelbasiert NUR noch das Matching - Freeze/Diagonale sind dort
  komplett vom Blocker subsumiert, die Ziel-Kreuz-Cluster liegen über seiner
  Kistengrenze.
- Idee dazu: **Pull-Matching** (Stufe-2-Spiegel für die Rückwärtssuche):
  pull-eingefrorene Kisten auf STARTfeldern wirken als Wände, jede andere Kiste
  muss per Ziehen noch ein freies Startfeld erreichen, Zuordnung per Matching -
  würde die Rückwärts-Front genau im Endspiel-Revier beschneiden (z.B. 2135),
  wo das Vorwärts-Matching nie hinkommt. Aufwand/Nutzen offen, erst messen
  lassen, wie stark die Rückwärtsseite überhaupt an unerreichbaren Stellungen
  leidet (Pull-Freeze-Treffer als Indikator).
- **Stufe 3 - Corral-Mini-Suche mit Cache** (Festival corral_deadlock.cpp): bei
  zerschnittenem Spielfeld je abgeschlossenem Bereich eine knoten-budgetierte
  Mini-Suche; vorher Kisten außerhalb der Zone löschen und ferne eingefrorene
  Kisten kanonisieren (Cache-Trefferquote!). Ergebnis-Cache 16 Byte/Eintrag ohne
  Aging, adaptives Budget für häufig gefragte unentschiedene Stellungen
  (Zweierpotenz-Regel). Mächtigste Stufe, erkennt beliebige regionale Deadlocks.
- Kür: **YASC-Kapazitäts-Sets** (Dead_.pas CalculateDeadlockSets): vorberechnete
  Feldmengen mit Kapazität = Ziele - Kisten, Live-Update ist reines Zählen (O(1)
  je Schub) - würde Teile von Stufe 1/2 nochmal deutlich billiger machen.
- ERLEDIGT: **Pull-Freeze für die Rückwärtssuche** (CheckPull in soko/rules.go):
  Spiegelbild der Stufe 1 - Pull-Mobilität statt Push-Mobilität, Startfelder statt
  Zielfelder, pull-tote Felder per Pull-BFS von den Startfeldern; O(1)-Vorabcheck
  plus Fixpunkt, einmal je Pull-Hypothese vor dem Pose-Flood. Vanilla nur Regeln:
  2,88 -> 1,83 Mio Knoten, fast Blocker-Niveau ohne jede Vorberechnung.
  (Ein direkter Totfeld-Check vorwärts wurde probiert und bewusst wieder entfernt:
  exakt 1-Steiner-Wissen, und Blocker 1-2 ist Betriebsvoraussetzung - der
  Freeze-Fixpunkt fängt die Fälle über die Beidseitig-tot-Achsenregel ohnehin,
  Vanilla-Knotenzahl mit/ohne identisch. Regeln ergänzen den Blocker, doppeln
  ihn nicht.)
- ERLEDIGT: **Regel-Filter im Blocker-Stufenbau, adaptiv** (Cache-Version 6): erst
  nachdem eine fertige Stufe mehr als RulesPatternThreshold Muster (4096, seit
  Cache-Version 8: 10240) produziert hat (Muster-Explosion), filtern alle
  weiteren Stufen ihre Vorwärts-Phasen mit
  einer eigenen Stufe-1-Regel-Instanz; davor baut alles klassisch (volle Muster als
  billige Vorfilter, bitgenau orakel-gleich - Vanilla und lid201 komplett).
  Die Rückwärtswelle bleibt ungefiltert (trägt den Beweis der bedingten Kill-Regel);
  verlorene Muster sind genau die live-erkennbaren (Monotonie-Argument in
  blocker.New). Vanilla: Blocker solo 1.595.042 (unverändert), Blocker+Regeln
  1.488.952. Historie und Messungen von Max (12.08.2026): v4 filterte alle Stufen -
  Ergebnisse identisch, aber Stufenbau und Suche messbar langsamer (die kleinen
  Muster fehlten als billige Vorfilter); v5 starr ab Stufe 4 - auf zahmen Levels
  (lid201) unter 5% Ersparnis bei Speed-Kosten. Beide Feinanpassungen auf Max'
  Vorschlag.
- Offen: Regel-Treffer auch im Blocker-Vergleich je Stufe aufschlüsseln.
- Praxis-Messungen von Max (12.08.2026, Suche bis Tiefe ~100 bzw. ~62, Hash-Einträge):
  - Level 25291 (kompakt vollgestopft, Ziel ~472 Züge): Regeln zusätzlich zum
    4-Blocker -2,6% Hash, zum 5-Blocker nur noch -0,1% - dichte kleine Cluster
    deckt der Blocker selbst ab. Der 5-Blocker brauchte beim Rechnen aber 102 Mio
    Hashtable-Einträge für 9.362 neue Muster (alle Stufen davor zusammen: 6.064).
  - Level 47484 (extremes Karo-Schachbrett, Ziel ~446 Züge): Regeln glänzen -
    3-Blocker 285,6 -> 108,0 Mio Hash (-62%!), 4-Blocker 144,1 -> 102,8 Mio.
    NUR Regeln (116,4 Mio) schlagen den nackten 4-Blocker (144,1 Mio), dessen
    Stufenbau 264 Mio Hash-Einträge kostete und teils länger lief als die Suche
    selbst. Bei gleichem Hash-Budget (~285 Mio) kam die Suche mit Regeln bis
    Tiefe 66 statt 62.
  - Level 43070 (sehr groß, endlos viele Kisten, Ziel ~1.864 Züge): der 3-Blocker
    war mit 141 Mio Hash beim Rechnen gerade noch machbar, der 4-Blocker hätte
    über 20.000 Mio gebraucht - hier sind die Regeln die einzige Ausbaustufe:
    3-Blocker 253,8 -> 128,7 Mio Hash (-49%), bei gleichem Budget (~254 Mio)
    Tiefe 45 statt 43. Genau das Speicherdruck-Szenario, für das die Regeln
    gebaut wurden.
  - Pull-Freeze (12.08.2026): bei gleicher Suchtiefe 25% Hashtable gespart, ohne
    messbare Verlangsamung - die meisten Treffer liefert der Pull-Totfeld-Check
    (feuert vor dem Pose-Flood und spart den samt aller Pose-Stellungen gleich mit).
  - Folgerung/Idee: bei aktiven Regeln lohnt ein früherer Blocker-Stopp (Kosten
    des nächsten Stufenbaus gegen erwartete Ersparnis abwägen, ggf. Heuristik
    über Muster-Zuwachs vs. Hash-Kosten der Stufe).

## Blocker-Ausbau

- Abfrage-Optimierung wie SokowahnBlockerB: Muster rekursiv nach der häufigsten Kiste
  sortieren und Sprungmarken setzen, damit ein Mismatch ganze Präfix-Gruppen überspringt.
  Lohnt, sobald hohe Steiner-Zahlen mit vielen Mustern aktiv sind.
- Ideen der 5th generation / BlockerB2: pro Kistenzahl vollständige Restdistanz-Hashes
  als Heuristik-Quelle (untere/obere Restzug-Schranken je Stellung) -> Basis für eine
  Best-First-Suche mit Zug-Limit. Das Original blieb hier experimentell/unfertig -
  wäre ein eigenes Forschungs-Kapitel.

## TUI / Komfort

- Levelsammlungen: mehrere Levels aus einer Datei (Standard-Formate wie .xsb/.slc),
  Auswahl-Liste; die alte TodoLevels.txt im SokoWahn-Ordner als Fundus.
- Lösungs-Replay als Animation (Frames automatisch abspielen), Verlaufs-Statistik
  (Knoten/s über die Zeit; die geglättete Momentan-Anzeige gibt es seit der
  Worker-Umschaltung in der Such-Ansicht).
- Level-Editor: Zeichen direkt im Feld setzen (die alte GUI konnte nur Text einfügen).
- Suchstand speichern/laden (nicht nur Blocker): erlaubt tagelange Läufe in Etappen.

## Aufgeräumtes / bekannte Quirks

- `SearchGoalStates` liefert (wie das Original) keine Zielstellungen für 1-Schub-Level -
  im Solver durch den forwardOnly-Sonderfall abgefangen. Dokumentiert, kein Handlungsbedarf.
- `goSokoWahn/` (alter Ansatz) enthält den vertauschten sortBoxes-Bug in der Rückwärtssuche;
  bleibt als eingefrorene Vorlage absichtlich unberührt.
- game-sokoban.com: https-Zertifikat der Seite ist abgelaufen, der Loader nutzt http.
  Falls die Seite das Format erneut ändert: tui/webload.go (beide Formate dokumentiert).
