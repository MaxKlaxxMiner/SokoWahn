package soko

// erstellt eine Kopie des Feldes mit reduzierter Kistenanzahl (für die Blocker-Teilspiele);
// die Kisten sind danach ungesetzt und müssen erst per SetBoxes/SetState platziert werden
func (f *Field) CloneWithBoxCount(count int) *Field {
	result := f.Clone()

	// alle Kisten-Verweise auf den neuen Leer-Marker (= neue Kistenanzahl) setzen
	for i := range result.wposToBoxes {
		result.wposToBoxes[i] = uint32(count)
	}
	for i := range result.boxBits {
		result.boxBits[i] = 0
	}

	result.boxes = result.boxes[:count]
	result.boxCount = uint32(count)

	return result
}
