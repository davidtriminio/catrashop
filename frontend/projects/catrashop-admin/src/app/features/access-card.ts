import { Component, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { I18n } from '@cs/i18n/i18n';
import { AuthStore } from '@cs/auth/auth.store';
import { Icon } from '../ui/icon';

/** What the signed-in account can do: its roles and, in plain words, the permissions that unlock admin sections. */
@Component({
  selector: 'app-access-card',
  imports: [Icon, TranslocoPipe],
  template: `
    @if (auth.user(); as u) {
      <section class="card access" aria-labelledby="access-title">
        <h2 id="access-title"><cs-icon name="shield" [size]="16" /> {{ 'admin.access.title' | transloco }}</h2>
        <div class="roles">
          <span class="muted">{{ 'admin.access.roles' | transloco }}</span>
          @for (r of u.roles; track r) { <span class="role">{{ roleLabel(r) }}</span> }
          @if (u.roles.length === 0) { <span class="muted">—</span> }
        </div>
        @if (u.permissions.length) {
          <p class="muted can">{{ 'admin.access.can' | transloco }}</p>
          <ul class="perms">
            @for (p of u.permissions; track p) {
              <li [title]="p"><cs-icon name="check" [size]="15" /> {{ permissionLabel(p) }}</li>
            }
          </ul>
        } @else { <p class="muted">{{ 'admin.access.none' | transloco }}</p> }
      </section>
    }
  `,
  styles: [`
    h2 { display: flex; align-items: center; gap: var(--space-2); }
    .roles { display: flex; align-items: center; flex-wrap: wrap; gap: var(--space-2); margin-bottom: var(--space-3); }
    .role { padding: 0.2rem 0.7rem; border-radius: var(--radius-pill); background: var(--accent); color: var(--accent-ink); font-size: 0.8rem; font-weight: 650; }
    .can { font-size: 0.8rem; margin-bottom: var(--space-2); }
    .perms { list-style: none; margin: 0; padding: 0; display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: var(--space-2) var(--space-5); }
    .perms li { display: flex; align-items: center; gap: var(--space-2); font-size: 0.9rem; }
    .perms cs-icon { color: var(--chart-5); }
  `],
})
export class AccessCard {
  readonly auth = inject(AuthStore);
  private readonly i18n = inject(I18n);

  /** Role and permission codes come from the API; show a readable name, or the code itself for one added later. */
  roleLabel(role: string): string {
    this.i18n.lang(); // recompute when the language changes
    return this.i18n.tOr(`admin.role.${role}`, role);
  }

  permissionLabel(permission: string): string {
    this.i18n.lang();
    return this.i18n.tOr(`admin.perm.${permission}`, permission);
  }
}
