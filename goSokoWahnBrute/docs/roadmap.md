# Roadmap / nächste Ideen

Lose Sammlung der nächsten Ausbaustufen, grob nach Nutzen sortiert.
Grundregel bleibt: jede Änderung am Suchverhalten gegen die verankerten
Go-Referenzwerte absichern (architektur.md, Kapitel Referenzwerte).
Erledigte Ausbaustufen samt Messwerten: docs/history.md.

## Performance-Kern

- **Archiv-Ausbau 1: 6-Byte-Records** (Analyse 13.08.2026): bei Index-Floor bits=26
  sind nur 64-26 = 38 Rest-Bits nötig (Record speichert key>>26 statt key>>16, Rest
  bleibt wie heute gegen höhere Bucket-Bits redundanz-tolerant); mit 10 Tiefen-Bits
  (Tiefen bis 1023, DepthUnknown wird nie gespeichert) sind das exakt 48 Bit = 6 Byte
  je Record - byte-aligned, kein Bit-Cursor, Goal-2-Leiter und Miss-Verhalten bleiben.
  Ersparnis 37-40% Archiv-RAM bis ~8G Einträge (1G: 10,0 -> 6,25 GB; 8G: 80 -> 48 GB),
  darüber noch 27-33%. Bei 12 Tiefen-Bits derselbe Trick mit bits=28 (36+12 = 48;
  Preis: 1-GB-Index-Floor je Tabelle). Tiefen-Bits beim Merge aus maxTiefe+Reserve
  ableiten; läuft ein in-place-Update über, erzwungener Merge mit Umpacken auf 7 Byte.
  Erwartete Lookup-Kosten: ähnlich dem alten 7-Byte-Format aus dem Format-Shootout
  (docs/history.md - Mikrobench damals 12-17% langsamer als volle uint64, der
  Preis kommt zurück).
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
- **Direct-Write auf die Solver-Suche übertragen**: der Blocker schreibt seine Funde
  bereits atomar selbst (ShardDirect/xsync), die Suche hat noch den seriellen Merge,
  der die Skalierung begrenzt - dort ist die Tiefen-Buchführung aber komplexer
  (Add/Update mit Tiefenvergleich braucht Compare-and-Swap statt monotoner Marker).
- **CollectStart/CollectGoals parallelisieren** (Kombinationen unabhängig; lohnt erst
  bei hohen Steiner-Zahlen mit vielen Kombinationen).
- **Byte-Modus-Äquivalent im RAM**: uint8-Sätze wenn walkEof < 255 (halber
  Listen-Speicher; das Disk-Format packt bereits auf Bytes).
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
- Refresh/RAM-Rückgabe zur Laufzeit (Listen-Puffer verkleinern).

## Regel-Filter (Live-Deadlock-Erkennung)

Recherche-Basis: Festival 3.1, JSoko 2.28 und YASC 1.689 in den `*_src`-Ordnern eine
Ebene über dem Repo (Analyse vom 11.08.2026). Grundsätze: nur beweisbare Deadlocks
(keine Dominanz-Prunings, Zugoptimalität bleibt), Knoten- statt Zeitbudgets
(Determinismus), nur die Vorwärtssuche filtern. Stufen 1+2 und Pull-Freeze sind
umgesetzt (architektur.md; Messungen in history.md) - offen:

- **Pull-Matching** (Stufe-2-Spiegel für die Rückwärtssuche):
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
- **Früherer Blocker-Stopp bei aktiven Regeln** (Folgerung aus Max' Praxis-Messungen
  vom 12.08.2026, siehe history.md): Kosten des nächsten Stufenbaus gegen erwartete
  Ersparnis abwägen, ggf. Heuristik über Muster-Zuwachs vs. Hash-Kosten der Stufe.

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
