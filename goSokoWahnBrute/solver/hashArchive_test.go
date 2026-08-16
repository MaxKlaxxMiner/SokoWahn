package solver

import (
	"testing"

	"goSokoWahnBrute/crc64"
	"goSokoWahnBrute/soko"
)

// Konsistenz: die ArchiveTable muss sich exakt wie eine Referenz-map verhalten,
// auch über viele erzwungene Delta-Merges hinweg (Updates treffen Delta UND Archiv)
func TestArchiveTableMatchesMap(t *testing.T) {
	oldMin := ArchiveDeltaMin
	ArchiveDeltaMin = 4096 // viele Merges erzwingen
	defer func() { ArchiveDeltaMin = oldMin }()

	reference := make(map[crc64.Value]uint16)
	archive := NewArchiveTable()

	seed := uint64(42)
	keys := make([]crc64.Value, 0, 50000)
	for n := 0; n < 50000; n++ {
		crc := nextCrc(&seed)
		keys = append(keys, crc)
		depth := uint16(n % 60001)
		reference[crc] = depth
		archive.Add(crc, depth)
	}

	// Updates auf einem Teil der Schlüssel (liegen teils im Delta, teils im Archiv)
	for n := 0; n < len(keys); n += 7 {
		reference[keys[n]] = uint16(n % 777)
		archive.Update(keys[n], uint16(n%777))
	}

	if int64(len(reference)) != archive.Len() {
		t.Fatalf("Längen weichen ab: map=%d archive=%d", len(reference), archive.Len())
	}
	for _, crc := range keys {
		if archive.Get(crc) != reference[crc] {
			t.Fatalf("Wert weicht ab für %x: map=%d archive=%d", uint64(crc), reference[crc], archive.Get(crc))
		}
	}

	// unbekannte Schlüssel
	seed = uint64(4711)
	for n := 0; n < 10000; n++ {
		crc := nextCrc(&seed)
		if _, exists := reference[crc]; exists {
			continue
		}
		if got := archive.Get(crc); got != DepthUnknown {
			t.Fatalf("Fehlschlag-Lookup liefert %d statt DepthUnknown für %x", got, uint64(crc))
		}
	}

	// Sonderfall Schlüssel 0: über einen Merge tragen und danach aktualisieren
	archive.Add(0, 123)
	for n := 0; n < 5000; n++ { // genug neue Schlüssel für den nächsten Merge
		crc := nextCrc(&seed)
		reference[crc] = uint16(n)
		archive.Add(crc, uint16(n))
	}
	if got := archive.Get(0); got != 123 {
		t.Fatalf("Schlüssel 0 nach Merge: erwartet 123, erhalten %d", got)
	}
	archive.Update(0, 321)
	if got := archive.Get(0); got != 321 {
		t.Fatalf("Schlüssel 0 nach Archiv-Update: erwartet 321, erhalten %d", got)
	}
}

// Konvertierung einer bestehenden CompactTable (Taste h): alle Einträge müssen
// verlustfrei übernommen werden, danach normale Weiterarbeit mit frischem Delta
func TestArchiveConvertFromCompact(t *testing.T) {
	src := newCompactTable(1 << 12)
	reference := make(map[crc64.Value]uint16)

	seed := uint64(1337)
	for n := 0; n < 30000; n++ {
		crc := nextCrc(&seed)
		depth := uint16(n % 50000)
		reference[crc] = depth
		src.Add(crc, depth)
	}
	src.Add(0, 999) // Sonderfall Schlüssel 0 der CompactTable
	reference[0] = 999

	archive := NewArchiveTableFrom(src)
	if archive.Len() != int64(len(reference)) {
		t.Fatalf("Längen weichen ab: map=%d archive=%d", len(reference), archive.Len())
	}
	if archive.delta.count != 0 {
		t.Fatalf("Delta muss nach der Konvertierung leer sein, enthält %d Einträge", archive.delta.count)
	}
	for crc, depth := range reference {
		if got := archive.Get(crc); got != depth {
			t.Fatalf("Wert weicht nach Konvertierung ab für %x: erwartet %d, erhalten %d", uint64(crc), depth, got)
		}
	}

	// Weiterarbeit: neue Einträge und Updates auf Archiv-Bestand
	crc := nextCrc(&seed)
	archive.Add(crc, 42)
	if got := archive.Get(crc); got != 42 {
		t.Fatalf("neuer Eintrag nach Konvertierung: erwartet 42, erhalten %d", got)
	}
	first := crc64.Value(0)
	for k := range reference {
		if k != 0 {
			first = k
			break
		}
	}
	archive.Update(first, 7)
	if got := archive.Get(first); got != 7 {
		t.Fatalf("Archiv-Update nach Konvertierung: erwartet 7, erhalten %d", got)
	}
}

// Max-Memory-Modus: das Delta darf kurz vor der Merge-Schwelle nicht mehr
// verdoppeln, sondern merged stattdessen vorzeitig; ein Schlüssel, der dabei
// aus dem Delta ins Archiv wandert, muss dort in-place aktualisiert werden
// statt doppelt zu existieren
func TestArchiveMaxMemoryMergesInsteadOfGrow(t *testing.T) {
	oldMin := ArchiveDeltaMin
	ArchiveDeltaMin = 1 << 12
	CompactMaxMemory = true
	defer func() {
		ArchiveDeltaMin = oldMin
		CompactMaxMemory = false
	}()

	archive := NewArchiveTable().(*ArchiveTable)
	reference := make(map[crc64.Value]uint16)

	// Delta exakt bis an den Verdopplungspunkt füllen: Start-Kapazität 4096,
	// Max-Memory-Schwelle 15/16 = 3840 - bis hierhin weder Grow noch Merge
	seed := uint64(7)
	keys := make([]crc64.Value, 0, 3840)
	for n := 0; n < 3840; n++ {
		crc := nextCrc(&seed)
		keys = append(keys, crc)
		reference[crc] = uint16(n)
		archive.Add(crc, uint16(n))
	}
	if archive.archiveCount != 0 || archive.delta.count != 3840 || len(archive.delta.crcs) != 4096 {
		t.Fatalf("Vorbedingung verfehlt: archiv=%d delta=%d Kapazität=%d",
			archive.archiveCount, archive.delta.count, len(archive.delta.crcs))
	}

	// Einfügung am Verdopplungspunkt (Update eines Delta-Schlüssels): statt der
	// Verdopplung muss der vorgezogene Merge laufen, der Schlüssel wandert dabei
	// ins Archiv und wird dort aktualisiert
	reference[keys[0]] = 60000
	archive.Add(keys[0], 60000)
	if archive.archiveCount != 3840 || archive.delta.count != 0 {
		t.Fatalf("vorgezogener Merge blieb aus: archiv=%d delta=%d", archive.archiveCount, archive.delta.count)
	}
	if len(archive.delta.crcs) != 4096 {
		t.Fatalf("Delta hat trotz Max-Memory-Modus verdoppelt: Kapazität=%d", len(archive.delta.crcs))
	}

	// Weiterarbeit und Konsistenz: neuer Schlüssel landet im frischen Delta,
	// Länge und alle Werte stimmen mit der Referenz überein (Len deckt auch
	// versehentliche Archiv/Delta-Duplikate auf)
	crc := nextCrc(&seed)
	reference[crc] = 4711
	archive.Add(crc, 4711)
	if archive.Len() != int64(len(reference)) {
		t.Fatalf("Längen weichen ab: map=%d archive=%d", len(reference), archive.Len())
	}
	for key, depth := range reference {
		if got := archive.Get(key); got != depth {
			t.Fatalf("Wert weicht ab für %x: erwartet %d, erhalten %d", uint64(key), depth, got)
		}
	}
}

// Bit-Wachstum: der Umbau auf mehr Bucket-Bits rekonstruiert die vollen Schlüssel
// aus Rest + Bucket - hier von Hand erzwungen (die echte Schwelle liegt bei
// über 200 Mio Einträgen und ist im Test unerreichbar)
func TestArchiveBitsGrowth(t *testing.T) {
	oldMin := ArchiveDeltaMin
	ArchiveDeltaMin = 1024
	defer func() { ArchiveDeltaMin = oldMin }()

	archive := NewArchiveTable().(*ArchiveTable)
	reference := make(map[crc64.Value]uint16)
	seed := uint64(2024)
	for n := 0; n < 10000; n++ {
		crc := nextCrc(&seed)
		depth := uint16(n)
		reference[crc] = depth
		archive.Add(crc, depth)
	}

	for _, bits := range []uint{26, 29} {
		archive.mergeBits(bits)
		if archive.bits != bits {
			t.Fatalf("erwartete %d Bucket-Bits, erhalten %d", bits, archive.bits)
		}
		if archive.Len() != int64(len(reference)) {
			t.Fatalf("Länge nach Bit-Wachstum auf %d: erwartet %d, erhalten %d", bits, len(reference), archive.Len())
		}
		for crc, depth := range reference {
			if got := archive.Get(crc); got != depth {
				t.Fatalf("Wert weicht nach Bit-Wachstum auf %d ab für %x: erwartet %d, erhalten %d", bits, uint64(crc), depth, got)
			}
		}
	}
}

// das kleine Mehrkisten-Level aus TestSolveSmall (Referenz: 16 Züge)
const archiveTestLevel = `
#######
#.@ # #
#$* $ #
#   $ #
# ..  #
#  *  #
#######
`

// die Suche muss mit der ArchiveTable exakt dieselben Knoten und Züge liefern
// wie mit der Standard-CompactTable (das Tabellen-Format ist reine Speicherfrage)
func TestSolveArchiveTableDeterminism(t *testing.T) {
	refSolver, refSolution := solveLevel(t, archiveTestLevel, 16)
	refNodes := refSolver.NodeCount()

	oldFactory, oldMin := TableFactory, ArchiveDeltaMin
	TableFactory, ArchiveDeltaMin = NewArchiveTable, 256 // viele Merges während der Suche
	defer func() { TableFactory, ArchiveDeltaMin = oldFactory, oldMin }()

	s, solution := solveLevel(t, archiveTestLevel, 16)
	if s.NodeCount() != refNodes {
		t.Errorf("Knotenzahl weicht ab: CompactTable=%d ArchiveTable=%d", refNodes, s.NodeCount())
	}
	if solution.Moves != refSolution.Moves {
		t.Errorf("Zugfolge weicht ab: %q gegen %q", refSolution.Moves, solution.Moves)
	}
}

// Konvertierung mitten in der Suche (Taste h, zweimal gedrückt): beide Richtungen
// wandern ins Archiv-Format, das Ergebnis bleibt bitgenau identisch
func TestSolveArchiveConversionMidSearch(t *testing.T) {
	refSolver, refSolution := solveLevel(t, archiveTestLevel, 16)
	refNodes := refSolver.NodeCount()

	oldMin := ArchiveDeltaMin
	ArchiveDeltaMin = 256
	defer func() { ArchiveDeltaMin = oldMin }()

	field, err := soko.Parse(archiveTestLevel)
	if err != nil {
		t.Fatal(err)
	}
	s := New(field)
	for i := 0; i < 10 && s.Step(3); i++ {
	}
	s.ArchiveLargerTable() // erste Richtung konvertieren
	s.ArchiveLargerTable() // beim zweiten Druck ist die andere Richtung dran
	forward, backward := s.TableInfos()
	if !forward.Archive || !backward.Archive {
		t.Fatalf("beide Tabellen müssen nach zwei Tastendrücken im Archiv-Format sein (V=%v R=%v)", forward.Archive, backward.Archive)
	}
	for s.Step(1000000000) {
	}

	stats := s.GetStats()
	if stats.FoundMoves != 16 {
		t.Fatalf("erwartete Lösungslänge 16, erhalten: %d", stats.FoundMoves)
	}
	if s.NodeCount() != refNodes {
		t.Errorf("Knotenzahl weicht ab: Referenz=%d konvertiert=%d", refNodes, s.NodeCount())
	}
	solution, err := s.GetSolution()
	if err != nil {
		t.Fatal(err)
	}
	if solution.Moves != refSolution.Moves {
		t.Errorf("Zugfolge weicht ab: %q gegen %q", refSolution.Moves, solution.Moves)
	}
}

// RAM-Notbremse als Archiv-Auslöser: steht bei einer CompactTable die Verdopplung
// an und läge der berechnete Verbrauch danach über solver.RamLimitBytes, wechselt
// der Solver sie automatisch ins Archiv-Format statt zu verdoppeln - beide
// Richtungen wandern so nacheinander von selbst ins Archiv, Ergebnis bitgenau
func TestSolveAutoArchiveOnRamLimit(t *testing.T) {
	refSolver, refSolution := solveLevel(t, archiveTestLevel, 16)
	refNodes := refSolver.NodeCount()

	oldFactory, oldLimit := TableFactory, RamLimitBytes
	// Mini-Tabellen erzwingen frühe Verdopplungspunkte, das 1-Byte-Limit lässt
	// jede anstehende Verdopplung die Notbremse rechnerisch reißen
	TableFactory = func() PosTable { return newCompactTable(1 << 6) }
	RamLimitBytes = 1
	defer func() { TableFactory, RamLimitBytes = oldFactory, oldLimit }()

	field, err := soko.Parse(archiveTestLevel)
	if err != nil {
		t.Fatal(err)
	}
	s := New(field)
	// kleine Steps, damit die 90%-Prüfpunkte (Step-Anfang) dicht genug liegen
	for s.Step(1) {
	}

	forward, backward := s.TableInfos()
	if !forward.Archive || !backward.Archive {
		t.Fatalf("beide Tabellen müssen automatisch ins Archiv-Format gewechselt sein (V=%v R=%v)", forward.Archive, backward.Archive)
	}
	if s.NodeCount() != refNodes {
		t.Errorf("Knotenzahl weicht ab: Referenz=%d autoArchive=%d", refNodes, s.NodeCount())
	}
	stats := s.GetStats()
	if stats.FoundMoves != 16 {
		t.Fatalf("erwartete Lösungslänge 16, erhalten: %d", stats.FoundMoves)
	}
	solution, err := s.GetSolution()
	if err != nil {
		t.Fatal(err)
	}
	if solution.Moves != refSolution.Moves {
		t.Errorf("Zugfolge weicht ab: %q gegen %q", refSolution.Moves, solution.Moves)
	}
}

// Auslöse-Kriterium der automatischen Archiv-Konvertierung ist die Umkopier-Spitze
// der Verdopplung (ram + 2*Bytes, alte und neue Arrays leben gleichzeitig), nicht
// der Dauerzustand danach (ram + Bytes): auf dem 640-GB-Server riss eine 80-GB-
// Tabelle mit ihrer 160-GB-Spitze das physische RAM (OOM-Kill), obwohl der Wert
// nach der Verdopplung unter der Notbremse gelegen hätte
func TestAutoArchivePeakCriterion(t *testing.T) {
	oldLimit := RamLimitBytes
	defer func() { RamLimitBytes = oldLimit }()

	makeSolver := func() (*Solver, *CompactTable) {
		field, err := soko.Parse(archiveTestLevel)
		if err != nil {
			t.Fatal(err)
		}
		s := New(field)
		// Tabelle bis in das 90%-Fenster vor der Verdopplung füllen
		ct := newCompactTable(1 << 8)
		for i := int64(1); ct.count < ct.growLimit()/10*9; i++ {
			ct.Add(crc64.Value(i), 1)
		}
		s.forwardKnown = ct
		return s, ct
	}

	// Limit zwischen Dauerzustand (ram+Bytes) und Spitze (ram+2*Bytes):
	// hier muss konvertiert werden (das alte Kriterium hätte verdoppeln lassen)
	s, ct := makeSolver()
	ram := s.RamBytes()
	RamLimitBytes = ram + ct.Bytes()*3/2
	s.autoArchive(ram)
	if _, ok := s.forwardKnown.(*ArchiveTable); !ok {
		t.Fatal("Verdopplungs-Spitze über der Notbremse: Tabelle muss ins Archiv-Format wechseln")
	}

	// Gegenprobe: passt auch die Spitze noch unter das Limit, bleibt die CompactTable
	s, ct = makeSolver()
	ram = s.RamBytes()
	RamLimitBytes = ram + ct.Bytes()*2 + 1024
	s.autoArchive(ram)
	if _, ok := s.forwardKnown.(*CompactTable); !ok {
		t.Fatal("Spitze unter der Notbremse: Tabelle darf nicht konvertiert werden")
	}
}

// Delta-Pfad der RAM-Notbremse: bei einer bereits konvertierten Tabelle ersetzt
// der vorgezogene Merge die anstehende Delta-Verdopplung; die Meldung für die
// Statuszeile ist genau einmal abholbar
func TestAutoArchiveDeltaMergeOnRamLimit(t *testing.T) {
	oldMin, oldLimit := ArchiveDeltaMin, RamLimitBytes
	ArchiveDeltaMin, RamLimitBytes = 64, 1 // Mini-Delta-Schutz aus, Limit immer gerissen
	defer func() { ArchiveDeltaMin, RamLimitBytes = oldMin, oldLimit }()

	field, err := soko.Parse(archiveTestLevel)
	if err != nil {
		t.Fatal(err)
	}
	s := New(field)
	at := NewArchiveTable().(*ArchiveTable)
	s.forwardKnown = at
	// Delta direkt bis in das 90%-Fenster vor seiner nächsten Verdopplung füllen
	for i := int64(1); at.delta.count < at.delta.growLimit()/10*9; i++ {
		at.delta.Add(crc64.Value(i), 1)
	}
	filled := at.delta.count

	s.autoArchive(s.RamBytes())
	if at.archiveCount != filled || at.delta.count != 0 {
		t.Fatalf("Delta-Merge muss vorgezogen sein: archiveCount=%d (erwartet %d), delta=%d",
			at.archiveCount, filled, at.delta.count)
	}
	if s.TakeArchiveNote() == "" {
		t.Fatal("die vorgezogene Konvertierung muss eine Statuszeilen-Meldung hinterlassen")
	}
	if s.TakeArchiveNote() != "" {
		t.Fatal("die Meldung darf nur einmal abholbar sein")
	}
}

// Push-Optimierung: unter den zugoptimalen Lösungen wird die mit minimaler
// Schub-Zahl rekonstruiert (Webseiten-Bewertung mo/pu) - gleich viele Züge,
// gültige Kette, und nie mehr Schübe als die einfache Rekonstruktion
func TestSolutionBestPushes(t *testing.T) {
	s, plain := solveLevel(t, archiveTestLevel, 16)

	best, err := s.GetSolutionBestPushes()
	if err != nil {
		t.Fatal(err)
	}
	if len(best.Moves) != 16 {
		t.Fatalf("push-optimierte Lösung hat %d Züge statt 16", len(best.Moves))
	}
	if CountPushes(best.Moves) > CountPushes(plain.Moves) {
		t.Errorf("Push-Optimierung verschlechtert: %d > %d Schübe", CountPushes(best.Moves), CountPushes(plain.Moves))
	}

	// Gültigkeit: letzte Stellung muss gelöst sein
	field, err := soko.Parse(archiveTestLevel)
	if err != nil {
		t.Fatal(err)
	}
	work := field.Clone()
	work.SetState(&best.States[len(best.States)-1])
	if !work.IsSolved() {
		t.Errorf("letzte Stellung der push-optimierten Lösung ist nicht gelöst")
	}
	t.Logf("Schübe: einfach %d, push-optimiert %d (Anker: %d)", CountPushes(plain.Moves), CountPushes(best.Moves), len(s.meetAnchors))
}

// simulierte 64-Bit-Hash-Kollision (Geburtstagsparadoxon): der Crc einer echten
// Vorwärts-Stellung wird als Schein-Eintrag in die Rückwärtstabelle gepflanzt -
// ohne Meet-Verifikation würde die Suche eine viel zu kurze "Lösung" übernehmen
// und sich selbst beschneiden. Mit Verifikation wird die Schein-Verbindung
// verworfen (Zähler) und die echte Lösung trotzdem bewiesen.
func TestSolveCollisionRejected(t *testing.T) {
	_, refSolution := solveLevel(t, archiveTestLevel, 16)

	field, err := soko.Parse(archiveTestLevel)
	if err != nil {
		t.Fatal(err)
	}
	s := New(field)
	victim := refSolution.States[2] // echte Stellung wenige Züge nach dem Start
	s.backwardKnown.Add(victim.Crc, 1)
	for s.Step(1000000000) {
	}

	stats := s.GetStats()
	if stats.CollisionRejects == 0 {
		t.Error("die Schein-Verbindung wurde nicht als Kollision erkannt")
	}
	if stats.FoundMoves != 16 {
		t.Fatalf("erwartete echte Lösungslänge 16, erhalten: %d", stats.FoundMoves)
	}
	solution, err := s.GetSolution()
	if err != nil {
		t.Fatal(err)
	}
	if len(solution.Moves) != 16 {
		t.Fatalf("Rekonstruktion liefert %d Züge statt 16", len(solution.Moves))
	}
}

// Vergleichs-Benchmark zur CompactTable (siehe BenchmarkCompactTableMaxMemory):
// gleicher Workload - misst RAM-Ersparnis und Lookup-Preis des Archiv-Formats
// (je Iteration ein Treffer- und ein Fehlschlag-Lookup). Zwei Messpunkte des
// Merge-Zyklus: Delta fast voll (15,6M, teuerster Punkt) und Delta frisch
// geleert (16,9M, günstigster Punkt) - der Suchalltag liegt dazwischen
func BenchmarkArchiveTable(b *testing.B) {
	for _, entries := range []int{15_600_000, 16_900_000} {
		name := "deltaVoll"
		if entries > 16_000_000 {
			name = "deltaLeer"
		}
		b.Run(name, func(b *testing.B) {
			table := NewArchiveTable()
			seed := uint64(12345)
			for n := 0; n < entries; n++ {
				table.Add(nextCrc(&seed), uint16(n&30000))
			}
			archive := table.(*ArchiveTable)
			b.Logf("RAM: %d MB (Archiv: %d MB, %d Einträge, %d Bucket-Bits | Delta: %d Einträge)",
				table.Bytes()>>20, archive.archiveBytes>>20, archive.archiveCount, archive.bits, archive.delta.count)

			hitSeed, missSeed := uint64(12345), uint64(98765)
			var sum uint32
			b.ResetTimer()
			for n := 0; n < b.N; n++ {
				sum += uint32(table.Get(nextCrc(&hitSeed)))
				sum += uint32(table.Get(nextCrc(&missSeed)))
			}
			_ = sum
		})
	}
}
