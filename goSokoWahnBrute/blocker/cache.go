package blocker

import (
	"compress/gzip"
	"encoding/binary"
	"fmt"
	"os"

	"goSokoWahnBrute/soko"
)

// Version des Cache-Dateiformats (3: Stufenbau mit bedingter Kill-Regel, siehe CheckAllowed;
// das Format selbst ist unverändert, aber v2-Caches wurden unter der fehlerhaften
// unbedingten Bx-Semantik gebaut und werden deshalb verworfen und neu gerechnet)
const cacheVersion = uint32(3)

// liefert den Standard-Dateinamen der Cache-Datei eines Spielfeldes
// (gleiche Idee wie im C#-Original: Hash über die Feldgeometrie)
func CacheName(field *soko.Field) string {
	return fmt.Sprintf("blocker_x%016x.gz", field.FieldCrc())
}

// speichert alle fertigen Stufen in die Cache-Datei
func (b *Blocker) saveCache() error {
	file, err := os.Create(b.cachePath)
	if err != nil {
		return err
	}
	defer file.Close()

	zip := gzip.NewWriter(file)
	defer zip.Close()

	write := func(values ...any) error {
		for _, v := range values {
			if err := binary.Write(zip, binary.LittleEndian, v); err != nil {
				return err
			}
		}
		return nil
	}

	if err := write(cacheVersion, b.base.FieldCrc(), uint32(b.walkCount), uint32(b.maxBoxes), uint32(len(b.stages))); err != nil {
		return err
	}

	for i := range b.stages {
		st := &b.stages[i]
		if err := write(uint32(st.boxCount), st.checkedStates); err != nil {
			return err
		}
		for pos := 0; pos < b.walkCount; pos++ {
			pat := st.patterns[pos]
			if err := write(uint32(len(pat))); err != nil {
				return err
			}
			for _, wpos := range pat {
				if err := write(uint16(wpos)); err != nil {
					return err
				}
			}
		}
	}

	return nil
}

// lädt alle fertigen Stufen aus der Cache-Datei (bei jedem Fehler bleibt der Blocker leer)
func (b *Blocker) loadCache() error {
	file, err := os.Open(b.cachePath)
	if err != nil {
		return err
	}
	defer file.Close()

	zip, err := gzip.NewReader(file)
	if err != nil {
		return err
	}
	defer zip.Close()

	read := func(values ...any) error {
		for _, v := range values {
			if err := binary.Read(zip, binary.LittleEndian, v); err != nil {
				return err
			}
		}
		return nil
	}

	var version, walkCount, maxBoxes, stageCount uint32
	var fieldCrc uint64
	if err := read(&version, &fieldCrc, &walkCount, &maxBoxes, &stageCount); err != nil {
		return err
	}
	if version != cacheVersion || fieldCrc != b.base.FieldCrc() ||
		int(walkCount) != b.walkCount || int(maxBoxes) != b.maxBoxes {
		return fmt.Errorf("cache file does not match the field: %s", b.cachePath)
	}

	stages := make([]stage, 0, stageCount)
	for s := uint32(0); s < stageCount; s++ {
		var boxCount uint32
		var checkedStates int64
		if err := read(&boxCount, &checkedStates); err != nil {
			return err
		}

		st := stage{
			boxCount:      int(boxCount),
			patterns:      make([][]soko.Wpos, b.walkCount),
			checkedStates: checkedStates,
		}
		for pos := 0; pos < b.walkCount; pos++ {
			var count uint32
			if err := read(&count); err != nil {
				return err
			}
			if count == 0 {
				continue
			}
			pat := make([]soko.Wpos, count)
			for i := range pat {
				var value uint16
				if err := read(&value); err != nil {
					return err
				}
				pat[i] = soko.Wpos(value)
			}
			st.patterns[pos] = pat
		}
		stages = append(stages, st)
	}

	// erst nach komplett fehlerfreiem Einlesen übernehmen
	b.stages = stages
	if len(stages) > 0 {
		b.searchBoxCount = stages[len(stages)-1].boxCount // Wiederaufnahme bei der nächsten Stufe
	}

	return nil
}
