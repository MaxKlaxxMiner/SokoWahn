package web

import (
	"testing"
	"time"
)

// wartet auf das Ende des laufenden Hintergrund-Jobs und liefert Ergebnis/Fehler
func waitJob(t *testing.T, s *Server) (result, errMsg string) {
	t.Helper()
	for deadline := time.Now().Add(10 * time.Second); ; {
		if state, _ := s.progress.snapshot(); !state.Busy && (state.Result != "" || state.Error != "") {
			return state.Result, state.Error
		}
		if time.Now().After(deadline) {
			t.Fatal("Job wurde nicht fertig")
		}
		time.Sleep(10 * time.Millisecond)
	}
}

// Solver-Sitzung über die API: starten (pausiert), Auto einschalten, auf das
// Ergebnis warten; die Lösung des Mini-Levels (Kiste 2x nach rechts) muss
// gemerkt und max moves (bestMoves) gesetzt sein
func TestSolveSession(t *testing.T) {
	s := testServer(t)
	post(t, s, "/api/solve", `{"maxMoves":0}`, 200, nil)
	post(t, s, "/api/solve/cmd", `{"auto":true}`, 200, &struct{}{})
	result, errMsg := waitJob(t, s)
	if errMsg != "" {
		t.Fatal("solve:", errMsg)
	}
	if result == "" {
		t.Fatal("kein Ergebnis")
	}

	var res struct {
		Solution *solutionJSON `json:"solution"`
	}
	get(t, s, "/api/solution", 200, &res)
	if res.Solution == nil {
		t.Fatal("keine Lösung gemerkt")
	}
	if res.Solution.Moves != 2 || res.Solution.Path != "rr" || !res.Solution.Complete {
		t.Errorf("lösung: %+v, want 2 züge (rr), complete", res.Solution)
	}
	var sum summaryJSON
	get(t, s, "/api/summary", 200, &sum)
	if sum.BestMoves != 2 {
		t.Errorf("bestMoves: got %d, want 2", sum.BestMoves)
	}

	// Kommandos ohne Sitzung werden abgewiesen
	post(t, s, "/api/solve/cmd", `{"bulk":10}`, 409, nil)
}

// Bulk-Schritte statt Auto: die Sitzung rechnet nur auf Zuruf; ein großzügiger
// Bulk löst das Mini-Level komplett, danach endet die Sitzung von selbst
func TestSolveSessionBulk(t *testing.T) {
	s := testServer(t)
	post(t, s, "/api/solve", `{"maxMoves":0}`, 200, nil)
	post(t, s, "/api/solve/cmd", `{"bulk":100000}`, 200, nil)
	if result, errMsg := waitJob(t, s); errMsg != "" || result == "" {
		t.Fatalf("solve per bulk: result=%q err=%q", result, errMsg)
	}

	// Stop beendet auch eine pausierte Sitzung
	post(t, s, "/api/solve", `{"maxMoves":0}`, 200, nil)
	post(t, s, "/api/stop", `{}`, 200, nil)
	if result, errMsg := waitJob(t, s); errMsg != "" || result == "" {
		t.Fatalf("solve-stop: result=%q err=%q", result, errMsg)
	}
}
