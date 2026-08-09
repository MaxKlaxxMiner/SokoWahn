# Roadmap / nächste Ideen

Lose Sammlung der nächsten Ausbaustufen, grob nach Nutzen sortiert.
Grundregel bleibt: jede Änderung am Suchverhalten gegen das C#-Orakel bzw.
die verankerten Referenzwerte absichern.

## Performance-Kern

- ERLEDIGT: **Kompakte Hashtabelle** (`CompactTable`, 10 Byte/Slot verlustfrei, crc==0 als
  Frei-Marker). Offen als weitere Idee: 8-Byte-Variante mit 48-Bit-Rest-Schlüssel -
  spart nochmal 20%, ist aber verlustbehaftet beim Vergleich (Aliasing-Risiko) und
  bricht die Bitgenauigkeit zum Orakel -> nur mit Vorsicht.
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
  vereinfacht: 64-MB-Puffer als Blockgröße, sequenzielle Temp-Datei statt Slot-Recycling -
  die Listen werden strikt erst geschrieben und dann gelesen, Wiederverwendung lohnt nicht).
  Zufalls-Dateinamen (os.CreateTemp) für parallele Prozesse, Aufräumen beim Start
  (Dateien älter als eine Woche), Handles nur pro Blockzugriff offen (NTFS-Komprimierung),
  bitgenau identisches Suchverhalten (Tests: TestSolveSpillDeterminism + Vanilla-Orakel).
- **Byte-Modus-Äquivalent**: uint8-Sätze wenn walkEof < 255 (halber Listen-Speicher).
  Eventuell generisch über den Satztyp statt zwei Codepfade.

## Solver-Feinheiten aus der List2-Variante (noch nicht portiert)

- Adaptive Richtungswahl: ab Tiefe > 10 den Hash-Zuwachs der letzten 10 Ebenen vergleichen
  statt nur der Tabellen-Größen (hashVorwärtsNutzung/hashRückwärtsNutzung im Original).
- Push-Anzahl als Sekundärkriterium: bei gleicher Move-Zahl die Lösung mit weniger
  Schüben bevorzugen (CountPushes + OrderBy in der Rekonstruktion).
- Refresh/RAM-Rückgabe zur Laufzeit (Listen-Puffer verkleinern).

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
