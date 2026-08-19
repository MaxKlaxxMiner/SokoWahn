package web

import (
	"encoding/json"
	"fmt"
	"net/http"
	"sync"
	"time"

	"goSokoWahnRooms/rooms"
)

// Fortschritts-Anzeige langer Rechnungen (Merge/Optimize), wie im alten
// C#-FormDebugger: die Arbeit läuft in einer Hintergrund-Goroutine unter der
// Schreibsperre des Servers; dieser Zustand hat einen EIGENEN Mutex, damit
// der SSE-Stream und der Stop-Knopf nicht hinter der Schreibsperre warten.
// Die GUI bekommt den Status-Text, die Felder der gerade bearbeiteten Räume
// (gelbe Markierung) und am Ende die Abschluss-Meldung.
type progressState struct {
	mu     sync.Mutex
	seq    uint64 // wächst bei jeder Änderung
	busy   bool
	stop   bool     // Stop angefordert (der nächste Callback bricht ab)
	text   string   // aktueller Arbeitsschritt
	fields []uint32 // Wpos der gerade bearbeiteten Räume
	result string   // Abschluss-Meldung des letzten Laufs
	errMsg string   // Fehler des letzten Laufs ("" = ok)
}

type progressJSON struct {
	Seq    uint64   `json:"seq"` // Änderungs-Zähler (das Frontend dedupliziert darüber)
	Busy   bool     `json:"busy"`
	Text   string   `json:"text"`
	Fields []uint32 `json:"fields"`
	Result string   `json:"result"`
	Error  string   `json:"error"`
}

func (p *progressState) snapshot() (progressJSON, uint64) {
	p.mu.Lock()
	defer p.mu.Unlock()
	fields := p.fields
	if fields == nil {
		fields = []uint32{}
	}
	return progressJSON{Seq: p.seq, Busy: p.busy, Text: p.text, Fields: fields, Result: p.result, Error: p.errMsg}, p.seq
}

// startet einen Lauf; false = es läuft schon einer
func (p *progressState) begin(text string) bool {
	p.mu.Lock()
	defer p.mu.Unlock()
	if p.busy {
		return false
	}
	p.busy, p.stop, p.text, p.fields, p.result, p.errMsg = true, false, text, nil, "", ""
	p.seq++
	return true
}

func (p *progressState) finish(result string, err error) {
	p.mu.Lock()
	defer p.mu.Unlock()
	p.busy, p.stop, p.text, p.fields, p.result = false, false, "", nil, result
	p.errMsg = ""
	if err != nil {
		p.errMsg = err.Error()
	}
	p.seq++
}

func (p *progressState) requestStop() bool {
	p.mu.Lock()
	defer p.mu.Unlock()
	if !p.busy {
		return false
	}
	p.stop = true
	p.seq++
	return true
}

// der ProgressFunc-Callback für die rooms-Schicht: Status übernehmen,
// Stop-Wunsch zurückmelden
func (p *progressState) report(text string, active []*rooms.Room) bool {
	var fields []uint32
	for _, room := range active {
		for _, f := range room.Fields {
			fields = append(fields, uint32(f))
		}
	}
	p.mu.Lock()
	defer p.mu.Unlock()
	p.text, p.fields = text, fields
	p.seq++
	return !p.stop
}

// führt eine lange Rechnung im Hintergrund unter der Schreibsperre aus;
// job liefert die Abschluss-Meldung
func (s *Server) runJob(title string, job func(info rooms.ProgressFunc) (string, error)) bool {
	if !s.progress.begin(title) {
		return false
	}
	go func() {
		s.mu.Lock()
		defer s.mu.Unlock()
		result, err := job(s.progress.report)
		s.progress.finish(result, err)
	}()
	return true
}

// SSE-Stream des Fortschritts: sendet den Zustand sofort und dann bei jeder
// Änderung (Abtastung 100 ms). Läuft bewusst OHNE die Netzwerk-Sperren.
func (s *Server) handleProgress(w http.ResponseWriter, r *http.Request) {
	flusher, ok := w.(http.Flusher)
	if !ok {
		writeError(w, http.StatusInternalServerError, "streaming nicht unterstützt")
		return
	}
	w.Header().Set("Content-Type", "text/event-stream")
	w.Header().Set("Cache-Control", "no-cache")

	send := func() bool {
		state, _ := s.progress.snapshot()
		data, _ := json.Marshal(state)
		if _, err := fmt.Fprintf(w, "data: %s\n\n", data); err != nil {
			return false
		}
		flusher.Flush()
		return true
	}
	if !send() {
		return
	}
	_, lastSeq := s.progress.snapshot()

	ticker := time.NewTicker(100 * time.Millisecond)
	defer ticker.Stop()
	for {
		select {
		case <-r.Context().Done():
			return
		case <-ticker.C:
			if _, seq := s.progress.snapshot(); seq != lastSeq {
				lastSeq = seq
				if !send() {
					return
				}
			}
		}
	}
}

// Stop-Knopf: bricht die laufende Rechnung ab (die Dominanzsuche wendet
// bereits bewiesene Streichungen trotzdem an, siehe Konzept M4b)
func (s *Server) handleStop(w http.ResponseWriter, r *http.Request) {
	if !s.progress.requestStop() {
		writeError(w, http.StatusConflict, "es läuft keine Rechnung")
		return
	}
	writeJSON(w, map[string]any{"stopping": true})
}
