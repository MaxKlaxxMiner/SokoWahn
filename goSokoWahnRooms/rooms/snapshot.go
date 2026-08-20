package rooms

import (
	"bufio"
	"encoding/binary"
	"fmt"
	"io"
	"slices"

	"goSokoWahnRooms/soko"
)

// Snapshot: kompletter Netzwerk-Stand (alle Räume samt Zuständen, Varianten
// und Portal-Verzeichnissen) als Binärdatei, little endian, ungezippt.
// Der Header trägt den FieldCrc des Levels (Bindung wie die Blocker-Caches
// in brute) und den Effort-String, damit die Snapshot-Liste der GUI ohne
// Voll-Laden beschriftet werden kann. Raum-Verweise der Portale (FromRoom,
// Opposite, Outgoing) stehen nicht in der Datei - sie entstehen beim Laden
// deterministisch neu über die Feld->Raum-Zuordnung und die Feld-Paare.

const snapshotMagic = "ROOMSNP1"
const snapshotVersion = uint32(1) // Zugfolgen 2-Bit-gepackt (Path, siehe path.go)

// ---------- gepufferte Schreib-/Lese-Hilfen (sammeln den ersten Fehler) ----------

type snapWriter struct {
	w   *bufio.Writer
	buf [8]byte
	err error
}

func (s *snapWriter) u8(v byte) {
	if s.err == nil {
		s.err = s.w.WriteByte(v)
	}
}

func (s *snapWriter) u32(v uint32) {
	if s.err == nil {
		binary.LittleEndian.PutUint32(s.buf[:4], v)
		_, s.err = s.w.Write(s.buf[:4])
	}
}

func (s *snapWriter) u64(v uint64) {
	if s.err == nil {
		binary.LittleEndian.PutUint64(s.buf[:8], v)
		_, s.err = s.w.Write(s.buf[:8])
	}
}

func (s *snapWriter) str(v string) {
	s.u32(uint32(len(v)))
	if s.err == nil {
		_, s.err = s.w.WriteString(v)
	}
}

func (s *snapWriter) wposList(list []soko.Wpos) {
	s.u32(uint32(len(list)))
	for _, p := range list {
		s.u32(uint32(p))
	}
}

type snapReader struct {
	r   *bufio.Reader
	buf [8]byte
	err error
}

func (s *snapReader) u8() byte {
	if s.err != nil {
		return 0
	}
	var v byte
	v, s.err = s.r.ReadByte()
	return v
}

func (s *snapReader) u32() uint32 {
	if s.err != nil {
		return 0
	}
	if _, s.err = io.ReadFull(s.r, s.buf[:4]); s.err != nil {
		return 0
	}
	return binary.LittleEndian.Uint32(s.buf[:4])
}

func (s *snapReader) u64() uint64 {
	if s.err != nil {
		return 0
	}
	if _, s.err = io.ReadFull(s.r, s.buf[:8]); s.err != nil {
		return 0
	}
	return binary.LittleEndian.Uint64(s.buf[:8])
}

func (s *snapReader) str() string {
	length := s.u32()
	if s.err != nil {
		return ""
	}
	raw := make([]byte, length)
	if _, s.err = io.ReadFull(s.r, raw); s.err != nil {
		return ""
	}
	return string(raw)
}

// liest eine Wpos-Liste und prüft jede Position gegen die Feldgrenze
func (s *snapReader) wposList(eof soko.Wpos) []soko.Wpos {
	count := s.u32()
	if s.err != nil {
		return nil
	}
	if count == 0 {
		return nil // wie beim Aufbau: leere Listen bleiben nil
	}
	list := make([]soko.Wpos, count)
	for i := range list {
		p := soko.Wpos(s.u32())
		if s.err == nil && p >= eof {
			s.err = fmt.Errorf("snapshot: wpos %d ausserhalb des Feldes", p)
		}
		list[i] = p
	}
	return list
}

// ---------- Speichern ----------

// WriteSnapshot schreibt den kompletten Netzwerk-Stand. info (optional)
// bekommt zeitgedrosselte Fortschritts-Meldungen; false bricht ab (der
// Aufrufer verwirft die halbe Datei).
func (n *Network) WriteSnapshot(w io.Writer, info ProgressFunc) error {
	sw := &snapWriter{w: bufio.NewWriterSize(w, 1<<20)}
	if _, err := sw.w.WriteString(snapshotMagic); err != nil {
		return err
	}
	sw.u32(snapshotVersion)
	sw.u64(n.Field.FieldCrc())
	sw.str(n.EffortString())
	sw.u32(uint32(len(n.Rooms)))

	var throttle progressThrottle
	for i, room := range n.Rooms {
		if info != nil && throttle.due() &&
			!info(fmt.Sprintf("save: raum %d/%d", i+1, len(n.Rooms)), []*Room{room}) {
			return fmt.Errorf("save: abgebrochen")
		}
		sw.wposList(room.Fields)
		sw.wposList(room.Goals)
		sw.wposList(room.StartBoxes)
		sw.u32(room.MaxBoxes)
		sw.u64(room.StartState)
		sw.u64(room.StartVariantCount)

		// Zustände: Offsets + flacher Kisten-Puffer (Rohformat der StateList)
		sw.u64(uint64(len(room.States.offs)))
		for _, o := range room.States.offs {
			sw.u32(o)
		}
		sw.u64(uint64(len(room.States.buf)))
		for _, p := range room.States.buf {
			sw.u32(uint32(p))
		}

		// Varianten
		sw.u64(room.Variants.Count())
		for id := range room.Variants.data {
			v := &room.Variants.data[id]
			sw.u64(v.OldState)
			sw.u64(v.NewState)
			sw.u32(v.Moves)
			sw.u32(v.Pushes)
			sw.u32(uint32(len(v.BoxPortals)))
			for _, b := range v.BoxPortals {
				sw.u32(b)
			}
			sw.u32(v.PlayerPortal)
			// Zugfolge materialisiert (Zuganzahl + dichte 2-Bit-Codes): die
			// Datei bleibt selbsttragend, und nur lebende Ketten werden
			// geschrieben - der Snapshot-Roundtrip wirkt damit als
			// Kompaktierung des PathStore (verwaiste Ketten bleiben zurück)
			data, moves := room.Paths.ExportPacked(v.Path)
			sw.u32(uint32(moves))
			if sw.err == nil && len(data) > 0 {
				_, sw.err = sw.w.Write(data)
			}
		}

		// eingehende Portale: Verzeichnisse mit sortierten Schlüsseln
		// (deterministische Dateien, Map-Reihenfolge wäre zufällig)
		sw.u32(uint32(len(room.Incoming)))
		for _, p := range room.Incoming {
			sw.u32(uint32(p.From))
			sw.u32(uint32(p.To))
			sw.u8(p.Dir)
			blocked := byte(0)
			if p.BlockedBox {
				blocked = 1
			}
			sw.u8(blocked)

			swapKeys := make([]uint64, 0, len(p.BoxSwap))
			for k := range p.BoxSwap {
				swapKeys = append(swapKeys, k)
			}
			slices.Sort(swapKeys)
			sw.u64(uint64(len(swapKeys)))
			for _, k := range swapKeys {
				sw.u64(k)
				sw.u64(p.BoxSwap[k])
			}

			spanKeys := make([]uint64, 0, len(p.VariantSpans))
			for k := range p.VariantSpans {
				spanKeys = append(spanKeys, k)
			}
			slices.Sort(spanKeys)
			sw.u64(uint64(len(spanKeys)))
			for _, k := range spanKeys {
				span := p.VariantSpans[k]
				sw.u64(k)
				sw.u64(span.Start)
				sw.u64(span.Count)
			}
		}
	}

	if sw.err == nil {
		sw.err = sw.w.Flush()
	}
	return sw.err
}

// ---------- Laden ----------

// ReadSnapshotHeader liest nur den Datei-Kopf (für die Snapshot-Liste):
// FieldCrc des Levels und der Effort-String zum Speicherzeitpunkt
func ReadSnapshotHeader(r io.Reader) (fieldCrc uint64, effort string, err error) {
	sr := &snapReader{r: bufio.NewReader(r)}
	magic := make([]byte, len(snapshotMagic))
	if _, err := io.ReadFull(sr.r, magic); err != nil {
		return 0, "", err
	}
	if string(magic) != snapshotMagic {
		return 0, "", fmt.Errorf("snapshot: unbekanntes Dateiformat")
	}
	if version := sr.u32(); sr.err == nil && version != snapshotVersion {
		return 0, "", fmt.Errorf("snapshot: version %d nicht unterstützt", version)
	}
	fieldCrc = sr.u64()
	effort = sr.str()
	return fieldCrc, effort, sr.err
}

// ReadSnapshot liest einen kompletten Netzwerk-Stand und baut alle
// Verlinkungen neu auf. field/scan kommen vom laufenden Netzwerk - der
// FieldCrc im Header stellt sicher, dass der Snapshot zu diesem Level
// gehört. Das gelieferte Netzwerk ist validiert; bei jedem Fehler bleibt
// das laufende Netzwerk unberührt (es wird nie angefasst).
func ReadSnapshot(field *soko.Field, scan *BoxScan, r io.Reader, info ProgressFunc) (*Network, error) {
	sr := &snapReader{r: bufio.NewReaderSize(r, 1<<20)}
	magic := make([]byte, len(snapshotMagic))
	if _, err := io.ReadFull(sr.r, magic); err != nil {
		return nil, err
	}
	if string(magic) != snapshotMagic {
		return nil, fmt.Errorf("snapshot: unbekanntes Dateiformat")
	}
	if version := sr.u32(); sr.err == nil && version != snapshotVersion {
		return nil, fmt.Errorf("snapshot: version %d nicht unterstützt", version)
	}
	if crc := sr.u64(); sr.err == nil && crc != field.FieldCrc() {
		return nil, fmt.Errorf("snapshot: gehört zu einem anderen Level")
	}
	sr.str() // Effort-String (nur für die Liste)

	eof := field.WalkEof()
	roomCount := sr.u32()
	if sr.err != nil {
		return nil, sr.err
	}
	roomList := make([]*Room, roomCount)
	var throttle progressThrottle
	for i := range roomList {
		if info != nil && throttle.due() &&
			!info(fmt.Sprintf("load: raum %d/%d", i+1, roomCount), nil) {
			return nil, fmt.Errorf("load: abgebrochen")
		}
		room := &Room{Index: uint32(i), States: NewStateList(), Variants: NewVariantList(), Paths: NewPathStore()}
		room.Fields = sr.wposList(eof)
		room.Goals = sr.wposList(eof)
		room.StartBoxes = sr.wposList(eof)
		room.MaxBoxes = sr.u32()
		room.StartState = sr.u64()
		room.StartVariantCount = sr.u64()

		offsCount := sr.u64()
		if sr.err != nil {
			return nil, sr.err
		}
		if offsCount == 0 {
			return nil, fmt.Errorf("snapshot: raum %d ohne Zustands-Offsets", i)
		}
		room.States.offs = make([]uint32, offsCount)
		for j := range room.States.offs {
			room.States.offs[j] = sr.u32()
		}
		bufCount := sr.u64()
		if sr.err != nil {
			return nil, sr.err
		}
		room.States.buf = make([]soko.Wpos, bufCount)
		for j := range room.States.buf {
			room.States.buf[j] = soko.Wpos(sr.u32())
		}

		variantCount := sr.u64()
		if sr.err != nil {
			return nil, sr.err
		}
		room.Variants.data = make([]VariantData, variantCount)
		for j := range room.Variants.data {
			v := &room.Variants.data[j]
			v.OldState = sr.u64()
			v.NewState = sr.u64()
			v.Moves = sr.u32()
			v.Pushes = sr.u32()
			if boxCount := sr.u32(); sr.err == nil && boxCount > 0 {
				v.BoxPortals = make([]uint32, boxCount)
				for b := range v.BoxPortals {
					v.BoxPortals[b] = sr.u32()
				}
			}
			v.PlayerPortal = sr.u32()
			if moves := sr.u32(); sr.err == nil && moves > 0 {
				data := make([]byte, (moves+3)/4)
				if _, sr.err = io.ReadFull(sr.r, data); sr.err == nil {
					v.Path = room.Paths.AddPacked(data, int(moves))
				}
			}
			if sr.err != nil {
				return nil, sr.err
			}
		}

		portalCount := sr.u32()
		if sr.err != nil {
			return nil, sr.err
		}
		room.Incoming = make([]*Portal, portalCount)
		for pi := range room.Incoming {
			p := &Portal{
				ToRoom:       room,
				Index:        uint32(pi),
				BoxSwap:      map[uint64]uint64{},
				VariantSpans: map[uint64]Span{},
			}
			p.From = soko.Wpos(sr.u32())
			p.To = soko.Wpos(sr.u32())
			p.Dir = sr.u8()
			p.BlockedBox = sr.u8() != 0
			if sr.err == nil && (p.From >= eof || p.To >= eof) {
				return nil, fmt.Errorf("snapshot: portal ausserhalb des Feldes")
			}
			swapCount := sr.u64()
			if sr.err != nil {
				return nil, sr.err
			}
			for range swapCount {
				k := sr.u64()
				p.BoxSwap[k] = sr.u64()
			}
			spanCount := sr.u64()
			if sr.err != nil {
				return nil, sr.err
			}
			for range spanCount {
				k := sr.u64()
				p.VariantSpans[k] = Span{Start: sr.u64(), Count: sr.u64()}
			}
			room.Incoming[pi] = p
		}
		roomList[i] = room
	}
	if sr.err != nil {
		return nil, sr.err
	}

	// --- Verlinkungen neu aufbauen: Feld -> Raum, Portal-Paare über Feld-Paare ---
	roomOf := make([]*Room, eof)
	for _, room := range roomList {
		for _, p := range room.Fields {
			roomOf[p] = room
		}
	}
	portalAt := make(map[uint64]*Portal) // From<<32|To -> Portal
	for _, room := range roomList {
		for _, p := range room.Incoming {
			p.FromRoom = roomOf[p.From]
			if p.FromRoom == nil {
				return nil, fmt.Errorf("snapshot: portal-quellfeld %d ohne Raum", p.From)
			}
			portalAt[uint64(p.From)<<32|uint64(p.To)] = p
		}
	}
	for _, room := range roomList {
		room.Outgoing = make([]*Portal, len(room.Incoming))
		for i, ip := range room.Incoming {
			op := portalAt[uint64(ip.To)<<32|uint64(ip.From)]
			if op == nil {
				return nil, fmt.Errorf("snapshot: gegenportal fehlt: %v", ip)
			}
			ip.Opposite = op
			room.Outgoing[i] = op
		}
	}

	n := &Network{Field: field, Scan: scan, Rooms: roomList}
	if err := n.Validate(true); err != nil {
		return nil, fmt.Errorf("validate after load: %w", err)
	}
	n.warmMinMoves() // Caches vorwärmen (lesende API-Zugriffe bleiben race-frei)
	return n, nil
}
