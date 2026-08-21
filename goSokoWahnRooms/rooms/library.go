package rooms

import (
	"bufio"
	"fmt"
	"io"
	"slices"

	"goSokoWahnRooms/crc64"
	"goSokoWahnRooms/soko"
	"goSokoWahnRooms/tools"
)

// Raum-Bibliothek (M8, Max' Konzept 2026-08-21): einzelne (gemergte und
// optimierte) Räume als eigenständige Dateien - je Geometrie und Budget ein
// wiederverwendbarer Baustein. Kern-Einsicht: alle Streichungs-Beweise
// (Dominanz, Deadlock, Budget) sind Aussagen über das LEVEL, nicht über die
// Raumaufteilung - ein Raum, der unter Budget B optimiert wurde, ist in
// jeder späteren Konstellation gültig, solange das aktuelle Budget <= B ist
// (kleiner = strengere Suche, die gestrichenen Nutzungen wären erst recht
// gestrichen; 0 im Dateikopf = nie mit Budget optimiert, unbedingt gültig).
// Damit lassen sich "Raumsalate" bauen: verschiedene Konstellationen heben
// die bewiesenen Min-Züge, der geschrumpfte Slack lässt Fokus-Räume weiter
// eindampfen, und die besten Stände je Geometrie wandern in die Bibliothek.
//
// Datei-Format: eigener Kopf (Magic, Level-Crc, Budget, Kennzahlen) plus
// GENAU der Raum-Block des Snapshot-Formats (writeRoomBlock/readRoomBlock,
// snapshot.go) - Portale "nackt" über Feld-Paare, die Verlinkung entsteht
// beim Einfügen neu.

const libraryMagic = "ROOMLIB1"
const libraryVersion = uint32(1)

// LibraryMeta ist der Datei-Kopf einer Bibliotheks-Datei: Level-Bindung,
// Gültigkeits-Budget und Kennzahlen für die Liste (ohne Voll-Laden lesbar)
type LibraryMeta struct {
	FieldCrc uint64 // Level-Bindung (wie Snapshot-Header)
	MaxMoves uint64 // Budget der Streichungs-Beweise (0 = unbedingt gültig)
	GeoCode  uint64 // Kennung der Feld-Geometrie (Crc der sortierten Feldliste)
	Fields   uint32 // Anzahl Felder
	States   uint64
	Variants uint64
	MinMoves uint64 // bewiesenes Pflicht-Minimum des Raums beim Speichern
	RestMin  uint64 // Summe der Minima der übrigen Räume beim Speichern (informativ)
}

// RoomGeoCode liefert die Geometrie-Kennung eines Raums: Crc über die
// sortierte Feldliste - gleiche Felder ergeben immer denselben Code
func RoomGeoCode(room *Room) uint64 {
	fields := slices.Clone(room.Fields)
	slices.Sort(fields)
	c := crc64.Start.UpdateInt(len(fields))
	for _, p := range fields {
		c = c.UpdateUInt32(uint32(p))
	}
	return uint64(c)
}

// WriteLibraryRoom schreibt einen Raum als Bibliotheks-Datei
func WriteLibraryRoom(w io.Writer, room *Room, meta LibraryMeta) error {
	sw := &snapWriter{w: bufio.NewWriterSize(w, 1<<20)}
	if _, err := sw.w.WriteString(libraryMagic); err != nil {
		return err
	}
	sw.u32(libraryVersion)
	sw.u64(meta.FieldCrc)
	sw.u64(meta.MaxMoves)
	sw.u64(meta.GeoCode)
	sw.u32(meta.Fields)
	sw.u64(meta.States)
	sw.u64(meta.Variants)
	sw.u64(meta.MinMoves)
	sw.u64(meta.RestMin)
	writeRoomBlock(sw, room)
	if sw.err == nil {
		sw.err = sw.w.Flush()
	}
	return sw.err
}

// readLibraryHeader liest Magic, Version und Metadaten (gemeinsamer Anfang
// von ReadLibraryHeader und ReadLibraryRoom)
func readLibraryHeader(sr *snapReader) (LibraryMeta, error) {
	magic := make([]byte, len(libraryMagic))
	if _, err := io.ReadFull(sr.r, magic); err != nil {
		return LibraryMeta{}, err
	}
	if string(magic) != libraryMagic {
		return LibraryMeta{}, fmt.Errorf("library: unbekanntes Dateiformat")
	}
	if version := sr.u32(); sr.err == nil && version != libraryVersion {
		return LibraryMeta{}, fmt.Errorf("library: version %d nicht unterstützt", version)
	}
	meta := LibraryMeta{
		FieldCrc: sr.u64(),
		MaxMoves: sr.u64(),
		GeoCode:  sr.u64(),
		Fields:   sr.u32(),
		States:   sr.u64(),
		Variants: sr.u64(),
		MinMoves: sr.u64(),
		RestMin:  sr.u64(),
	}
	return meta, sr.err
}

// ReadLibraryHeader liest nur den Datei-Kopf (für die Bibliotheks-Liste)
func ReadLibraryHeader(r io.Reader) (LibraryMeta, error) {
	return readLibraryHeader(&snapReader{r: bufio.NewReader(r)})
}

// ReadLibraryFields liest Kopf UND Feldliste (sie steht als erstes im
// Raum-Block) - billig genug für die Liste, die GUI markiert damit die
// Einfüge-Vorschau (betroffene Felder und aufzulösende Räume)
func ReadLibraryFields(field *soko.Field, r io.Reader) (LibraryMeta, []soko.Wpos, error) {
	sr := &snapReader{r: bufio.NewReader(r)}
	meta, err := readLibraryHeader(sr)
	if err != nil {
		return meta, nil, err
	}
	fields := sr.wposList(field.WalkEof())
	return meta, fields, sr.err
}

// ReadLibraryRoom liest eine Bibliotheks-Datei komplett; der FieldCrc im
// Kopf muss zum Level passen. Der Raum ist noch UNVERLINKT (kein FromRoom/
// Opposite/Outgoing) - das übernimmt InsertRoom.
func ReadLibraryRoom(field *soko.Field, r io.Reader) (*Room, LibraryMeta, error) {
	sr := &snapReader{r: bufio.NewReaderSize(r, 1<<20)}
	meta, err := readLibraryHeader(sr)
	if err != nil {
		return nil, meta, err
	}
	if meta.FieldCrc != field.FieldCrc() {
		return nil, meta, fmt.Errorf("library: gehört zu einem anderen Level")
	}
	room, err := readRoomBlock(sr, 0, field.WalkEof())
	if err != nil {
		return nil, meta, err
	}
	return room, meta, nil
}

// InsertRoom setzt einen aus der Bibliothek geladenen Raum ins Netzwerk ein:
// überlappende Mehr-Feld-Räume werden vorher per ResetRooms auf ihre
// 1-Feld-Start-Räume aufgelöst (Max' Konzept: "der vorherige Blockierer
// wird aufgelöst"), dann ersetzen die Felder des neuen Raums ihre
// 1-Feld-Räume und die Portale verlinken sich über die Feld-Paare neu.
// Die Budget-Gültigkeit (meta.MaxMoves) prüft der Aufrufer - hier zählt
// nur die Struktur. Schlägt das Einfügen nach einem gelungenen Reset fehl,
// bleibt das Netzwerk im (gültigen) zurückgesetzten Zustand.
func (n *Network) InsertRoom(room *Room, info ProgressFunc) error {
	eof := n.Field.WalkEof()
	inNew := make([]bool, eof)
	for _, p := range room.Fields {
		inNew[p] = true
	}

	// überlappende Mehr-Feld-Räume einsammeln und auflösen
	roomOf := make([]*Room, eof)
	for _, r := range n.Rooms {
		for _, p := range r.Fields {
			roomOf[p] = r
		}
	}
	var resetIdx []uint32
	seen := map[*Room]bool{}
	for _, p := range room.Fields {
		r := roomOf[p]
		if r == nil {
			return fmt.Errorf("insert: feld %d gehört zu keinem raum", p)
		}
		if len(r.Fields) > 1 && !seen[r] {
			seen[r] = true
			resetIdx = append(resetIdx, r.Index)
		}
	}
	if len(resetIdx) > 0 {
		if _, err := n.ResetRooms(resetIdx, info); err != nil {
			return fmt.Errorf("insert: reset der überlappenden räume: %w", err)
		}
		for _, r := range n.Rooms {
			for _, p := range r.Fields {
				roomOf[p] = r
			}
		}
	}
	if info != nil {
		info(fmt.Sprintf("insert: raum mit %d feldern, %s varianten verlinken",
			len(room.Fields), tools.FormatInt(room.Variants.Count())), nil)
	}

	// Portale verlinken - erst ALLE Gegenportale finden (bei jedem Fehler
	// bleibt das Netzwerk im Zustand nach dem Reset), dann umhängen
	type link struct {
		ip *Portal // eingehendes Portal des neuen Raums
		op *Portal // bestehendes eingehendes Portal des Nachbarn
	}
	links := make([]link, len(room.Incoming))
	for i, ip := range room.Incoming {
		if inNew[ip.From] || !inNew[ip.To] {
			return fmt.Errorf("insert: portal %d->%d passt nicht zur geometrie", ip.From, ip.To)
		}
		neighbor := roomOf[ip.From]
		if neighbor == nil {
			return fmt.Errorf("insert: portal-quellfeld %d ohne raum", ip.From)
		}
		ip.FromRoom = neighbor
		var op *Portal
		for _, p := range neighbor.Incoming {
			if p.From == ip.To && p.To == ip.From {
				op = p
				break
			}
		}
		if op == nil {
			return fmt.Errorf("insert: gegenportal %d->%d fehlt beim nachbarn", ip.To, ip.From)
		}
		links[i] = link{ip: ip, op: op}
	}

	// Commit: Portale beidseitig verdrahten und die 1-Feld-Räume der
	// Geometrie durch den neuen Raum ersetzen (an der Position des ersten)
	room.Outgoing = make([]*Portal, len(room.Incoming))
	for i, l := range links {
		room.Outgoing[i] = l.op
		l.ip.Opposite = l.op
		l.op.Opposite = l.ip
		l.op.FromRoom = room
		l.op.ToRoom.Outgoing[l.op.Index] = l.ip
	}
	newList := make([]*Room, 0, len(n.Rooms))
	placed := false
	for _, r := range n.Rooms {
		if inNew[r.Fields[0]] {
			if len(r.Fields) != 1 {
				panic("insert: überlappender mehr-feld-raum nach reset")
			}
			if !placed {
				newList = append(newList, room)
				placed = true
			}
			continue
		}
		newList = append(newList, r)
	}
	for i, r := range newList {
		r.Index = uint32(i)
	}
	n.Rooms = newList

	// Struktur komplett, Varianten nur vom neuen Raum (die übrigen sind
	// unangetastet, siehe ValidateRooms)
	if err := n.ValidateRooms(room); err != nil {
		return fmt.Errorf("validate after insert: %w", err)
	}
	n.warmMinMoves() // Caches vorwärmen (lesende API-Zugriffe bleiben race-frei)
	return nil
}
