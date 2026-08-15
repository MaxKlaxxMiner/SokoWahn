# SokoWahn - Projektanweisungen

Sokoban-Löser-Projekt von Max. Das Repo enthält die historischen C#-Solver (Copyright 2013,
ins Repo gewandert 2015 als "alter Kram") und den aktiven Go-Nachbau `goSokoWahnBrute`.

## Ordnerstruktur

- `goSokoWahnBrute/` - **aktives Projekt**: Go-Nachbau des SokoWahn_4th-Solvers mit TUI.
  Details siehe `goSokoWahnBrute/docs/architektur.md`, offene Ideen in `goSokoWahnBrute/docs/roadmap.md`.
- `goSokoWahn/` - erster Go-Ansatz (eingefroren). Diente als Kopiervorlage, **nicht mehr anfassen**.
  Achtung: enthält einen bekannten Bug (vertauschte sortBoxes-Aufrufe in der Rückwärtssuche),
  der nur in goSokoWahnBrute gefixt wurde.
- `oldstuff/` - die C#-Originale (2nd bis 5th generation, WinForms-GUI, SokowahnTools).
  - `oldstuff/refcli/` - Konsolen-Orakel + Build-Skripte (siehe unten). Die alten Solver gelten
    als Referenz: der Go-Port wird bitgenau gegen sie verifiziert.
- `SokoWahn/` - späteres C#-Projekt (Raum-/Ketten-Ansatz), ruht.
- `sokosolver-forms.exe` / `goSokoWahnBrute.exe` im Root - von Max genutzte Binary-Kopien,
  werden von den Build-Skripten automatisch aktualisiert.

## Arbeitsregeln

- Sprache: Prosa/Kommentare/Doku/Commits deutsch, Code-Identifier englisch.
  ASCII plus korrekte Umlaute (ä ö ü ß), keine Sonder-Unicode-Zeichen (Pfeil als `->`).
- **Commits macht Max selbst** (TortoiseGit, meist Einzeiler) - nur Commit-Messages vorschlagen,
  nie selbst committen.
- Verhalten des Go-Ports wird **gegen das C#-Orakel verifiziert**, soweit die Semantik
  noch deckungsgleich ist: die Suche ohne Filter (Knoten je Tiefe, Lösungslängen) mit
  `-dirclassic` und alle Blocker-Stufen bis zur ersten Muster-Explosion sind bitgenau
  vergleichbar. Zwei bewusste Go-Weiterentwicklungen weichen per Default ab: die
  Richtungswahl der Suche (Effizienz-Verhältnis Tiefe je Hash-Eintrag statt kleinerer
  Tabelle, siehe solver.chooseForward; das Original-Verhalten bleibt als DirClassic
  bzw. CLI-Flag `-dirclassic` erhalten) und der adaptive Stufenbau des Blockers mit
  den Stufe-1-Regeln (siehe blocker.RulesPatternThreshold) - Referenz sind die in
  den Go-Tests verankerten Werte. Abweichung = Bug oder bewusste, dokumentierte
  Entscheidung.
- Vergleichs- und Debug-Läufe klein halten: Blocker-Stufen begrenzen (`-stages N` bzw.
  `blockerbx N`), kleine Levels bevorzugen. 2-3-Steiner rechnen in Sekunden durch.
- Bei längerer Fehlersuche: Max Bescheid geben und den Stand zeigen, er schaut direkt mit drüber.
- `go.mod`: Go-Version exakt festgenagelt (`go 1.26.0`), keine `toolchain`-Zeile.

## Build und Test

Alles läuft in der MSYS2/UCRT64-Bash.

```
# Go-Projekt (in goSokoWahnBrute/)
go build ./... && go vet ./... && go test ./...   # Tests; -short überspringt die Orakel-Läufe
bash build.sh                                     # baut goSokoWahnBrute.exe + Kopie im Root

# C#-Orakel (in oldstuff/refcli/, .NET-Framework-csc, kein SDK nötig)
bash build.sh                                     # baut refcli.exe (mit parallelDeaktivieren)
bash build-winforms.sh                            # baut die alte WinForms-GUI + Kopie im Root
```

## Orakel-Vergleiche

```
# Suche (deterministisch, Tiefenzeilen sind byte-gleich diffbar; -dirclassic
# erzwingt die Richtungswahl des Originals - ohne das Flag wählt der Go-Default
# per Effizienz-Verhältnis anders):
./refcli.exe <level.txt> [batch] [prepBatches] [-v]     # C#
go run . -cli -dirclassic [-blocker] <level.txt>        # Go

# Blocker-Stufen (schnell, ohne Suche):
./refcli.exe <level.txt> blockerbx <maxStufe>           # C# (Bx = Referenz-Verhalten)
go run . -stages <maxStufe> <level.txt>                 # Go (byte-gleich zum C# bis zur ersten Stufe
                                                        #     mit >4096 Mustern, siehe RulesPatternThreshold)
```

Vor Vergleichen die `temp/`-Ordner löschen (Blocker-Caches). Referenzwerte sind als
Tests verankert (`solver`: Vanilla 230 Züge / 8.710.434 Knoten ohne Blocker mit
DirClassic, 8.608.727 Knoten mit der Default-Richtungswahl;
`blocker`: Vanilla- und Level-201-Stufen exakt gleich SokowahnBlockerBx).

---

*Go-Port erdacht und gebaut im August 2026 im Pairing: Max (Architekt des Originals,
Blocker-Flüsterer) und Claude Fable 5 (Anthropic), das hier hiermit ordnungsgemäß
verewigt ist. Der alte C#-Code (Copyright 2013, seit 2019 unangetastet) hat nach
13 Jahren auf Anhieb wieder kompiliert - Respekt an das Original.* :)
