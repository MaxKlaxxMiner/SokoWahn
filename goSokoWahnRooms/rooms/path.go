package rooms

// Pfad-Speicher als Rope/DAG (Max' Konzept, 2026-08-20): Varianten halten
// statt der Zugfolge nur noch eine 4-Byte-ID in den PathStore ihres Raums.
// Ein Knoten ist entweder ein Blatt (2-Bit-gepackte Züge im flachen
// leaf-Puffer, 4 je Byte) oder eine Verkettung zweier Kind-IDs - das
// Verketten beim Mergen kostet damit 8 Bytes statt einer Kopie des ganzen
// Präfixes, geteilte Präfixe existieren genau einmal. Die Länge wird nie
// gespeichert: Moves ist per Validate-Invariante die Pfadlänge, Len()
// läuft nur über die Knoten. Zwei flache Slices, keine Pointer - der GC
// sieht den Store als zwei Blöcke.
//
// Der Store wächst nur (Optimize hinterlässt verwaiste Ketten entfernter
// Varianten); kompaktiert wird per CopyFrom in einen frischen Store bzw.
// über den Snapshot-Roundtrip (Speichern materialisiert nur Lebendes).

// ID einer Zugfolge im PathStore; 0 = leere Zugfolge, 1-4 = die in jedem
// Store identisch vorbelegten Ein-Zug-Blätter l/u/r/d (Init-Varianten der
// 1-Feld-Räume kosten damit keinen Speicher, CopyFrom reicht sie durch)
type PathID uint32

const EmptyPath PathID = 0

// Zeichen <-> 2-Bit-Code; die Reihenfolge "lurd" macht codeLURD zur Umkehrung
const codeLURD = "lurd"

var lurdCode = [256]byte{'l': 0, 'u': 1, 'r': 2, 'd': 3}

// Blatt-Flag im b-Feld eines Knotens (a = Byte-Offset in leaf, b = Zuganzahl);
// ohne Flag ist der Knoten eine Verkettung (a, b = Kind-IDs, nie EmptyPath)
const pathLeafFlag = uint32(1) << 31

type pathNode struct{ a, b uint32 }

type PathStore struct {
	nodes []pathNode
	leaf  []byte // 2-Bit-gepackte Züge aller Blätter, je Blatt ab Bit 0 eines Bytes
}

func NewPathStore() *PathStore {
	// Knoten 0 = leer, Knoten 1-4 = Ein-Zug-Blätter in codeLURD-Reihenfolge
	ps := &PathStore{nodes: make([]pathNode, 5, 16), leaf: make([]byte, 4, 64)}
	for code := uint32(0); code < 4; code++ {
		ps.leaf[code] = byte(code)
		ps.nodes[code+1] = pathNode{a: code, b: 1 | pathLeafFlag}
	}
	return ps
}

// PathOfDir liefert die (store-unabhängige) ID des Ein-Zug-Pfades einer Richtung
func PathOfDir(dir byte) PathID {
	return PathID(lurdCode[dir]) + 1
}

// liest den 2-Bit-Code des Zuges i eines Blattes ab Byte-Offset off
func (ps *PathStore) leafCode(off uint32, i int) byte {
	return ps.leaf[int(off)+i/4] >> ((i % 4) * 2) & 3
}

// legt ein neues Blatt an; codes liefert den 2-Bit-Code je Zug
func (ps *PathStore) addLeaf(moves int, codes func(i int) byte) PathID {
	if moves == 0 {
		return EmptyPath
	}
	off := len(ps.leaf)
	ps.leaf = append(ps.leaf, make([]byte, (moves+3)/4)...)
	for i := 0; i < moves; i++ {
		ps.leaf[off+i/4] |= codes(i) << ((i % 4) * 2)
	}
	ps.nodes = append(ps.nodes, pathNode{a: uint32(off), b: uint32(moves) | pathLeafFlag})
	return PathID(len(ps.nodes) - 1)
}

// AddLURD packt eine Klartext-Zugfolge (nur 'l','u','r','d') als Blatt
func (ps *PathStore) AddLURD(s string) PathID {
	return ps.addLeaf(len(s), func(i int) byte { return lurdCode[s[i]] })
}

// AddPacked übernimmt ein dichtes 2-Bit-Array (Format von ExportPacked)
func (ps *PathStore) AddPacked(data []byte, moves int) PathID {
	return ps.addLeaf(moves, func(i int) byte { return data[i/4] >> ((i % 4) * 2) & 3 })
}

// Concat verkettet zwei Zugfolgen (a gefolgt von b) - ein 8-Byte-Knoten,
// kein Kopieren; leere Seiten reichen die andere ID unverändert durch
func (ps *PathStore) Concat(a, b PathID) PathID {
	if a == EmptyPath {
		return b
	}
	if b == EmptyPath {
		return a
	}
	ps.nodes = append(ps.nodes, pathNode{a: uint32(a), b: uint32(b)})
	return PathID(len(ps.nodes) - 1)
}

// Len liefert die Zuganzahl einer Kette (läuft nur über die Knoten,
// nicht über die Züge - Kosten = Kettenglieder, nicht Pfadlänge)
func (ps *PathStore) Len(id PathID) int {
	if id == EmptyPath {
		return 0
	}
	total := 0
	stack := []PathID{id}
	for len(stack) > 0 {
		n := ps.nodes[stack[len(stack)-1]]
		stack = stack[:len(stack)-1]
		if n.b&pathLeafFlag != 0 {
			total += int(n.b &^ pathLeafFlag)
		} else {
			stack = append(stack, PathID(n.a), PathID(n.b))
		}
	}
	return total
}

// lens liefert die Zuglänge JEDES Knotens in einem einzigen Vorwärts-
// Durchlauf (der Store ist append-only, Kinder haben immer kleinere IDs
// als Eltern): O(Knoten) statt O(Ketten x Tiefe) - für Massen-Längenprüfungen
// wie Validate oder das Eintragen der Merge-Ergebnisse
func (ps *PathStore) lens() []uint32 {
	lens := make([]uint32, len(ps.nodes))
	lens[1], lens[2], lens[3], lens[4] = 1, 1, 1, 1 // Ein-Zug-Blätter
	for i := 5; i < len(ps.nodes); i++ {
		n := ps.nodes[i]
		if n.b&pathLeafFlag != 0 {
			lens[i] = n.b &^ pathLeafFlag
		} else {
			lens[i] = lens[n.a] + lens[n.b]
		}
	}
	return lens
}

// appendCodes rekonstruiert die Züge einer Kette als 2-Bit-Codes (je Byte
// ein Zug) - iterative Tiefensuche, a vor b
func (ps *PathStore) appendCodes(id PathID, dst []byte) []byte {
	if id == EmptyPath {
		return dst
	}
	stack := []PathID{id}
	for len(stack) > 0 {
		n := ps.nodes[stack[len(stack)-1]]
		stack = stack[:len(stack)-1]
		if n.b&pathLeafFlag != 0 {
			moves := int(n.b &^ pathLeafFlag)
			for i := 0; i < moves; i++ {
				dst = append(dst, ps.leafCode(n.a, i))
			}
		} else {
			stack = append(stack, PathID(n.b), PathID(n.a)) // a zuerst verarbeiten
		}
	}
	return dst
}

// LURD rekonstruiert die Zugfolge als Klartext (GUI, Debug, Tests)
func (ps *PathStore) LURD(id PathID) string {
	codes := ps.appendCodes(id, nil)
	for i, c := range codes {
		codes[i] = codeLURD[c]
	}
	return string(codes)
}

// ExportPacked materialisiert eine Kette als dichtes 2-Bit-Array
// (4 Züge je Byte ab Bit 0; Format von AddPacked, fürs Snapshot-Speichern)
func (ps *PathStore) ExportPacked(id PathID) (data []byte, moves int) {
	codes := ps.appendCodes(id, nil)
	if len(codes) == 0 {
		return nil, 0
	}
	data = make([]byte, (len(codes)+3)/4)
	for i, c := range codes {
		data[i/4] |= c << ((i % 4) * 2)
	}
	return data, len(codes)
}

// CopyFrom kopiert den erreichbaren Teilgraphen einer Kette aus einem
// fremden Store hierher (memo erhält das Sharing und dedupliziert über
// mehrere Aufrufe derselben Quelle, z.B. je Merge-Suche) und liefert die
// neue ID. Rekursionstiefe = Kettenglieder (durch die Zuganzahl begrenzt,
// jedes Glied trägt mindestens einen Zug).
func (ps *PathStore) CopyFrom(src *PathStore, id PathID, memo map[PathID]PathID) PathID {
	if id <= 4 {
		return id // leer und Ein-Zug-Blätter sind in jedem Store identisch vorbelegt
	}
	if nid, ok := memo[id]; ok {
		return nid
	}
	n := src.nodes[id]
	var nid PathID
	if n.b&pathLeafFlag != 0 {
		moves := int(n.b &^ pathLeafFlag)
		nid = ps.addLeaf(moves, func(i int) byte { return src.leafCode(n.a, i) })
	} else {
		a := ps.CopyFrom(src, PathID(n.a), memo)
		b := ps.CopyFrom(src, PathID(n.b), memo)
		nid = ps.Concat(a, b)
	}
	memo[id] = nid
	return nid
}
