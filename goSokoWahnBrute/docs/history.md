# Historie goSokoWahnBrute

Vergangenes, das aus architektur.md und roadmap.md ausgezogen wurde: Herkunft,
verworfene Varianten, Messprotokolle und die Chronik der erledigten Ausbaustufen.
Alles hier ist abgeschlossen - die aktuelle Architektur steht in architektur.md,
offene Ideen in roadmap.md.

## Herkunft und Orakel-Ära (bis 08/2026)

goSokoWahnBrute entstand im August 2026 als Go-Nachbau des C#-Solvers
`SokoWahn_4th_generation` (Copyright 2013, in `oldstuff/` archiviert). Zur
Verifikation diente ein Konsolen-Orakel (`oldstuff/refcli/`, .NET-Framework-csc):
die Suche ohne Filter und alle Blocker-Stufen wurden bitgenau gegen den C#-Code
diffbar gehalten - jede Tiefenzeile, jede Muster-/Prüfzahl byte-gleich. Auf dieser
Basis wurde der Port Stück für Stück abgenommen; der 13 Jahre alte C#-Code
kompilierte dafür auf Anhieb wieder.

Drei bewusste Weiterentwicklungen wichen per Default vom Original ab und waren
über das CLI-Flag `-dirclassic` abschaltbar (volle Original-Semantik):

1. **Richtungswahl der Suche**: Effizienz-Verhältnis (erreichte Tiefe je
   Hash-Eintrag) statt "kleinere Tabelle zuerst" (Original Z. 519-523).
2. **Gleichstands-Stellungen behalten** (keepEqual, 08/2026): die
   Nach-Fund-Beschneidung des Originals verwarf Stellungen auf alternativen
   zugoptimalen Pfaden - das Futter der Push-Optimierung (siehe Level 361 unten).
3. **Adaptiver Stufenbau des Blockers** mit den Stufe-1-Regeln
   (RulesPatternThreshold, siehe Cache-Versionen unten).

Die bitgenauen Referenzwerte der Orakel-Ära (Vanilla = Level lid214):

| Messung | Wert |
|---|---|
| Vanilla ohne Blocker, Original-Semantik (DirClassic + Beschneidung) | 230 Züge, 8.710.434 Knoten (bitgenau = refcli) |
| Vanilla ohne Blocker, Go-Default vor keepEqual | 8.608.727 Knoten (~1,2% unter dem Orakel) |
| Vanilla Blocker-Stufen 1-5 | bitgenau = refcli blockerbx 5 (nach dem Rückport des Hinterland-Fixes) |
| Level 201 Blocker-Stufen 1-3 | bitgenau = refcli blockerbx 3 |

Mit dem Orakel-Abschied (Refactoring 18.08.2026) wurden `-dirclassic`, der
DirClassic-Modus und der keepEqual-Schalter entfernt - die Go-Weiterentwicklungen
sind seitdem der einzige Codepfad, Referenz sind die in den Go-Tests verankerten
Anker (u.a. Vanilla ohne Filter: 8.747.345 Knoten). Der letzte Stand mit voller
Orakel-Vergleichbarkeit ist der Commit vor diesem Refactoring. Im selben Zug
entfielen die CLI-Flags `-blocker`/`-rules` (Filter im CLI seitdem immer an,
wie in der TUI - die Opt-in-Asymmetrie existierte nur für die
Orakel-Vergleichbarkeit) und `-rulescompare` samt CompareBlocker-Debugmodus.

Historische Vorgänger-Werte der heutigen Test-Anker:

| Anker (heute) | Vorgänger |
|---|---|
| Vanilla ohne Filter: 8.747.345 | 8.608.727 vor keepEqual (das Behalten kostet ~1,6% Knoten) |
| Vanilla nur Blocker: 1.624.408 | 1.595.042 vor keepEqual; 1.568.540 mit der alten unbedingten Kill-Regel |
| Vanilla Blocker+Regeln: 1.524.476 | 1.494.811 vor keepEqual; 1.488.952 vor der Effizienz-Richtungswahl |
| Vanilla nur Regeln: 1.866.791 (Freeze 1.108.508, Diag 71.007, PullTot 123.209, PullFreeze 783) | 1.828.193 vor keepEqual (Freeze 1.106.391, PullTot 123.160); 1.825.644 vor der Effizienz-Richtungswahl |

## Hashtable-Shootout (05.08.2026)

Unter Realbedingungen (echter Blocker-Workload lid349 bis 4-Steiner, 4 Worker,
je 2 Läufe): CompactTable 6,3 s, builtin map 6,8 s, cockroachdb/swiss 6,9 s,
brentp/intintmap 6,9 s, dolthub/swiss 7,4 s, tidwall/hashmap 7,8 s,
puzpuzpuz/xsync 8,0 s (einzige Concurrent-Map im Limit); alphadose/haxmap und
cornelk/hashmap DNF (>12 s, brechen unter Masseninserts ein). Die
Verlierer-Adapter (Paket tables) wurden danach entfernt - für neue Kandidaten
einfach wieder einen kleinen PosTable-Adapter schreiben und über SetTableFactory
bzw. SetDirectTableFactory unter Realbedingungen messen. Die CompactTable
gewinnt, weil die Crc64-Schlüssel bereits Hashes sind (Identity-Hashing, kein
Re-Hash, keine Metadaten). Anmerkung zu xsync: im Serial-Merge-Design zahlt sie
Atomic-Kosten ohne Nutzen - ihr Potenzial zeigte sich erst im Direct-Write-Modus.

Wichtige Lehre: synthetische Micro-Benchmarks übertreiben die Unterschiede stark
(Hash-Zugriffe sind nur ~20-25% des Workloads) - neue Kandidaten immer unter
Realbedingungen messen.

## Archiv-Format-Shootout (ArchiveTable)

Beim Record-Format der ArchiveTable verloren gepackte 7-Byte-Records (+Unsafe-Load)
und ein cacheline-ausgerichtetes 9er-Zeilen-Layout beide 12-17% Lookup-Tempo gegen
das volle uint64 (Details in der Git-Historie) - das Frei-Byte kauft Alignment,
Bounds-Check-Eliminierung und Einfachheit. Der Mikrobench-Preis der 7-Byte-Variante
ist zugleich die Kosten-Schätzung für den geplanten 6-Byte-Ausbau (roadmap.md).

## Blocker-Cache-Versionen

- **v2**: Ausgangsstand des Go-Ports (gzip-Cache, benannt per FNV über die
  Feldgeometrie).
- **v3 - bedingte Kill-Regel** (08/2026): Fix des Bx-Hinterland-Bugs (siehe
  unten). Die Stufenwerte änderten sich (der Stufenbau filtert sich selbst mit
  der bedingten Regel: größere Hüllen, längere Rückwärtswellen, teils deutlich
  mehr Hinterland-Muster) - dank Rückport ins C# blieben sie unter v3 bitgenau
  vergleichbar mit dem gefixten refcli (verifiziert: vanilla blockerbx 5 und
  lid201 blockerbx 3 exakt gleich).
- **v4**: Regel-Filterung ALLER Stufen im Stufenbau - Ergebnisse identisch, aber
  Stufenbau und Suche messbar langsamer (die kleinen Muster fehlten als billige
  Vorfilter).
- **v5**: Filterung starr ab Stufe 4 - auf zahmen Levels (lid201) unter 5%
  Ersparnis bei Speed-Kosten. Beide Feinanpassungen (v4/v5) auf Max' Vorschlag
  verworfen zugunsten der adaptiven Schwelle.
- **v6 - adaptive Regel-Filterung** (RulesPatternThreshold 4096): erst nach einer
  Muster-Explosion filtern alle weiteren Stufen ihre Vorwärts-Phasen.
- **v7**: in den gefilterten Phasen wirkt auch das Ziel-Matching (Regel-Stufe 2)
  mit.
- **v8**: Schwelle auf 10240 angehoben (mittelgroße Stufen wie die 5.061 Muster
  der 201-Stufe-4 bauen damit wieder klassisch).

Alte Caches werden beim Laden verworfen und neu gerechnet.

## Bx-Hinterland-Bug (Level 29632, 08/2026)

Die Bx-Hinterland-Muster ("rückwärts erreichbar, vorwärts nie gesehen") beweisen
nur, dass eine Stellung nicht durch den Schub einer MUSTER-Kiste entstanden sein
kann - steht der Spieler nach dem Schub einer fremden Kiste in der Muster-Pose,
ist die Stellung trotzdem legal. Die unbedingte Anwendung (so auch im C#-Original
SokowahnBlockerBx) verwarf bei Level 29632 eine Stellung der optimalen
304-Züge-Lösung, der Solver fand nur 306. Der Fix ist die bedingte Kill-Regel
(seit Cache-Version 3, Beweisskizze in architektur.md); Kosten auf Vanilla nur
ca. 1,7% mehr Knoten.

Der Fix wurde ins C# zurückportiert (SokowahnBlockerBx.CheckErlaubt, Cache-Version
107 -> 108, alte Caches werden ignoriert statt Exception). gen4-plain
(SokowahnBlocker), SokowahnBlockerB und gen5 (SokowahnBlockerB2) hatten den Bug
nie: sie registrieren kein Ziel-Hinterland (B2s Blocker sind per Konstruktion
echte Teilspiel-Deadlocks). Der damalige Regressionstest
(blocker/lid29632_debug_test.go, brauchte solution-29632.txt im Repo-Root) wurde
beim Orakel-Abschied entfernt - die bedingte Kill-Regel selbst ist über die
verankerten Stufen- und Knoten-Anker abgesichert.

## Level 361: keepEqual (08/2026)

Die Nach-Fund-Beschneidung des Originals verwarf Stellungen, die die Lösung nicht
mehr verkürzen konnten - darunter genau die, die NUR auf alternativen zugoptimalen
Pfaden liegen. Der Push-Optimierung fehlten dadurch Kanten und Anker an der Naht
der Suchfronten: Level 361 fand 110 statt der 108 Schübe einer bekannten
315-Züge-Lösung. Die Diagnose lief über einen 361-Kanten-Test (Filter als Ursache
ausgeschlossen) und den `-checksol`-Report, der die Bruchstelle exakt bei Schub 29
zeigte: rückwärts als Gleichstand verworfen, vorwärts nie expandiert, 0 Anker auf
dem Pfad. Seitdem speichert und expandiert die Suche auch exakte
Gleichstands-Kandidaten (keepForward/keepBackward); Preis auf Vanilla ~1,6% mehr
Knoten. Seit dem Orakel-Abschied ist das der einzige Codepfad.

## Level 201: Schein-Lösung durch Hash-Kollision (08/2026)

Erster echter Fall des Geburtstagsparadoxons bei den 64-Bit-Schlüsseln: Level 201
meldete eine Schein-Lösung mit 129 Zügen statt 146 - ein kollidierender Schlüssel
in der Gegentabelle gaukelte ein Treffen der Suchfronten vor. Seitdem verifiziert
`verifyMeet` jeden Verbindungs-Kandidaten sofort per Probe-Rekonstruktion
(Mechanik in architektur.md).

## Check-Beschleunigung des Blockers (Chronik)

Naiver Feld-für-Feld-Vergleich -> Muster als Bitmasken (branchloser Subset-Test
`pattern &^ state == 0`) -> Anker-Index (Muster-Buckets nach kleinstem Muster-Feld;
lid46084, 190.708 Muster, Suche bis Tiefe 266: 7,8 -> 2,8 -> 1,0 s) -> Set-Trie
(16.08.2026). Der lineare Anker-Scan brach bei Muster-Explosionen ein: Level 25523
(nach Freeze-Filter 9 Kisten) hat 3,39 Mio 6-Steiner-Muster, der Scan kostete bis
zu ~280.000 Maskenvergleiche pro Schub-Check - Suche bis Tiefe 379 mit allen 6
Stufen 20,1 s gegen 0,64 s mit Stufen 1-4 (Faktor 31 für 12,5% Knotenersparnis).
Mit dem Trie: 1,1 s (Faktor ~18, Knotenzahlen bitgenau gleich; der Vollausbau
kostet nur noch ~70% Aufpreis) - und der Trie ist mit 5,2M Knoten à ~6 B sogar
kleiner als die alten Muster-Bitmasken (3,76M Muster à 16 B). Der frühere
`emptyBoxNumber`-Mechanismus (kistenNummerLeer-Pendant des Originals) entfiel:
der Kisten-Test kennt nur "Kiste ja/nein" und ist damit unabhängig von der
Kistenanzahl des abfragenden Feldes.

## Erledigt-Chronik der Roadmap

- **Kompakte Hashtabelle (CompactTable)**: 10 Byte/Slot verlustfrei, crc==0 als
  Frei-Marker.
- **SegmentTable**: 8 Byte/Slot und trotzdem verlustfrei (Top-16-Bits implizit im
  Segment-Index, Tiefe invertiert in den freien 16 Bit, Slot 0 = frei; Sondieren
  strikt im Segment, Grow parallel je Segment). Per solver.TableFactory schaltbar,
  bitgenau. Messung Level 38044: ~20% weniger Tabellen-RAM, ~10% langsamer ->
  Wahl je nach RAM-Druck.
- **ArchiveTable ("SlowCompactArchiveTable")**: zweistufig nach dem Vorbild von
  SokowahnHash_Index24Multi, gebaut als Bucket-Archiv statt der ursprünglich
  skizzierten Sortierung mit Interpolationssuche (Details in architektur.md).
- **Parallelisierung der Blocker-Phasen** (SearchVariants + MergeGoals,
  Worker-Pool mit Atomic-Chunks, seriell-identische Ergebnisse): lid349/4-Steiner
  13,8 s -> 3,8 s bei 16 Kernen.
- **Solver-Suche parallelisieren** (statische zusammenhängende Satz-Bereiche +
  serieller Merge in Bereichs-Reihenfolge = bitgenau zur seriellen Suche;
  Benchmark-Sweep lid4208 09.08.2026: Faktor 4,4 bei 12 Kernen, daher Default
  NumCPU*4; Bulk-Größe im Fan-out-Design praktisch egal - der alte
  C#-Erfahrungswert "Bulk ~200" gilt nicht mehr).
- **Direct-Write statt seriellem Merge im Blocker** (ShardDirect als sparsamer
  Standard, xsync als schnellere Alternative): lid349/4-Steiner 3,0 s -> 2,2 s
  bei 128 Workern. Gesamt seit Baseline: 13,8 s -> 2,2 s (Faktor 6+).
- **Disk-Auslagerung der Tiefenlisten** (List2-Muster, vereinfacht: 16-MB-Puffer,
  sequenzielle Temp-Datei statt Slot-Recycling, Zufalls-Dateinamen, Aufräumen beim
  Start, Handles nur pro Blockzugriff offen), bitgenau identisches Suchverhalten.
- **RAM-Schwelle vor dem Auslagern** (SpillRamThresholdBytes): unterhalb bleiben
  die Listen im RAM. Die erste Fassung entschied einmalig je Liste - zu statisch,
  einzelne Zugtiefen wuchsen nach ihrer Entscheidung noch um mehrere GB; seitdem
  wird beim ersten Puffer-Überlauf und nach jeweils 16 MB Zuwachs geprüft.
- **Byte-Packung des Disk-Formats** bei WalkCount <= 256: 1 Byte je Wert statt
  uint16, halbes IO-Volumen.
- **Push-Anzahl als Sekundärkriterium**: DP über den Optimal-DAG
  (solver/pushopt.go) statt OrderBy in der Rekonstruktion; seit keepEqual auch
  über alternative Optimalpfade vollständig (Level 361: 108 statt 110 Schübe).
- **Regel-Stufe 1 - Freeze + Diagonale** (soko/rules.go): Frozen-Boxes-Fixpunkt
  nach Festival-Vorbild plus Closed-Diagonal-Port aus JSoko. Vanilla: Faktor 3
  weniger Knoten ohne Blocker. (Der damalige Debug-Vergleichsmodus -rulescompare
  wurde beim Orakel-Abschied entfernt.)
- **Regel-Stufe 2 - Ziel-Matching** (soko/rulesMatch.go, 12.08.2026):
  eingefrorene Ziel-Kisten als Wände, bipartites Matching per Kuhn-Augmentierung,
  Erreichbarkeits-Cache je eingefrorener Menge (JSoko-Idee). Nicht übernommen:
  Distanz-Matching per Auktionsalgorithmus (JSoko BipartiteMatchings.java) -
  Erreichbarkeit statt Distanzen reicht für den reinen Deadlock-Beweis.
  Praxis-Befund (Max, 12.08.2026): Ausbeute hängt daran, WIE FRÜH die
  Vorwärtssuche Ziel-Kisten einfriert - 2164 (Zielkammern neben den Starts) über
  1 Mio Treffer, 5003 (Zielraum im Mittelspiel) wenige, 2135 (Korridor-Packing
  als Endspiel) null (das Endspiel deckt die Rückwärtssuche ab, die per
  Konstruktion deadlock-frei ist); 201 strukturell ungeeignet. Paradebeispiel
  29628 (14 Kisten, dichtes Ziel-Kreuz): trotz 6-Steiner-Blocker feuert
  regelbasiert NUR noch das Matching.
- **Pull-Freeze für die Rückwärtssuche** (CheckPull): Spiegelbild der Stufe 1.
  Vanilla nur Regeln: 2,88 -> 1,83 Mio Knoten, fast Blocker-Niveau ohne jede
  Vorberechnung. (Ein direkter Totfeld-Check vorwärts wurde probiert und bewusst
  wieder entfernt: exakt 1-Steiner-Wissen, der Freeze-Fixpunkt fängt die Fälle
  ohnehin - Regeln ergänzen den Blocker, doppeln ihn nicht.)
- **Regel-Filter im Blocker-Stufenbau, adaptiv** (Cache-Version 6-8, siehe
  Cache-Versions-Chronik oben). Vanilla: Blocker solo 1.595.042 (unverändert,
  damaliger Stand), Blocker+Regeln 1.488.952.
- **Set-Trie für CheckAllowed** (16.08.2026, siehe Check-Beschleunigung oben).

## Praxis-Messungen von Max (12.08.2026)

Suche bis Tiefe ~100 bzw. ~62, gemessen in Hash-Einträgen:

- **Level 25291** (kompakt vollgestopft, Ziel ~472 Züge): Regeln zusätzlich zum
  4-Blocker -2,6% Hash, zum 5-Blocker nur noch -0,1% - dichte kleine Cluster
  deckt der Blocker selbst ab. Der 5-Blocker brauchte beim Rechnen aber 102 Mio
  Hashtable-Einträge für 9.362 neue Muster (alle Stufen davor zusammen: 6.064).
- **Level 47484** (extremes Karo-Schachbrett, Ziel ~446 Züge): Regeln glänzen -
  3-Blocker 285,6 -> 108,0 Mio Hash (-62%!), 4-Blocker 144,1 -> 102,8 Mio. NUR
  Regeln (116,4 Mio) schlagen den nackten 4-Blocker (144,1 Mio), dessen
  Stufenbau 264 Mio Hash-Einträge kostete und teils länger lief als die Suche
  selbst. Bei gleichem Hash-Budget (~285 Mio) kam die Suche mit Regeln bis
  Tiefe 66 statt 62.
- **Level 43070** (sehr groß, endlos viele Kisten, Ziel ~1.864 Züge): der
  3-Blocker war mit 141 Mio Hash gerade noch machbar, der 4-Blocker hätte über
  20.000 Mio gebraucht - hier sind die Regeln die einzige Ausbaustufe: 3-Blocker
  253,8 -> 128,7 Mio Hash (-49%), bei gleichem Budget Tiefe 45 statt 43. Genau
  das Speicherdruck-Szenario, für das die Regeln gebaut wurden.
- **Pull-Freeze**: bei gleicher Suchtiefe 25% Hashtable gespart, ohne messbare
  Verlangsamung - die meisten Treffer liefert der Pull-Totfeld-Check (feuert vor
  dem Pose-Flood und spart den samt aller Pose-Stellungen gleich mit).

Abgeleitete offene Idee (früherer Blocker-Stopp bei aktiven Regeln): roadmap.md.

## Sonstiges

- **Hashtable-Shootout-Ära**: das Paket `tables` mit den Verlierer-Adaptern wurde
  nach dem Shootout entfernt (siehe oben).
- **crc64-Trimm** (18.08.2026): das Paket crc64 war eine übernommene universelle
  Fluent-Hash-Bibliothek (Bytes/Strings/Zeit/Komplexzahlen, Unsafe-Loads,
  Endian-Build-Tags); übrig blieb der eine FNV-1a-Schritt, den die Suche braucht
  (UpdateUInt32).
- **TUI-Aufräumen** (18.08.2026): Taste s (Ministep/Einzelschritt) entfernt
  (Bulkgröße 1 leistet dasselbe), Tasten 4/5/6 (Blocker/Regeln/Matching umschalten)
  samt der Umschalt-API im Solver (solver/filters.go) entfernt - die Filter sind
  seitdem in TUI und CLI fest an. Die atomaren Regel-Trefferzähler (RuleStats:
  Freeze/Diagonale/Matching/Totfeld/PullFreeze) flogen komplett raus, inklusive
  TUI-Anzeige, CLI-Diagnosezeile und Treffer-Ankern in den Tests; der letzte
  gemessene Stand (Vanilla nur Regeln) steht in der Anker-Tabelle oben, die
  Knoten-Anker sichern die Regeln seither allein ab. Die versteckte Taste c
  (Feld kopieren) wanderte in die Hilfezeile.
- **Endlosschleifen-Hänger Level 25327** (08/2026): gefixt in soko.Steps
  (Wand-Prüfung der Schub-Position); der erste Einsatz des -debugport-Workflows,
  die Praxis-Rezepte daraus stehen in architektur.md.
