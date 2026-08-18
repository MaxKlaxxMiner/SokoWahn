package solver

import "goSokoWahnBrute/soko"

// Laufzeit-Umschalter für die Deadlock-Filter der Suche (TUI-Tasten).
// Die Quell-Filter liegen am Basisfeld (base.Blocker()/base.Rules()); die Schalter
// setzen sie auf dem Arbeitsfeld und verwerfen die Worker-Kontexte, die sich beim
// nächsten parallelen Batch neu vom Arbeitsfeld klonen (gleiche Mechanik wie
// SetWorkers - damit ist das Umschalten auch mitten in der Suche gefahrlos).
// Achtung: Umschalten ändert natürlich die Knotenzahlen des Laufs - für
// Vergleiche mit den verankerten Referenzwerten die Schalter nicht anfassen.

// schaltet den Blocker-Filter (vorwärts und rückwärts) an/aus;
// ohne vorhandenen Blocker bleibt der Filter aus
func (s *Solver) SetBlockerEnabled(on bool) {
	if on {
		s.work.SetBlocker(s.base.Blocker())
		s.work.SetBlockerBackward(s.base.Blocker())
	} else {
		s.work.SetBlocker(nil)
		s.work.SetBlockerBackward(nil)
	}
	s.workers = nil // beim nächsten parallelen Batch mit dem neuen Filterstand neu klonen
}

// gibt an, ob der Blocker-Filter aktiv ist
func (s *Solver) BlockerEnabled() bool {
	return s.work.Blocker() != nil
}

// schaltet den regelbasierten Live-Deadlock-Filter an/aus;
// ohne vorbereitete Regeln (base ohne SetRules) bleibt der Filter aus
func (s *Solver) SetRulesEnabled(on bool) {
	if on {
		if r := s.base.Rules(); r != nil {
			s.work.SetRules(r.Clone())
			s.work.SetRulesBackward(r.Clone())
		}
	} else {
		s.work.SetRules(nil)
		s.work.SetRulesBackward(nil)
	}
	s.workers = nil
}

// gibt an, ob der Regel-Filter aktiv ist
func (s *Solver) RulesEnabled() bool {
	return s.work.Rules() != nil
}

// schaltet das Ziel-Matching (Regel-Stufe 2) an/aus; der Schalter lebt auf dem
// Quell-Filter des Basisfeldes und wird über frische Klone aufs Arbeitsfeld
// übertragen (wirkungslos, solange keine Regel-Quelle gesetzt ist)
func (s *Solver) SetMatchEnabled(on bool) {
	r := s.base.Rules()
	if r == nil {
		return
	}
	r.MatchEnabled = on
	s.SetRulesEnabled(s.RulesEnabled()) // Arbeits-Klone mit dem neuen Stand neu erzeugen
}

// gibt an, ob das Ziel-Matching (Regel-Stufe 2) aktiv ist
func (s *Solver) MatchEnabled() bool {
	r := s.base.Rules()
	return r != nil && r.MatchEnabled
}

// gibt den Regel-Filter des Basisfeldes zurück (nil = keiner);
// die Statistik (Stats) ist über alle Klone geteilt und hier ablesbar
func (s *Solver) Rules() *soko.Rules {
	return s.base.Rules()
}
