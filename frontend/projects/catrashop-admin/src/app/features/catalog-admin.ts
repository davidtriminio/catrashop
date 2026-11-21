import { Component, OnInit, inject, signal } from '@angular/core';
import { BusyButton } from '@cs/ui/busy-button';
import { TableSkeleton } from '../ui/skeleton';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { CatalogApi } from '@cs/api/catalog.api';
import { CatalogAdminApi } from '@cs/api/catalog-admin.api';
import { Brand, Category, Paged, ProductListItem } from '@cs/api/models';
import { I18n } from '@cs/i18n/i18n';
import { MoneyPipe } from '@cs/util/money.pipe';
import { Icon } from '../ui/icon';
import { ProductEditor } from './product-editor';

type Filter = 'All' | 'Active' | 'Draft' | 'Archived';
const FILTERS: Filter[] = ['All', 'Active', 'Draft', 'Archived'];

@Component({
  selector: 'app-catalog-admin',
  imports: [ReactiveFormsModule, TranslocoPipe, MoneyPipe, Icon, ProductEditor, BusyButton, TableSkeleton],
  template: `
    <div class="page-head">
      <div>
        <h1>{{ 'admin.nav.catalog' | transloco }}</h1>
        @if (result(); as r) { <p class="muted count">{{ r.totalCount }} {{ 'admin.products.count' | transloco }}</p> }
      </div>
      <button type="button" class="btn" (click)="open(null)"><cs-icon name="plus" [size]="16" /> {{ 'admin.products.new' | transloco }}</button>
    </div>

    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (message()) { <div class="alert success">{{ message() }}</div> }

    <div class="toolbar">
      <div class="chips" role="group" [attr.aria-label]="'admin.orders.filter' | transloco">
        @for (f of filters; track f) {
          <button class="chip" type="button" [class.active]="filter() === f" (click)="setFilter(f)">{{ 'admin.products.filter.' + f | transloco }}</button>
        }
      </div>
      <input class="input search" type="search" [placeholder]="'admin.products.search' | transloco" [value]="q()" (input)="onSearch($any($event.target).value)" />
    </div>

    @if (!result()) {
      <div class="card flush"><app-table-skeleton [rows]="6" [cols]="6" /></div>
    } @else {
    <div class="card flush" [class.dim]="loading()">
      <div class="scroll">
        <table>
          <thead><tr><th>{{ 'admin.products.product' | transloco }}</th><th>{{ 'admin.c.category' | transloco }}</th><th>{{ 'admin.catalog.price' | transloco }}</th><th>{{ 'admin.products.variantsCol' | transloco }}</th><th>{{ 'admin.c.status' | transloco }}</th><th></th></tr></thead>
          <tbody>
            @for (p of result()?.items ?? []; track p.id) {
              <tr class="row-link" (click)="open(p.id)">
                <td><div class="prod">
                  <span class="thumb">@if (p.primaryImageUrl) { <img [src]="admin.imageSrc(p.primaryImageUrl)" [alt]="p.name" loading="lazy" /> } @else { <cs-icon name="image" [size]="18" /> }</span>
                  <span><strong>{{ p.name }}</strong>@if (p.brandName) { <br /><span class="muted">{{ p.brandName }}</span> }</span>
                </div></td>
                <td>{{ p.categoryName }}</td>
                <td>@if (p.minPrice !== null) { {{ p.minPrice | money: p.currency }}@if (p.maxPrice !== p.minPrice) { – {{ p.maxPrice | money: p.currency }} } } @else { <span class="muted">–</span> }</td>
                <td>{{ p.variantCount }}</td>
                <td><span class="badge" [class]="tone(p.status)">{{ 'admin.products.status.' + p.status | transloco }}</span></td>
                <td class="right actions" (click)="$event.stopPropagation()">
                  @if (p.status === 'Draft') { <button class="btn secondary sm" type="button" [csBusy]="busy()" [csBusy]="busy()" [disabled]="busy()" (click)="setStatus(p, true)">{{ 'admin.products.publish' | transloco }}</button> }
                  @if (p.status === 'Archived') { <button class="btn secondary sm" type="button" [csBusy]="busy()" [csBusy]="busy()" [disabled]="busy()" (click)="setStatus(p, true)"><cs-icon name="restore" [size]="14" /> {{ 'admin.products.restore' | transloco }}</button> }
                  @if (p.status === 'Active') { <button class="btn secondary sm" type="button" [csBusy]="busy()" [csBusy]="busy()" [disabled]="busy()" (click)="setStatus(p, false)">{{ 'admin.products.archive' | transloco }}</button> }
                  <button class="btn secondary sm" type="button" (click)="open(p.id)"><cs-icon name="pencil" [size]="14" /> {{ 'admin.c.edit' | transloco }}</button>
                </td>
              </tr>
            } @empty {
              <tr><td colspan="6" class="empty">{{ 'admin.products.empty' | transloco }}</td></tr>
            }
          </tbody>
        </table>
      </div>
    </div>
    }

    @if (result(); as r) {
      @if (r.totalPages > 1) {
        <nav class="pager" [attr.aria-label]="'pagination.label' | transloco">
          <button class="btn secondary sm" type="button" [disabled]="!r.hasPrevious" (click)="go(r.page - 1)">{{ 'pagination.previous' | transloco }}</button>
          <span class="muted">{{ r.page }} / {{ r.totalPages }}</span>
          <button class="btn secondary sm" type="button" [disabled]="!r.hasNext" (click)="go(r.page + 1)">{{ 'pagination.next' | transloco }}</button>
        </nav>
      }
    }

    <h2 class="section-title">{{ 'admin.products.taxonomy' | transloco }}</h2>
    <div class="cols">
      <section class="card">
        <h2>{{ 'admin.catalog.categories' | transloco }}</h2>
        @if (!catsLoaded()) { <app-table-skeleton class="mini" [head]="false" [rows]="3" [cols]="1" /> } @else {
        <ul class="list">@for (c of categories(); track c.id) { <li>{{ c.name }} <span class="muted">/{{ c.slug }}</span></li> } @empty { <li class="muted">{{ 'admin.catalog.noneYet' | transloco }}</li> }</ul>
        }
        <form [formGroup]="categoryForm" (ngSubmit)="createCategory()">
          <div class="field"><label>{{ 'admin.c.name' | transloco }}</label><input class="input" formControlName="name" /></div>
          <div class="field"><label>{{ 'admin.c.description' | transloco }}</label><input class="input" formControlName="description" /></div>
          <button class="btn sm" [csBusy]="busy()" [disabled]="categoryForm.invalid || busy()">{{ 'admin.catalog.addCategory' | transloco }}</button>
        </form>
      </section>

      <section class="card">
        <h2>{{ 'admin.catalog.brands' | transloco }}</h2>
        @if (!brandsLoaded()) { <app-table-skeleton class="mini" [head]="false" [rows]="3" [cols]="1" /> } @else {
        <ul class="list">@for (b of brands(); track b.id) { <li>{{ b.name }} <span class="muted">/{{ b.slug }}</span></li> } @empty { <li class="muted">{{ 'admin.catalog.noneYet' | transloco }}</li> }</ul>
        }
        <form [formGroup]="brandForm" (ngSubmit)="createBrand()">
          <div class="field"><label>{{ 'admin.c.name' | transloco }}</label><input class="input" formControlName="name" /></div>
          <div class="field"><label>{{ 'admin.c.description' | transloco }}</label><input class="input" formControlName="description" /></div>
          <button class="btn sm" [csBusy]="busy()" [disabled]="brandForm.invalid || busy()">{{ 'admin.catalog.addBrand' | transloco }}</button>
        </form>
      </section>
    </div>

    @if (editing(); as e) {
      <app-product-editor [productId]="e.id" [categories]="categories()" [brands]="brands()" (closed)="close()" (changed)="reload()" />
    }
  `,
  styles: [`
    .count { margin: var(--space-1) 0 0; }
    .toolbar { display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: var(--space-3); margin-bottom: var(--space-4); }
    .search { max-width: 300px; }
    .dim { opacity: 0.6; }
    .scroll { overflow-x: auto; }
    td { white-space: nowrap; }
    .row-link { cursor: pointer; }
    .prod { display: flex; align-items: center; gap: var(--space-3); }
    .thumb { display: grid; place-items: center; width: 44px; height: 44px; flex-shrink: 0; border-radius: 8px; overflow: hidden; background: var(--surface-2); border: 1px solid var(--border); color: var(--muted); }
    .thumb img { width: 100%; height: 100%; object-fit: cover; }
    .actions { display: flex; gap: var(--space-2); justify-content: flex-end; }
    .list { margin: 0 0 var(--space-4); padding-left: 1.1rem; }
    .pager { display: flex; align-items: center; justify-content: center; gap: var(--space-3); margin: var(--space-5) 0 var(--space-2); }
  `],
})
export class CatalogAdmin implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(CatalogApi);
  readonly admin = inject(CatalogAdminApi);
  private readonly i18n = inject(I18n);
  private timer: ReturnType<typeof setTimeout> | undefined;

  readonly filters = FILTERS;
  readonly filter = signal<Filter>('All');
  readonly q = signal('');
  private page = 1;

  readonly categories = signal<Category[]>([]);
  readonly brands = signal<Brand[]>([]);
  readonly catsLoaded = signal(false);
  readonly brandsLoaded = signal(false);
  readonly result = signal<Paged<ProductListItem> | null>(null);
  readonly loading = signal(false);
  readonly editing = signal<{ id: string | null } | null>(null);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busy = signal(false);

  readonly categoryForm = this.fb.nonNullable.group({ name: ['', Validators.required], description: [''] });
  readonly brandForm = this.fb.nonNullable.group({ name: ['', Validators.required], description: [''] });

  ngOnInit(): void { this.reloadRefs(); this.reload(); }

  tone(status: string): string { return status === 'Active' ? 'ok' : status === 'Draft' ? 'warn' : ''; }

  private reloadRefs(): void {
    this.api.listCategories().subscribe({ next: (c) => { this.categories.set(c); this.catsLoaded.set(true); }, error: () => this.catsLoaded.set(true) });
    this.api.listBrands().subscribe({ next: (b) => { this.brands.set(b); this.brandsLoaded.set(true); }, error: () => this.brandsLoaded.set(true) });
  }

  reload(): void {
    this.loading.set(true);
    this.admin.listProducts(this.filter() === 'All' ? null : this.filter(), this.q(), this.page, 20).subscribe({
      next: (r) => { this.result.set(r); this.loading.set(false); },
      error: (e) => { this.error.set(this.i18n.problem(e)); this.loading.set(false); },
    });
  }

  setFilter(f: Filter): void { this.filter.set(f); this.page = 1; this.reload(); }
  go(p: number): void { this.page = p; this.reload(); }

  onSearch(value: string): void {
    this.q.set(value);
    clearTimeout(this.timer);
    this.timer = setTimeout(() => { this.page = 1; this.reload(); }, 300);
  }

  open(id: string | null): void { this.editing.set({ id }); }
  close(): void { this.editing.set(null); this.reloadRefs(); }

  /** Publish (also restores an archived product) or archive straight from the list. */
  setStatus(p: ProductListItem, publish: boolean): void {
    this.busy.set(true);
    this.error.set(null);
    this.message.set(null);
    const text = publish ? (p.status === 'Archived' ? 'admin.products.restored' : 'admin.products.published') : 'admin.products.archivedDone';
    (publish ? this.admin.publish(p.id) : this.admin.archive(p.id)).subscribe({
      next: () => { this.message.set(this.i18n.t(text)); this.busy.set(false); this.reload(); },
      error: (e) => { this.error.set(this.i18n.problem(e)); this.busy.set(false); },
    });
  }

  private ok(m: string): void { this.message.set(m); this.error.set(null); this.busy.set(false); }
  private fail(e: unknown): void { this.error.set(this.i18n.problem(e)); this.busy.set(false); }

  createCategory(): void {
    if (this.categoryForm.invalid) return;
    this.busy.set(true);
    const v = this.categoryForm.getRawValue();
    this.admin.createCategory({ name: v.name, description: v.description || null }).subscribe({
      next: () => { this.ok(this.i18n.t('admin.catalog.categoryCreated')); this.categoryForm.reset(); this.reloadRefs(); }, error: (e) => this.fail(e),
    });
  }
  createBrand(): void {
    if (this.brandForm.invalid) return;
    this.busy.set(true);
    const v = this.brandForm.getRawValue();
    this.admin.createBrand({ name: v.name, description: v.description || null }).subscribe({
      next: () => { this.ok(this.i18n.t('admin.catalog.brandCreated')); this.brandForm.reset(); this.reloadRefs(); }, error: (e) => this.fail(e),
    });
  }
}
