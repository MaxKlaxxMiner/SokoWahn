package rooms

import (
	"sort"
	"testing"
)

// Tests der Graph-Sicht (usagegraph.go) an der 202er-Kammer:
//  1. Kreuzvalidierung: der Automat reproduziert die Labor-Enumeration exakt
//  2. Loop-Analyse: die Zyklen der Kammer sichtbar machen
//  3. der Beweis: die Minimal-Menge ist für ALLE Nutzungslängen kostengleich
//  4. Negativprobe: eine zu kleine Menge wird mit Gegenbeispiel abgelehnt

// sichtbare Ereignisse einer Signatur (ohne das Terminal "!")
func visibleLen(sig string) int {
	n := 0
	for _, c := range sig {
		if c == 'E' || c == 'X' {
			n++
		}
	}
	return n
}

// vergleicht die Automaten-Signaturen mit enumerateUsages. Verglichen wird
// bis sichtbare Länge labHorizon-1: exakt am Horizont beschneidet das Labor
// auch B-Besuche und End-Varianten (der Automat nicht), darunter sind beide
// Semantiken identisch.
func crossCheck(t *testing.T, room *Room, allowed func(id uint64) bool, name string) {
	t.Helper()
	lab := map[string]*labSigInfo{}
	for sig, info := range enumerateUsages(room, labHorizon, allowed) {
		if visibleLen(sig) < labHorizon {
			lab[sig] = info
		}
	}
	graph := map[string]usageCost{}
	for sig, cost := range buildUsageGraph(room, allowed).signatures(labHorizon) {
		if visibleLen(sig) < labHorizon {
			graph[sig] = cost
		}
	}

	for sig, info := range lab {
		cost, exists := graph[sig]
		if !exists {
			t.Errorf("%s: signatur %s fehlt im Graphen", name, sig)
			continue
		}
		if cost.moves != int64(info.moves) || cost.pushes != int64(info.pushes) {
			t.Errorf("%s: signatur %s: Graph %s statt %d/%d", name, sig, cost, info.moves, info.pushes)
		}
	}
	for sig := range graph {
		if _, exists := lab[sig]; !exists {
			t.Errorf("%s: signatur %s nur im Graphen", name, sig)
		}
	}
	if len(lab) != len(graph) {
		t.Errorf("%s: %d Labor-Signaturen vs %d Graph-Signaturen", name, len(lab), len(graph))
	}
}

func TestUsageGraphCrossCheck202(t *testing.T) {
	_, room := merge202Chamber(t)
	crossCheck(t, room, nil, "voll")
	crossCheck(t, room, func(id uint64) bool { return lab202MinimalSet[id] }, "minimal")
}

// macht die Loop-Struktur der Kammer sichtbar (Max' Präfix + N x Loop +
// Abschluss): jeder Zyklus trägt ein sichtbares Außenwort und Kosten
func TestUsageGraphLoops202(t *testing.T) {
	_, room := merge202Chamber(t)

	for _, tc := range []struct {
		name    string
		allowed func(id uint64) bool
	}{
		{"voll", nil},
		{"minimal", func(id uint64) bool { return lab202MinimalSet[id] }},
	} {
		g := buildUsageGraph(room, tc.allowed)
		loops := g.enumerateLoops(1000)
		t.Logf("%s: %d Knoten, %d Zyklen", tc.name, len(g.nodes), len(loops))

		sort.Slice(loops, func(i, j int) bool {
			if loops[i].word != loops[j].word {
				return loops[i].word < loops[j].word
			}
			return loops[i].cost.less(loops[j].cost)
		})
		for _, loop := range loops {
			names := ""
			for i, node := range loop.nodes {
				if i > 0 {
					names += " -> "
				}
				names += g.nodeName(node)
			}
			t.Logf("  loop wort=%-4s kosten=%-5s vars=%v (%s)", loop.word, loop.cost, loop.variants, names)
		}

		// Grundinvarianten: Zyklen existieren (Garagen-Loops), und ALLE
		// tragen dieselbe Garagen-Rate 9 moves / 5 pushes je Einschub+Export-
		// Paar (Max' Loop-These: "alle Garagen-Loops kosten 9") - im vollen
		// wie im minimalen Graphen
		if len(loops) == 0 {
			t.Errorf("%s: keine Zyklen gefunden (Garagen-Loop fehlt)", tc.name)
		}
		for _, loop := range loops {
			perLoop := int64(len(loop.word) / 2) // Zyklen bestehen aus E+X-Paaren
			if loop.cost.moves != 9*perLoop || loop.cost.pushes != 5*perLoop {
				t.Errorf("%s: Zyklus %s kostet %s statt Garagen-Rate 9/5 je E+X", tc.name, loop.word, loop.cost)
			}
		}
	}
}

// DER Beweis (löst den Labor-Horizont ab): die 7 Varianten der Minimal-Menge
// bedienen JEDES Außenwort - egal wie lang - zu denselben Optimal-Kosten wie
// alle 25 Varianten. Die Vergleichs-Suche sättigt (endlich viele normalisierte
// Situationen), damit gilt die Gleichheit per Induktion für alle Längen.
func TestUsageGraphMinimalProven(t *testing.T) {
	_, room := merge202Chamber(t)

	full := buildUsageGraph(room, nil)
	reduced := buildUsageGraph(room, func(id uint64) bool { return lab202MinimalSet[id] })

	verdict, detail := compareUsageGraphs(full, reduced, 100000)
	t.Logf("voll (%d Knoten) vs minimal (%d Knoten): %v - %s",
		len(full.nodes), len(reduced.nodes), verdict, detail)
	if verdict != usageEqual {
		t.Errorf("Minimal-These nicht bewiesen: %v - %s", verdict, detail)
	}
}

// Negativprobe: ohne den Garagen-Loop (v20) bzw. ohne den Trick-Abschluss
// (v19) muss der Vergleich eine konkrete Differenz mit Gegenbeispiel finden
func TestUsageGraphDetectsMissing(t *testing.T) {
	_, room := merge202Chamber(t)
	full := buildUsageGraph(room, nil)

	for _, drop := range []uint64{19, 20} {
		reduced := buildUsageGraph(room, func(id uint64) bool {
			return lab202MinimalSet[id] && id != drop
		})
		verdict, detail := compareUsageGraphs(full, reduced, 100000)
		t.Logf("ohne v%d: %v - %s", drop, verdict, detail)
		if verdict != usageDiffers {
			t.Errorf("ohne v%d: Differenz nicht erkannt (%v - %s)", drop, verdict, detail)
		}
	}
}
