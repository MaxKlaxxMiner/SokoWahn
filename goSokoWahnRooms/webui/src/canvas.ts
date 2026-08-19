// Spielfeld-Canvas nach dem Vorbild des C#-FieldDisplay (SokoWahnWin):
// schwarzer Grund, ZigZag-Boden, Backstein-Wände mit Lichtkante, Kisten mit
// 3D-Fase, grüner Spieler-Kreis, Räume als Kontur-Ketten (DrawHighlight),
// Varianten als Animation (300 ms je Schritt, Schleife). Das Feld passt sich
// wie das Original immer pixelgenau der Fenstergröße an (2% Rand, zentriert) -
// kein Zoom, kein Scrollen. Ergänzungen gegenüber dem Original:
// Portal-Pfeile des gewählten Raums und eine dezente Laufweg-Linie.

import { Field, Portal } from './api';

// Schrittvektoren je LURD-Richtung (in Zellkoordinaten)
const DIRS: Record<string, [number, number]> = { l: [-1, 0], r: [1, 0], u: [0, -1], d: [0, 1] };
export const OPPOSITE: Record<string, string> = { l: 'r', r: 'l', u: 'd', d: 'u' };

// Farben des C#-Originals (FieldDisplay.cs)
const COLOR_FLOOR = '#442200'; // ZigZag-Boden
const COLOR_WALL = '#666666'; // Backstein-Wände
const COLOR_WALL_LIGHT = '#888888'; // Lichtkante oben/links
const COLOR_WALL_DARK = '#444444'; // Schattenkante unten/rechts
const COLOR_GOAL = '#888833'; // Zielfeld-Quadrat
const COLOR_ROOM_BACK = '#003366'; // Kontur aller Räume
const COLOR_ROOM_SEL = '#0080ff'; // Kontur der ausgewählten Räume
const COLOR_ROOM_ACTIVE = '#66ccff'; // Kontur des aktiven Raums (Listen folgen ihm)
const COLOR_ROOM_STATE = '#ffff00'; // Kontur bei gewähltem Zustand/Variante
const HIGHLIGHT_SIZE = 0.7; // Größe der Kontur-Ketten (wie C#)

// Maße aus dem Original
const PLAYER_SIZE = 0.8;
const BOX_SIZE = 0.98;
const BOX_INNER = 0.66;
const GOAL_SIZE = 0.2;

// ein Schritt der Varianten-Animation (Positionen als Feldindex x + y*width, -1 = versteckt)
export interface AnimFrame {
  player: number;
  boxes: number[];
}

// Vorschau einer Variante (von main.ts aufbereitet)
export interface VariantPreview {
  entry: Portal | null; // eingehendes Portal (null = Startvariante)
  exit: Portal | null; // Portal, über das der Spieler den Raum verlässt (null = bleibt drin)
  path: string; // Laufweg inkl. Eintritts-Schritt (bei Portal-Varianten vorangestellt)
  boxPortals: Portal[]; // eingehende Portale, über deren Gegenrichtung Kisten rausgehen
  ends: boolean; // true = Spieler bleibt drin = Spielende
  frames: AnimFrame[]; // Animations-Schritte
}

const ANIM_DELAY = 300; // Millisekunden je Animations-Schritt (wie C#: VariantDelay)

export class FieldCanvas {
  private canvas: HTMLCanvasElement;
  private ctx: CanvasRenderingContext2D;
  private field: Field | null = null;
  private roomOf: number[] = [];
  private gridWpos: Int32Array = new Int32Array(0); // Feldindex -> Wpos (-1 = nicht begehbar)
  private roomFields = new Map<number, number[]>(); // Raum-Index -> Feldindizes
  private cell = 0; // Kantenlänge eines Feldes in Pixeln (aus der Fenstergröße berechnet)
  private ox = 0; // Versatz zum Zentrieren des Feldes
  private oy = 0;
  private resizeObserver: ResizeObserver | null = null;

  private selection = new Set<number>(); // ausgewählte Räume (Einfüge-Reihenfolge bleibt erhalten)
  private busyFields: number[] = []; // Wpos der gerade berechneten Räume (gelb)
  private active = -1; // aktiver Raum (zuletzt hinzugefügt) - die Listen folgen ihm
  private dragMode: 'add' | 'remove' | null = null; // laufende Maus-Geste
  private portals: Portal[] = []; // eingehende Portale des aktiven Raums
  private stateBoxes: number[] | null = null; // Kisten des gewählten Zustands (Feldindizes)
  private playerHidden = false; // Zustand gewählt -> Spieler versteckt (wie C#)
  private variant: VariantPreview | null = null;
  private animTick = 0;
  private animTimer: ReturnType<typeof setInterval> | null = null;

  private floorPattern: CanvasPattern | null = null;
  private wallPattern: CanvasPattern | null = null;

  // Auswahl geändert: aktuelle Auswahl (Einfüge-Reihenfolge) + aktiver Raum (-1 = keiner)
  onSelectionChange: ((selection: number[], active: number) => void) | null = null;
  // Maus über einem Feld: (wpos, raumIndex), beide -1 = Wand/außerhalb
  onHover: ((wpos: number, room: number) => void) | null = null;

  constructor(canvas: HTMLCanvasElement) {
    this.canvas = canvas;
    this.ctx = canvas.getContext('2d')!;
    this.makePatterns();

    // Raumselektion wie im C#-FormDebugger: Linksklick fügt den Raum zur Auswahl
    // hinzu, gedrückt halten und ziehen fügt mehrere hinzu, Rechtsklick entfernt
    canvas.addEventListener('mousedown', ev => {
      if (ev.button !== 0 && ev.button !== 2) return;
      this.dragMode = ev.button === 0 ? 'add' : 'remove';
      this.applyDrag(this.roomAt(ev));
      ev.preventDefault();
    });
    canvas.addEventListener('mousemove', ev => {
      if (this.dragMode) this.applyDrag(this.roomAt(ev));
      const wpos = this.wposAt(ev);
      this.onHover?.(wpos, wpos >= 0 ? this.roomOf[wpos] : -1);
    });
    canvas.addEventListener('mouseleave', () => this.onHover?.(-1, -1));
    window.addEventListener('mouseup', () => (this.dragMode = null));
    canvas.addEventListener('contextmenu', ev => ev.preventDefault());
  }

  // Feld (Wpos) unter dem Mauszeiger (-1 = Wand/außerhalb)
  private wposAt(ev: MouseEvent): number {
    if (!this.field || this.cell <= 0) return -1;
    const rect = this.canvas.getBoundingClientRect();
    const x = Math.floor((ev.clientX - rect.left - this.ox) / this.cell);
    const y = Math.floor((ev.clientY - rect.top - this.oy) / this.cell);
    if (x < 0 || y < 0 || x >= this.field.width || y >= this.field.height) return -1;
    return this.gridWpos[y * this.field.width + x];
  }

  // Raum unter dem Mauszeiger (-1 = Wand/außerhalb)
  private roomAt(ev: MouseEvent): number {
    const wpos = this.wposAt(ev);
    return wpos >= 0 ? this.roomOf[wpos] : -1;
  }

  // wendet die laufende Maus-Geste auf einen Raum an und meldet Änderungen
  private applyDrag(room: number): void {
    if (room < 0) return;
    if (this.dragMode === 'add') {
      if (this.selection.has(room) && this.active === room) return; // nichts Neues
      this.selection.add(room);
      this.active = room;
    } else {
      if (!this.selection.has(room)) return;
      this.selection.delete(room);
      if (this.active === room) {
        this.active = -1;
        for (const r of this.selection) this.active = r; // letzter verbleibender wird aktiv
      }
    }
    this.portals = [];
    this.resetPreview();
    this.draw();
    this.onSelectionChange?.([...this.selection], this.active);
  }

  // aktuelle Auswahl in Einfüge-Reihenfolge
  getSelection(): number[] {
    return [...this.selection];
  }

  // Hatch-Muster des Originals nachgebildet (GDI+ zeichnet Hatches in Gerätepixeln,
  // daher bleiben die Kacheln unabhängig von der Zoomstufe 8x8 Pixel groß)
  private makePatterns(): void {
    const zigzag = document.createElement('canvas');
    zigzag.width = 8;
    zigzag.height = 8;
    const zctx = zigzag.getContext('2d')!;
    zctx.strokeStyle = COLOR_FLOOR;
    zctx.lineWidth = 1.4;
    zctx.beginPath();
    zctx.moveTo(0, 6.5);
    zctx.lineTo(4, 2.5);
    zctx.lineTo(8, 6.5);
    zctx.stroke();
    this.floorPattern = this.ctx.createPattern(zigzag, 'repeat');

    const brick = document.createElement('canvas');
    brick.width = 8;
    brick.height = 8;
    const bctx = brick.getContext('2d')!;
    bctx.strokeStyle = COLOR_WALL;
    bctx.lineWidth = 1.2;
    bctx.beginPath();
    bctx.moveTo(0, 8);
    bctx.lineTo(8, 0); // Diagonale
    bctx.moveTo(0, 0);
    bctx.lineTo(3.5, 3.5); // halber Fugen-Versatz -> Backstein-Wirkung
    bctx.stroke();
    this.wallPattern = this.ctx.createPattern(brick, 'repeat');
  }

  setData(field: Field, roomOf: number[]): void {
    this.field = field;
    this.roomOf = roomOf;
    this.gridWpos = new Int32Array(field.width * field.height).fill(-1);
    this.roomFields.clear();
    field.walk.forEach((cell, wpos) => {
      const idx = cell.y * field.width + cell.x;
      this.gridWpos[idx] = wpos;
      const room = roomOf[wpos];
      const list = this.roomFields.get(room);
      if (list) list.push(idx);
      else this.roomFields.set(room, [idx]);
    });
    this.clearSelection();

    // Feld folgt immer der Größe des umgebenden Containers (wie das Original)
    if (!this.resizeObserver) {
      this.resizeObserver = new ResizeObserver(() => this.draw());
      this.resizeObserver.observe(this.canvas.parentElement!);
    }
    this.draw();
  }

  clearSelection(): void {
    this.selection.clear();
    this.active = -1;
    this.portals = [];
    this.resetPreview();
  }

  // ersetzt die Auswahl komplett (z.B. Klick in der Raum-Liste), ohne
  // onSelectionChange auszulösen - der Aufrufer weiß selbst Bescheid
  setSelection(rooms: number[], active: number): void {
    this.selection = new Set(rooms);
    this.active = active;
    this.portals = [];
    this.resetPreview();
    this.draw();
  }

  // setzt die Portal-Pfeile des aktiven Raums (kommen asynchron vom Raum-Detail)
  setActivePortals(portals: Portal[]): void {
    this.portals = portals;
    this.draw();
  }

  private resetPreview(): void {
    this.stateBoxes = null;
    this.playerHidden = false;
    this.stopAnimation();
    this.variant = null;
  }

  // Kisten-Vorschau eines Zustands: nur dessen Kisten, Spieler versteckt (wie C#)
  showState(boxes: number[]): void {
    this.stopAnimation();
    this.variant = null;
    this.stateBoxes = boxes;
    this.playerHidden = true;
    this.draw();
  }

  // Varianten-Vorschau: Laufweg-Linie/Pfeile plus Animation der Schritte
  showVariant(preview: VariantPreview): void {
    this.stopAnimation();
    this.variant = preview;
    this.playerHidden = false;
    if (preview.frames.length > 0) {
      this.animTick = 0;
      this.animTimer = setInterval(() => {
        this.animTick++;
        this.draw();
      }, ANIM_DELAY);
    }
    this.draw();
  }

  private stopAnimation(): void {
    if (this.animTimer !== null) {
      clearInterval(this.animTimer);
      this.animTimer = null;
    }
    this.animTick = 0;
  }

  private isGoalIdx(idx: number): boolean {
    const wpos = this.gridWpos[idx];
    return wpos >= 0 && this.field!.walk[wpos].goal;
  }

  draw(): void {
    const f = this.field;
    if (!f) return;
    const ctx = this.ctx;

    // Canvas füllt den Container, das Feld wird pixelgenau eingepasst und
    // zentriert (wie das Original: Skalierung mit 2% Rand, keine Zoomstufen)
    const wrap = this.canvas.parentElement!;
    const cssW = wrap.clientWidth;
    const cssH = wrap.clientHeight;
    if (cssW < 4 || cssH < 4) return;
    this.cell = Math.min(cssW / f.width, cssH / f.height) * 0.98;
    this.ox = (cssW - f.width * this.cell) / 2;
    this.oy = (cssH - f.height * this.cell) / 2;
    const c = this.cell;

    const dpr = window.devicePixelRatio || 1;
    this.canvas.width = Math.round(cssW * dpr);
    this.canvas.height = Math.round(cssH * dpr);
    this.canvas.style.width = cssW + 'px';
    this.canvas.style.height = cssH + 'px';

    // --- schwarzer Grund + ZigZag-Boden auf den begehbaren Feldern ---
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.fillStyle = '#000000';
    ctx.fillRect(0, 0, cssW, cssH);
    ctx.translate(this.ox, this.oy); // ab hier in Feld-Koordinaten (zentriert)
    ctx.fillStyle = this.floorPattern!;
    for (let y = 0; y < f.height; y++) {
      for (let x = 0; x < f.width; x++) {
        if (this.gridWpos[y * f.width + x] >= 0) ctx.fillRect(x * c, y * c, c, c);
      }
    }

    // --- Wände: Backstein-Muster mit Licht- und Schattenkanten ---
    const isWall = (x: number, y: number): boolean => {
      if (x < 0 || y < 0 || x >= f.width || y >= f.height) return false;
      return ((f.rows[y] ?? '')[x] ?? ' ') === '#';
    };
    const edge = Math.max(1, c * 0.045);
    for (let y = 0; y < f.height; y++) {
      for (let x = 0; x < f.width; x++) {
        if (!isWall(x, y)) continue;
        ctx.fillStyle = this.wallPattern!;
        ctx.fillRect(x * c, y * c, c, c);
        ctx.lineWidth = edge;
        ctx.strokeStyle = COLOR_WALL_LIGHT;
        ctx.beginPath();
        if (!isWall(x - 1, y)) {
          ctx.moveTo(x * c, y * c);
          ctx.lineTo(x * c, (y + 1) * c);
        }
        if (!isWall(x, y - 1)) {
          ctx.moveTo(x * c, y * c);
          ctx.lineTo((x + 1) * c, y * c);
        }
        ctx.stroke();
        ctx.strokeStyle = COLOR_WALL_DARK;
        ctx.beginPath();
        if (!isWall(x + 1, y)) {
          ctx.moveTo((x + 1) * c, y * c);
          ctx.lineTo((x + 1) * c, (y + 1) * c);
        }
        if (!isWall(x, y + 1)) {
          ctx.moveTo(x * c, (y + 1) * c);
          ctx.lineTo((x + 1) * c, (y + 1) * c);
        }
        ctx.stroke();
      }
    }

    // --- Zielfelder: kleine olivfarbene Quadrate ---
    ctx.strokeStyle = COLOR_GOAL;
    ctx.lineWidth = Math.max(1, c * 0.03);
    for (const cell of f.walk) {
      if (!cell.goal) continue;
      ctx.strokeRect((cell.x + 0.5 - GOAL_SIZE / 2) * c, (cell.y + 0.5 - GOAL_SIZE / 2) * c, GOAL_SIZE * c, GOAL_SIZE * c);
    }

    // --- gelbe Markierung der gerade berechneten Räume (Fortschritts-Anzeige) ---
    if (this.busyFields.length > 0) {
      ctx.fillStyle = 'rgba(255, 220, 0, 0.35)';
      for (const wpos of this.busyFields) {
        const idx = this.wposToIdx(wpos);
        ctx.fillRect((idx % f.width) * c, Math.floor(idx / f.width) * c, c, c);
      }
    }

    // --- Basis-Konturen aller Räume (dunkelblau, unter den Figuren; die
    // Auswahl-Konturen kommen NACH den Kisten, damit sie sichtbar bleiben) ---
    for (const [room, fields] of this.roomFields) {
      if (this.selection.has(room)) continue; // Auswahl kommt zuletzt (obenauf)
      this.drawChain(fields, COLOR_ROOM_BACK);
    }

    // --- Kisten und Spieler (Startaufstellung, Zustand oder Animations-Schritt) ---
    let boxes: number[];
    let player: number;
    if (this.variant && this.variant.frames.length > 0) {
      const frames = this.variant.frames;
      // wie das Original: letzten Schritt kurz stehen lassen, dann von vorn
      const tick = this.animTick % (frames.length + 2);
      const frame = frames[Math.min(tick, frames.length - 1)];
      boxes = frame.boxes;
      player = frame.player;
    } else if (this.stateBoxes !== null) {
      boxes = this.stateBoxes;
      player = -1;
    } else {
      boxes = f.boxes.map(w => this.wposToIdx(w));
      player = this.playerHidden ? -1 : this.wposToIdx(f.player);
    }

    // --- Laufweg-Linie der Variante (dezent unter den Figuren) ---
    if (this.variant) this.drawVariantPath(this.variant);

    for (const idx of boxes) this.drawBox(idx);
    if (player >= 0) this.drawPlayer(player);

    // --- Auswahl- und Aktiv-Konturen über den Figuren (der blaue Marker
    // bleibt sichtbar, auch wenn eine Kiste auf einem Randfeld steht) ---
    for (const room of this.selection) {
      if (room === this.active) continue;
      this.drawChain(this.roomFields.get(room) ?? [], COLOR_ROOM_SEL);
    }
    if (this.active >= 0) {
      const color = this.stateBoxes !== null || this.variant ? COLOR_ROOM_STATE : COLOR_ROOM_ACTIVE;
      this.drawChain(this.roomFields.get(this.active) ?? [], color);
    }

    if (this.variant) {
      // bei gewählter Variante: grüne Ein-/Austritts-Markierungen und
      // Kistenschub-Pfeile statt der Standardpfeile (über den Figuren)
      this.drawVariantPortals(this.variant);
    } else {
      // --- Portal-Pfeile des gewählten Raums (Ergänzung gegenüber dem Original) ---
      for (const portal of this.portals) {
        this.drawPortalArrow(this.wposToIdx(portal.to), portal.dir, portal.blockedBox ? '#ff6a6a' : '#56c8ff');
      }
    }
  }

  // grüne Markierungen, wo der Spieler den Raum betritt und verlässt (Quadrat,
  // wenn Ein- und Ausgang dasselbe Portal sind), dazu die Kistenschub-Pfeile
  // und der Spielende-Ring - alles über den Figuren gezeichnet
  private drawVariantPortals(v: VariantPreview): void {
    const green = '#33ff33';
    if (v.entry && v.exit && v.entry.index === v.exit.index) {
      this.drawEdgeSquare(this.wposToIdx(v.entry.to), v.entry.dir, green);
    } else {
      if (v.entry) this.drawPortalArrow(this.wposToIdx(v.entry.to), v.entry.dir, green);
      if (v.exit) this.drawPortalArrow(this.wposToIdx(v.exit.from), OPPOSITE[v.exit.dir], green);
    }

    // Kistenschübe: Pfeil über die Austrittskante (Gegenrichtung des eingehenden Portals)
    for (const portal of v.boxPortals) {
      this.drawPortalArrow(this.wposToIdx(portal.from), OPPOSITE[portal.dir], '#ff9c3f');
    }
  }

  // kleines gefülltes Quadrat auf dem Kantenmittelpunkt eines Portals
  private drawEdgeSquare(idx: number, dir: string, color: string): void {
    const c = this.cell;
    const w = this.field!.width;
    const cx = ((idx % w) + 0.5) * c;
    const cy = (Math.floor(idx / w) + 0.5) * c;
    const [dx, dy] = DIRS[dir];
    const ex = cx - (dx * c) / 2;
    const ey = cy - (dy * c) / 2;
    const s = c * 0.2;
    const ctx = this.ctx;
    ctx.fillStyle = color;
    ctx.fillRect(ex - s / 2, ey - s / 2, s, s);
    ctx.strokeStyle = 'rgba(0,0,0,0.6)';
    ctx.lineWidth = 1;
    ctx.strokeRect(ex - s / 2, ey - s / 2, s, s);
  }

  // gelbe Fortschritts-Markierung setzen (Wpos-Liste der bearbeiteten Räume)
  setBusyFields(fields: number[]): void {
    this.busyFields = fields;
    this.draw();
  }

  wposToIdx(wpos: number): number {
    const cell = this.field!.walk[wpos];
    return cell.y * this.field!.width + cell.x;
  }

  // Kontur-Kette um eine Feldmenge (1:1-Port von FieldDisplay.DrawHighlight)
  private drawChain(fieldList: number[], color: string): void {
    const f = this.field!;
    const w = f.width;
    const c = this.cell;
    const ctx = this.ctx;
    const set = new Set(fieldList);
    const has = (idx: number) => set.has(idx);
    const hs = 0.5 - HIGHLIGHT_SIZE / 2;

    ctx.strokeStyle = color;
    ctx.lineWidth = Math.max(1, c * 0.03);
    ctx.beginPath();
    const line = (x1: number, y1: number, x2: number, y2: number) => {
      ctx.moveTo(x1 * c, y1 * c);
      ctx.lineTo(x2 * c, y2 * c);
    };
    for (const idx of fieldList) {
      const x = idx % w;
      const y = (idx - x) / w;
      const l = has(idx - 1);
      const r = has(idx + 1);
      const u = has(idx - w);
      const d = has(idx + w);
      const x1 = x + hs;
      const x2 = x1 + HIGHLIGHT_SIZE;
      const y1 = y + hs;
      const y2 = y1 + HIGHLIGHT_SIZE;
      if (!l) line(x1, u ? y : y1, x1, d ? y + 1 : y2);
      if (!r) line(x2, u ? y : y1, x2, d ? y + 1 : y2);
      if (!u) line(l ? x : x1, y1, r ? x + 1 : x2, y1);
      if (!d) line(l ? x : x1, y2, r ? x + 1 : x2, y2);
      if (l && u && !has(idx - 1 - w)) { line(x, y1, x1, y1); line(x1, y1, x1, y); }
      if (l && d && !has(idx - 1 + w)) { line(x, y2, x1, y2); line(x1, y2, x1, y + 1); }
      if (r && u && !has(idx + 1 - w)) { line(x + 1, y1, x2, y1); line(x2, y1, x2, y); }
      if (r && d && !has(idx + 1 + w)) { line(x + 1, y2, x2, y2); line(x2, y2, x2, y + 1); }
    }
    ctx.stroke();
  }

  // Kiste mit 3D-Fase (1:1-Port aus dem Original, Farben je nach Zielfeld)
  private drawBox(idx: number): void {
    const c = this.cell;
    const ctx = this.ctx;
    const w = this.field!.width;
    const x = idx % w;
    const y = (idx - x) / w;
    const goal = this.isGoalIdx(idx);

    const l1 = (x + 0.5 - BOX_SIZE / 2) * c;
    const l2 = (x + 0.5 - BOX_INNER / 2) * c;
    const r1 = l1 + BOX_SIZE * c;
    const r2 = l2 + BOX_INNER * c;
    const t1 = (y + 0.5 - BOX_SIZE / 2) * c;
    const t2 = (y + 0.5 - BOX_INNER / 2) * c;
    const b1 = t1 + BOX_SIZE * c;
    const b2 = t2 + BOX_INNER * c;

    const poly = (points: number[][], fill: string) => {
      ctx.fillStyle = fill;
      ctx.beginPath();
      ctx.moveTo(points[0][0], points[0][1]);
      for (const [px, py] of points.slice(1)) ctx.lineTo(px, py);
      ctx.closePath();
      ctx.fill();
    };
    // Schattenseite unten/rechts, Lichtseite oben/links, dann die Frontfläche
    poly([[l1, b1], [r1, b1], [r1, t1], [r2, t2], [r2, b2], [l2, b2]], goal ? '#222209' : '#222222');
    poly([[l1, t1], [r1, t1], [r2, t2], [l2, t2], [l2, b2], [l1, b1]], goal ? '#666633' : '#888888');
    ctx.fillStyle = goal ? '#333311' : '#444444';
    ctx.fillRect(l2, t2, BOX_INNER * c, BOX_INNER * c);
    ctx.strokeStyle = goal ? '#888833' : '#aaaaaa';
    ctx.lineWidth = Math.max(1, c * 0.03);
    ctx.strokeRect(l2, t2, BOX_INNER * c, BOX_INNER * c);
  }

  // grüner Spieler-Kreis (1:1-Port aus dem Original)
  private drawPlayer(idx: number): void {
    const c = this.cell;
    const ctx = this.ctx;
    const w = this.field!.width;
    const cx = ((idx % w) + 0.5) * c;
    const cy = (Math.floor(idx / w) + 0.5) * c;

    // Füllungen mit Alpha - Boden, Ziele und Kisten scheinen durch den Spieler durch
    ctx.beginPath();
    ctx.arc(cx, cy, (BOX_SIZE / 2) * c, 0, Math.PI * 2);
    ctx.fillStyle = 'rgba(9, 42, 9, 0.55)';
    ctx.fill();
    ctx.beginPath();
    ctx.arc(cx, cy, (PLAYER_SIZE / 2) * c, 0, Math.PI * 2);
    ctx.fillStyle = 'rgba(17, 85, 17, 0.55)';
    ctx.fill();
    ctx.strokeStyle = '#33ff33';
    ctx.lineWidth = Math.max(1, c * 0.03);
    ctx.stroke();

    if (this.isGoalIdx(idx)) {
      ctx.strokeStyle = '#669900';
      ctx.strokeRect(cx - (GOAL_SIZE / 2) * c, cy - (GOAL_SIZE / 2) * c, GOAL_SIZE * c, GOAL_SIZE * c);
    }
  }

  // Pfeil an der Kante, über die ein Portal in Richtung dir ins Feld führt
  private drawPortalArrow(idx: number, dir: string, color: string): void {
    const c = this.cell;
    const w = this.field!.width;
    const cx = ((idx % w) + 0.5) * c;
    const cy = (Math.floor(idx / w) + 0.5) * c;
    const [dx, dy] = DIRS[dir];
    const ex = cx - (dx * c) / 2; // Kantenmittelpunkt
    const ey = cy - (dy * c) / 2;
    const len = c * 0.19;
    const wid = c * 0.13;
    const ctx = this.ctx;
    ctx.beginPath();
    ctx.moveTo(ex + dx * len, ey + dy * len); // Spitze zeigt in den Raum hinein
    ctx.lineTo(ex - dy * wid, ey - dx * wid);
    ctx.lineTo(ex + dy * wid, ey + dx * wid);
    ctx.closePath();
    ctx.fillStyle = color;
    ctx.fill();
    ctx.strokeStyle = 'rgba(0,0,0,0.6)';
    ctx.lineWidth = 1;
    ctx.stroke();
  }

  // dezente Laufweg-Linie der Variante (liegt unter den Figuren)
  private drawVariantPath(v: VariantPreview): void {
    const f = this.field!;
    const c = this.cell;
    const ctx = this.ctx;
    const w = f.width;

    let idx = v.entry ? this.wposToIdx(v.entry.from) : this.wposToIdx(f.player);
    let x = idx % w;
    let y = (idx - x) / w;
    const points: Array<[number, number]> = [[(x + 0.5) * c, (y + 0.5) * c]];
    for (const step of v.path) {
      const [dx, dy] = DIRS[step];
      x += dx;
      y += dy;
      points.push([(x + 0.5) * c, (y + 0.5) * c]);
    }
    if (points.length > 1) {
      ctx.beginPath();
      ctx.moveTo(points[0][0], points[0][1]);
      for (const [px, py] of points.slice(1)) ctx.lineTo(px, py);
      ctx.strokeStyle = 'rgba(255,215,94,0.45)';
      ctx.lineWidth = Math.max(2, c * 0.1);
      ctx.lineJoin = 'round';
      ctx.lineCap = 'round';
      ctx.stroke();
    }

    // Spielende: kleiner Ring um die Endposition des Spielers (liegt wie die
    // Laufweg-Linie unter den Figuren und scheint durch den Spieler durch)
    if (v.ends) {
      const [ex, ey] = points[points.length - 1];
      ctx.beginPath();
      ctx.arc(ex, ey, c * 0.3, 0, Math.PI * 2);
      ctx.strokeStyle = 'rgba(255,215,94,0.8)';
      ctx.lineWidth = Math.max(1.5, c * 0.035);
      ctx.stroke();
    }
  }
}
