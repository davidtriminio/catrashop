import { Injectable, computed, effect, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark' | 'system';
const KEY = 'catrashop-admin-theme';
const MODES: ThemeMode[] = ['light', 'dark', 'system'];

function readStored(): ThemeMode {
  try {
    const v = localStorage.getItem(KEY);
    return MODES.includes(v as ThemeMode) ? (v as ThemeMode) : 'system';
  } catch {
    return 'system';
  }
}

/** Light / dark / follow-the-system, saved per browser. index.html applies the saved choice before first paint. */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly mode = signal<ThemeMode>(readStored());
  private readonly systemDark = signal(matchMedia('(prefers-color-scheme: dark)').matches);
  readonly resolved = computed<'light' | 'dark'>(() =>
    this.mode() === 'system' ? (this.systemDark() ? 'dark' : 'light') : (this.mode() as 'light' | 'dark'));

  constructor() {
    matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => this.systemDark.set(e.matches));
    effect(() => {
      const root = document.documentElement;
      root.dataset['theme'] = this.resolved();
      root.style.colorScheme = this.resolved();
      try { localStorage.setItem(KEY, this.mode()); } catch { /* storage blocked: the choice just won't persist */ }
    });
  }

  /** light -> dark -> system -> light */
  cycle(): void {
    this.mode.set(MODES[(MODES.indexOf(this.mode()) + 1) % MODES.length]);
  }
}
