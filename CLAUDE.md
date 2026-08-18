# SokoWahn - Projektanweisungen

Sokoban-Löser-Projekt von Max. Aktiv entwickelt wird der Go-Solver `goSokoWahnBrute`;
die historischen C#-Solver (Copyright 2013) liegen als ruhendes Archiv im Repo.

## Ordnerstruktur

- `goSokoWahnBrute/` - **aktives Projekt**: Go-Solver mit TUI. Architektur in
  `goSokoWahnBrute/docs/architektur.md`, offene Ideen in `docs/roadmap.md`,
  Vergangenes (Herkunft, Orakel-Ära, erledigte Ausbaustufen, Messprotokolle)
  in `docs/history.md`.
- `goSokoWahn/` - erster Go-Ansatz (eingefroren). Diente als Kopiervorlage, **nicht mehr anfassen**.
  Achtung: enthält einen bekannten Bug (vertauschte sortBoxes-Aufrufe in der Rückwärtssuche),
  der nur in goSokoWahnBrute gefixt wurde.
- `oldstuff/` - die C#-Originale (2nd bis 5th generation, WinForms-GUI, SokowahnTools),
  ruhendes Archiv. Bis 08/2026 diente `oldstuff/refcli/` als bitgenaues Verifikations-Orakel
  des Go-Ports (Geschichte in `goSokoWahnBrute/docs/history.md`); seitdem sind die
  Go-Test-Anker die Referenz. `oldstuff/refcli/build-winforms.sh` baut weiterhin die
  alte WinForms-GUI.
- `SokoWahn/` - späteres C#-Projekt (Raum-/Ketten-Ansatz), ruht.
- `sokosolver-forms.exe` / `goSokoWahnBrute.exe` im Root - von Max genutzte Binary-Kopien,
  werden von den Build-Skripten automatisch aktualisiert.

## Arbeitsregeln

- Sprache: Prosa/Kommentare/Doku/Commits deutsch, Code-Identifier englisch.
  ASCII plus korrekte Umlaute (ä ö ü ß), keine Sonder-Unicode-Zeichen (Pfeil als `->`).
  Prüfwerkzeug: `bash tools/umlaut.sh` (Check/Fix für ASCII-Ersatzschreibweisen).
- **Commits macht Max selbst** (TortoiseGit, meist Einzeiler) - nur Commit-Messages vorschlagen,
  nie selbst committen.
- **Referenz des Suchverhaltens sind die in den Go-Tests verankerten Anker-Werte**
  (Tabelle in `docs/architektur.md`, Kapitel Referenzwerte; u.a. Vanilla ohne Filter
  8.747.345 Knoten, Blocker-Stufen Vanilla/lid201). Abweichung = Bug oder bewusste,
  dokumentierte Entscheidung - dann neu verankern und die Vorgänger-Werte in
  `docs/history.md` festhalten.
- Vergleichs- und Debug-Läufe klein halten: Blocker-Stufen begrenzen (`-stages N`),
  kleine Levels bevorzugen. 2-3-Steiner rechnen in Sekunden durch.
- Bei längerer Fehlersuche: Max Bescheid geben und den Stand zeigen, er schaut direkt mit drüber.
- `go.mod`: Go-Version exakt festgenagelt (`go 1.26.0`), keine `toolchain`-Zeile.

## Build und Test

Alles läuft in der MSYS2/UCRT64-Bash.

```
# Go-Projekt (in goSokoWahnBrute/)
go build ./... && go vet ./... && go test ./...   # Tests; -short überspringt die langen Suchläufe
bash build.sh                                     # baut goSokoWahnBrute.exe + Kopie im Root

# alte WinForms-GUI (in oldstuff/refcli/, .NET-Framework-csc, kein SDK nötig)
bash build-winforms.sh                            # baut sokosolver-forms.exe + Kopie im Root
```

## Referenz-Läufe

```
# Suche im CLI-Modus (deterministisch, Tiefenzeilen sind zwischen Go-Ständen
# byte-gleich diffbar; Blocker inkl. temp/-Cache und Regel-Filter laufen immer mit):
go run . -cli <level.txt>

# Blocker-Stufen (schnell, ohne Suche, ohne Cache):
go run . -stages <maxStufe> <level.txt>
```

Vor Diff-Vergleichen zwischen Go-Ständen die `temp/`-Ordner löschen (Blocker-Caches).
Die Anker-Werte laufen im vollen Testlauf mit (`go test ./...` ohne `-short`).

---

*Go-Port erdacht und gebaut im August 2026 im Pairing: Max (Architekt des Originals,
Blocker-Flüsterer) und Claude Fable 5 (Anthropic), das hier hiermit ordnungsgemäß
verewigt ist. Der alte C#-Code (Copyright 2013, seit 2019 unangetastet) hat nach
13 Jahren auf Anhieb wieder kompiliert und den Go-Port als Orakel bitgenau
abgenommen - Respekt an das Original.* :)
