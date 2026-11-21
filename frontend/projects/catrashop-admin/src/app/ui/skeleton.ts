import { Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

const WIDTHS = [62, 38, 48, 28, 54, 34, 44, 24];

/**
 * Placeholder rows shaped like a table, shown while a list loads so the page keeps its layout instead of jumping when
 * the data arrives. Screen readers hear "Loading…" once; the shimmer is decorative.
 */
@Component({
  selector: 'app-table-skeleton',
  imports: [TranslocoPipe],
  template: `
    <div class="sk" role="status" [style.--cols]="cols()">
      <span class="sr">{{ 'common.loading' | transloco }}</span>
      @if (head()) { <div class="sk-row head" aria-hidden="true">@for (c of colList(); track c) { <span class="skeleton"></span> }</div> }
      @for (r of rowList(); track r) {
        <div class="sk-row" aria-hidden="true">
          @for (c of colList(); track c) { <span class="skeleton" [style.width.%]="width(r, c)"></span> }
        </div>
      }
    </div>
  `,
  styles: [`
    .sk-row { display: grid; grid-template-columns: repeat(var(--cols), minmax(0, 1fr)); align-items: center; gap: var(--space-5); padding: 1.15rem var(--space-5); border-bottom: 1px solid var(--border); }
    .sk-row:last-child { border-bottom: 0; }
    .sk-row.head { padding-block: 0.8rem; background: var(--surface-2); }
    .sk-row.head .skeleton { height: 0.6rem; width: 45%; opacity: 0.6; }
    .skeleton { display: block; height: 0.9rem; }
    .sr { position: absolute; width: 1px; height: 1px; overflow: hidden; clip-path: inset(50%); }
  `],
})
export class TableSkeleton {
  readonly rows = input(5);
  readonly cols = input(5);
  readonly head = input(true);
  readonly rowList = computed(() => Array.from({ length: this.rows() }, (_, i) => i));
  readonly colList = computed(() => Array.from({ length: this.cols() }, (_, i) => i));

  /** Varied but stable widths, so the placeholder looks like text and does not flicker between renders. */
  width(row: number, col: number): number { return WIDTHS[(row * 3 + col * 5) % WIDTHS.length] + 30; }
}
