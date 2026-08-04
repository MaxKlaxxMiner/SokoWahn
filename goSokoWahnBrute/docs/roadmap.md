# Roadmap / nächste Ideen

Lose Sammlung der nächsten Ausbaustufen, grob nach Nutzen sortiert.
Grundregel bleibt: jede Änderung am Suchverhalten gegen das C#-Orakel bzw.
die verankerten Referenzwerte absichern.

## Performance-Kern

- **Kompakte Hashtabelle** als `PosTable`-Drop-in: Open Addressing, 8 Byte pro Eintrag
  (48 Bit Rest-Schlüssel + 16 Bit Tiefe, Bucket aus den unteren Crc-Bits), Kapazität 2^k
  mit Verdopplung, lineares Sondieren. Mit Benchmarks gegen die map-Variante
  (die builtin map braucht ca. 3-4x mehr RAM). Das Original brauchte dafür das
  Dictionary+Index24-Archiv-Hybrid - in Go geht das deutlich einfacher.
- **Parallelisierung** der Suche: Worker-Pool, ein Field-Clone pro Worker (Clone() ist
  dafür vorbereitet), Batch-Fan-out über PopBatch -> Varianten sammeln -> seriell
  einsortieren. Erst danach: parallele Blocker-Erstellung (das Original parallelisierte
  auch dort). Achtung: Determinismus für Orakel-Vergleiche über eine Seriell-Option erhalten.
- **Disk-Auslagerung der Tiefenlisten** hinter dem DepthList-Interface (List2-Muster:
  32-KiB-Blöcke, nur volle Blöcke auslagern, freigelesene Slots wiederverwenden).
  Nötig erst bei Levels, deren Fronten nicht mehr in den RAM passen (128 GB verfügbar).
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
- Blocker-Erstellung parallelisieren (Kombinationen sind unabhängig).

## TUI / Komfort

- Levelsammlungen: mehrere Levels aus einer Datei (Standard-Formate wie .xsb/.slc),
  Auswahl-Liste; die alte TodoLevels.txt im SokoWahn-Ordner als Fundus.
- Lösungs-Replay als Animation (Frames automatisch abspielen), Steps/s-Anzeige,
  Verlaufs-Statistik (Knoten/s über die Zeit).
- Level-Editor: Zeichen direkt im Feld setzen (die alte GUI konnte nur Text einfügen).
- Suchstand speichern/laden (nicht nur Blocker): erlaubt tagelange Läufe in Etappen.

## Aufgeräumtes / bekannte Quirks

- `SearchGoalStates` liefert (wie das Original) keine Zielstellungen für 1-Schub-Level -
  im Solver durch den forwardOnly-Sonderfall abgefangen. Dokumentiert, kein Handlungsbedarf.
- `goSokoWahn/` (alter Ansatz) enthält den vertauschten sortBoxes-Bug in der Rückwärtssuche;
  bleibt als eingefrorene Vorlage absichtlich unberührt.
- game-sokoban.com: https-Zertifikat der Seite ist abgelaufen, der Loader nutzt http.
  Falls die Seite das Format erneut ändert: tui/webload.go (beide Formate dokumentiert).
