// Virtualisierte Liste: rendert nur den sichtbaren Ausschnitt und holt die Daten
// seitenweise von der API - Zustands-/Variantenlisten großer Räume können
// Millionen Einträge haben (harte Anforderung aus dem Konzept).

import { Page } from './api';

const PAGE_SIZE = 200;
const OVERSCAN = 4; // zusätzliche Zeilen ober-/unterhalb des Sichtbereichs

export class VirtualList<T> {
  private container: HTMLElement;
  private spacer: HTMLElement;
  private rowHeight: number;
  private total = 0;
  private pages = new Map<number, T[]>();
  private loading = new Set<number>();
  private generation = 0; // entwertet laufende Fetches nach reset()
  private renderRow: (item: T, index: number) => string;
  private fetchPage: (offset: number, limit: number) => Promise<Page<T>>;
  private selectable: (item: T) => boolean;
  private selected = -1;

  onSelect: ((item: T, index: number) => void) | null = null;

  constructor(
    container: HTMLElement,
    rowHeight: number,
    renderRow: (item: T, index: number) => string,
    fetchPage: (offset: number, limit: number) => Promise<Page<T>>,
    selectable: (item: T) => boolean = () => true,
  ) {
    this.container = container;
    this.rowHeight = rowHeight;
    this.renderRow = renderRow;
    this.fetchPage = fetchPage;
    this.selectable = selectable;

    container.classList.add('vlist');
    this.spacer = document.createElement('div');
    this.spacer.className = 'vlist-spacer';
    container.appendChild(this.spacer);

    container.addEventListener('scroll', () => this.render());
    container.addEventListener('click', ev => {
      const row = (ev.target as HTMLElement).closest('.vrow') as HTMLElement | null;
      if (row && row.dataset.index !== undefined) this.select(+row.dataset.index);
    });
  }

  // rendert den Sichtbereich neu - nötig, nachdem der Container wieder
  // eingeblendet wurde (mit display:none ist clientHeight 0, ein reset()
  // in dem Zustand rendert sonst nur die Overscan-Zeilen)
  refresh(): void {
    this.render();
  }

  // leert die Liste und setzt die neue Gesamtzahl (Daten kommen lazy beim Rendern)
  reset(total: number): void {
    this.generation++;
    this.total = total;
    this.pages.clear();
    this.loading.clear();
    this.selected = -1;
    this.spacer.style.height = total * this.rowHeight + 'px';
    this.container.scrollTop = 0;
    this.render();
  }

  // wählt einen Eintrag und meldet ihn über onSelect (sobald die Daten da sind);
  // nicht wählbare Einträge (z.B. Sektions-Überschriften) werden ignoriert
  select(index: number): void {
    if (index < 0 || index >= this.total) return;
    const item = this.getItem(index);
    if (item !== undefined && !this.selectable(item)) return;
    this.selected = index;
    this.render();
    if (item !== undefined && this.onSelect) this.onSelect(item, index);
  }

  // markiert einen Eintrag ohne onSelect auszulösen (Selektion von außen,
  // z.B. Raumwahl per Canvas-Klick) und scrollt ihn in den Sichtbereich
  highlight(index: number): void {
    this.selected = index;
    const top = index * this.rowHeight;
    if (top < this.container.scrollTop || top + this.rowHeight > this.container.scrollTop + this.container.clientHeight) {
      this.container.scrollTop = top - this.container.clientHeight / 2;
    }
    this.render();
  }

  private getItem(index: number): T | undefined {
    const page = Math.floor(index / PAGE_SIZE);
    return this.pages.get(page)?.[index - page * PAGE_SIZE];
  }

  private ensurePage(page: number): void {
    if (this.pages.has(page) || this.loading.has(page)) return;
    this.loading.add(page);
    const gen = this.generation;
    this.fetchPage(page * PAGE_SIZE, PAGE_SIZE)
      .then(result => {
        if (gen !== this.generation) return; // Liste wurde inzwischen zurückgesetzt
        this.loading.delete(page);
        this.pages.set(page, result.items);
        this.render();
        // ausstehende Selektion nachreichen (Klick auf noch ladende Zeile)
        if (this.selected >= 0 && this.onSelect) {
          const first = page * PAGE_SIZE;
          if (this.selected >= first && this.selected < first + result.items.length) {
            const item = result.items[this.selected - first];
            if (this.selectable(item)) this.onSelect(item, this.selected);
            else this.selected = -1; // nachgeladene Zeile ist doch nicht wählbar
          }
        }
      })
      .catch(() => this.loading.delete(page));
  }

  private render(): void {
    this.container.querySelectorAll('.vrow').forEach(r => r.remove());
    if (this.total === 0) return;

    const first = Math.max(0, Math.floor(this.container.scrollTop / this.rowHeight) - OVERSCAN);
    const last = Math.min(
      this.total - 1,
      Math.ceil((this.container.scrollTop + this.container.clientHeight) / this.rowHeight) + OVERSCAN,
    );

    for (let p = Math.floor(first / PAGE_SIZE); p <= Math.floor(last / PAGE_SIZE); p++) this.ensurePage(p);

    for (let i = first; i <= last; i++) {
      const item = this.getItem(i);
      const row = document.createElement('div');
      let cls = 'vrow' + (i === this.selected ? ' sel' : '');
      if (item !== undefined && !this.selectable(item)) cls += ' head';
      row.className = cls;
      row.style.top = i * this.rowHeight + 'px';
      row.style.height = this.rowHeight + 'px';
      row.dataset.index = String(i);
      row.innerHTML = item !== undefined ? this.renderRow(item, i) : '<span class="dim">…</span>';
      this.container.appendChild(row);
    }
  }
}
