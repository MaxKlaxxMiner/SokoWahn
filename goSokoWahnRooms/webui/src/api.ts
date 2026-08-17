// Typen und Fetch-Hilfen für die JSON-API des Servers (web/api.go)

export interface Summary {
  title: string;
  width: number;
  height: number;
  boxCount: number;
  walkCount: number;
  roomCount: number;
  stateCount: number;
  variantCount: number;
  effort: string;
}

export interface WalkCell {
  x: number;
  y: number;
  goal: boolean;
  corner: boolean;
  boxPath: boolean;
}

export interface Field {
  width: number;
  height: number;
  rows: string[]; // Grundriss ohne Kisten/Spieler
  walk: WalkCell[]; // Index = Wpos
  player: number;
  boxes: number[];
}

export interface RoomSummary {
  index: number;
  fields: number[];
  goals: number[];
  startBoxes: number[];
  maxBoxes: number;
  portals: number;
  states: number;
  variants: number;
  startState: number;
  startVariantCount: number;
}

export interface Span {
  start: number;
  count: number;
}

export interface Portal {
  index: number;
  from: number;
  to: number;
  dir: string;
  fromRoom: number;
  oppositeIndex: number;
  blockedBox: boolean;
  boxSwap: Record<string, number>;
  variantSpans: Record<string, Span>;
}

export interface RoomDetail extends RoomSummary {
  portalList: Portal[];
}

export interface StateItem {
  id: number;
  boxes: number[];
}

export interface VariantItem {
  id: number;
  oldState: number;
  newState: number;
  moves: number;
  pushes: number;
  boxPortals: number[];
  playerPortal: number; // -1 = Spieler bleibt drin = Spielende
  path: string;
  start: boolean;
}

export interface Page<T> {
  total: number;
  offset: number;
  items: T[];
}

async function decode<T>(resp: Response): Promise<T> {
  if (!resp.ok) {
    let msg = resp.status + ' ' + resp.statusText;
    try {
      const body = await resp.json();
      if (body.error) msg = body.error;
    } catch {
      // kein JSON-Fehlertext - Statuszeile reicht
    }
    throw new Error(msg);
  }
  return resp.json() as Promise<T>;
}

export async function getJSON<T>(url: string): Promise<T> {
  return decode(await fetch(url));
}

export async function postJSON<T>(url: string, body: unknown): Promise<T> {
  return decode(
    await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    }),
  );
}
