package web

import (
	"testing"
	"time"
)

// LURD-Lösung per Strg+V: passt sie zum Feld, kommt die Zuglänge zurück
// (miniLevel: Kiste zweimal nach rechts = "RR")
func TestPasteSolution(t *testing.T) {
	s := testServer(t)
	var res struct {
		Kind  string `json:"kind"`
		Moves int    `json:"moves"`
	}
	post(t, s, "/api/paste", `{"text":" R\nR "}`, 200, &res)
	if res.Kind != "solution" || res.Moves != 2 {
		t.Errorf("got %+v, want solution mit 2 Zügen", res)
	}

	post(t, s, "/api/paste", `{"text":"RL"}`, 400, nil)         // spielbar, aber nicht gelöst
	post(t, s, "/api/paste", `{"text":"hallo welt"}`, 400, nil) // gar nichts erkannt
	post(t, s, "/api/paste", `{"text":"  "}`, 400, nil)         // leer
}

// Levelnotation per Strg+V ersetzt das Spielfeld (Hintergrund-Job):
// levelSeq wächst, Titel und Feld sind neu, max moves (bestMoves) ist 0
func TestPasteLevel(t *testing.T) {
	s := testServer(t)
	var start struct {
		Kind string `json:"kind"`
	}
	post(t, s, "/api/paste", `{"text":"\n ######\n #@$ .#\n ######\n"}`, 200, &start)
	if start.Kind != "level" {
		t.Fatalf("kind: got %q, want level", start.Kind)
	}
	// auf das Job-Ende warten (der Lade-Job läuft als Goroutine)
	for deadline := time.Now().Add(5 * time.Second); ; {
		if state, _ := s.progress.snapshot(); !state.Busy && state.Result != "" {
			break
		}
		if time.Now().After(deadline) {
			t.Fatal("load-level-Job wurde nicht fertig")
		}
		time.Sleep(10 * time.Millisecond)
	}
	var sum summaryJSON
	get(t, s, "/api/summary", 200, &sum)
	if sum.LevelSeq != 1 || sum.Title != "Level aus der Zwischenablage" || sum.BestMoves != 0 {
		t.Errorf("summary nach paste: %+v", sum)
	}
	if sum.WalkCount != 4 {
		t.Errorf("walkCount: got %d, want 4", sum.WalkCount)
	}
}
