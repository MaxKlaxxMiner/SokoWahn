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
	s.mux.HandleFunc("GET /api/summary", s.handleSummary)
	s.mux.HandleFunc("GET /api/field", s.handleField)
	s.mux.HandleFunc("GET /api/map", s.handleMap)
	s.mux.HandleFunc("GET /api/rooms", s.handleRooms)
	s.mux.HandleFunc("GET /api/rooms/{index}", s.handleRoom)
	s.mux.HandleFunc("GET /api/rooms/{index}/states", s.handleStates)
	s.mux.HandleFunc("GET /api/rooms/{index}/variants", s.handleVariants)

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

// liefert Netzwerk und Titel unter Lesesperre (Handler arbeiten mit dem Schnappschuss
// weiter - das Netzwerk selbst wird nur per SetNetwork komplett ersetzt, nie mutiert;
// sobald M3 mutierende Aktionen bringt, wandert die Sperre um die ganze Anfrage)
func (s *Server) snapshot() (*rooms.Network, string) {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return s.network, s.title
}
