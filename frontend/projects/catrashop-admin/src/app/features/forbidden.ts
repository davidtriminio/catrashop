import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Icon } from '../ui/icon';

@Component({
  selector: 'app-forbidden',
  imports: [RouterLink, Icon],
  template: `
    <div class="card denied">
      <span class="ico"><cs-icon name="shield" [size]="26" /></span>
      <h1>Access denied</h1>
      <p class="muted">Your account doesn't have permission to view that section.</p>
      <a routerLink="/dashboard" class="btn">Back to dashboard</a>
    </div>
  `,
  styles: [`
    .denied { max-width: 460px; margin: var(--space-7) auto 0 !important; text-align: center; padding: var(--space-6); }
    .ico { display: inline-grid; place-items: center; width: 56px; height: 56px; border-radius: 16px; margin-bottom: var(--space-4); background: var(--tint-bad-bg); color: var(--tint-bad-fg); }
  `],
})
export class Forbidden {}
