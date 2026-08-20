// Einstieg der Debug-GUI: Aufbau und Verhalten nach dem Vorbild des
// C#-FormDebugger - Räume-Liste links, Zustände über Varianten daneben,
// Spielfeld rechts mit Effort-Zeile, Aktions-Buttons am Rand (folgen ab M3).
// Die Varianten-Liste ist zustandsgetrieben und in Sektionen gegliedert
// (-- Starts --, -- Portal 1r --, ...) wie im Original; Varianten werden
// als Animation abgespielt, per Cursortasten lässt sich die Animation anhalten
// und je Kistenschub durchsteppen (wie die Lösungsanzeige in brute).

import './style.css';
import {
  Field,
  Page,
  Portal,
  RoomDetail,
  RoomSummary,
  StateItem,
  Summary,
  VariantItem,
  getJSON,
  postJSON,
} from './api';
import { VirtualList } from './vlist';
import { AnimFrame, FieldCanvas, OPPOSITE } from './canvas';

const $ = (id: string) => document.getElementById(id)!;
const fmt = (n: number) => n.toLocaleString('de-DE');

// eine Zeile der Varianten-Liste (Sektions-Kopf, Variante oder Kisten-Schub "Variant B")
interface VRow {
  text: string;
  head?: boolean; // Überschrift/Hinweis - nicht wählbar
  variant?: VariantItem;
  entry?: Portal | null; // eingehendes Portal der Variante (null = Startvariante)
  boxSwap?: { portal: Portal; newState: number };
}

// Abschnitt der Varianten-Liste: feste Zeilen oder ein lazy geladener Span
interface Segment {
  count: number;
  get(local: number, take: number): Promise<VRow[]> | VRow[];
}

let field: Field;
let canvas: FieldCanvas;
let roomsList: VirtualList<RoomSummary>;
let statesList: VirtualList<StateItem>;
let variantsList: VirtualList<VRow>;
let currentRoom: RoomDetail | null = null;
let currentState: StateItem | null = null;
let variantsFetch: ((offset: number, limit: number) => Promise<Page<VRow>>) | null = null;
let networkEffort = ''; // Effort des ganzen Netzwerks (Anzeige ohne Auswahl)
let levelSeq = -1; // Level-Wechsel-Zähler des Servers (Strg+V kann das Feld ersetzen)
let clearSelection = false; // nach einem Reset (Entf) die Auswahl nicht wiederherstellen
let mergeBusy = false;
let selectionGen = 0; // entwertet überholte Auswahl-Berechnungen (Drag erzeugt viele)
const roomCache = new Map<number, RoomDetail>(); // Raum-Details je Index (bis zum nächsten Merge)

function showError(err: unknown): void {
  const box = $('error');
  box.className = '';
  box.textContent = 'Fehler: ' + (err instanceof Error ? err.message : String(err));
  box.hidden = false;
  setTimeout(() => (box.hidden = true), 6000);
}

function showStatus(msg: string): void {
  const box = $('error');
  box.className = 'ok';
  box.textContent = msg;
  box.hidden = false;
  setTimeout(() => (box.hidden = true), 6000);
}

async function roomDetail(index: number): Promise<RoomDetail> {
  let detail = roomCache.get(index);
  if (!detail) {
    detail = await getJSON<RoomDetail>(`/api/rooms/${index}`);
    roomCache.set(index, detail);
  }
  return detail;
}

// ---------- Raumwahl ----------

// reagiert auf jede Auswahl-Änderung: Listen folgen dem aktiven Raum,
// die Effort-Zeile zeigt das Produkt der Variantenzahlen der Auswahl
async function handleSelection(selection: number[], active: number, fromList: boolean): Promise<void> {
  const gen = ++selectionGen;
  updateMergeButton(selection);

  if (active < 0) {
    currentRoom = null;
    currentState = null;
    variantsFetch = null;
    $('statesHead').textContent = '-- States --';
    statesList.reset(0);
    variantsList.reset(0);
    $('info').textContent = 'Effort: ' + networkEffort;
    return;
  }

  try {
    const detail = await roomDetail(active);
    if (gen !== selectionGen) return; // Auswahl hat sich inzwischen geändert
    currentRoom = detail;
    currentState = null;
    variantsFetch = null;
    canvas.setActivePortals(detail.portalList);
    if (!fromList) roomsList.highlight(active);

    $('statesHead').textContent = `-- Room ${active + 1} [${fmt(detail.states)}] --`;
    statesList.reset(detail.states);
    variantsList.reset(0);

    // wie das Original: Effort der Raum-Auswahl = Produkt der Variantenzahlen
    // (Räume ohne Varianten zählen nicht mit, wie im Netzwerk-Effort);
    // dazu die Summe der bewiesenen Pflicht-Minima der Auswahl
    let product = 1n;
    let minMoves = 0;
    for (const idx of selection) {
      const room = await roomDetail(idx);
      if (room.variants > 0) product *= BigInt(room.variants);
      minMoves += room.minMoves;
    }
    if (gen !== selectionGen) return;
    const rooms = selection.length > 1 ? ` (${selection.length} Räume)` : '';
    $('info').textContent =
      'Effort: ' + product.toLocaleString('de-DE') + rooms + ' - min moves: ' + fmt(minMoves);
  } catch (err) {
    showError(err);
  }
}

function updateMergeButton(selection: number[]): void {
  ($('mergeBtn') as HTMLButtonElement).disabled = mergeBusy || selection.length < 2;
  ($('optimizeBtn') as HTMLButtonElement).disabled = mergeBusy || selection.length < 1;
  updateSnapshotButtons();
}

// ---------- Aktionen (M3) ----------

async function doMerge(): Promise<void> {
  const selection = canvas.getSelection();
  if (mergeBusy || selection.length < 2) return;
  try {
    await postJSON<{ started: boolean }>('/api/merge', { rooms: selection });
  } catch (err) {
    showError(err);
  }
}

async function doOptimize(): Promise<void> {
  const selection = canvas.getSelection();
  if (mergeBusy || selection.length < 1) return;
  // Max-Moves-Budget (leer = kein Limit): verifizierte obere Schranke der
  // Gesamtlösung, der Optimizer kappt Nutzungen über dem Raum-Budget
  const maxMoves = Math.max(0, Number(($('maxMoves') as HTMLInputElement).value) || 0);
  try {
    await postJSON<{ started: boolean }>('/api/optimize', { rooms: selection, maxMoves });
  } catch (err) {
    showError(err);
  }
}

// ---------- universelles Strg+V (wie in brute) ----------

// Der Server klassifiziert den Text: Level-Nummer/URL oder Levelnotation
// ersetzen das Spielfeld (Hintergrund-Job, Reload kommt über den Progress-
// Stream); eine LURD-Zugfolge wird als Lösung geprüft und setzt bei Erfolg
// das max-moves-Feld auf die Zuglänge.
async function doPaste(text: string): Promise<void> {
  try {
    const res = await postJSON<{ kind: string; moves?: number }>('/api/paste', { text });
    if (res.kind === 'solution' && res.moves !== undefined) {
      ($('maxMoves') as HTMLInputElement).value = String(res.moves);
      showStatus(`Lösung passt: ${fmt(res.moves)} Züge - max moves gesetzt`);
    }
  } catch (err) {
    showError(err);
  }
}

// ---------- Snapshots (Buttons unter "Solver...") ----------

// ein Snapshot des aktuellen Levels (vom Backend in temp/room-snapshots verwaltet)
interface SnapshotItem {
  name: string;
  effort: string; // Effort-String zum Speicherzeitpunkt
  size: number; // Dateigröße in Bytes
}

let snapshots: SnapshotItem[] = [];
let selectedSnapshot: SnapshotItem | null = null;
let snapshotsList: VirtualList<SnapshotItem>;

function updateSnapshotButtons(): void {
  ($('snapSaveBtn') as HTMLButtonElement).disabled = mergeBusy;
  ($('snapLoadBtn') as HTMLButtonElement).disabled = mergeBusy || !selectedSnapshot;
  ($('snapDeleteBtn') as HTMLButtonElement).disabled = mergeBusy || !selectedSnapshot;
}

async function loadSnapshots(): Promise<void> {
  try {
    const res = await getJSON<{ items: SnapshotItem[] }>('/api/snapshots');
    snapshots = res.items;
    selectedSnapshot = null;
    updateSnapshotButtons();
    snapshotsList.reset(snapshots.length);
  } catch (err) {
    showError(err);
  }
}

async function doSnapshotSave(): Promise<void> {
  if (mergeBusy) return;
  try {
    await postJSON<{ started: boolean }>('/api/snapshots', {});
  } catch (err) {
    showError(err);
  }
}

async function doSnapshotLoad(): Promise<void> {
  if (mergeBusy || !selectedSnapshot) return;
  try {
    await postJSON<{ started: boolean }>('/api/snapshots/load', { name: selectedSnapshot.name });
  } catch (err) {
    showError(err);
  }
}

async function doSnapshotDelete(): Promise<void> {
  if (!selectedSnapshot) return;
  try {
    await postJSON<{ ok: boolean }>('/api/snapshots/delete', { name: selectedSnapshot.name });
    await loadSnapshots();
  } catch (err) {
    showError(err);
  }
}

// Entf-Taste: gemergte Räume der Auswahl auf ihre Ein-Feld-Start-Räume
// zurücksetzen (Fehlgriff beim Mergen), ohne das ganze Level neu zu laden
async function doReset(): Promise<void> {
  const selection = canvas.getSelection();
  if (mergeBusy || selection.length < 1) return;
  try {
    await postJSON<{ started: boolean }>('/api/reset', { rooms: selection });
    // die zurückgesetzten Räume nach dem Reload NICHT wieder selektieren -
    // sonst bliebe je gelöschtem Raum ein 1-Feld-Raum markiert stehen
    clearSelection = true;
  } catch (err) {
    showError(err);
  }
}

async function doStop(): Promise<void> {
  try {
    await postJSON<{ stopping: boolean }>('/api/stop', {});
  } catch (err) {
    showError(err);
  }
}

// Fortschritts-Stream des Servers: Status-Text und gelbe Markierung der
// gerade bearbeiteten Räume, wie im alten C#-FormDebugger; nach dem Ende
// eines Jobs wird das Netzwerk neu geladen
function connectProgress(): void {
  interface ProgressMsg {
    seq: number;
    busy: boolean;
    text: string;
    fields: number[];
    result: string;
    error: string;
  }
  let doneSeq = -1; // zuletzt behandeltes Abschluss-Event (dedupliziert Reconnects)
  const source = new EventSource('/api/progress');
  source.onmessage = ev => {
    const p = JSON.parse(ev.data) as ProgressMsg;
    const stopBtn = $('stopBtn') as HTMLButtonElement;
    if (p.busy) {
      mergeBusy = true;
      stopBtn.disabled = false;
      $('info').textContent = p.text;
      canvas.setBusyFields(p.fields);
      updateMergeButton(canvas.getSelection());
      return;
    }
    stopBtn.disabled = true;
    canvas.setBusyFields([]);
    mergeBusy = false;
    updateMergeButton(canvas.getSelection());
    // Abschluss eines Jobs: auch blitzschnelle Jobs liefern genau ein
    // Ergebnis-Event (Erkennung über die Sequenznummer, nicht über busy)
    if ((p.result === '' && p.error === '') || p.seq === doneSeq) return;
    doneSeq = p.seq;
    void loadSnapshots(); // z.B. nach "Speichern" die Liste nachziehen
    void reloadNetwork().then(() => {
      if (p.error) showError(new Error(p.error));
      else if (p.result) showStatus(p.result);
    });
  };
}

async function doValidate(): Promise<void> {
  try {
    await postJSON<{ ok: boolean }>('/api/validate', {});
    showStatus('Validate: ok');
  } catch (err) {
    showError(err);
  }
}

// holt Kennzahlen und Raum-Karte neu (nach Merge) - das Feld selbst bleibt
// meist gleich. Die Auswahl überlebt den Reload: je Raum dient ein Feld (Wpos)
// als stabile Kennung, gemergte Räume bleiben also selektiert (der nächste
// Arbeitsschritt betrifft meist genau sie). Hat sich das LEVEL geändert
// (Strg+V mit URL/Nummer/Levelnotation), werden auch Feld, Titel und das
// max-moves-Feld (bekannter Rekord) neu gesetzt; die Auswahl verfällt dann.
async function reloadNetwork(): Promise<void> {
  const keep = canvas.getSelectionWpos();
  roomCache.clear();
  const summary = await getJSON<Summary>('/api/summary');
  const map = await getJSON<{ rooms: number[] }>('/api/map');
  const levelChanged = summary.levelSeq !== levelSeq;
  if (levelChanged) {
    levelSeq = summary.levelSeq;
    field = await getJSON<Field>('/api/field');
    document.title = 'Rooms - ' + summary.title;
    $('title').textContent = summary.title;
    ($('maxMoves') as HTMLInputElement).value = summary.bestMoves > 0 ? String(summary.bestMoves) : '';
  }
  networkEffort = summary.effort;
  updateStats(summary);
  canvas.setData(field, map.rooms); // setzt auch die Auswahl zurück
  roomsList.reset(summary.roomCount);
  currentRoom = null;
  currentState = null;
  variantsFetch = null;
  $('statesHead').textContent = '-- States --';
  statesList.reset(0);
  variantsList.reset(0);
  $('info').textContent = 'Effort: ' + summary.effort;

  if (levelChanged || clearSelection) {
    // neues Level bzw. Reset: alte Auswahl nicht wiederherstellen
    clearSelection = false;
    updateMergeButton([]);
    return;
  }

  // Auswahl wiederherstellen: alte Felder -> neue Raum-Indizes (nach einem
  // Merge fallen mehrere alte Räume auf denselben neuen zusammen)
  const rooms = [...new Set(keep.rooms.map(w => map.rooms[w]))];
  const active = keep.active >= 0 ? map.rooms[keep.active] : -1;
  if (rooms.length > 0) {
    canvas.setSelection(rooms, active);
    void handleSelection(rooms, active, false);
  }
}

function updateStats(summary: Summary): void {
  // Effort kompakt in e-Schreibweise (EffortString: "4,566e117 (4.565...)")
  const effortShort = summary.effort.split(' ')[0];
  $('stats').innerHTML =
    `<span>R&auml;ume <b>${fmt(summary.roomCount)}</b></span>` +
    `<span>Zust&auml;nde <b>${fmt(summary.stateCount)}</b></span>` +
    `<span>Varianten <b>${fmt(summary.variantCount)}</b></span>` +
    `<span>Min-Z&uuml;ge <b>${fmt(summary.minMoves)}</b></span>` +
    `<span>Effort <b>${effortShort}</b></span>`;
}

// ---------- Zustandswahl -> Varianten-Sektionen aufbauen ----------

function stateText(state: StateItem): string {
  if (state.id === 0) return 'State finish';
  if (currentRoom && state.id === currentRoom.startState) return `State ${fmt(state.id)} (start)`;
  return `State ${fmt(state.id)}`;
}

function selectState(state: StateItem): void {
  currentState = state;
  canvas.showState(state.boxes.map(w => canvas.wposToIdx(w)));

  const room = currentRoom!;
  const segments = buildVariantSegments(room, state);
  let total = 0;
  const starts: number[] = [];
  for (const seg of segments) {
    starts.push(total);
    total += seg.count;
  }
  variantsFetch = async (offset, limit) => {
    const items: VRow[] = [];
    let pos = offset;
    let remaining = limit;
    for (let i = 0; i < segments.length && remaining > 0; i++) {
      const segEnd = starts[i] + segments[i].count;
      if (pos >= segEnd) continue;
      const local = pos - starts[i];
      const take = Math.min(remaining, segments[i].count - local);
      items.push(...(await segments[i].get(local, take)));
      pos += take;
      remaining -= take;
    }
    return { total, offset, items };
  };
  variantsList.reset(total);
}

// baut die Sektionen wie der C#-FormDebugger: Startvarianten (nur beim
// Startzustand des Spieler-Raums), dann je Portal Kopfzeile, ggf. die
// Kisten-Schub-Zeile "Variant B" und die Varianten des Spans
function buildVariantSegments(room: RoomDetail, state: StateItem): Segment[] {
  const segments: Segment[] = [];
  const staticSeg = (rows: VRow[]) => segments.push({ count: rows.length, get: (l, t) => rows.slice(l, l + t) });

  if (room.startVariantCount > 0 && state.id === room.startState) {
    staticSeg([{ text: '-- Starts --', head: true }]);
    segments.push(spanSegment(room, 0, room.startVariantCount, null));
  }

  for (const portal of room.portalList) {
    staticSeg([{ text: `-- Portal ${portal.index + 1}${portal.dir}${portal.blockedBox ? ' - [BB] --' : ' --'}`, head: true }]);
    const swapTo = portal.boxSwap[String(state.id)];
    if (swapTo !== undefined) {
      staticSeg([{ text: `Variant B (${portal.dir}) -> ${fmt(swapTo)}`, boxSwap: { portal, newState: swapTo } }]);
    }
    const span = portal.variantSpans[String(state.id)];
    if (span && span.count > 0) {
      segments.push(spanSegment(room, span.start, span.count, portal));
    } else if (swapTo === undefined) {
      staticSeg([{ text: 'no variants', head: true }]);
    }
  }
  return segments;
}

// lazy geladener Varianten-Bereich (IDs start..start+count-1, lückenlos)
function spanSegment(room: RoomDetail, start: number, count: number, entry: Portal | null): Segment {
  return {
    count,
    get: async (local, take) => {
      const page = await getJSON<Page<VariantItem>>(
        `/api/rooms/${room.index}/variants?offset=${start + local}&limit=${take}`,
      );
      return page.items.map((v, i) => {
        let path = v.path;
        if (entry) path = entry.dir + path; // Eintritts-Schritt wie im Original voranstellen
        if (v.boxPortals.length > 0) {
          path += ' > ' + v.boxPortals.map(bi => `${bi + 1}${OPPOSITE[room.portalList[bi].dir]}`).join(',');
        }
        const name = v.playerPortal < 0 ? 'End' : fmt(local + i + 1);
        return { text: `Variant ${name} [${fmt(v.moves)}/${fmt(v.pushes)}] -> ${fmt(v.newState)} (${path})`, variant: v, entry };
      });
    },
  };
}

// ---------- Variantenwahl -> Animation ----------

async function selectVariant(row: VRow): Promise<void> {
  const room = currentRoom!;
  const toIdx = (w: number) => canvas.wposToIdx(w);

  // "Variant B": Kiste wird von außen reingeschoben (2 Schritte wie im Original)
  if (row.boxSwap) {
    const { portal, newState } = row.boxSwap;
    try {
      const page = await getJSON<Page<StateItem>>(`/api/rooms/${room.index}/states?offset=${newState}&limit=1`);
      const fromIdx = toIdx(portal.from);
      const targetIdx = toIdx(portal.to);
      const frames: AnimFrame[] = [
        { player: fromIdx + (fromIdx - targetIdx), boxes: [...currentState!.boxes.map(toIdx), fromIdx] },
        { player: fromIdx, boxes: page.items[0].boxes.map(toIdx) },
      ];
      canvas.showVariant({ entry: portal, exit: null, path: '', boxPortals: [], ends: false, frames });
    } catch (err) {
      showError(err);
    }
    return;
  }

  const v = row.variant!;
  const entry = row.entry ?? null;
  const path = entry ? entry.dir + v.path : v.path;
  let player = entry ? toIdx(entry.from) : toIdx(field.player);
  const boxes = currentState!.boxes.map(toIdx);
  const frames: AnimFrame[] = [{ player, boxes: [...boxes] }];
  const delta: Record<string, number> = { l: -1, r: 1, u: -field.width, d: field.width };
  for (const step of path) {
    player += delta[step];
    const pushed = boxes.indexOf(player);
    if (pushed >= 0) boxes[pushed] += delta[step]; // Kiste wird weitergeschoben
    frames.push({ player, boxes: [...boxes] });
  }

  canvas.showVariant({
    entry,
    exit: v.playerPortal >= 0 ? room.portalList[v.playerPortal] : null,
    path,
    boxPortals: v.boxPortals.map(i => room.portalList[i]),
    ends: v.playerPortal < 0,
    frames,
  });

  // Statuszeile: Kosten der gewählten Variante. Achtung Zähl-Konvention
  // (rooms/variantlist.go): der Eintritts-Schritt gehört zur Vorgänger-
  // Variante - bei Startvarianten (z.B. der Einzellösung nach einem
  // Voll-Merge) ist daher alles enthalten.
  $('info').textContent = `Variant ${fmt(v.id)}: ${fmt(v.moves)} moves, ${fmt(v.pushes)} pushes`;
}

// ---------- Aufbau ----------

async function boot(): Promise<void> {
  const summary = await getJSON<Summary>('/api/summary');
  field = await getJSON<Field>('/api/field');
  const map = await getJSON<{ rooms: number[] }>('/api/map');

  document.title = 'Rooms - ' + summary.title;
  $('title').textContent = summary.title;
  levelSeq = summary.levelSeq;
  if (summary.bestMoves > 0) {
    ($('maxMoves') as HTMLInputElement).value = String(summary.bestMoves); // bekannter Rekord
  }
  networkEffort = summary.effort;
  updateStats(summary);
  $('info').textContent = 'Effort: ' + summary.effort;

  canvas = new FieldCanvas($('field') as HTMLCanvasElement);
  canvas.onSelectionChange = (selection, active) => void handleSelection(selection, active, false);
  // Feldnummer (Wpos, stabile Raum-Kennung) unterm Mauszeiger anzeigen
  canvas.onHover = (wpos, room) => {
    $('hoverInfo').innerHTML = wpos >= 0 ? `Feld ${wpos} - Room ${room + 1}` : '&nbsp;';
  };
  canvas.setData(field, map.rooms);

  roomsList = new VirtualList<RoomSummary>(
    $('roomsList'),
    20,
    room => `Room ${room.index + 1} [${room.fields.length}]`,
    (offset, limit) => getJSON<Page<RoomSummary>>(`/api/rooms?offset=${offset}&limit=${limit}`),
  );
  // Klick in der Raum-Liste ersetzt die Auswahl (Einzelwahl wie im Original)
  roomsList.onSelect = room => {
    canvas.setSelection([room.index], room.index);
    void handleSelection([room.index], room.index, true);
  };
  roomsList.reset(summary.roomCount);

  // universelles Strg+V: Level-URL/-Nummer, Levelnotation oder LURD-Lösung
  // (Einfügen in Eingabefelder wie max moves bleibt normales Einfügen)
  window.addEventListener('paste', ev => {
    if (ev.target instanceof HTMLInputElement) return;
    const text = ev.clipboardData?.getData('text') ?? '';
    if (!text.trim()) return;
    ev.preventDefault();
    void doPaste(text);
  });

  // Taststeuerung der Varianten-Animation (wie die Lösungsanzeige in brute):
  // der erste Druck stoppt die Animation am Anfang der Variante, danach springen
  // links/rechts von Kistenschub zu Kistenschub, Home/End an Anfang/Ende.
  // Neu-Anklicken einer Variante spielt wieder die normale Animation ab.
  window.addEventListener('keydown', ev => {
    if (ev.target instanceof HTMLInputElement) return; // z.B. max-moves-Feld
    if (ev.key === 'Delete') {
      void doReset(); // gemergte Räume der Auswahl zurücksetzen
      return;
    }
    const keys: Record<string, 'left' | 'right' | 'home' | 'end'> = {
      ArrowLeft: 'left', ArrowRight: 'right', Home: 'home', End: 'end',
    };
    const key = keys[ev.key];
    if (!key) return;
    const pos = canvas.stepVariant(key);
    if (!pos) return;
    ev.preventDefault();
    $('info').textContent = `Variante angehalten: Zug ${fmt(pos.frame)}/${fmt(pos.total)}` +
      ' - links/rechts = Kistenschübe, Klick auf die Variante = Animation';
  });

  ($('mergeBtn') as HTMLButtonElement).addEventListener('click', () => void doMerge());
  ($('optimizeBtn') as HTMLButtonElement).addEventListener('click', () => void doOptimize());
  ($('stopBtn') as HTMLButtonElement).addEventListener('click', () => void doStop());
  ($('validateBtn') as HTMLButtonElement).addEventListener('click', () => void doValidate());
  ($('snapSaveBtn') as HTMLButtonElement).addEventListener('click', () => void doSnapshotSave());
  ($('snapLoadBtn') as HTMLButtonElement).addEventListener('click', () => void doSnapshotLoad());
  ($('snapDeleteBtn') as HTMLButtonElement).addEventListener('click', () => void doSnapshotDelete());

  // Snapshot-Liste: zweizeilig (Effort in e-Schreibweise, Dateigröße darunter),
  // die Daten kommen komplett aus loadSnapshots (kein Paging nötig)
  snapshotsList = new VirtualList<SnapshotItem>(
    $('snapList'),
    38,
    snap => {
      const mb = (snap.size / 1048576).toLocaleString('de-DE', { maximumFractionDigits: 1 });
      return `${snap.effort.split(' ')[0]}<br><span class="dim">${mb} MB</span>`;
    },
    (offset, limit) => Promise.resolve({ total: snapshots.length, offset, items: snapshots.slice(offset, offset + limit) }),
  );
  snapshotsList.onSelect = snap => {
    selectedSnapshot = snap;
    updateSnapshotButtons();
  };
  void loadSnapshots();

  connectProgress();

  statesList = new VirtualList<StateItem>(
    $('statesList'),
    20,
    state => stateText(state),
    (offset, limit) =>
      getJSON<Page<StateItem>>(`/api/rooms/${currentRoom!.index}/states?offset=${offset}&limit=${limit}`),
  );
  statesList.onSelect = state => selectState(state);

  variantsList = new VirtualList<VRow>(
    $('variantsList'),
    20,
    row => row.text,
    (offset, limit) => (variantsFetch ? variantsFetch(offset, limit) : Promise.resolve({ total: 0, offset: 0, items: [] })),
    row => !row.head,
  );
  variantsList.onSelect = row => void selectVariant(row);
}

boot().catch(showError);
