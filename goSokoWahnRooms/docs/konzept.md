# Konzept goSokoWahnRooms

Go-Nachbau des Rooms-Konzepts aus dem C#-Projekt `SokoWahn/SokoWahnLib/Rooms` (2017-2019).
Kein bitgenauer Port: das Konzept wird übernommen, Datenstrukturen und Abläufe dürfen
neu gedacht werden. Verifiziert wird visuell über eine Debug-GUI (Browser) durch Max,
nicht über ein C#-Orakel.

## 1. Ziel und Abgrenzung

Die Grundidee hinter der Raum-Aufteilung (Max, 2026-08-18): Komplette Levels sind
nur bis zu einer bestimmten Größe/Kistenzahl/Komplexität direkt lösbar (z.B. per
goSokoWahnBrute - im Grunde "alles Mögliche durchrechnen"). Zu große Levels werden
deshalb in kleinere Räume aufgeteilt, und diese Räume werden einzeln PERFEKT
optimiert: doppelte und ineffizientere Varianten/Zustände fliegen raus, bis je
Raum nur noch das Nötige übrig ist. Die Optimierung der Räume ist damit kein
Nebenschritt, sondern der Kern des Ansatzes.

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

## 4b. Optimalitäts-Standard (festgelegt 2026-08-18)

Es zählt der Move-Standard: move-perfekt zuerst, Pushes nur als Tie-Break.

- Varianten mit schlechteren Moves bei gleicher Wirkung werden entfernt.
- Bei gleichen Moves überlebt die Variante mit weniger Pushes.
- Bei vollständig gleichwertigen Varianten (gleiche Wirkung, gleiche Moves und
  Pushes) wird nur eine behalten.
- Reine Push-Optimierungen (weniger Pushes auf Kosten von Moves) sind kein Ziel
  und werden ignoriert.

"Wirkung" einer Variante aus Außensicht: Endzustand des Raumes, rausgeschobene
Kisten je Portal und Austritts-Portal des Spielers (bzw. Spielende) - die
interne Historie zählt nicht.

Umgesetzt im Merge-Emit (2026-08-18): die Best-Moves-Suche vergleicht
(moves, pushes) lexikographisch, und je Netto-Wirkung wird nur die beste
Variante eingetragen. Der alte Task-Schlüssel war bereits fast wirkungs-scharf;
real neu sind der Push-Tie-Break bei Move-Gleichstand und das Zusammenfallen
wirkungsgleicher End-Varianten aus verschiedenen Teilraum-Seiten.

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
- **Solver/Rückwärtssuche**: beide inzwischen da (M7) - die Datenstrukturen
  haben das wie erhofft nicht verbaut (BoxSwap und Varianten-Verzeichnis
  ließen sich invertieren, die Pfade verketten in beide Richtungen).
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
- ERLEDIGT: **M4 - Deadlock-Scan** (2026-08-17, rooms/deadlock.go): Vorwärts-Scan
  (von der Startsituation erreichbare Varianten) und Rückwärts-Scan (Varianten,
  die zum lokalen Ende führen können) über Zustands-Masken (alle Portal-Teilmengen
  von BoxSwaps bzw. Pull-Swaps), Schnittmenge entfernt den Rest samt verwaister
  Zustände (renewVariants + removeUnusedStates). Läuft automatisch nach jedem
  Merge (Gating wie C#: max. 12 Portale, mehr als 2 Räume übrig, unter 10 Mio
  Varianten) und manuell per Optimize-Button/POST /api/optimize auf der Auswahl.
  Die Selbes-Portal-Regel des Originals (Wiedereintritt durchs Austritts-Portal
  nur nach rausgeschobener Kiste, dort noch mit "todo: bug?" markiert) ist in
  REPARIERTER Form übernommen (2026-08-18, Kommentar in deadlock.go): sie gilt
  nur, wenn der Besuch durch dasselbe Portal herein- UND hinauskam. Dann ist sie
  bewiesen: das Portal-Außenfeld war beim Eintritt frei (der Spieler stand
  darauf) und bleibt es bis zum Austritt, der Besuch lässt die Außenwelt also
  komplett unverändert - statt raus- und wieder reinzulaufen kann der Spieler
  drinbleiben und dieselbe Fortsetzung 2 Züge billiger spielen. Die pauschale
  C#-Fassung hatte ein Loch: bei Eintritt über ein anderes Portal kann der
  Austritts-Schritt draußen eine Nachbar-Kiste schieben (wird beim Nachbarraum
  verbucht, lokal unsichtbar) - der Besuch ist dann NICHT wirkungslos, und die
  Regel hätte zwingend nötige Wiedereintritte wegwerfen können. Die Regel ist
  die Hauptquelle der Ausdünnung: bei Ein-Portal-Kammern (wo sie immer greift)
  kaskadiert sie ganze tote Zustands-Äste weg (Fassung ganz ohne Regel:
  20 Zustände / 90 Varianten in der 202er-Kammer statt 10 / 25). Kleinere Abweichungen: Varianten
  werden einzeln statt Span-weise markiert (Doppelzähl-Bug im C#), Ziele-Check
  auch für End-Startvarianten, Aufgaben werden dedupliziert.
  Referenz-Zahlen als Tests verankert: Vanilla erste 20 Räume (Endergebnis
  211 Zustände / 7867 Varianten, Effort 7,8e30; seit der effort-sortierten
  Merge-Reihenfolge 2026-08-19 - wie das C#-Original: verbundenes Paar mit
  kleinstem Varianten-Produkt zuerst - braucht der Weg nur noch 2 statt 5
  Scan-Eingriffe), Idempotenz (zweiter Scan findet 0)
  und der Orakel-Vergleich der 202er-Kammer gegen die SokoWahnLib
  (rooms/oracle202_test.go, C#-Seite: SokoWahn/roomscli/ - kleines Konsolen-Tool,
  das dieselben Merges im Original ausführt und Zustände/Varianten dumpt;
  Muster für künftige Vergleichsfälle).
- IN DISKUSSION: **M4b - konstruktive Dominanzsuche** (Ansatz gewählt 2026-08-18,
  Semantik noch offen): statt weiterer handgemachter Prune-Regeln (jede braucht
  einen eigenen Beweis, siehe das Portalfeld-belegt-Loch im verworfenen
  "Park-Verbot") rechnet eine lokale Suche pro Raum die Dominanz nach. Ein Raum
  ist aus Außensicht eine Black Box; sichtbar ist nur die Signatur seiner
  Nutzung (Folge von Portal-Ereignissen: Besuch mit Eintritt/Austritt/Exporten,
  Kisten-Einschub; plus Endzustand). Eine Variante ist überflüssig, wenn jede
  Nutzung, in der sie vorkommt, durch eine Nutzung mit identischer Signatur und
  besseren Kosten (moves, dann pushes) ersetzbar ist - inklusive
  signatur-kompatibler Reduktionen (Besuch weglassen/fusionieren, wenn Ein- und
  Austritt am selben Portal liegen). Die bisherige Selbes-Portal-Regel ist der
  bewiesene Spezialfall davon.
  Stand der Handrechnung (2026-08-18, Werkzeug: rooms/domlab_test.go - Labor
  enumeriert alle Nutzungen der 202er-Kammer je Außen-Signatur und kürt die
  kostenoptimalen Ketten): Signatur-Alphabet ist E (Einschub), X (Besuch mit
  Export), ! (End-Besuch); exportlose Besuche sind außenweltlich TRANSPARENT
  (Theorem von Max: jedes Außen-Ereignis findet am Portal statt, ein exportloser
  Besuch endet wo er beginnt und ist dort kostenlos anhängbar - Bedienungen mit
  verschieden vielen solchen Besuchen sind dieselbe Bedienung).
  WICHTIGE PRÄZISIERUNG (Bug-Repro Level 5005, 2026-08-19): die Transparenz
  gilt NICHT für einen exportlosen Besuch als allererste Aktion einer Nutzung.
  Das Theorem stützt sich darauf, dass der Spieler beim benachbarten sichtbaren
  Ereignis ohnehin am Portal steht - vor dem allerersten Ereignis war er dort
  aber nachweislich noch nie, und das erste Ereignis selbst kann die Zufahrt
  versperren (5005: Kiste (2,10) muss als Erstes durch den Ein-Feld-Schacht in
  den unteren Raum eingeschoben werden; ein Besuch VOR diesem Einschub ist
  geometrisch unmöglich). Ein transparentes B an Position 0 ließ die Dominanz
  "EXXX" durch die real unspielbare Reihenfolge "[B]EXXX" ersetzen - sie
  entfernte den Einschub-Zweig des Start-Zustands, removeUnusedStates riss den
  BoxSwap-Eintrag mit, das Level war tot. Fix: ein exportloser Besuch als
  allererste Aktion ist das eigene sichtbare Zeichen "B"; alle späteren bleiben
  transparent. Regressions-Tests: rooms/repro5005_test.go (Merge der Region
  unterhalb Feld (2,12) mit/ohne Zwischen-Optimize; der Start-Einschub muss
  die Dominanz überleben). Die 202er-Ergebnisse blieben unverändert. Ergebnis für die
  Kammer (Horizont 11 sichtbare Ereignisse): 5 essenzielle Varianten (rein-raus-
  Garage, parken-auf-26 + Kombi-Abschluss = der einmalige 2-Züge-Trick, direkt-
  ins-Ziel mit/ohne Spielende), 10 austauschbare (nur in Gleichstands-
  Alternativen, z.B. die Park-Treppe über 25), 10 nie-optimale (Abkürzungen).
  Minimal-These (Max, 2026-08-18): die Kammer zerfällt in zwei Regime - "ohne
  parken" (direkt ins Ziel, mit/ohne raus, plus Garage davor) und "mit parken"
  (immer über parken-auf-26, weil der 2-Züge-Trick genau einmal wirkt; Garagen
  danach als Loop beliebig oft). Ergebnis: 7 der 25 Varianten und 5 der 10
  Zustände bedienen ALLE Außen-Signaturen kostengleich (Test
  TestDominanceLab202Minimal).
  Graph-Sicht als Beweis statt Horizont (rooms/usagegraph.go, 2026-08-18):
  Max' Loop-Idee (Nutzung = Präfix + N x Loop + Abschluss) in Automaten-Form.
  Der Nutzungsraum eines Ein-Portal-Raums ist ein ENDLICHER Graph (Knoten =
  Zustand + Besuchs-Sperre der Selbes-Portal-Regel; Kanten = Einschub E,
  Export-Besuch X, exportloser Besuch als unsichtbare Epsilon-Kante). Der
  Kostenvergleich voll vs. reduziert läuft als synchrone Suche über
  Konfigurations-Paare mit Offset-Normalisierung und Memoisierung: wiederholt
  sich eine normalisierte Vergleichs-Situation, wiederholt sich alles Weitere -
  die Wiederholung IST der Loop, die Memoisierung ersetzt die Induktion über
  N Durchläufe. Sättigt die Suche ohne Differenz, ist die Kostengleichheit für
  ALLE Nutzungslängen bewiesen; sonst gibt es ein konkretes Gegenbeispiel-Wort
  (oder "unentschieden" am Sicherheitslimit, falls Loop-Raten divergieren).
  Für die 202er-Kammer: voll (14 Knoten) vs. minimal (7 Knoten) sättigt nach
  nur 5 Situationen -> Minimal-These BEWIESEN, ohne Horizont
  (TestUsageGraphMinimalProven); die Negativprobe ohne v19/v20 liefert
  Gegenbeispiele (TestUsageGraphDetectsMissing). Die Zyklen-Enumeration
  bestätigt nebenbei Max' Loop-These wörtlich: alle Garagen-Loops der Kammer
  kosten 9 moves / 5 pushes je Einschub+Export-Paar (TestUsageGraphLoops202);
  der Automat ist gegen das Labor kreuzvalidiert (TestUsageGraphCrossCheck202).
  Kandidaten-Finder (rooms/dominance.go, 2026-08-18): Greedy-Elimination auf
  dem Verifier. Dank Monotonie (weniger Varianten bedienen nie billiger, nur
  teurer) genügt EIN Durchlauf für ein lokales Minimum: was einmal als nicht
  streichbar erkannt wurde, bleibt es in jeder kleineren Menge. Zwei Phasen:
  erst ganze ZUSTÄNDE probeweise eliminieren (alle Varianten streichen, die
  den Zustand berühren - Zustände sind die teure Größe, sie gehen beim Mergen
  multiplikativ ein), dann einzelne Varianten. Die Zustands-Phase entschärft
  das Überdeckungsproblem: reine Varianten-Elimination in ID-Reihenfolge
  strandet in der 202er-Kammer bei 9 Varianten auf 8 Zuständen (streicht früh
  die "richtigen" und hält Gleichstands-Familien am Leben), das Zwei-Phasen-
  Greedy trifft exakt die Handrechnung: 7 Varianten {1,3,5,6,17,19,20} auf
  5 Zuständen, jede Streichung einzeln bewiesen (TestReduceVariants202).
  Anwendbar war das zunächst nur auf Ein-Portal-Räume ohne Startvarianten
  (aufgehoben durch die Mehr-Portal-Signatur, siehe unten).
  Eingebaut in den Optimize-Button (2026-08-18): OptimizeRooms fährt je Raum
  erst den billigen Deadlock-Scan, dann DominanceReduce (Finder + echtes
  Entfernen über renewVariants/removeUnusedStates, danach Validate). Die
  Dominanz läuft NUR am Button, nicht beim Auto-Scan der Merges
  (Arbeitsteilung siehe unten). End-to-End verankert: nach Merge der
  202er-Kammer entfernt der Button 18 Varianten, übrig 7 auf 5 Zuständen,
  zweiter Lauf findet nichts (TestOptimizeRoomsDominance202).
  Härtetest Level 5018 (aenigma "soko 47", zweites Test-Level, jetzt als
  maps.Map5018 eingebaut): der Ziel-Trakt links (3x5 Felder, 9 Ziele, ein
  Portal) hat nach dem Merge 1718 Zustände / 42887 Varianten. Dafür nötig
  waren zwei Ausbauten (2026-08-18): Gruppen-Tests (divide & conquer statt
  Variante-für-Variante - Differenz-Antworten sind billig, Abbruch am ersten
  Gegenbeispiel-Wort; Kostengleichheit ist teuer, räumt dafür ganze Gruppen
  ab; Blätter sind Einzeltests, lokale Minimalität bleibt) und ein
  performanter Vergleichskern (Konfigurationen als dichte Arrays mit
  Versions-Zähler + Worklist statt Maps mit Fixpunkt-Schleifen, Kanten nach
  Label vorsortiert, binäre Fingerprints - Einzelvergleich 277ms -> 46ms).
  Dazu Ernte-Runden (2026-08-19, ersetzt das anfängliche 30s-Zeitbudget aus
  der Ära vor Hintergrund-Jobs und Stop-Button): sobald eine Runde über die
  Hälfte der Varianten als entbehrlich bewiesen hat, wird angewandt und auf
  dem geschrumpften Raum frisch weitergesucht - alle weiteren Vergleiche
  werden dadurch drastisch billiger (derselbe Effekt, den die Budget-Schnitte
  zufällig hatten, nur deterministisch); DominanceReduce läuft so in einem
  Aufruf bis zum Fixpunkt, abbrechbar jederzeit per Stop (Bewiesenes bleibt
  angewandt). Ergebnis: Fixpunkt in einem Optimize-Druck nach ~90s, der Trakt
  schrumpft auf 310 Zustände / 321 Varianten - 99,25% der Varianten bewiesen
  entbehrlich, ohne Zeitbudget deterministisch und exakt verankert
  (TestOptimizeRoomsDominance5018, läuft nur ohne -short).
  Max-Moves-Budget (Max' Idee, 2026-08-19): eine VERIFIZIERTE obere Schranke
  der Gesamtlösung (z.B. die Länge einer bekannten Lösung, GUI-Eingabefeld
  "max moves") macht die Dominanz schärfer und den Vergleich endlich. Die
  Zerlegung: jeder Zug gehört genau einem Raum (Zähl-Konvention), also gilt
  Nutzung(R) <= B - Summe der Pflicht-Minima aller anderen Räume. Das
  Pflicht-Minimum je Raum (Room.MinMoves) gilt für JEDEN Raum, auch
  Mehr-Portal und Startvarianten: ein Dijkstra über die Zustände, jede
  Variante als Kante (Portal egal, Spieler-Verfügbarkeit über-approximiert),
  Einschübe als 0-Kanten - beweisbar sichere Untergrenze der im Raum
  anfallenden Züge (Ziel am Portal per Einschub ergibt korrekt 0). Der Wert
  ist am Raum gecacht (invalidiert bei Strukturänderung, vorgewärmt nach
  Init/Merge/Optimize für racefreie Lese-Zugriffe) und in der GUI beim
  Anklicken sichtbar (Effort-Zeile: "min moves", Summe der Auswahl).
  OptimizeRooms rechnet slack =
  B - Summe der Minima (B unter der Summe = Fehler: Schranke bewiesen
  unerreichbar) und kappt je Raum Nutzungen über Minimum + slack: Akzeptanzen
  über dem Budget werden ignoriert, Zweige mit Mindestkosten darüber gekappt.
  Nebeneffekte: divergierende Loop-Raten laufen ins Budget statt ins
  maxConfigs-Netz (weniger "unentschieden"), und bei knappem Budget fallen
  auch kostengleich unersetzbare, aber zu teure Varianten (202er-Kammer mit
  Slack 0: 4 statt 7 Varianten, TestOptimizeMoveLimitTight; Optimum bleibt
  erhalten, TestOptimizeWithMoveLimit). ACHTUNG: ein zu kleines B wirft die
  Optimallösung weg - die Verantwortung liegt beim Eingebenden; der saubere
  Lieferant wird später M6 (verifizierte eingespielte Lösung).
  Budget-Schnellscan (Max' Idee, 2026-08-19): wirkt im Gegensatz zur Dominanz
  auf JEDEN Raum (auch Mehr-Portal/Startvarianten) - je Variante ist
  fwd[OldState] + Kosten + bwd[NewState] (Vorwärts-/Rückwärts-Dijkstra über
  die Zustände) eine sichere Untergrenze jeder Nutzung, die sie enthält;
  liegt sie über Minimum + Slack, fliegt die Variante (Distanz-Korridor wie
  Brutes Tiefenschranke; unerreichbare Varianten fallen gratis mit). Läuft
  als Fixpunkt (Streichungen heben Minima, kleinerer Slack schärft die
  anderen Räume) automatisch vor der Dominanz, wenn max moves gesetzt ist.
  Verliert ein Raum dabei alle nötigen Varianten, wird die Schranke als
  BEWIESEN unerreichbar gemeldet (das Netz ist dann absichtlich unlösbar
  zurückgelassen - Level neu laden); TestBudgetScan/-ProvesUnreachable.
  Auch der MERGE beachtet das Budget (2026-08-20): die Merge-Suche erzeugt
  Verbund-Varianten über min1 + min2 + Slack gar nicht erst (Kosten wachsen
  monoton, der Cutoff in follow kappt ganze Fortsetzungs-Bäume; der billigste
  Vertreter jeder Wirkung liegt unter dem Limit und überlebt den effectKey-
  Dedup normal) - das kappt die Varianten-Explosion an der Wurzel, der
  nachgelagerte Budget-Scan mit Distanz-Korridor bleibt schärfer
  (TestMergeWithMoveLimit: TwoBox-Optimum überlebt Budget 9, 202er-Kammer
  mit Budget 83 identisch zum ungebremsten Merge).
  Mehr-Portal-Signatur + Startvarianten (Konzept mit Max abgestimmt
  2026-08-20): die Dominanzsuche erfasst damit ALLE Räume. Das Alphabet wird
  portal-annotiert:
    - Einschub "E@p" (Kiste kommt durch Portal p rein, BoxSwap des Portals).
    - Besuch als Tupel (Eintritts-Portal, Export-Portale in Schub-Reihenfolge,
      Austritts-Portal). Die Export-SEQUENZ ist sichtbar inkl. Anzahl - eine
      Verschärfung gegenüber dem Ein-Portal-'X' (das die Anzahl nicht trug;
      dort rettete die Kisten-Bilanz die Korrektheit: jede Akzeptanz endet im
      Zustand 0, die Gesamt-Exportzahl ist damit wortbestimmt - bei mehreren
      Portalen ist aber die VERTEILUNG auf die Portale außen relevant).
    - Start-Besuch "S" (Startvarianten-Erweiterung): bei Räumen mit
      Startvarianten beginnt jedes Wort zwingend mit einem S-Zeichen
      (Exporte + Austritts-Portal bzw. End-Variante "S!") - der Spieler ist
      der einzige Akteur, solange er im Raum steht, kann draußen nichts
      passieren. Nach dem S-Austritt läuft das normale Spiel; die nackte
      Out-Akzeptanz (Zustand 0 hinterlassen) gilt erst nach dem S.
  Weil jedes Zeichen sein Austritts-Portal trägt, ist die Spieler-Position
  nach jedem Ereignis WORTBESTIMMT. Daraus folgen die beiden B-Regeln
  strukturell (statt als Sonderregeln):
    - Epsilon-Transparenz: ein exportloser Selbst-Besuch B(q,q) ist nur
      DIREKT NACH einem Ereignis mit Spieler-Endposition q unsichtbar - dort
      steht der Spieler nachweislich, und der Durchgang ist nachweislich frei
      (nach Austritt: er kam gerade durch; nach Einschub E@q: die Kiste ist
      jetzt drin und im Zustand verbucht). VOR einem Ereignis gibt es keine
      Transparenz - vor einem Einschub kann die noch draußen liegende Kiste
      die Zufahrt versperren (die 5005-Lektion, jetzt strukturell). Der
      Epsilon-Abschluss ist einschrittig (das B sperrt sein Portal).
    - Sichtbares B: V(q,[],q) ist als eigenes Zeichen zulässig, wenn der
      Spieler laut Wort NICHT bei q steht (Anforderung "Außenwelt liefert
      den Spieler bei q an" - nötig für die Vollständigkeit: eine echte
      Nutzung darf zwischendurch anderswo einen exportlosen Besuch machen).
      Ein-Portal-Spezialfall: nur an Position 0 möglich = exakt die
      bisherige Erste-Aktion-Regel, die Anker bleiben.
  Selbes-Portal-Sperre portal-genau: nach einem exportlosen Besuch mit
  Austritt bei p ist nur der direkt nächste EINTRITT bei p gesperrt
  (fusionierbar); Knoten = (Zustand, gesperrtes Portal), jeder Einschub und
  jeder andere Besuch löst. Nach Startvarianten KEINE Sperre (der
  Fusions-Beweis ist für Startvarianten nicht geführt - konservativ).
  Spieler-Konnektivität (Max' Idee, 2026-08-20): die Außenwelt zerfällt je
  Raum in statische Komponenten (Zusammenhang der begehbaren Felder OHNE die
  Raum-Felder; nur Wände zählen, fremde Raum-Felder optimistisch frei -
  Nichtzusammenhang ist damit BEWIESEN, Zusammenhang nur angenommen =
  konservativ). Ein Ereignis an Portal q ist nur legal, wenn q in der
  Komponente der aktuellen Spieler-Position liegt (Anfang: Komponente des
  Spieler-Startfelds); die Seite wechseln kann der Spieler nur durch
  sichtbare Durchgangs-Besuche. Illegale Wörter fallen aus der
  Anforderungsmenge -> mehr Reduktion. Der Filter ist REINE Optimierung, die
  Korrektheit hängt nicht an ihm (sie kommt aus den portal-annotierten
  Zeichen). Kisten-Konnektivität bewusst NICHT einbezogen (fremde Kisten,
  zustandsabhängige Flüsse - notierte Ausbaustufe).
  Der Vergleich (compareUsageGraphs) bleibt strukturell gleich: die
  Spieler-Position läuft als Wort-Zustand mit (im Fingerprint), statt fester
  Labels iteriert die Suche über die an der Situation legalen
  Zeichen-Klassen (Symbole, geteilte Interning-Tabelle zwischen voll und
  reduziert).
  Kosten-Steuerung (Lehre aus der 5005-Repro, 2026-08-20): bei
  Mehr-Portal-Monstern sättigt der Vergleich oft gar nicht (Raum 118 der
  Repro: 22 Felder, 7 Portale, 8990 Varianten, 659 Symbole - selbst der
  Vergleich des vollen Graphen MIT SICH SELBST sättigt nach 1 Mio
  Situationen nicht), jeder Einzeltest klappert dann nur das maxConfigs-Netz
  ab und die Greedy-Suche verbrennt Minuten. Zwei deterministische Bremsen:
  (1) Selbst-Sättigungs-Probe als Eingangstor von reduceVariants - sättigt
  voll-gegen-voll nicht innerhalb des Limits, wird der Raum mit Meldung
  übersprungen (ein max-moves-Budget kappt die Wortvielfalt und öffnet das
  Tor wieder); (2) unentschiedene GRUPPEN werden nicht mehr geteilt (die
  Teil-Gruppen erben den explodierenden Situationsraum fast sicher - vorher
  liefen Tausende Einzeltests je ~100k Situationen), sondern konservativ
  komplett behalten.
  Arbeitsteilung in der GUI (Max, 2026-08-18): die schnellen, bewiesenen Regeln
  (Deadlock-Scan) laufen wie bisher automatisch bei jedem Merge mit; die
  Dominanzsuche hängt am Optimize-Button und darf aufs Ganze gehen - angedacht
  als inkrementelle Endlos-Funktion, die immer tiefer/komplexer weiterrechnet
  (Budget/Horizont wächst), bis der Nutzer per Stop-Button entscheidet, dass
  genug gerechnet ist; bis dahin gefundene überflüssige Varianten/Zustände sind
  dann bereits entfernt.
  Livestatus + Stop ERLEDIGT (2026-08-19): rooms.ProgressFunc(text, rooms)
  ersetzt die reinen Text-Callbacks - Merger (Step3, alle 4096 Zustände),
  Deadlock-Scan und Dominanzsuche (alle 64 Gruppen-Tests) melden Schritt und
  bearbeitete Räume. Merge/Optimize laufen als Hintergrund-Job unter der
  Schreibsperre; /api/progress streamt den Status per SSE (eigener Mutex,
  nicht hinter der Schreibsperre), die GUI zeigt den Text in der Effort-Zeile
  und markiert die Felder der bearbeiteten Räume gelb (wie der alte
  C#-FormDebugger). /api/stop bricht ab: Merge/Scan lassen den Raum
  unverändert, die Dominanz wendet bereits Bewiesenes an.
- **M5 - Automerge**: Aufwands-Schätzung + Kachel-Pattern wie im C#-FormDebugger,
  Abbruchkriterien, jederzeit stoppbar; Effort-Verlauf sichtbar.
- **M6 - Path-Mapping** (neu, gab es im C# nicht; bewusst vor dem Solver):
  bestehende LURD-Lösung einspielen und anhand der Räume auf deren Varianten
  abbilden. Existiert die geforderte (ggf. ineffizientere) Variante im Raum nicht
  mehr, wird sie durch eine passende bekannte Alternative ersetzt.
- **M7 - Solver** ERLEDIGT (2026-08-20, vorgezogen vor M5/M6):
  Brute-Force-Vorwärtssuche auf dem Netzwerk (rooms/solver.go, C#-Vorbild
  RoomSolver + brute-Technik). Aufgabe = Zustand je Raum + Spieler (Raum +
  eingehendes Portal) + Pfad-ID; Aufgaben-Listen je Zugtiefe, Hash-Dedup;
  je Aufgabe werden reine Lauf-Varianten transitiv expandiert, Push-Varianten
  erzeugen Folge-Aufgaben (BoxSwaps in die Nachbarräume). Gelöst = alle Räume
  in Zustand 0, Spieler bleibt drin; gesucht wird bis das Optimum BEWIESEN ist
  (Tiefe erreicht die beste Lösung). Gegenüber dem C#: echter LURD-Laufweg
  über einen Solver-eigenen PathStore-DAG (jede Lösung wird gegen das
  Spielfeld verifiziert) und Untergrenzen-Pruning - je Raum liefert der
  Zustands-Dijkstra (minmoves.go, rückwärts) die bewiesene Schranke
  "Zustand -> gelöst", die Summe kappt gegen Budget/beste Lösung.
  Performance-Lehren (profiliert am frischen 202er, 67s -> 21s): Hash-Dedup
  beim ERZEUGEN statt beim Abholen (sonst >80% Duplikate in den Listen),
  Laufwege lazy erst für akzeptierte Aufgaben materialisieren (vorher 301 Mio.
  PathStore-Knoten = 2,4 GB, jetzt 16 Mio.), Deltas statt O(Räume) je
  Push-Kandidat (XOR-Zustands-Hash), Lauf-Dedup über Per-Raum-Arrays mit
  Generationszähler statt Map. Test-Anker: 200 frisch 78 Züge (~10 ms), 202
  frisch 83 Züge. Vanilla nur als Messwert (zu teuer für den Testlauf),
  siehe unten - das Brute-Optimum fällt ohne Voll-Merge.
  Bedienung wie brute (Max, 2026-08-20): Sitzung startet PAUSIERT als
  Hintergrund-Job unter der LESE-Sperre (GUI bleibt bedienbar, Mutationen
  sperrt der Busy-Status); Tasten b = Bulk-Schritte, a/Leertaste = Auto
  (EIN Bulk je Anzeige-Tick von 100 ms - jedes Update zeigt genau einen
  Bulk-Schritt, das Tempo steuert die Bulkgröße; Default 100), +/- = Bulkgröße x10,
  1/2/3 = Suchrichtung, Stop-Button bricht ab (beste bisherige Lösung zählt).
  Das Solver-Panel ersetzt währenddessen die beiden linken Listen-Spalten
  (kein neues Fenster) und zeigt Tiefenzeilen wie brute. Die Lösung wird am
  Server gemerkt (überlebt Merges, nicht den Level-Wechsel), setzt max moves
  und ist per Pfeiltasten je Kistenschub durchsteppbar (c = LURD kopieren).
  **Rückwärtssuche/bidirektional** (2026-08-20, Technik aus brute, Indizes
  nach C#-RoomReverse-Idee): dieselben Vorwärts-Varianten werden rückwärts
  benutzt (rooms/solverback.go) - je Raum ein invertiertes Varianten-
  Verzeichnis (Austrittsportal, NewState) samt Eintrittsportal je Variante
  und invertierte BoxSwaps ("Kiste zurückziehen", mehrdeutige Umkehrungen
  verzweigen). Eine Rückwärts-Aufgabe hat EXAKT die Vorwärts-Normalform
  (Zustände + Eintrittsportal), trägt aber den RESTWEG bis zum gelösten
  Level; Saat = End-Varianten mit gelöstem Endzustand, zurückgerollt.
  Fronten-Treffen per Hash-Lookup in der Gegentabelle (der Hash speichert
  je Stellung Tiefe + Pfad-ID), jede Kandidatin wird gegen das Spielfeld
  verifiziert - scheitert das, war es eine 64-Bit-Kollision und sie wird
  gezählt verworfen (brutes verifyMeet). Untergrenzen rückwärts spiegelbildlich
  über stateDistances(StartState, vorwärts) = "Start -> Zustand". BEWIESEN ist
  das Optimum, wenn die Tiefensumme das Limit übersteigt oder eine Front
  erschöpft ist (Startvarianten deckt immer die Vorwärts-Saat ab - brutes
  Lehre: die Rückwärtsfront kann die rohe Startstellung nie treffen, wohl
  aber deren Push-Nachfolger; darum ist auch die reine Rückwärtssuche
  vollständig). Richtungs-Automatik wie brute: einmal je Gesamttiefe das
  Effizienz-Verhältnis "erreichte Tiefe je Hash-Eintrag" (Kreuzmultiplikation),
  manuell per SetDirMode (GUI 1/2/3). Messwerte (2026-08-20): 202 frisch
  bidirektional ~6 s statt ~21 s rein vorwärts (Anker läuft mit Automatik);
  Vanilla frisch OHNE Budget bidirektional 50 s / 12,7 Mio. Hash-Einträge
  (vorher ~65 s rein vorwärts MIT Budget 230), rein rückwärts 122 s / 27,5
  Mio. (rückwärts streut auf dem Rooms-Modell breiter als vorwärts - jeder
  Laufketten-Eintrittspunkt wird eine Aufgabe, nicht nur jeder Push).
  Beifang der verifizierten Treffen: der alte FNV-Zustands-Hash (crc64-
  Paket) kollidierte am Vanilla zigtausendfach zwischen VERSCHIEDENEN
  Stellungen (für kleine Zustands-IDs sind FNV-Werte hochkorreliert, die
  XOR-Differenzen löschen sich über mehrere Räume systematisch aus) - die
  Vorwärts-Dedup hätte damit still Stellungen verschmelzen können; seit dem
  SplitMix64-Zobrist-Mix (stateMix/taskKey) laufen 202 und Vanilla mit
  0 Kollisionen.
- **Später**: Optimizer bestehender Lösungen, Lösungshilfe, ProfileFilter-Ideen
  (Merge-Simulation, Nachbarraum-Scans); M5 (Automerge) und M6 (Path-Mapping)
  siehe oben, beide noch offen.

## 8. Tests ohne C#-Orakel

Zwei Stufen (Max, 2026-08-20 - "das nimmt langsam überhand"): der alltägliche
`go test ./...` läuft in Sekunden; die Anker-LANGLÄUFER laufen nur auf
Anforderung und gehören vor jeden Commit, der das Suchverhalten ändert:

    SOKO_ANKER=1 go test ./...

Die Anker-ERGEBNISWERTE (Knoten/Varianten/States) sind maschinenunabhängig;
die LAUFZEITEN dagegen nicht - Max arbeitet abwechselnd auf mehreren Rechnern
(Befund 2026-08-21: "72 s vs 80 s" entpuppte sich erst nach einer Baseline-
Messung auf demselben Rechner als echte Regression). Vergleiche daher nur
gegen die Spalte der eigenen Maschine; bei Änderungen am heißen Pfad die
Tabelle nachziehen:

| Anker-Langläufer                | i5 (64 GB) | Ultra 9 (128 GB) |
|---------------------------------|-----------:|-----------------:|
| 5018-Dominanz                   |      ~73 s |                ? |
| Repro-5005 (alle 3 Tests)       |      ~33 s |                ? |
| 202-Solver (frisch, Automatik)  |       ~6 s |                ? |

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
   leichteste zuerst): **202, 5018, 5005, 37708, 38013** (5005 nachträglich
   eingereiht, 2026-08-19: Single-Portal-Kammern wie 5018, aber verschachtelt,
   für Brute bislang unlösbar - der Ernstfall; Achtung, der Spieler startet
   dort im Ziel-Trakt, dessen Startvarianten die Ein-Portal-Dominanz noch
   nicht modelliert).
