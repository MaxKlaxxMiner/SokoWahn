package blocker

// Direct-Write-Modus: die Worker schreiben ihre Funde direkt atomar in die
// Stufen-Hashtabelle (first-wins), der serielle Merge entfällt komplett.
// Die Ergebnis-Sets bleiben identisch zum seriellen Ablauf, weil jeder Crc
// atomar genau einmal beansprucht wird und die Marker-Übergänge monoton sind
// (unbekannt -> pending -> good).

import (
	"sync"

	"goSokoWahnBrute/crc64"
	"goSokoWahnBrute/solver"

	"github.com/puzpuzpuz/xsync/v4"
)

// Hashtabelle mit atomaren Übergängen für direkt schreibende Worker
type DirectTable interface {
	solver.PosTable

	// trägt markerPending ein, falls der Crc unbekannt ist (true = neu beansprucht)
	ClaimPending(crc crc64.Value) bool

	// Marker-Übergang für MergeGoals: unbekannt -> pending (claimed),
	// pending -> good (promoted), good -> unverändert
	MergeTransition(crc crc64.Value) (claimed, promoted bool)
}

// aktiviert den Direct-Write-Modus mit der übergebenen Tabellen-Implementierung
// (nil = Standard: Vorfiltern + serieller Merge); wirkt ab der nächsten Stufe
func (b *Blocker) SetDirectTableFactory(factory func() DirectTable) {
	b.directFactory = factory
}

// --- Variante 1: puzpuzpuz/xsync (lock-freie concurrent Map) ---

type xsyncDirect struct {
	m *xsync.Map[uint64, uint16]
}

func NewXsyncDirect() DirectTable {
	return &xsyncDirect{m: xsync.NewMap[uint64, uint16]()}
}

func (t *xsyncDirect) Get(crc crc64.Value) uint16 {
	if depth, ok := t.m.Load(uint64(crc)); ok {
		return depth
	}
	return solver.DepthUnknown
}
func (t *xsyncDirect) Add(crc crc64.Value, depth uint16)    { t.m.Store(uint64(crc), depth) }
func (t *xsyncDirect) Update(crc crc64.Value, depth uint16) { t.m.Store(uint64(crc), depth) }
func (t *xsyncDirect) Len() int64                           { return int64(t.m.Size()) }

func (t *xsyncDirect) ClaimPending(crc crc64.Value) bool {
	_, loaded := t.m.LoadOrStore(uint64(crc), markerPending)
	return !loaded
}

func (t *xsyncDirect) MergeTransition(crc crc64.Value) (claimed, promoted bool) {
	t.m.Compute(uint64(crc), func(old uint16, loaded bool) (uint16, xsync.ComputeOp) {
		switch {
		case !loaded:
			claimed = true
			return markerPending, xsync.UpdateOp
		case old == markerPending:
			promoted = true
			return markerGood, xsync.UpdateOp
		default:
			return old, xsync.CancelOp
		}
	})
	return
}

// --- Variante 2: geshardete CompactTable (64 Shards mit je eigenem Mutex) ---

const shardCount = 64

// ein Shard: eigene Tabelle mit eigenem Mutex, gepolstert gegen False Sharing
type tableShard struct {
	mu sync.Mutex
	t  solver.PosTable
	_  [40]byte
}

type shardDirect struct {
	shards [shardCount]tableShard
}

func NewShardDirect() DirectTable {
	t := &shardDirect{}
	for i := range t.shards {
		t.shards[i].t = solver.NewCompactTable()
	}
	return t
}

// Shard aus den oberen Crc-Bits (die unteren Bits nutzt die CompactTable als Bucket)
func (t *shardDirect) shard(crc crc64.Value) *tableShard {
	return &t.shards[uint64(crc)>>58]
}

func (t *shardDirect) Get(crc crc64.Value) uint16 {
	s := t.shard(crc)
	s.mu.Lock()
	depth := s.t.Get(crc)
	s.mu.Unlock()
	return depth
}

func (t *shardDirect) Add(crc crc64.Value, depth uint16) {
	s := t.shard(crc)
	s.mu.Lock()
	s.t.Add(crc, depth)
	s.mu.Unlock()
}

func (t *shardDirect) Update(crc crc64.Value, depth uint16) {
	t.Add(crc, depth)
}

func (t *shardDirect) Len() int64 {
	var sum int64
	for i := range t.shards {
		t.shards[i].mu.Lock()
		sum += t.shards[i].t.Len()
		t.shards[i].mu.Unlock()
	}
	return sum
}

func (t *shardDirect) ClaimPending(crc crc64.Value) bool {
	s := t.shard(crc)
	s.mu.Lock()
	claimed := s.t.Get(crc) == solver.DepthUnknown
	if claimed {
		s.t.Add(crc, markerPending)
	}
	s.mu.Unlock()
	return claimed
}

func (t *shardDirect) MergeTransition(crc crc64.Value) (claimed, promoted bool) {
	s := t.shard(crc)
	s.mu.Lock()
	switch s.t.Get(crc) {
	case solver.DepthUnknown:
		s.t.Add(crc, markerPending)
		claimed = true
	case markerPending:
		s.t.Update(crc, markerGood)
		promoted = true
	}
	s.mu.Unlock()
	return
}
