package soko

// Ergänzungen für das rooms-Paket: Nachbar-Navigation und Feld-Abfragen
// über die kompakten Wpos-Indizes.

// Richtungszeichen wie im LURD-Format
const (
	DirLeft  = byte('l')
	DirRight = byte('r')
	DirUp    = byte('u')
	DirDown  = byte('d')
)

// alle vier Richtungen in fester Reihenfolge (links, rechts, oben, unten -
// gleiche Reihenfolge wie die Portal-Erstellung im C#-Original)
var Dirs = [4]byte{DirLeft, DirRight, DirUp, DirDown}

// gibt die Gegenrichtung zurück
func OppositeDir(dir byte) byte {
	switch dir {
	case DirLeft:
		return DirRight
	case DirRight:
		return DirLeft
	case DirUp:
		return DirDown
	case DirDown:
		return DirUp
	}
	panic("invalid dir")
}

// gibt den Sentinel-Wert für "nicht begehbar" zurück (Wand bzw. Void)
func (f *Field) WalkEof() Wpos {
	return f.walkEof
}

// gibt die Startposition des Spielers zurück
func (f *Field) InitPlayer() Wpos {
	return f.initPlayer
}

// gibt die Breite des gesamten Spielfeldes zurück
func (f *Field) Width() int {
	return f.width
}

// gibt die Höhe des gesamten Spielfeldes zurück
func (f *Field) Height() int {
	return f.height
}

// gibt die absolute Feldposition (x + y*Breite) eines begehbaren Feldes zurück
func (f *Field) FieldPos(p Wpos) int {
	return f.wposToField[p]
}

// gibt das Nachbarfeld in der angegebenen Richtung zurück (walkEof = Wand/Void)
func (f *Field) Neighbor(p Wpos, dir byte) Wpos {
	switch dir {
	case DirLeft:
		return f.walkLeft[p]
	case DirRight:
		return f.walkRight[p]
	case DirUp:
		return f.walkUp[p]
	case DirDown:
		return f.walkDown[p]
	}
	panic("invalid dir")
}

// gibt an, ob das Feld ein Zielfeld ist
func (f *Field) IsGoal(p Wpos) bool {
	for _, g := range f.goals {
		if g == p {
			return true
		}
	}
	return false
}

// gibt an, ob das Feld eine Ecke ist (Kiste dort nie mehr bewegbar);
// nicht begehbare Felder zählen wie im C#-Original ebenfalls als Ecke
func (f *Field) IsCorner(p Wpos) bool {
	if p >= f.walkEof {
		return true
	}
	horizWall := f.walkLeft[p] == f.walkEof || f.walkRight[p] == f.walkEof
	vertWall := f.walkUp[p] == f.walkEof || f.walkDown[p] == f.walkEof
	return horizWall && vertWall
}
