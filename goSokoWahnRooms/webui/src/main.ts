// Einstieg der Debug-GUI: Aufbau und Verhalten nach dem Vorbild des
// C#-FormDebugger - Räume-Liste links, Zustände über Varianten daneben,
// Spielfeld rechts mit Effort-Zeile, Aktions-Buttons am Rand (folgen ab M3).
// Die Varianten-Liste ist zustandsgetrieben und in Sektionen gegliedert
// (-- Starts --, -- Portal 1r --, ...) wie im Original; Varianten werden
// als Animation abgespielt. Read-only (M2).

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

function showError(err: unknown): void {
  const box = $('error');
  box.textContent = 'Fehler: ' + (err instanceof Error ? err.message : String(err));
  box.hidden = false;
  setTimeout(() => (box.hidden = true), 6000);
}

// ---------- Raumwahl ----------

async function selectRoom(index: number, fromList: boolean): Promise<void> {
  try {
    currentRoom = await getJSON<RoomDetail>(`/api/rooms/${index}`);
  } catch (err) {
    showError(err);
    return;
  }
  currentState = null;
  variantsFetch = null;
  canvas.selectRoom(index, currentRoom.portalList);
  if (!fromList) roomsList.highlight(index);

  $('statesHead').textContent = `-- Room ${index + 1} [${fmt(currentRoom.states)}] --`;
  statesList.reset(currentRoom.states);
  variantsList.reset(0);

  // wie das Original: Effort der Raum-Auswahl (bei einem Raum = dessen Variantenzahl)
  $('info').textContent = 'Effort: ' + fmt(currentRoom.variants);
}

// ---------- Zustandswahl -> Varianten-Sektionen aufbauen ----------

function stateText(state: StateItem): string {
  if (state.id === 0) return 'State finish';
  if (currentRoom && state.id === currentRoom.startState) return `State ${state.id} (start)`;
  return `State ${state.id}`;
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
      staticSeg([{ text: `Variant B (${portal.dir}) -> ${swapTo}`, boxSwap: { portal, newState: swapTo } }]);
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
        const name = v.playerPortal < 0 ? 'End' : String(local + i + 1);
        return { text: `Variant ${name} -> ${v.newState} (${path})`, variant: v, entry };
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
}

// ---------- Aufbau ----------

async function boot(): Promise<void> {
  const summary = await getJSON<Summary>('/api/summary');
  field = await getJSON<Field>('/api/field');
  const map = await getJSON<{ rooms: number[] }>('/api/map');

  document.title = 'Rooms - ' + summary.title;
  $('title').textContent = summary.title;
  $('stats').innerHTML =
    `<span>R&auml;ume <b>${fmt(summary.roomCount)}</b></span>` +
    `<span>Zust&auml;nde <b>${fmt(summary.stateCount)}</b></span>` +
    `<span>Varianten <b>${fmt(summary.variantCount)}</b></span>`;
  $('info').textContent = 'Effort: ' + summary.effort;

  canvas = new FieldCanvas($('field') as HTMLCanvasElement);
  canvas.onRoomClick = index => void selectRoom(index, false);
  canvas.setData(field, map.rooms);

  roomsList = new VirtualList<RoomSummary>(
    $('roomsList'),
    20,
    room => `Room ${room.index + 1} [${room.fields.length}]`,
    (offset, limit) => getJSON<Page<RoomSummary>>(`/api/rooms?offset=${offset}&limit=${limit}`),
  );
  roomsList.onSelect = room => void selectRoom(room.index, true);
  roomsList.reset(summary.roomCount);

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
