package rooms

import "math"

// Sentinel: Spieler verlässt den Raum nicht (Zielstellung erreicht, Spieler bleibt drin)
const NoPortal = uint32(math.MaxUint32)

// Daten einer Variante: "Spieler betritt den Raum über ein Portal (bzw. startet darin)
// bei Zustand OldState" -> was dabei passieren kann.
type VariantData struct {
	OldState uint64 // Raum-Zustand vor der Variante
	NewState uint64 // Raum-Zustand nach der Variante

	// Bewegungen innerhalb des Raumes plus der Schritt beim Verlassen; der
	// Eintritts-Schritt wird von der Vorgänger-Variante gezählt (deren Austritt)
	Moves  uint32
	Pushes uint32 // Kistenverschiebungen innerhalb bzw. beim Verlassen des Raumes

	BoxPortals   []uint32 // ausgehende Portale, wohin Kisten rausgeschoben wurden
	PlayerPortal uint32   // ausgehendes Portal des Spielers (NoPortal = bleibt drin = Spielende)

	Path Path // Laufweg 2-Bit-gepackt (siehe path.go), ohne den Eintritts-Schritt
}

// Liste aller Varianten eines Raumes. Startvarianten (Spieler startet im Raum)
// belegen immer die IDs 0..StartVariantCount-1, danach folgen die Portal-Varianten
// je (Portal, Zustand) lückenlos aufsteigend (ermöglicht Span-Verzeichnisse).
type VariantList struct {
	data []VariantData
}

func NewVariantList() *VariantList {
	return &VariantList{}
}

// gibt die Anzahl der gespeicherten Varianten zurück
func (v *VariantList) Count() uint64 {
	return uint64(len(v.data))
}

// fügt eine weitere Variante hinzu und gibt deren ID zurück
func (v *VariantList) Add(d VariantData) uint64 {
	id := uint64(len(v.data))
	v.data = append(v.data, d)
	return id
}

// gibt die Daten einer Variante zurück (nur lesen!)
func (v *VariantList) Get(id uint64) *VariantData {
	return &v.data[id]
}
