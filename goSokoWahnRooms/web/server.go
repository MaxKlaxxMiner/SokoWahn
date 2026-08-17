// Paket web: HTTP-Server mit JSON-API für die Debug-GUI (M2, siehe docs/konzept.md).
// Alle Listen-Endpunkte liefern grundsätzlich seitenweise (offset/limit), nie
// Komplettlisten - Zustands-/Variantenlisten großer Räume können Millionen
// Einträge haben. Das Frontend (webui) wird als statisches Bundle eingebettet.
package web

import (
	"embed"
	"io/fs"
	"net/http"
	"sync"

	"goSokoWahnRooms/rooms"
)

//go:embed static
var staticFS embed.FS

// Server hält das aktuelle Netzwerk und beantwortet die API-Anfragen.
// Das Netzwerk ist austauschbar (SetNetwork), damit die GUI später neue
// Levels laden kann, ohne den Server neu zu starten.
type Server struct {
	mu      sync.RWMutex
	network *rooms.Network
	title   string
	mux     *http.ServeMux
}

func New(network *rooms.Network, title string) *Server {
	s := &Server{network: network, title: title}

	s.mux = http.NewServeMux()
	s.mux.HandleFunc("GET /api/summary", s.read(s.handleSummary))
	s.mux.HandleFunc("GET /api/field", s.read(s.handleField))
	s.mux.HandleFunc("GET /api/map", s.read(s.handleMap))
	s.mux.HandleFunc("GET /api/rooms", s.read(s.handleRooms))
	s.mux.HandleFunc("GET /api/rooms/{index}", s.read(s.handleRoom))
	s.mux.HandleFunc("GET /api/rooms/{index}/states", s.read(s.handleStates))
	s.mux.HandleFunc("GET /api/rooms/{index}/variants", s.read(s.handleVariants))
	s.mux.HandleFunc("POST /api/merge", s.write(s.handleMerge))
	s.mux.HandleFunc("POST /api/validate", s.read(s.handleValidate))

	static, err := fs.Sub(staticFS, "static")
	if err != nil {
		panic(err) // kann nur bei kaputtem embed passieren
	}
	s.mux.Handle("GET /", http.FileServerFS(static))

	return s
}

func (s *Server) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	s.mux.ServeHTTP(w, r)
}

// tauscht das Netzwerk aus (für das Level-Laden aus der GUI)
func (s *Server) SetNetwork(network *rooms.Network, title string) {
	s.mu.Lock()
	defer s.mu.Unlock()
	s.network = network
	s.title = title
}

// kapselt einen Handler unter der Lesesperre: das Netzwerk bleibt für die ganze
// Anfrage stabil, während mutierende Aktionen (Merge & Co.) exklusiv laufen
func (s *Server) read(h http.HandlerFunc) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		s.mu.RLock()
		defer s.mu.RUnlock()
		h(w, r)
	}
}

// kapselt einen mutierenden Handler unter der Schreibsperre
func (s *Server) write(h http.HandlerFunc) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		s.mu.Lock()
		defer s.mu.Unlock()
		h(w, r)
	}
}

// liefert Netzwerk und Titel; die Aufrufer laufen bereits unter der
// read-/write-Sperre der Anfrage
func (s *Server) snapshot() (*rooms.Network, string) {
	return s.network, s.title
}
