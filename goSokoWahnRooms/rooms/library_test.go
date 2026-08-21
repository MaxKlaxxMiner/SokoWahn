package rooms

import (
	"bytes"
	"testing"

	"goSokoWahnRooms/maps"
)

// Roundtrip der Raum-Bibliothek am 202er: die gemergte Kammer speichern,
// im selben Netzwerk wieder einfügen (löst die alte Kammer per Reset auf)
// und in ein frisches Netzwerk einfügen - beide Male muss exakt der
// gespeicherte Stand herauskommen.
func TestLibraryRoundtrip202(t *testing.T) {
	n, chamber := merge202Chamber(t)
	wantRooms := len(n.Rooms)
	wantEffort := n.EffortString()
	wantStates := chamber.States.Count()
	wantVariants := chamber.Variants.Count()
	geo := RoomGeoCode(chamber)

	meta := LibraryMeta{
		FieldCrc: n.Field.FieldCrc(),
		MaxMoves: 0, // ohne Budget optimiert = unbedingt gültig
		GeoCode:  geo,
		Fields:   uint32(len(chamber.Fields)),
		States:   wantStates,
		Variants: wantVariants,
		MinMoves: chamber.MinMoves(),
	}
	var buf bytes.Buffer
	if err := WriteLibraryRoom(&buf, chamber, meta); err != nil {
		t.Fatal("write:", err)
	}
	data := buf.Bytes()

	// Kopf ohne Voll-Laden lesbar, Werte unverändert
	head, err := ReadLibraryHeader(bytes.NewReader(data))
	if err != nil {
		t.Fatal("header:", err)
	}
	if head != meta {
		t.Fatalf("header: got %+v, want %+v", head, meta)
	}

	// findet den Bibliotheks-Raum (per Geometrie-Code) in einem Netzwerk
	findRoom := func(n *Network) *Room {
		for _, room := range n.Rooms {
			if RoomGeoCode(room) == geo {
				return room
			}
		}
		t.Fatal("eingefügter raum nicht gefunden")
		return nil
	}
	check := func(name string, n *Network) {
		t.Helper()
		if err := n.Validate(true); err != nil {
			t.Fatalf("%s: validate: %v", name, err)
		}
		if len(n.Rooms) != wantRooms {
			t.Fatalf("%s: rooms: got %d, want %d", name, len(n.Rooms), wantRooms)
		}
		if got := n.EffortString(); got != wantEffort {
			t.Fatalf("%s: effort: got %s, want %s", name, got, wantEffort)
		}
		room := findRoom(n)
		if room.States.Count() != wantStates || room.Variants.Count() != wantVariants {
			t.Fatalf("%s: raum: got %d states / %d variants, want %d / %d",
				name, room.States.Count(), room.Variants.Count(), wantStates, wantVariants)
		}
	}

	// 1. ins selbe Netzwerk einfügen: die alte Kammer überlappt und wird
	// per Reset aufgelöst, der Bibliotheks-Raum ersetzt ihre Felder
	loaded, head2, err := ReadLibraryRoom(n.Field, bytes.NewReader(data))
	if err != nil {
		t.Fatal("read:", err)
	}
	if head2 != meta {
		t.Fatalf("read-header: got %+v, want %+v", head2, meta)
	}
	if err := n.InsertRoom(loaded, nil); err != nil {
		t.Fatal("insert (überlappend):", err)
	}
	check("überlappend", n)

	// 2. in ein frisches Netzwerk einfügen (nur 1-Feld-Räume, kein Reset
	// nötig) - gleiche Kennzahlen wie im gemergten Original
	fresh := buildNetwork(t, maps.Map202)
	loaded2, _, err := ReadLibraryRoom(fresh.Field, bytes.NewReader(data))
	if err != nil {
		t.Fatal("read (frisch):", err)
	}
	if err := fresh.InsertRoom(loaded2, nil); err != nil {
		t.Fatal("insert (frisch):", err)
	}
	check("frisch", fresh)
}

// Bibliotheks-Datei eines fremden Levels wird abgewiesen
func TestLibraryWrongLevel(t *testing.T) {
	n, chamber := merge202Chamber(t)
	var buf bytes.Buffer
	meta := LibraryMeta{FieldCrc: n.Field.FieldCrc(), GeoCode: RoomGeoCode(chamber)}
	if err := WriteLibraryRoom(&buf, chamber, meta); err != nil {
		t.Fatal("write:", err)
	}
	other := buildNetwork(t, maps.Map200)
	if _, _, err := ReadLibraryRoom(other.Field, bytes.NewReader(buf.Bytes())); err == nil {
		t.Fatal("fremdes level wurde nicht abgewiesen")
	}
}
