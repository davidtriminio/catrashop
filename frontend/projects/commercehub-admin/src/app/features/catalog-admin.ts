import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CatalogApi } from '@ch/api/catalog.api';
import { CatalogAdminApi } from '@ch/api/catalog-admin.api';
import { Brand, Category, ProductListItem } from '@ch/api/models';
import { problemMessage } from '@ch/util/problem';

@Component({
  selector: 'app-catalog-admin',
  imports: [ReactiveFormsModule],
  template: `
    <h1>Catalog</h1>
    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (message()) { <div class="alert success">{{ message() }}</div> }

    <div class="cols">
      <section class="card">
        <h2>Categories</h2>
        <ul class="list">@for (c of categories(); track c.id) { <li>{{ c.name }} <span class="muted">/{{ c.slug }}</span></li> } @empty { <li class="muted">None yet.</li> }</ul>
        <form [formGroup]="categoryForm" (ngSubmit)="createCategory()">
          <div class="field"><label>Name</label><input class="input" formControlName="name" /></div>
          <div class="field"><label>Description</label><input class="input" formControlName="description" /></div>
          <button class="btn sm" [disabled]="categoryForm.invalid || busy()">Add category</button>
        </form>
      </section>

      <section class="card">
        <h2>Brands</h2>
        <ul class="list">@for (b of brands(); track b.id) { <li>{{ b.name }} <span class="muted">/{{ b.slug }}</span></li> } @empty { <li class="muted">None yet.</li> }</ul>
        <form [formGroup]="brandForm" (ngSubmit)="createBrand()">
          <div class="field"><label>Name</label><input class="input" formControlName="name" /></div>
          <div class="field"><label>Description</label><input class="input" formControlName="description" /></div>
          <button class="btn sm" [disabled]="brandForm.invalid || busy()">Add brand</button>
        </form>
      </section>
    </div>

    <section class="card">
      <h2>New product</h2>
      <form [formGroup]="productForm" (ngSubmit)="createProduct()">
        <div class="row">
          <div class="field" style="flex:2"><label>Name</label><input class="input" formControlName="name" /></div>
          <div class="field" style="flex:1"><label>Category</label>
            <select formControlName="categoryId">
              <option value="">Select…</option>
              @for (c of categories(); track c.id) { <option [value]="c.id">{{ c.name }}</option> }
            </select>
          </div>
          <div class="field" style="flex:1"><label>Brand (optional)</label>
            <select formControlName="brandId">
              <option value="">None</option>
              @for (b of brands(); track b.id) { <option [value]="b.id">{{ b.name }}</option> }
            </select>
          </div>
        </div>
        <div class="field"><label>Description</label><textarea rows="2" formControlName="description"></textarea></div>
        <h3>First variant</h3>
        <div class="row">
          <div class="field" style="flex:1"><label>SKU</label><input class="input" formControlName="sku" /></div>
          <div class="field" style="flex:2"><label>Variant name</label><input class="input" formControlName="variantName" /></div>
          <div class="field" style="flex:1"><label>Price</label><input class="input" type="number" min="0.01" max="1000000" step="0.01" formControlName="unitPrice" /></div>
          <div class="field" style="flex:1"><label>Currency</label><input class="input" maxlength="3" formControlName="currency" /></div>
        </div>
        <button class="btn" [disabled]="productForm.invalid || busy()">Create product (draft)</button>
      </form>
    </section>

    <section class="card">
      <h2>Products</h2>
      <p class="muted">Published products (the catalog search returns active items).</p>
      <table>
        <thead><tr><th>Name</th><th>Category</th><th>Status</th><th></th></tr></thead>
        <tbody>
          @for (p of products(); track p.id) {
            <tr>
              <td>{{ p.name }}</td><td>{{ p.categoryName }}</td><td><span class="badge">{{ p.status }}</span></td>
              <td class="right">
                <button class="btn secondary sm" (click)="publish(p)">Publish</button>
                <button class="btn secondary sm" (click)="archive(p)">Archive</button>
              </td>
            </tr>
          } @empty { <tr><td colspan="4" class="muted">No products found.</td></tr> }
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

  readonly categories = signal<Category[]>([]);
  readonly brands = signal<Brand[]>([]);
  readonly products = signal<ProductListItem[]>([]);
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
    currency: ['USD', [Validators.required, Validators.maxLength(3)]],
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
  private fail(e: unknown): void { this.error.set(problemMessage(e)); this.busy.set(false); }

  createCategory(): void {
    if (this.categoryForm.invalid) return;
    this.busy.set(true);
    const v = this.categoryForm.getRawValue();
    this.admin.createCategory({ name: v.name, description: v.description || null }).subscribe({
      next: () => { this.ok('Category created.'); this.categoryForm.reset(); this.reloadRefs(); }, error: (e) => this.fail(e),
    });
  }
  createBrand(): void {
    if (this.brandForm.invalid) return;
    this.busy.set(true);
    const v = this.brandForm.getRawValue();
    this.admin.createBrand({ name: v.name, description: v.description || null }).subscribe({
      next: () => { this.ok('Brand created.'); this.brandForm.reset(); this.reloadRefs(); }, error: (e) => this.fail(e),
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
      next: () => { this.ok('Product created as draft. Publish it to make it visible.'); this.productForm.reset({ currency: 'USD', unitPrice: 0 }); this.reloadProducts(); },
      error: (e) => this.fail(e),
    });
  }
  publish(p: ProductListItem): void { this.busy.set(true); this.admin.publish(p.id).subscribe({ next: () => { this.ok('Published.'); this.reloadProducts(); }, error: (e) => this.fail(e) }); }
  archive(p: ProductListItem): void { this.busy.set(true); this.admin.archive(p.id).subscribe({ next: () => { this.ok('Archived.'); this.reloadProducts(); }, error: (e) => this.fail(e) }); }
}
