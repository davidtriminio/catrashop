import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { Icon } from '../ui/icon';

@Component({
  selector: 'app-forbidden',
  imports: [RouterLink, Icon, TranslocoPipe],
  template: `
    <div class="card denied">
      <span class="ico"><cs-icon name="shield" [size]="26" /></span>
      <h1>{{ 'admin.forbidden.title' | transloco }}</h1>
      <p class="muted">{{ 'admin.forbidden.text' | transloco }}</p>
      <a routerLink="/dashboard" class="btn">{{ 'admin.forbidden.back' | transloco }}</a>
    </div>
  `,
  styles: [`
    .denied { max-width: 460px; margin: var(--space-7) auto 0 !important; text-align: center; padding: var(--space-6); }
    .ico { display: inline-grid; place-items: center; width: 56px; height: 56px; border-radius: 16px; margin-bottom: var(--space-4); background: var(--tint-bad-bg); color: var(--tint-bad-fg); }
  `],
})
export class Forbidden {}
