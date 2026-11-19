import { STORE_CURRENCY } from '@cs/util/store';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CatalogApi } from '@cs/api/catalog.api';
import { CatalogAdminApi } from '@cs/api/catalog-admin.api';
import { Brand, Category, ProductListItem } from '@cs/api/models';
import { TranslocoPipe } from '@jsverse/transloco';
import { I18n } from '@cs/i18n/i18n';
import { Select } from '../ui/select';

@Component({
  selector: 'app-catalog-admin',
  imports: [ReactiveFormsModule, Select, TranslocoPipe],
  template: `
    <h1>{{ 'admin.nav.catalog' | transloco }}</h1>
    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (message()) { <div class="alert success">{{ message() }}</div> }

    <div class="cols">
      <section class="card">
        <h2>{{ 'admin.catalog.categories' | transloco }}</h2>
        <ul class="list">@for (c of categories(); track c.id) { <li>{{ c.name }} <span class="muted">/{{ c.slug }}</span></li> } @empty { <li class="muted">{{ 'admin.catalog.noneYet' | transloco }}</li> }</ul>
        <form [formGroup]="categoryForm" (ngSubmit)="createCategory()">
          <div class="field"><label>{{ 'admin.c.name' | transloco }}</label><input class="input" formControlName="name" /></div>
          <div class="field"><label>{{ 'admin.c.description' | transloco }}</label><input class="input" formControlName="description" /></div>
          <button class="btn sm" [disabled]="categoryForm.invalid || busy()">{{ 'admin.catalog.addCategory' | transloco }}</button>
        </form>
      </section>

      <section class="card">
        <h2>{{ 'admin.catalog.brands' | transloco }}</h2>
        <ul class="list">@for (b of brands(); track b.id) { <li>{{ b.name }} <span class="muted">/{{ b.slug }}</span></li> } @empty { <li class="muted">{{ 'admin.catalog.noneYet' | transloco }}</li> }</ul>
        <form [formGroup]="brandForm" (ngSubmit)="createBrand()">
          <div class="field"><label>{{ 'admin.c.name' | transloco }}</label><input class="input" formControlName="name" /></div>
          <div class="field"><label>{{ 'admin.c.description' | transloco }}</label><input class="input" formControlName="description" /></div>
          <button class="btn sm" [disabled]="brandForm.invalid || busy()">{{ 'admin.catalog.addBrand' | transloco }}</button>
        </form>
      </section>
    </div>

    <section class="card">
      <h2>{{ 'admin.catalog.newProduct' | transloco }}</h2>
      <form [formGroup]="productForm" (ngSubmit)="createProduct()">
        <div class="row">
          <div class="field" style="flex:2"><label>{{ 'admin.c.name' | transloco }}</label><input class="input" formControlName="name" /></div>
          <div class="field" style="flex:1"><label>{{ 'admin.c.category' | transloco }}</label>
            <cs-select formControlName="categoryId" [options]="categoryOptions()" [placeholder]="'admin.c.select' | transloco" />
          </div>
          <div class="field" style="flex:1"><label>{{ 'admin.catalog.brandOptional' | transloco }}</label>
            <cs-select formControlName="brandId" [options]="brandOptions()" [placeholder]="'admin.c.none' | transloco" />
          </div>
        </div>
        <div class="field"><label>{{ 'admin.c.description' | transloco }}</label><textarea rows="2" formControlName="description"></textarea></div>
        <h3>{{ 'admin.catalog.firstVariant' | transloco }}</h3>
        <div class="row">
          <div class="field" style="flex:1"><label>{{ 'admin.c.sku' | transloco }}</label><input class="input" formControlName="sku" /></div>
          <div class="field" style="flex:2"><label>{{ 'admin.catalog.variantName' | transloco }}</label><input class="input" formControlName="variantName" /></div>
          <div class="field" style="flex:1"><label>{{ 'admin.catalog.price' | transloco }}</label><input class="input" type="number" min="0.01" max="1000000" step="0.01" formControlName="unitPrice" /></div>
          <div class="field" style="flex:1"><label>{{ 'admin.c.currency' | transloco }}</label><input class="input" maxlength="3" formControlName="currency" readonly [title]="'admin.c.oneCurrency' | transloco" /></div>
        </div>
        <button class="btn" [disabled]="productForm.invalid || busy()">{{ 'admin.catalog.createProduct' | transloco }}</button>
      </form>
    </section>

    <section class="card">
      <h2>{{ 'admin.catalog.products' | transloco }}</h2>
      <p class="muted">{{ 'admin.catalog.productsSub' | transloco }}</p>
      <table>
        <thead><tr><th>{{ 'admin.c.name' | transloco }}</th><th>{{ 'admin.c.category' | transloco }}</th><th>{{ 'admin.c.status' | transloco }}</th><th></th></tr></thead>
        <tbody>
          @for (p of products(); track p.id) {
            <tr>
              <td>{{ p.name }}</td><td>{{ p.categoryName }}</td><td><span class="badge">{{ p.status }}</span></td>
              <td class="right">
                <button class="btn secondary sm" (click)="publish(p)">{{ 'admin.catalog.publish' | transloco }}</button>
                <button class="btn secondary sm" (click)="archive(p)">{{ 'admin.catalog.archive' | transloco }}</button>
              </td>
            </tr>
          } @empty { <tr><td colspan="4" class="muted">{{ 'admin.catalog.noProducts' | transloco }}</td></tr> }
        </tbody>
      </table>
    </section>
  `,
  styles: [`
    .cols { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    .list { margin: 0 0 1rem; padding-left: 1.1rem; }
    section { margin-bottom: 1.25rem; }
    @media (max-width: 760px) { .cols { grid-template-columns: 1fr; } }
  `],
})
export class CatalogAdmin implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(CatalogApi);
  private readonly admin = inject(CatalogAdminApi);
  private readonly i18n = inject(I18n);

  readonly categories = signal<Category[]>([]);
  readonly brands = signal<Brand[]>([]);
  readonly products = signal<ProductListItem[]>([]);
  readonly categoryOptions = computed(() => this.categories().map((c) => ({ value: c.id, label: c.name })));
  readonly brandOptions = computed(() => {
    this.i18n.lang(); // recompute when the language changes
    return [{ value: '', label: this.i18n.t('admin.c.none') }, ...this.brands().map((b) => ({ value: b.id, label: b.name }))];
  });
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busy = signal(false);

  readonly categoryForm = this.fb.nonNullable.group({ name: ['', Validators.required], description: [''] });
  readonly brandForm = this.fb.nonNullable.group({ name: ['', Validators.required], description: [''] });
  readonly productForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    description: ['', Validators.required],
    categoryId: ['', Validators.required],
    brandId: [''],
    sku: ['', Validators.required],
    variantName: ['', Validators.required],
    unitPrice: [0, [Validators.required, Validators.min(0.01), Validators.max(1_000_000)]],
    currency: [STORE_CURRENCY, [Validators.required, Validators.maxLength(3)]],
  });

  ngOnInit(): void { this.reloadRefs(); this.reloadProducts(); }

  private reloadRefs(): void {
    this.api.listCategories().subscribe({ next: (c) => this.categories.set(c), error: () => {} });
    this.api.listBrands().subscribe({ next: (b) => this.brands.set(b), error: () => {} });
  }
  private reloadProducts(): void {
    this.api.searchProducts({ pageSize: 50 }).subscribe({ next: (r) => this.products.set(r.items), error: () => {} });
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
  createProduct(): void {
    if (this.productForm.invalid) return;
    this.busy.set(true);
    const v = this.productForm.getRawValue();
    this.admin.createProduct({
      name: v.name, description: v.description, categoryId: v.categoryId, brandId: v.brandId || null,
      variants: [{ sku: v.sku, name: v.variantName, unitPrice: Number(v.unitPrice), currency: v.currency.toUpperCase() }],
    }).subscribe({
      next: () => { this.ok(this.i18n.t('admin.catalog.productCreated')); this.productForm.reset({ currency: STORE_CURRENCY, unitPrice: 0 }); this.reloadProducts(); },
      error: (e) => this.fail(e),
    });
  }
  publish(p: ProductListItem): void { this.busy.set(true); this.admin.publish(p.id).subscribe({ next: () => { this.ok(this.i18n.t('admin.catalog.published')); this.reloadProducts(); }, error: (e) => this.fail(e) }); }
  archive(p: ProductListItem): void { this.busy.set(true); this.admin.archive(p.id).subscribe({ next: () => { this.ok(this.i18n.t('admin.catalog.archived')); this.reloadProducts(); }, error: (e) => this.fail(e) }); }
}
