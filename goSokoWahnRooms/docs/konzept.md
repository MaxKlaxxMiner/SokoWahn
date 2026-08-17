# Konzept goSokoWahnRooms

Go-Nachbau des Rooms-Konzepts aus dem C#-Projekt `SokoWahn/SokoWahnLib/Rooms` (2017-2019).
Kein bitgenauer Port: das Konzept wird übernommen, Datenstrukturen und Abläufe dürfen
neu gedacht werden. Verifiziert wird visuell über eine Debug-GUI (Browser) durch Max,
nicht über ein C#-Orakel.

## 1. Ziel und Abgrenzung

Primäres Ziel ist das **Rooms-Framework** selbst:

- Spielfeld in Räume zerlegen (Start: 1-Feld-Räume), verbunden über Portale
- Räume manuell und automatisch verschmelzen (Merge), dabei Zustände/Varianten
  konsolidieren und per Deadlock-Scan ausdünnen
- Debug-GUI, in der jeder Raum mit seinen Zuständen, Varianten und Portalen
  detailliert begutachtet werden kann (Nachfolger des FormDebugger)

Spätere Aufsätze (nicht Teil der ersten Ausbaustufen): Path-Mapping (bestehende
LURD-Lösung auf die Räume abbilden, siehe M6), Solver, Optimizer für bestehende
Lösungen, Hilfstool für manuelles Lösen. Das Framework muss so gebaut sein, dass
diese Aufsätze möglich bleiben (Pfad-Informationen erhalten, Vorwärts- und
Rückwärtsdenken nicht verbauen).

Merge bleibt ein **Vorbereitungsschritt mit Kompromiss**: Rechenzeit/Speicher der
Vorbereitung gegen Aufwand des späteren Verwenders. Automerge ist Komfort, nie Pflicht -
manuelles, gezieltes Mergen nach Levelaufbau ist ein Kern-Anwendungsfall.

## 2. Kernkonzept (Rekapitulation)

- **Raum**: Menge begehbarer Felder. Kennt seine Zielfelder und die maximal mögliche
  Kistenzahl.
- **Portal**: gerichtete Verbindung zweier Nachbarfelder in verschiedenen Räumen.
  Portale existieren immer paarweise (`oppositePortal`). Ein Portal trägt:
  - `blockedBox`-Flag: durchgeschobene Kiste steckt fest bzw. kann nicht weitergeschoben
    werden (aus dem Single-Box-Scan abgeleitet)
  - `stateBoxSwap`: Zustandswechsel des Zielraums, wenn eine Kiste durchgeschoben wird
  - Verzeichnis Zustand -> Varianten (welche Varianten sind möglich, wenn der Spieler
    hier reinkommt und der Raum in Zustand S ist)
- **Zustand (State)**: Kisten-Konfiguration innerhalb des Raums (sortierte Positionen).
  **Konvention: Zustand 0 ist immer der gelöste Endzustand** (alle Raum-Ziele belegt
  bzw. leer). Bleibt beim Merge erhalten (0*mul+0 = 0).
- **Variante**: "Spieler betritt Raum über Portal P bei Zustand S" ->
  (moves, pushes, rausgeschobene Kisten je Ausgangsportal, Ausgangsportal des Spielers
  oder 'bleibt drin = Spielende', neuer Zustand, LURD-Pfad). Startvarianten sind der
  Sonderfall ohne eingehendes Portal (Spieler startet im Raum).
- **Merge**: zwei benachbarte Räume -> ein Raum. Zustände = Kreuzprodukt (danach
  aufgeräumt), Varianten entstehen durch Best-Moves-Suche über beide Teilräume
  (Task-Queue mit CRC-Dedup), innere Portale lösen sich auf.
- **Optimierung nach Merge**: unbenutzte Zustände entfernen; Deadlock-Scan
  (vorwärts/rückwärts erreichbare Varianten) entfernt tote Varianten/Zustände.
- **Effort**: Produkt der Variantenzahlen aller Räume = theoretischer Suchaufwand.
  Anzeige als große Zahl (math/big), Fortschrittsmaß für das Ausdünnen.

## 3. Projekt- und Paketstruktur

Eigenständiges Modul `goSokoWahnRooms/` (go 1.26.0, keine toolchain-Zeile),
Schwester von `goSokoWahnBrute/`. Kopiert aus Brute (danach unabhängig):

- `soko` - Spielfeld, Parser, Wpos-Kompaktindizes, Nachbar-Tabellen. Wird um
  Rooms-Hilfen ergänzt (Ecken-Check, Nachbar-Enumeration), Suchfunktionen die
  Rooms nicht braucht fliegen raus oder bleiben ungenutzt.
- `crc64` - FNV-1a-Hashing (Fluent-API)
- `tools` - FormatInt u.a.

Neu:

- `rooms` - der Kern: Network, Room, Portal, StateList, VariantList, Merger,
  DeadlockScanner, Validate, Effort
- `web` - HTTP-Server, JSON-API, SSE-Status, eingebettetes Frontend (go:embed)
- `webui` - Frontend-Quellen (TypeScript), gebaut mit esbuild
- `main.go` - startet den Server und öffnet den Browser

## 4. Datenmodell in Go (Entwurf)

IDs: Zustände und Varianten als `uint64` (Kreuzprodukte können groß werden,
erst das Aufräumen schrumpft sie). Feldpositionen als `soko.Wpos` (uint32).

```go
type Network struct {
    Field *soko.Field
    Rooms []*Room          // Index = RoomIndex, wird beim Merge kompaktiert
}

type Room struct {
    Index      uint32
    Fields     []soko.Wpos // aufsteigend sortiert, Fields[0] = stabile Kennung
    Goals      []soko.Wpos
    MaxBoxes   uint32
    Incoming   []*Portal   // eingehende Portale (gehören dem Raum)
    Outgoing   []*Portal   // ausgehende = Incoming der Nachbarn (gleiche Reihenfolge)
    States     *StateList
    StartState uint64
    Variants   *VariantList
    StartVariantCount uint64 // Varianten 0..n-1 sind Startvarianten (nur im Startraum)
}

type Portal struct {
    From, To     soko.Wpos
    FromRoom     *Room
    ToRoom       *Room
    Index        uint32   // Position in ToRoom.Incoming
    Dir          byte     // 'l','r','u','d'
    Opposite     *Portal
    BlockedBox   bool
    BoxSwap      map[uint64]uint64 // Zustand -> Zustand, wenn Kiste reingeschoben wird
    VariantSpans map[uint64]Span   // Zustand -> zusammenhängender Varianten-Bereich
}

type Span struct{ Start, Count uint64 }
```

**StateList**: Kistenpositionen aller Zustände in einem flachen `[]soko.Wpos`-Puffer
mit Offsets (kein Slice-of-Slices), plus Hash->ID-Map für Dedup beim Einfügen.
Get(id) liefert einen Blick in den Puffer (kein Kopieren).

**VariantList**: Struct-of-Arrays oder kompakte Records in einem flachen Puffer:
oldState, newState (uint64), **moves, pushes als uint32** (uint16 würde real fast
immer reichen - Levels über 65535 Moves sind extrem selten -, uint32 = mehr als
genug Reserve), BoxPortals (kleine uint32-Liste), PlayerPortal (uint32, MaxUint32 =
Spieler bleibt im Raum = Spielende), Pfad.

**Pfade**: bleiben immer erhalten (GUI-Vorschau, Path-Mapping, späterer Optimizer,
Lösungs-Ableitung) und werden bewusst **einfach gehalten: plain string je Variante**
(unkomprimiert, lrud) - Fehlerquellen minimieren geht vor Speicher sparen.
RLE (`3r` statt `rrr`) nur als Darstellungsoption im Frontend. Blob/Kompression/
Auslagerung erst, wenn es real drückt.

**Invarianten** (per Validate prüfbar, wie C#-Vorbild):

- jedes begehbare Feld gehört genau einem Raum
- Portale paarweise konsistent verlinkt, Incoming/Outgoing gleich lang
- Varianten pro (Portal, Zustand) lückenlos aufsteigend -> Span-Speicherung möglich
- alle Zustände/Varianten werden referenziert (nach Optimierung)
- genau ein Startraum

## 5. Bewusste Abweichungen vom C#

- **Kein Orakel, keine Bitgleichheit.** Verifikation: GUI-Sichtprüfung + Tests (Kap. 8).
- **Statusmeldungen/Abbruch**: statt GUI-Callback-Funktionen ein Status-Callback +
  `context.Context` für Abbruch. Kern bleibt frei von Web-Abhängigkeiten.
- **Merge materialisiert das Zustands-Kreuzprodukt** wie das Original (einfach,
  nachvollziehbar) und räumt danach auf. Lazy-Erzeugung nur der wirklich erreichten
  Zustände ist eine spätere Option, kein Startziel.
- **Abstraktion schlank**: konkrete Typen statt Basisklassen-Hierarchie
  (StateList/VariantList/... waren in C# für Disk-Varianten vorbereitet).
  Aufbohren später möglich.
- **RoomProfileFilter** (experimentell im C#) wird nicht portiert, bleibt aber
  als späterer Aufsatz eingeplant: Meta-Daten je Raum plus komplexere Scans unter
  Einbeziehung der Nachbarräume - z.B. einen Merge nur "simulieren", um überflüssige/
  doppelte Varianten zu filtern, ohne wirklich mergen zu müssen.
- **Solver/Rückwärtssuche** kommen später als Aufsatz; die Datenstrukturen
  (Pfade, BoxSwap in beide Richtungen denkbar) verbauen das nicht.
- Der **Single-Box-Scan** (welche Einzelkisten-Schübe sind überhaupt möglich)
  wird übernommen - er filtert schon beim Init Zustände, Varianten und setzt
  `BlockedBox`. Umsetzung über `soko` (Feld-Klon mit einer Kiste, Rückwärtsscan
  von den Zielen wie SokoBoxScanner).
- **Freeze-Ersetzung beim Parsen** (neu, JSoko-Verhalten, immer aktiv):
  eingefrorene Kisten auf Zielfeldern werden durch Wände ersetzt, Kiste und Ziel
  entfallen. Erkennung per klassischer Freeze-Analyse (Wände, rekursiv auch
  gegenseitig blockierte Zielfeld-Kisten wie 2x2-Blöcke), kaskadierend bis zum
  Fixpunkt. Kisten abseits der Ziele zählen konservativ nie als Blockade.

## 6. Debug-GUI (Browser)

### Architektur

- Go-Webserver (net/http) auf localhost, `main.go` öffnet den Browser automatisch.
- Frontend als statisches Bundle per `go:embed` in der exe - eine einzelne Binary.
- Frontend-Quellen in TypeScript, gebaut mit esbuild über `build.sh`
  (esbuild als Go-Dependency im Build-Schritt, kein npm-Baum).
  Das fertige Bundle wird mit eingecheckt, damit `go build` allein reicht.
- **Live-Updates per SSE** (Server-Sent Events, stdlib, keine Dependency):
  Status während Merge/Optimize/Automerge, Fortschritt, Effort.
  Kommandos (Merge, Optimize, Validate, Abbruch) als normale HTTP-POSTs.

### Listen mit Millionen Einträgen (harte Anforderung)

Zustands- und Variantenlisten großer Räume können mehrere Mio Einträge haben:

- API liefert grundsätzlich **seitenweise** (`offset`/`limit`), nie Komplettlisten
- Frontend rendert **virtualisiert** (nur sichtbare Zeilen, Scrollbalken rechnet
  mit Gesamtzahl)
- Sprung zu Eintrag N per Direkteingabe, Filter (z.B. Varianten eines Portals oder
  eines Zustands) laufen serverseitig über die Span-Indizes

### Ansichten (an FormDebugger angelehnt)

- **Spielfeld-Canvas**: Räume als farbige Flächen (gewählte Räume hervorgehoben),
  Portale sichtbar. Selektion wie im Original-FormDebugger: Linksklick fügt den
  Raum zur Auswahl hinzu, Linksklick gedrückt halten und ziehen fügt mehrere
  Räume hinzu, Rechtsklick deselektiert. Zoom.
- **Raum-Liste**: Index, Felderzahl, Zustände, Varianten, Portale, Kennfeld.
- **Zustands-Liste** des gewählten Raums; gewählter Zustand setzt die Kisten
  auf dem Canvas.
- **Varianten-Liste**, filterbar nach eingehendem Portal und Zustand; gewählte
  Variante zeigt Vorher/Nachher und den Laufweg (Pfad-Schritte, später animierbar).
- **Aktionen**: Merge (2 gewählte Räume), Optimize (Deadlock-Scan auf Auswahl),
  Validate, Automerge Start/Stop, Effort-Anzeige, Level laden (Datei/Text).
- **Statuszeile**: laufende Aktion, Fortschritt, Abbruch-Button.

## 7. Meilensteine

- ERLEDIGT: **M1 - Gründung + Init**: Modul, Kopien (soko/crc64/tools), Network-Aufbau
  mit 1-Feld-Räumen: Portale, Zustände, Startvarianten, Portalvarianten, BoxSwap,
  Single-Box-Scan, Validate. Unit-Tests auf kleinen Levels.
- ERLEDIGT: **M2 - GUI read-only**: Server + Frontend: Canvas, Raum-/Zustands-/Varianten-
  Listen mit Paging, Level laden. Ab hier kann Max sichtprüfen.
- ERLEDIGT: **M3 - Manueller Merge** (2026-08-17): Merge in 6 Schritten wie C#
  (MixStates, StartVariants, PortalVariants, UpdatePortals, OptimizeStates,
  UpdateRooms), Validate nach jedem Merge. Abweichungen vom Original: die
  Best-Moves-Suche arbeitet mit orientierungsfesten Aufgaben (state1/state2 fest
  an Raum 1/2 gebunden) statt gespiegelter Parameterlisten, und renewStates zählt
  StartVariantCount neu (C#-Bug: OptimizeTools.RenewStates vergaß das, wenn eine
  Start-Variante ihren Zielzustand verlor). Netzwerk-API: MergeRooms (Paar) und
  MergeSelection (Auswahl, merged paarweise solange verbunden). GUI: Selektion
  wie im Original-FormDebugger (Linksklick/Ziehen fügt hinzu, Rechtsklick
  entfernt), Merge-Button (synchron unter Schreibsperre), Validate-Button,
  Effort-Produkt der Auswahl. Tests: Voll-Merge = perfektes Spiel (Mini/TwoPush:
  genau 1 Variante mit optimaler Move-Zahl; TwoBox gegen Brute-Orakel 9 Züge),
  Merge-Reihenfolge-Unabhängigkeit, Abbruch lässt das Netzwerk unverändert.
  Noch offen (folgt mit M4/M5): SSE-Livestatus und Abbruch-Button in der GUI -
  der Kern hat den info-Callback samt gefahrlosem Abbruch vor Schritt 4 bereits.
- **M4 - Deadlock-Scan**: Optimize-Schritt (Reverse-Map, Scan vorwärts/rückwärts,
  unbenutzte Varianten/Zustände entfernen).
- **M5 - Automerge**: Aufwands-Schätzung + Kachel-Pattern wie im C#-FormDebugger,
  Abbruchkriterien, jederzeit stoppbar; Effort-Verlauf sichtbar.
- **M6 - Path-Mapping** (neu, gab es im C# nicht; bewusst vor dem Solver):
  bestehende LURD-Lösung einspielen und anhand der Räume auf deren Varianten
  abbilden. Existiert die geforderte (ggf. ineffizientere) Variante im Raum nicht
  mehr, wird sie durch eine passende bekannte Alternative ersetzt.
- **M7+ (später)**: Solver auf dem Netzwerk, Optimizer, Lösungshilfe,
  ProfileFilter-Ideen (Merge-Simulation, Nachbarraum-Scans).

## 8. Tests ohne C#-Orakel

- **Validate** als Invarianten-Test nach jedem Schritt (Init, Merge, Optimize).
- **Mini-Levels mit handverifizierten Zahlen**: Zustände/Varianten je Raum nach
  Init sind bei 1-Feld-Räumen von Hand nachrechenbar; als Regressionstests verankern.
- **Voll-Merge-Test**: Level komplett zu einem Raum mergen. Wenn Merger und
  Optimizer sauber arbeiten, bleibt darin **genau ein Start-State, ein End-State
  und eine einzige Variante** übrig - das perfekte Spiel. (Erlauben wir parallel
  neben moves/pushes- auch pushes/moves-Priorität, sind es entsprechend zwei
  Varianten.) Bleibt mehr übrig, hat der Optimizer Lücken. Die Move-Zahl der
  Variante muss der optimalen Lösungslänge entsprechen, die **goSokoWahnBrute**
  liefert - Brute ist unser hauseigenes Orakel, ohne das C# anzuwerfen.
  (Laut Max u.a. mit Vanilla machbar, wenn auch aufwendig.)
- **Merge-Reihenfolge-Test**: verschiedene Merge-Reihenfolgen müssen beim
  Voll-Merge auf dieselben Lösungs-Kennzahlen führen (minimale Moves, Anzahl
  optimaler Lösungen).
- Kennzahlen-Regression: State-/Variantenzahlen nach definierter Merge-Sequenz
  auf einem festen Level einfrieren (Schutz gegen stille Verhaltensänderungen).

## 9. Entschiedene Punkte (abgesegnet 2026-08-07)

1. **SSE statt WebSocket** - reicht locker, Input-Events per POST.
2. **esbuild (Frontend) und go build getrennt** - das gebaute Bundle wird
   eingecheckt, `go build` allein bleibt lauffähig; esbuild nur nötig, wenn am
   Frontend gearbeitet wird.
3. **Pfade immer mitführen, einfach als string** (unkomprimiert); RLE nur als
   Darstellung im Frontend. Performance/Speicher später.
4. **moves/pushes als uint32** (uint16 würde real fast immer reichen, uint32 =
   Reserve für alle Fälle).
5. Levelquellen: maps aus Brute plus Mini-Levels für Handverifikation;
   **levelcache mitbenutzen und Laden per URL erlauben** (game-sokoban.com wie
   in Brute). Für Rooms besonders geeignete Test-Level (IDs, nach Schwierigkeit,
   leichteste zuerst): **202, 5018, 37708, 38013**.
