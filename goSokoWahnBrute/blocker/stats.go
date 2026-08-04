package blocker

import (
	"fmt"
	"strings"

	"goSokoWahnBrute/tools"
)

// Kennzahlen einer fertigen Stufe
type StageStats struct {
	BoxCount      int   // Kistenanzahl der Stufe
	PatternCount  int   // Anzahl der gefundenen Deadlock-Muster
	CheckedStates int64 // Anzahl der geprüften Stellungen
}

// Momentaufnahme des Blocker-Fortschritts
type Stats struct {
	Status          Status
	CurrentBoxCount int          // Kistenanzahl der laufenden Stufe (0 = noch keine gestartet)
	MaxBoxes        int          // Kistenanzahl des Levels (Erstellung endet bei MaxBoxes-1)
	Stages          []StageStats // fertig berechnete Stufen
	KnownStates     int64        // Stellungs-Marker der laufenden Stufe
	OpenStates      int64        // noch abzuarbeitende Stellungen der laufenden Phase
	BadStates       int64        // gesammelte möglicherweise verbotene Stellungen
	EstimateNext    int64        // geschätzter Aufwand der nächsten Stufe (0 = unbekannt)
	Done            bool
}

func (b *Blocker) GetStats() Stats {
	stats := Stats{
		Status:          b.status,
		CurrentBoxCount: b.searchBoxCount,
		MaxBoxes:        b.maxBoxes,
		Stages:          make([]StageStats, len(b.stages)),
		EstimateNext:    b.estimateNext(),
		Done:            b.status == StatusDone,
	}

	for i := range b.stages {
		st := &b.stages[i]
		patternCount := 0
		for _, pat := range st.patterns {
			patternCount += len(pat) / st.boxCount
		}
		stats.Stages[i] = StageStats{
			BoxCount:      st.boxCount,
			PatternCount:  patternCount,
			CheckedStates: st.checkedStates,
		}
	}

	if b.known != nil {
		stats.KnownStates = b.known.Len()
	}
	if b.checkList != nil {
		stats.OpenStates += int64(b.checkList.Count())
	}
	if b.collectList != nil {
		stats.OpenStates += int64(b.collectList.Count())
	}
	if b.goodList != nil {
		stats.OpenStates += int64(b.goodList.Count())
	}
	if b.badList != nil {
		stats.BadStates = int64(b.badList.Count())
	}

	return stats
}

// schätzt den Aufwand (Anzahl der Stellungen) für die nächste Stufe
// anhand des Wachstums der letzten beiden Stufen
func (b *Blocker) estimateNext() int64 {
	if len(b.stages) < 2 {
		return 0
	}
	last := b.stages[len(b.stages)-1].checkedStates
	prev := b.stages[len(b.stages)-2].checkedStates
	if last == 0 || prev == 0 || prev > last {
		return 0
	}
	return int64(float64(last) / float64(prev) * float64(last))
}

func (s Status) String() string {
	switch s {
	case StatusInit:
		return "Init"
	case StatusCollectStart:
		return "Starter sammeln"
	case StatusCollectGoals:
		return "Ziele sammeln"
	case StatusSearchVariants:
		return "Suche"
	case StatusMergeGoals:
		return "Verschmelzen"
	case StatusCreatePatterns:
		return "Muster erstellen"
	case StatusDone:
		return "Fertig"
	default:
		return fmt.Sprintf("Status(%d)", int(s))
	}
}

// lesbare Fortschritts-Übersicht (eine Zeile je fertige Stufe plus Statuszeile)
func (b *Blocker) String() string {
	stats := b.GetStats()
	var sb strings.Builder

	for _, st := range stats.Stages {
		fmt.Fprintf(&sb, "[%d] - %s Muster - %s geprüft\n", st.BoxCount, tools.FormatInt(st.PatternCount), tools.FormatInt(st.CheckedStates))
	}

	if !stats.Done {
		fmt.Fprintf(&sb, "[%d] - %s: %s offen / %s bekannt", stats.CurrentBoxCount, stats.Status, tools.FormatInt(stats.OpenStates), tools.FormatInt(stats.KnownStates))
		if stats.EstimateNext > 0 {
			fmt.Fprintf(&sb, " (nächste Stufe ca. %s)", tools.FormatInt(stats.EstimateNext))
		}
		sb.WriteByte('\n')
	}

	return sb.String()
}
