import { Component, ElementRef, OnDestroy, OnInit, computed, inject, input, output, signal, viewChild } from '@angular/core';
import { BusyButton } from '../ui/busy-button';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { concatMap, from } from 'rxjs';
import { CatalogAdminApi } from '@cs/api/catalog-admin.api';
import { AdminProductDetail, Brand, Category } from '@cs/api/models';
import { I18n } from '@cs/i18n/i18n';
import { STORE_CURRENCY } from '@cs/util/store';
import { Icon } from '../ui/icon';
import { Select, SelectOption } from '../ui/select';

const MAX_IMAGES = 10;
const MAX_MB = 5;
const TYPES = ['image/jpeg', 'image/png', 'image/webp'];

interface VariantRow { id: string; sku: string; name: string; unitPrice: number; isActive: boolean; original: string; }

/**
 * Create or edit one product in a dialog: details, photos (upload, main photo, remove) and variants (name, price,
 * availability). A new product is created as a draft; right after, the dialog switches to edit mode so photos can be
 * added (they need the product to exist). The list behind it is told through `changed` whenever something is saved.
 */
@Component({
  selector: 'app-product-editor',
  imports: [ReactiveFormsModule, FormsModule, TranslocoPipe, Icon, Select, BusyButton],
  template: `
    <div class="overlay" (mousedown)="onBackdrop($event)">
      <div #dialog class="dialog" role="dialog" aria-modal="true" aria-labelledby="pe-title" tabindex="-1" (keydown.escape)="closed.emit()">
        <header class="dialog-head">
          <h2 id="pe-title">{{ (id() ? 'admin.products.editTitle' : 'admin.products.createTitle') | transloco }}</h2>
          @if (product(); as p) { <span class="badge" [class]="tone(p.status)">{{ 'admin.products.status.' + p.status | transloco }}</span> }
          <span class="spacer"></span>
          @if (product(); as p) {
            @if (p.status !== 'Active') {
              <button type="button" class="btn sm" [csBusy]="busy()" [disabled]="busy()" (click)="setStatus(true)">
                <cs-icon [name]="p.status === 'Archived' ? 'restore' : 'check'" [size]="14" />
                {{ (p.status === 'Archived' ? 'admin.products.restore' : 'admin.products.publish') | transloco }}
              </button>
            }
            @if (p.status !== 'Archived') {
              <button type="button" class="btn secondary sm" [csBusy]="busy()" [disabled]="busy()" (click)="setStatus(false)"><cs-icon name="archive" [size]="14" /> {{ 'admin.products.archive' | transloco }}</button>
            }
          }
          <button type="button" class="icon-btn" [attr.aria-label]="'admin.receipt.close' | transloco" (click)="closed.emit()"><cs-icon name="close" /></button>
        </header>

        <div class="editor">
          @if (error()) { <div class="alert error">{{ error() }}</div> }
          @if (message()) { <div class="alert success">{{ message() }}</div> }

          @if (loadingProduct() && !error()) {
            <div role="status" aria-busy="true">
              <span class="skeleton sk-text" style="width: 6rem; margin-bottom: var(--space-3)"></span>
              <span class="skeleton sk-block" style="height: 2.7rem; margin-bottom: var(--space-4)"></span>
              <span class="skeleton sk-block" style="height: 5.5rem; margin-bottom: var(--space-4)"></span>
              <div class="row"><span class="skeleton sk-block" style="height: 2.7rem; flex: 1"></span><span class="skeleton sk-block" style="height: 2.7rem; flex: 1"></span></div>
              <span class="skeleton sk-text" style="width: 5rem; margin: var(--space-6) 0 var(--space-3)"></span>
              <div class="photos">@for (n of [1, 2, 3]; track n) { <span class="skeleton sk-block ph"></span> }</div>
            </div>
          }
          <div [class.hide]="loadingProduct()">
          <section>
            <h3>{{ 'admin.products.details' | transloco }}</h3>
            <form [formGroup]="form" (ngSubmit)="saveDetails()">
              <div class="field"><label>{{ 'admin.c.name' | transloco }}</label><input class="input" formControlName="name" /></div>
              <div class="field"><label>{{ 'admin.c.description' | transloco }}</label><textarea class="input" rows="3" formControlName="description"></textarea></div>
              <div class="row">
                <div class="field"><label>{{ 'admin.c.category' | transloco }}</label>
                  <cs-select formControlName="categoryId" [options]="categoryOptions()" [placeholder]="'admin.c.select' | transloco" />
                </div>
                <div class="field"><label>{{ 'admin.catalog.brandOptional' | transloco }}</label>
                  <cs-select formControlName="brandId" [options]="brandOptions()" [placeholder]="'admin.c.none' | transloco" />
                </div>
              </div>
              @if (!id()) {
                <h3>{{ 'admin.catalog.firstVariant' | transloco }}</h3>
                <div class="row" [formGroup]="first">
                  <div class="field"><label>{{ 'admin.c.sku' | transloco }}</label><input class="input" formControlName="sku" /></div>
                  <div class="field wide"><label>{{ 'admin.catalog.variantName' | transloco }}</label><input class="input" formControlName="name" /></div>
                  <div class="field"><label>{{ 'admin.catalog.price' | transloco }}</label><input class="input" type="number" min="0.01" step="0.01" formControlName="unitPrice" /></div>
                </div>
              }
              <button class="btn" [csBusy]="busy()" [disabled]="busy() || form.invalid || (!id() && first.invalid)">{{ (id() ? 'admin.products.saveDetails' : 'admin.products.create') | transloco }}</button>
            </form>
          </section>

          @if (product(); as p) {
            <section>
              <h3>{{ 'admin.products.photos' | transloco }} <span class="muted n">{{ p.images.length }}/{{ MAX }}</span></h3>
              <p class="muted small">{{ 'admin.products.photosHint' | transloco: { mb: MB, max: MAX } }}</p>
              <div class="photos">
                @for (img of p.images; track img.id) {
                  <figure [class.main]="img.isPrimary">
                    <img [src]="api.imageSrc(img.url)" [alt]="img.altText || p.name" loading="lazy" />
                    @if (img.isPrimary) { <span class="tag"><cs-icon name="star" [size]="12" /> {{ 'admin.products.primary' | transloco }}</span> }
                    <figcaption>
                      @if (!img.isPrimary) {
                        <button type="button" class="icon-btn" [csBusy]="busy()" [disabled]="busy()" [title]="'admin.products.makePrimary' | transloco" [attr.aria-label]="'admin.products.makePrimary' | transloco" (click)="makePrimary(img.id)"><cs-icon name="star" [size]="16" /></button>
                      }
                      <button type="button" class="icon-btn del" [csBusy]="busy()" [disabled]="busy()" [title]="'admin.products.removePhoto' | transloco" [attr.aria-label]="'admin.products.removePhoto' | transloco" (click)="removePhoto(img.id)"><cs-icon name="trash" [size]="16" /></button>
                    </figcaption>
                  </figure>
                }
                @for (n of pending(); track n) { <span class="skeleton sk-block ph" aria-hidden="true"></span> }
                @if (p.images.length < MAX) {
                  <label class="drop" [class.over]="over()" (dragover)="$event.preventDefault(); over.set(true)" (dragleave)="over.set(false)" (drop)="onDrop($event)">
                    <input #file type="file" accept="image/jpeg,image/png,image/webp" multiple hidden (change)="onPick($event)" />
                    <cs-icon name="upload" [size]="22" />
                    <span>{{ (uploading() ? 'admin.products.uploading' : 'admin.products.drop') | transloco: { n: progress().done, total: progress().total } }}</span>
                  </label>
                }
              </div>
            </section>

            <section>
              <h3>{{ 'admin.products.variants' | transloco }}</h3>
              <div class="scroll">
                <table>
                  <thead><tr><th>{{ 'admin.c.sku' | transloco }}</th><th>{{ 'admin.catalog.variantName' | transloco }}</th><th>{{ 'admin.catalog.price' | transloco }}</th><th>{{ 'admin.promotions.active' | transloco }}</th><th></th></tr></thead>
                  <tbody>
                    @for (v of rows(); track v.id) {
                      <tr>
                        <td><code>{{ v.sku }}</code></td>
                        <td><input class="input sm" [(ngModel)]="v.name" [ngModelOptions]="{ standalone: true }" /></td>
                        <td class="num"><input class="input sm" type="number" min="0.01" step="0.01" [(ngModel)]="v.unitPrice" [ngModelOptions]="{ standalone: true }" /></td>
                        <td><input type="checkbox" [(ngModel)]="v.isActive" [ngModelOptions]="{ standalone: true }" [attr.aria-label]="'admin.promotions.active' | transloco" /></td>
                        <td class="right"><button type="button" class="btn secondary sm" [csBusy]="busy()" [disabled]="busy() || !dirty(v)" (click)="saveVariant(v)">{{ 'admin.products.saveVariant' | transloco }}</button></td>
                      </tr>
                    }
                    <tr class="add">
                      <td><input class="input sm" [placeholder]="'admin.c.sku' | transloco" [(ngModel)]="newVariant.sku" [ngModelOptions]="{ standalone: true }" /></td>
                      <td><input class="input sm" [placeholder]="'admin.catalog.variantName' | transloco" [(ngModel)]="newVariant.name" [ngModelOptions]="{ standalone: true }" /></td>
                      <td class="num"><input class="input sm" type="number" min="0.01" step="0.01" [(ngModel)]="newVariant.unitPrice" [ngModelOptions]="{ standalone: true }" /></td>
                      <td></td>
                      <td class="right"><button type="button" class="btn sm" [csBusy]="busy()" [disabled]="busy() || !canAddVariant()" (click)="addVariant()"><cs-icon name="plus" [size]="14" /> {{ 'admin.products.addVariant' | transloco }}</button></td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </section>
          } @else {
            <p class="muted small"><cs-icon name="image" [size]="14" /> {{ 'admin.products.photosAfter' | transloco }}</p>
          }
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dialog-head { gap: var(--space-2); }
    .editor { flex: 1; overflow-y: auto; padding: var(--space-4) var(--space-5) var(--space-6); }
    section { margin-bottom: var(--space-6); }
    h3 { margin: 0 0 var(--space-3); }
    .n { font-weight: 500; font-size: 0.8rem; }
    .small { font-size: 0.82rem; margin: calc(var(--space-2) * -1) 0 var(--space-3); }
    .row .field { flex: 1; } .row .field.wide { flex: 2; }
    .photos { display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: var(--space-3); }
    figure { position: relative; margin: 0; aspect-ratio: 1; border: 1px solid var(--border); border-radius: var(--radius-sm); overflow: hidden; background: var(--surface-2); }
    figure.main { border-color: var(--primary); box-shadow: var(--ring); }
    figure img { width: 100%; height: 100%; object-fit: cover; }
    .tag { position: absolute; top: 6px; left: 6px; display: inline-flex; align-items: center; gap: 4px; padding: 2px 8px; border-radius: var(--radius-pill); background: var(--primary); color: #fff; font-size: 0.7rem; font-weight: 650; }
    figcaption { position: absolute; inset: auto 0 0 0; display: flex; justify-content: flex-end; gap: 4px; padding: 6px; background: linear-gradient(transparent, rgba(5, 9, 22, 0.7)); opacity: 0; transition: opacity 0.15s; }
    figure:hover figcaption, figure:focus-within figcaption { opacity: 1; }
    @media (hover: none) { figcaption { opacity: 1; } }
    figcaption .icon-btn { background: rgba(255, 255, 255, 0.92); color: #0e1630; height: 30px; min-width: 30px; }
    figcaption .del:hover { color: var(--danger); }
    .drop { display: grid; place-content: center; justify-items: center; gap: var(--space-2); aspect-ratio: 1; padding: var(--space-3); text-align: center; font-size: 0.82rem; color: var(--muted); border: 2px dashed var(--border-strong); border-radius: var(--radius-sm); cursor: pointer; }
    .drop:hover, .drop.over { border-color: var(--primary); color: var(--primary); background: var(--accent); }
    .scroll { overflow-x: auto; }
    .hide { display: none; }
    .ph { aspect-ratio: 1; }
    .input.sm { padding: 0.4rem 0.6rem; min-width: 90px; }
    td.num .input { max-width: 120px; }
    tr.add td { background: var(--surface-2); }
    code { font-size: 0.82rem; }
    @media (max-width: 640px) { .editor { padding: var(--space-3); } .dialog-head .btn { padding-inline: 0.6rem; } }
  `],
})
export class ProductEditor implements OnInit, OnDestroy {
  readonly api = inject(CatalogAdminApi);
  private readonly fb = inject(FormBuilder);
  private readonly i18n = inject(I18n);
  private readonly dialog = viewChild.required<ElementRef<HTMLElement>>('dialog');
  private opener: Element | null = null;

  /** Product to edit; null to create a new one. */
  readonly productId = input<string | null>(null);
  readonly categories = input.required<Category[]>();
  readonly brands = input.required<Brand[]>();
  readonly closed = output<void>();
  /** Something was saved, so the list behind the dialog should reload. */
  readonly changed = output<void>();

  readonly MAX = MAX_IMAGES;
  readonly MB = MAX_MB;
  readonly id = signal<string | null>(null);
  readonly product = signal<AdminProductDetail | null>(null);
  readonly rows = signal<VariantRow[]>([]);
  readonly busy = signal(false);
  readonly over = signal(false);
  readonly uploading = signal(false);
  readonly progress = signal({ done: 0, total: 0 });
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  newVariant = { sku: '', name: '', unitPrice: 0 };

  /** Editing an existing product whose data has not arrived yet. */
  readonly loadingProduct = computed(() => !!this.id() && !this.product());
  /** Photos still waiting in the upload queue, drawn as placeholders next to the finished ones. */
  readonly pending = computed(() => {
    const p = this.progress();
    return this.uploading() ? Array.from({ length: Math.max(p.total - (p.done - 1), 0) }, (_, i) => i) : [];
  });
  readonly categoryOptions = computed<SelectOption[]>(() => this.categories().map((c) => ({ value: c.id, label: c.name })));
  readonly brandOptions = computed<SelectOption[]>(() => {
    this.i18n.lang(); // recompute when the language changes
    return [{ value: '', label: this.i18n.t('admin.c.none') }, ...this.brands().map((b) => ({ value: b.id, label: b.name }))];
  });

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.required, Validators.maxLength(8000)]],
    categoryId: ['', Validators.required],
    brandId: [''],
  });
  readonly first = this.fb.nonNullable.group({
    sku: ['', [Validators.required, Validators.maxLength(64)]],
    name: ['', [Validators.required, Validators.maxLength(150)]],
    unitPrice: [0, [Validators.required, Validators.min(0.01), Validators.max(1_000_000)]],
  });

  ngOnInit(): void {
    this.opener = document.activeElement;
    document.body.style.overflow = 'hidden';
    queueMicrotask(() => this.dialog().nativeElement.focus());
    this.id.set(this.productId());
    if (this.id()) this.load();
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
    (this.opener as HTMLElement | null)?.focus?.();
  }

  tone(status: string): string { return status === 'Active' ? 'ok' : status === 'Draft' ? 'warn' : ''; }

  onBackdrop(e: MouseEvent): void { if (e.target === e.currentTarget) this.closed.emit(); }

  private load(): void {
    const id = this.id();
    if (!id) return;
    this.api.getProduct(id).subscribe({
      next: (p) => this.apply(p),
      error: (e) => this.error.set(this.i18n.problem(e)),
    });
  }

  private apply(p: AdminProductDetail): void {
    this.product.set(p);
    this.form.reset({ name: p.name, description: p.description, categoryId: p.categoryId, brandId: p.brandId ?? '' });
    this.rows.set(p.variants.map((v) => ({ id: v.id, sku: v.sku, name: v.name, unitPrice: v.unitPrice, isActive: v.isActive, original: this.key(v) })));
  }

  private key(v: { name: string; unitPrice: number; isActive: boolean }): string { return `${v.name}|${Number(v.unitPrice)}|${v.isActive}`; }
  dirty(v: VariantRow): boolean { return this.key(v) !== v.original && !!v.name.trim() && Number(v.unitPrice) > 0; }
  canAddVariant(): boolean { return !!this.newVariant.sku.trim() && !!this.newVariant.name.trim() && Number(this.newVariant.unitPrice) > 0; }

  private ok(text: string): void { this.message.set(text); this.error.set(null); this.busy.set(false); this.changed.emit(); }
  private fail(e: unknown): void { this.error.set(this.i18n.problem(e)); this.message.set(null); this.busy.set(false); }
  private begin(): void { this.busy.set(true); this.error.set(null); this.message.set(null); }

  saveDetails(): void {
    if (this.form.invalid) return;
    this.begin();
    const v = this.form.getRawValue();
    const body = { name: v.name, description: v.description, categoryId: v.categoryId, brandId: v.brandId || null };
    const id = this.id();
    if (id) {
      this.api.updateProduct(id, body).subscribe({ next: () => { this.ok(this.i18n.t('admin.products.saved')); this.load(); }, error: (e) => this.fail(e) });
      return;
    }
    const f = this.first.getRawValue();
    this.api.createProduct({ ...body, variants: [{ sku: f.sku, name: f.name, unitPrice: Number(f.unitPrice), currency: STORE_CURRENCY }] }).subscribe({
      next: (created) => { this.id.set(created.id); this.ok(this.i18n.t('admin.products.created')); this.load(); },
      error: (e) => this.fail(e),
    });
  }

  setStatus(publish: boolean): void {
    const p = this.product();
    if (!p) return;
    this.begin();
    const text = publish ? (p.status === 'Archived' ? 'admin.products.restored' : 'admin.products.published') : 'admin.products.archivedDone';
    (publish ? this.api.publish(p.id) : this.api.archive(p.id)).subscribe({
      next: () => { this.ok(this.i18n.t(text)); this.load(); }, error: (e) => this.fail(e),
    });
  }

  saveVariant(v: VariantRow): void {
    const id = this.id();
    if (!id) return;
    this.begin();
    this.api.updateVariant(id, v.id, { name: v.name.trim(), unitPrice: Number(v.unitPrice), isActive: v.isActive }).subscribe({
      next: () => { this.ok(this.i18n.t('admin.products.variantSaved')); this.load(); }, error: (e) => this.fail(e),
    });
  }

  addVariant(): void {
    const id = this.id();
    if (!id) return;
    this.begin();
    const n = this.newVariant;
    this.api.addVariant(id, { sku: n.sku.trim(), name: n.name.trim(), unitPrice: Number(n.unitPrice), currency: STORE_CURRENCY }).subscribe({
      next: () => { this.newVariant = { sku: '', name: '', unitPrice: 0 }; this.ok(this.i18n.t('admin.products.variantAdded')); this.load(); },
      error: (e) => this.fail(e),
    });
  }

  makePrimary(imageId: string): void {
    const id = this.id();
    if (!id) return;
    this.begin();
    this.api.setPrimaryImage(id, imageId).subscribe({ next: () => { this.ok(this.i18n.t('admin.products.primarySet')); this.load(); }, error: (e) => this.fail(e) });
  }

  removePhoto(imageId: string): void {
    const id = this.id();
    if (!id) return;
    this.begin();
    this.api.deleteImage(id, imageId).subscribe({ next: () => { this.ok(this.i18n.t('admin.products.photoRemoved')); this.load(); }, error: (e) => this.fail(e) });
  }

  onPick(e: Event): void {
    const input = e.target as HTMLInputElement;
    this.upload(Array.from(input.files ?? []));
    input.value = ''; // let the same file be picked again
  }

  onDrop(e: DragEvent): void {
    e.preventDefault();
    this.over.set(false);
    this.upload(Array.from(e.dataTransfer?.files ?? []));
  }

  /** Validates each file in the browser (type, size, how many fit), then uploads them one after another. */
  private upload(files: File[]): void {
    const id = this.id();
    const current = this.product()?.images.length ?? 0;
    if (!id || !files.length) return;

    const errors: string[] = [];
    const valid: File[] = [];
    for (const f of files) {
      if (!TYPES.includes(f.type)) errors.push(this.i18n.t('admin.products.errType', { name: f.name }));
      else if (f.size > MAX_MB * 1024 * 1024) errors.push(this.i18n.t('admin.products.errSize', { name: f.name, mb: MAX_MB }));
      else valid.push(f);
    }
    const room = MAX_IMAGES - current;
    if (valid.length > room) { errors.push(this.i18n.t('admin.products.errMax', { max: MAX_IMAGES })); valid.length = Math.max(room, 0); }
    this.error.set(errors.length ? errors.join(' ') : null);
    this.message.set(null);
    if (!valid.length) return;

    this.uploading.set(true);
    this.busy.set(true);
    this.progress.set({ done: 1, total: valid.length });
    let done = 0;
    from(valid).pipe(concatMap((f) => this.api.uploadImage(id, f))).subscribe({
      next: () => { done++; this.progress.set({ done: Math.min(done + 1, valid.length), total: valid.length }); },
      error: (e) => { this.uploading.set(false); this.fail(e); this.load(); },
      complete: () => {
        this.uploading.set(false);
        this.ok(this.i18n.t('admin.products.uploaded', { count: valid.length }));
        if (errors.length) this.error.set(errors.join(' ')); // the files that were skipped stay visible next to the success note
        this.load();
      },
    });
  }
}
