import { Component, computed, input } from '@angular/core';

/** Rounded rectangle as a path, so every icon below is just a list of `d` strings (no innerHTML, no icon dependency). */
const rect = (x: number, y: number, w: number, h: number, r = 1): string =>
  `M${x + r} ${y}h${w - 2 * r}a${r} ${r} 0 0 1 ${r} ${r}v${h - 2 * r}a${r} ${r} 0 0 1-${r} ${r}H${x + r}a${r} ${r} 0 0 1-${r}-${r}V${y + r}a${r} ${r} 0 0 1 ${r}-${r}z`;
const circle = (cx: number, cy: number, r: number): string =>
  `M${cx - r} ${cy}a${r} ${r} 0 1 0 ${2 * r} 0a${r} ${r} 0 1 0-${2 * r} 0`;

/** Paths follow the Lucide icon set (ISC licence), 24x24, 2px stroke. */
const ICONS = {
  dashboard: [rect(3, 3, 7, 9), rect(14, 3, 7, 5), rect(14, 12, 7, 9), rect(3, 16, 7, 5)],
  package: ['M11 21.73a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73z', 'M12 22V12', 'm3.3 7 8.7 5 8.7-5', 'm7.5 4.27 9 5.15'],
  warehouse: ['M22 8.35V20a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V8.35A2 2 0 0 1 3.26 6.5l8-3.2a2 2 0 0 1 1.48 0l8 3.2A2 2 0 0 1 22 8.35Z', 'M6 18h12', 'M6 14h12', rect(6, 10, 12, 12, 0)],
  bag: ['M6 2 3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4Z', 'M3 6h18', 'M16 10a4 4 0 0 1-8 0'],
  card: [rect(2, 5, 20, 14, 2), 'M2 10h20'],
  tag: ['M12.586 2.586A2 2 0 0 0 11.172 2H4a2 2 0 0 0-2 2v7.172a2 2 0 0 0 .586 1.414l8.704 8.704a2.426 2.426 0 0 0 3.42 0l6.58-6.58a2.426 2.426 0 0 0 0-3.42z', circle(7.5, 7.5, 0.5)],
  truck: ['M14 18V6a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2v11a1 1 0 0 0 1 1h2', 'M15 18H9', 'M19 18h2a1 1 0 0 0 1-1v-3.65a1 1 0 0 0-.22-.624l-3.48-4.35A1 1 0 0 0 17.52 8H14', circle(17, 18, 2), circle(7, 18, 2)],
  bell: ['M10.268 21a2 2 0 0 0 3.464 0', 'M3.262 15.326A1 1 0 0 0 4 17h16a1 1 0 0 0 .74-1.673C19.41 13.956 18 12.499 18 8A6 6 0 0 0 6 8c0 4.499-1.411 5.956-2.738 7.326'],
  sun: [circle(12, 12, 4), 'M12 2v2', 'M12 20v2', 'm4.93 4.93 1.41 1.41', 'm17.66 17.66 1.41 1.41', 'M2 12h2', 'M20 12h2', 'm6.34 17.66-1.41 1.41', 'm19.07 4.93-1.41 1.41'],
  moon: ['M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z'],
  monitor: [rect(2, 3, 20, 14, 2), 'M8 21h8', 'M12 17v4'],
  logout: ['M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4', 'm16 17 5-5-5-5', 'M21 12H9'],
  menu: ['M4 12h16', 'M4 6h16', 'M4 18h16'],
  close: ['M18 6 6 18', 'm6 6 12 12'],
  arrow: ['M5 12h14', 'm12 5 7 7-7 7'],
  trend: ['M16 7h6v6', 'm22 7-8.5 8.5-5-5L2 17'],
  clock: [circle(12, 12, 10), 'M12 6v6l4 2'],
  check: ['M21.801 10A10 10 0 1 1 17 3.335', 'm9 11 3 3L22 4'],
  chevron: ['m6 9 6 6 6-6'],
  tick: ['M20 6 9 17l-5-5'],
  shield: ['M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z'],
} as const;

export type IconName = keyof typeof ICONS;

@Component({
  selector: 'cs-icon',
  template: `
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"
         stroke-linejoin="round" aria-hidden="true" focusable="false"
         [attr.width]="size()" [attr.height]="size()">
      @for (d of paths(); track $index) { <path [attr.d]="d" /> }
    </svg>
  `,
  styles: [`:host { display: inline-flex; flex-shrink: 0; line-height: 0; } svg { display: block; }`],
})
export class Icon {
  readonly name = input.required<IconName>();
  readonly size = input(20);
  readonly paths = computed<readonly string[]>(() => ICONS[this.name()]);
}
