package soko

import (
	"math/bits"
	"sync/atomic"
)

// Regelbasierter Live-Deadlock-Filter: erkennt strukturelle Deadlocks mit
// beliebig vielen Kisten, die dem k-Steiner-Blocker wegen seiner Kistenzahl-Grenze
// entgehen. Stufe 1: Freeze-Fixpunkt + Closed-Diagonal (Vorbilder: Festival,
// JSoko). Stufe 2: Ziel-Matching mit eingefrorenen Ziel-Kisten als Wänden
// (rulesMatch.go, JSoko-Vorbild).
//
// Wichtig fürs Verständnis:
//   - Es werden NUR beweisbar tote Stellungen verworfen (keine Dominanz-Prunings) -
//     die zugoptimale Suche bleibt korrekt, nur die Knotenzahlen ändern sich.
//   - Der Filter gehört ausschließlich in die Vorwärtssuche: rückwärts erreichte
//     Stellungen sind per Konstruktion vorwärts lösbar (die Pull-Folge rückwärts
//     ist ein gültiger Schub-Pfad zum Ziel), die Regeln könnten dort nie feuern.
//   - Der Blocker-Stufenbau filtert seine Vorwärts-Phasen adaptiv mit denselben
//     Regeln (siehe blocker.RulesPatternThreshold), die Rückwärtswelle nie.

// unveränderliche Vorberechnung, von allen Klonen geteilt (nur Lese-Zugriffe)
type rulesShared struct {
	walkEof     Wpos
	walkLeft    []Wpos
	walkRight   []Wpos
	walkUp      []Wpos
	walkDown    []Wpos
	width       int
	height      int
	fieldData   []byte   // '#' = Wand (absolute Positionen)
	fieldToWpos []Wpos   // absolute Position -> Wpos (walkEof = unbegehbar)
	wposToField []int    // Wpos -> absolute Position
	goalBits    []uint64 // Zielfelder als Bitmaske über die begehbaren Felder
	deadBits    []uint64 // tote Felder: von dort erreicht keine Kiste mehr ein Ziel (per Schieben)
	goals       []Wpos   // Zielfelder in fester Reihenfolge (Bit-Index der Matching-Masken)
	goalWords   int      // Wortbreite der Ziel-Bitmasken (je 64 Ziele ein uint64)

	// --- Spiegelwelt für die Rückwärtssuche (Pull-Freeze) ---
	startBits    []uint64 // Start-Kistenfelder als Bitmaske
	pullDeadBits []uint64 // pull-tote Felder: von dort erreicht keine Kiste per Ziehen ein Startfeld

	// --- Statistik (atomar: alle Klone/Worker zählen gemeinsam) ---
	freezeKills     atomic.Uint64 // Freeze-Regel hat eine Stellung verworfen
	diagonalKills   atomic.Uint64 // Diagonal-Regel hat eine Stellung verworfen
	matchKills      atomic.Uint64 // Ziel-Matching-Regel hat eine Stellung verworfen
	pullDeadKills   atomic.Uint64 // Kiste auf pull-totem Feld (rückwärts, O(1))
	pullFreezeKills atomic.Uint64 // Pull-Freeze-Regel hat eine Rückwärts-Stellung verworfen
}

// Momentaufnahme der Regel-Statistik
type RuleStats struct {
	FreezeKills     uint64 // von der Freeze-Regel verworfene Stellungen
	DiagonalKills   uint64 // von der Diagonal-Regel verworfene Stellungen
	MatchKills      uint64 // von der Ziel-Matching-Regel verworfene Stellungen
	PullDeadKills   uint64 // Kiste auf pull-totem Feld verworfen (rückwärts)
	PullFreezeKills uint64 // von der Pull-Freeze-Regel verworfene Rückwärts-Stellungen
}

// Live-Regel-Filter; jede Field-Instanz braucht ihren eigenen (Scratch-Puffer),
// die Vorberechnung wird geteilt (Clone)
type Rules struct {
	shared *rulesShared

	// Einzelschalter (Freeze + Diagonale = Stufe 1); primär für Tests und
	// Messungen, die Stufen-Umschaltung von außen läuft über SetRules(nil/rules)
	FreezeEnabled   bool
	DiagonalEnabled bool

	// Stufe 2: Ziel-Matching mit eingefrorenen Ziel-Kisten als Wänden
	// (rulesMatch.go); setzt den Freeze-Fixpunkt voraus (FreezeEnabled)
	MatchEnabled bool

	work []uint64 // Scratch-Maske für den Freeze-Fixpunkt

	// --- Scratch und Cache des Ziel-Matchings (rulesMatch.go, lazy angelegt) ---
	matchCache map[string][]uint64 // Erreichbarkeits-Masken je eingefrorener Kistenmenge
	matchKey   []byte              // Schlüssel-Puffer (exakte Masken-Bytes, kein Hash)
	matchBoxes []Wpos              // bewegliche Kisten der aktuellen Prüfung
	matchOwner []int16             // Matching: zugeordnete Kiste je Ziel (-1 = frei)
	matchSeen  []uint64            // Matching: je Augmentation besuchte Ziele
	reachSeen  []bool              // BFS-Markierungen der Erreichbarkeits-Berechnung
	reachQueue []Wpos              // BFS-Queue der Erreichbarkeits-Berechnung
}

// erstellt den Regel-Filter für ein Spielfeld (Vorberechnung: tote Felder per
// Rückwärts-Erreichbarkeit von den Zielen, wie die "simple deadlock squares"
// von JSoko/YASC - entspricht inhaltlich den 1-Steiner-Blockern, aber unabhängig
// vom Blocker nutzbar)
func NewRules(f *Field) *Rules {
	sh := &rulesShared{
		walkEof:     f.walkEof,
		walkLeft:    f.walkLeft,
		walkRight:   f.walkRight,
		walkUp:      f.walkUp,
		walkDown:    f.walkDown,
		width:       f.width,
		height:      f.height,
		fieldData:   f.fieldData,
		fieldToWpos: f.fieldToWpos,
		wposToField: f.wposToField,
		goalBits:     make([]uint64, len(f.boxBits)),
		deadBits:     make([]uint64, len(f.boxBits)),
		goals:        f.goals,
		goalWords:    (len(f.goals) + 63) / 64,
		startBits:    make([]uint64, len(f.boxBits)),
		pullDeadBits: make([]uint64, len(f.boxBits)),
	}

	for _, g := range f.goals {
		sh.goalBits[g>>6] |= 1 << (g & 63)
	}
	for _, b := range f.initBoxes {
		sh.startBits[b>>6] |= 1 << (b & 63)
	}

	// lebendige Felder: von dort kann eine einzelne Kiste (bei leerem Feld) noch ein
	// Ziel erreichen. BFS von den Zielen rückwärts über Pull-Züge: Kiste auf q kann
	// zum lebendigen Feld a geschoben werden, wenn der Spieler hinter q Platz hat.
	alive := make([]bool, f.walkEof)
	queue := make([]Wpos, 0, f.walkEof)
	for _, g := range f.goals {
		if !alive[g] {
			alive[g] = true
			queue = append(queue, g)
		}
	}
	mark := func(q, pp Wpos) { // q = Kistenfeld, pp = Spielerfeld hinter der Kiste
		if q < f.walkEof && pp < f.walkEof && !alive[q] {
			alive[q] = true
			queue = append(queue, q)
		}
	}
	for len(queue) > 0 {
		a := queue[len(queue)-1]
		queue = queue[:len(queue)-1]
		if q := f.walkRight[a]; q < f.walkEof {
			mark(q, f.walkRight[q]) // Schub nach links auf a: Kiste stand rechts, Spieler rechts daneben
		}
		if q := f.walkLeft[a]; q < f.walkEof {
			mark(q, f.walkLeft[q]) // Schub nach rechts auf a
		}
		if q := f.walkDown[a]; q < f.walkEof {
			mark(q, f.walkDown[q]) // Schub nach oben auf a
		}
		if q := f.walkUp[a]; q < f.walkEof {
			mark(q, f.walkUp[q]) // Schub nach unten auf a
		}
	}
	for pos := Wpos(0); pos < f.walkEof; pos++ {
		if !alive[pos] {
			sh.deadBits[pos>>6] |= 1 << (pos & 63)
		}
	}

	// pull-lebendige Felder (Spiegelwelt der Rückwärtssuche): von dort kann eine Kiste
	// per Ziehen (bei leerem Feld) noch ein Start-Kistenfeld erreichen. BFS von den
	// Startfeldern: Kiste auf q kann zum lebendigen Feld a gezogen werden, wenn
	// HINTER a (in Zugrichtung) Platz für den Spieler ist.
	pullAlive := make([]bool, f.walkEof)
	queue = queue[:0]
	for _, b := range f.initBoxes {
		if !pullAlive[b] {
			pullAlive[b] = true
			queue = append(queue, b)
		}
	}
	pullMark := func(q, beyond Wpos) { // q = Kistenfeld, beyond = Spielerfeld hinter dem Ziel
		if q < f.walkEof && beyond < f.walkEof && !pullAlive[q] {
			pullAlive[q] = true
			queue = append(queue, q)
		}
	}
	for len(queue) > 0 {
		a := queue[len(queue)-1]
		queue = queue[:len(queue)-1]
		if beyond := f.walkLeft[a]; beyond < f.walkEof {
			pullMark(f.walkRight[a], beyond) // Zug nach links auf a: Kiste stand rechts, Spieler zieht von a nach links weiter
		}
		if beyond := f.walkRight[a]; beyond < f.walkEof {
			pullMark(f.walkLeft[a], beyond) // Zug nach rechts auf a
		}
		if beyond := f.walkUp[a]; beyond < f.walkEof {
			pullMark(f.walkDown[a], beyond) // Zug nach oben auf a
		}
		if beyond := f.walkDown[a]; beyond < f.walkEof {
			pullMark(f.walkUp[a], beyond) // Zug nach unten auf a
		}
	}
	for pos := Wpos(0); pos < f.walkEof; pos++ {
		if !pullAlive[pos] {
			sh.pullDeadBits[pos>>6] |= 1 << (pos & 63)
		}
	}

	return &Rules{
		shared:          sh,
		FreezeEnabled:   true,
		DiagonalEnabled: true,
		MatchEnabled:    true,
		work:            make([]uint64, len(f.boxBits)),
	}
}

// erstellt eine Kopie mit eigenem Scratch-Puffer (die Vorberechnung wird geteilt);
// nötig für parallele Worker - ein Rules-Objekt ist nicht threadsicher
func (r *Rules) Clone() *Rules {
	return &Rules{
		shared:          r.shared,
		FreezeEnabled:   r.FreezeEnabled,
		DiagonalEnabled: r.DiagonalEnabled,
		MatchEnabled:    r.MatchEnabled,
		work:            make([]uint64, len(r.work)),
	}
}

// liest die (über alle Klone gemeinsame) Regel-Statistik
func (r *Rules) Stats() RuleStats {
	sh := r.shared
	return RuleStats{
		FreezeKills:     sh.freezeKills.Load(),
		DiagonalKills:   sh.diagonalKills.Load(),
		MatchKills:      sh.matchKills.Load(),
		PullDeadKills:   sh.pullDeadKills.Load(),
		PullFreezeKills: sh.pullFreezeKills.Load(),
	}
}

// prüft die Stellung nach einem Kistenschub (box = neue Position der geschobenen
// Kiste, player = Spielerposition nach dem Schub). false = beweisbarer Deadlock,
// die Stellung wird verworfen.
func (r *Rules) CheckPush(player, box Wpos, boxBits []uint64) bool {
	// Bewusst KEIN direkter Totfeld-Check der geschobenen Kiste: das ist exakt das
	// 1-Steiner-Wissen, und die Blocker-Stufen 1-2 sind Betriebsvoraussetzung
	// (Millisekunden Rechenzeit, egal wie groß das Level). Die Regeln sollen den
	// Blocker ergänzen, nicht doppeln. Die Totfeld-Maske (deadBits) bleibt trotzdem
	// wichtig: als Verschärfung im Freeze-Fixpunkt (beidseitig tote Achse = blockiert).
	if r.FreezeEnabled {
		frozen, ok := r.checkFreeze(box, boxBits)
		if !ok {
			r.shared.freezeKills.Add(1)
			return false
		}
		// Stufe 2 direkt im Anschluss: frozen zeigt in den work-Puffer des
		// Fixpunkts und ist nur bis zum nächsten Freeze-Check gültig
		if frozen != nil && !r.checkGoalMatch(frozen, boxBits) {
			r.shared.matchKills.Add(1)
			return false
		}
	}
	if r.DiagonalEnabled && r.isDiagonalDeadlock(player, box, boxBits) {
		r.shared.diagonalKills.Add(1)
		return false
	}
	return true
}

// prüft die Kisten-Konfiguration nach einem Rückwärtszug (box = neue Position der
// gezogenen Kiste). false = Konfiguration ist vorwärts beweisbar unerreichbar,
// alle zugehörigen Rückwärts-Stellungen entfallen.
//
// Spiegelbild von CheckPush: die Rückwärtssuche stirbt nicht an unlösbaren, sondern
// an vorwärts UNERREICHBAREN Stellungen. Wäre die Konfiguration vorwärts erreichbar,
// gäbe es (Partie rückwärts abgespielt) eine Pull-Folge zur Startkonfiguration -
// eine pull-eingefrorene Kiste abseits der Startfelder widerlegt genau das.
// Die Prüfung ist spielerunabhängig und läuft deshalb einmal je Pull-Hypothese
// (vor dem Pose-Flood), nicht je emittierter Stellung.
func (r *Rules) CheckPull(box Wpos, boxBits []uint64) bool {
	if !r.FreezeEnabled {
		return true
	}
	// billigster Check zuerst: von einem pull-toten Feld erreicht die Kiste per
	// Ziehen nie mehr ein Startfeld (Startfelder sind per Konstruktion nie pull-tot)
	if r.shared.pullDeadAt(box) {
		r.shared.pullDeadKills.Add(1)
		return false
	}
	if !r.checkPullFreeze(box, boxBits) {
		r.shared.pullFreezeKills.Add(1)
		return false
	}
	return true
}

// Pull-Freeze-Check (Spiegel des Freeze-Fixpunkts): alle naiv ziehbaren Kisten
// iterativ von der Arbeitsmaske nehmen; der Rest lässt sich gegenseitig nie mehr
// auseinanderziehen. Steht davon eine Kiste abseits der Startfelder, kann die
// Startkonfiguration nicht mehr hergestellt werden -> vorwärts unerreichbar.
// Der Early-Exit über die gezogene Kiste ist verlustfrei (Spiegelargument):
// ein Cluster friert nur durch den Zug einer Kiste ein, die selbst mit einfriert;
// die Ziel-Konfiguration selbst kann bei lösbaren Levels nie betroffen sein
// (sie ist vorwärts erreichbar).
func (r *Rules) checkPullFreeze(box Wpos, boxBits []uint64) bool {
	sh := r.shared
	if sh.pullMovableNaive(box, boxBits) {
		return true // häufigster Fall: die gezogene Kiste ist frei ziehbar
	}

	work := r.work[:len(boxBits)]
	copy(work, boxBits)
	work[sh.walkEof>>6] &^= 1 << (sh.walkEof & 63) // Sentinel-Bit ausblenden

	for changed := true; changed; {
		changed = false
		for w, word := range work {
			for word != 0 {
				pos := Wpos(w<<6 + bits.TrailingZeros64(word))
				word &= word - 1
				if sh.pullMovableNaive(pos, work) {
					work[pos>>6] &^= 1 << (pos & 63)
					changed = true
				}
			}
		}
	}

	// verbleibende (pull-eingefrorene) Kisten: alle auf Start-Kistenfeldern?
	for w, word := range work {
		if word&^sh.startBits[w] != 0 {
			return false // eingefrorene Kiste abseits der Startfelder -> vorwärts unerreichbar
		}
	}
	return true
}

// naiv ziehbar (allmächtiger Spieler, konservativ): es gibt eine Richtung mit zwei
// freien Feldern (Lande- und Spielerfeld), deren Landefeld nicht pull-tot ist
// (ein Zug auf ein pull-totes Feld strandet die Kiste und zählt nicht als Ausweg)
func (sh *rulesShared) pullMovableNaive(pos Wpos, mask []uint64) bool {
	if n := sh.walkLeft[pos]; sh.freeAt(n, mask) && sh.freeAt(sh.walkLeft[n], mask) && !sh.pullDeadAt(n) {
		return true
	}
	if n := sh.walkRight[pos]; sh.freeAt(n, mask) && sh.freeAt(sh.walkRight[n], mask) && !sh.pullDeadAt(n) {
		return true
	}
	if n := sh.walkUp[pos]; sh.freeAt(n, mask) && sh.freeAt(sh.walkUp[n], mask) && !sh.pullDeadAt(n) {
		return true
	}
	if n := sh.walkDown[pos]; sh.freeAt(n, mask) && sh.freeAt(sh.walkDown[n], mask) && !sh.pullDeadAt(n) {
		return true
	}
	return false
}

// pull-totes Feld (nur für gültige Wpos < walkEof aufrufen)
func (sh *rulesShared) pullDeadAt(p Wpos) bool {
	return sh.pullDeadBits[p>>6]&(1<<(p&63)) != 0
}

// Freeze-Deadlock-Check (Festival-Stil, deadlock.cpp is_freeze_deadlock):
// alle naiv schiebbaren Kisten iterativ vom Brett nehmen; was übrig bleibt, ist
// gegenseitig dauerhaft blockiert. Steht eine der verbliebenen Kisten abseits
// der Ziele, ist die Stellung bewiesen unlösbar.
//
// Der Early-Exit über die geschobene Kiste ist verlustfrei: ein Cluster friert
// immer durch den Schub einer Kiste ein, die selbst mit einfriert (eine naiv
// bewegliche Kiste kann per Definition keine Nachbarkiste dauerhaft blockieren).
// Start-eingefrorene Cluster behandelt bereits der Parse-Filter (freeze.go).
//
// Rückgabe: ok=false bedeutet Freeze-Deadlock. frozen liefert die Menge der
// eingefrorenen Ziel-Kisten für das Ziel-Matching (Stufe 2, Alias auf den
// work-Puffer, nur bis zum nächsten Check gültig); nil, wenn das Matching aus
// ist oder nichts eingefroren ist. Mit aktivem Matching entfällt der Early-Exit,
// sobald irgendeine Kiste auf einem Ziel steht: auch der Schub einer frei
// beweglichen Kiste muss dann gegen die BESTEHENDEN eingefrorenen Ziel-Kisten
// geprüft werden (die Menge selbst kann dabei nicht wachsen, aber die neue
// Kistenposition kann hinter den eingefrorenen Wänden stranden).
func (r *Rules) checkFreeze(box Wpos, boxBits []uint64) (frozen []uint64, ok bool) {
	sh := r.shared
	matchActive := r.MatchEnabled && sh.anyBoxOnGoal(boxBits)
	if !matchActive && sh.movableNaive(box, boxBits) {
		return nil, true // häufigster Fall: die geschobene Kiste ist frei beweglich
	}

	work := r.work[:len(boxBits)]
	copy(work, boxBits)
	work[sh.walkEof>>6] &^= 1 << (sh.walkEof & 63) // Sentinel-Bit ausblenden

	for changed := true; changed; {
		changed = false
		for w, word := range work {
			for word != 0 {
				pos := Wpos(w<<6 + bits.TrailingZeros64(word))
				word &= word - 1
				if sh.movableNaive(pos, work) {
					work[pos>>6] &^= 1 << (pos & 63)
					changed = true
				}
			}
		}
	}

	// verbleibende (eingefrorene) Kisten: alle auf Zielfeldern?
	frozenAny := false
	for w, word := range work {
		if word&^sh.goalBits[w] != 0 {
			return nil, false // eingefrorene Kiste abseits der Ziele -> Deadlock
		}
		if word != 0 {
			frozenAny = true
		}
	}
	if !matchActive || !frozenAny {
		return nil, true
	}
	return work, true
}

// steht mindestens eine Kiste auf einem Zielfeld?
func (sh *rulesShared) anyBoxOnGoal(mask []uint64) bool {
	for w, word := range mask {
		if word&sh.goalBits[w] != 0 {
			return true
		}
	}
	return false
}

// naiv schiebbar (allmächtiger Spieler, konservativ): es gibt eine Achse, deren
// beide Nachbarfelder frei sind und die nicht beidseitig tot ist (ein Schub auf
// ein totes Feld wäre selbst eine verlorene Stellung und zählt nicht als Ausweg)
func (sh *rulesShared) movableNaive(pos Wpos, mask []uint64) bool {
	if l, r := sh.walkLeft[pos], sh.walkRight[pos]; sh.freeAt(l, mask) && sh.freeAt(r, mask) && !(sh.deadAt(l) && sh.deadAt(r)) {
		return true
	}
	if u, d := sh.walkUp[pos], sh.walkDown[pos]; sh.freeAt(u, mask) && sh.freeAt(d, mask) && !(sh.deadAt(u) && sh.deadAt(d)) {
		return true
	}
	return false
}

// begehbar und kistenfrei
func (sh *rulesShared) freeAt(p Wpos, mask []uint64) bool {
	return p < sh.walkEof && mask[p>>6]&(1<<(p&63)) == 0
}

// totes Feld (nur für gültige Wpos < walkEof aufrufen)
func (sh *rulesShared) deadAt(p Wpos) bool {
	return sh.deadBits[p>>6]&(1<<(p&63)) != 0
}
