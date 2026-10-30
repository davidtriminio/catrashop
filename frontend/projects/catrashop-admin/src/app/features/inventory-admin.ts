import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { InventoryAdminApi } from '@cs/api/inventory-admin.api';
import { InventoryItem, Paged } from '@cs/api/models';
import { problemMessage } from '@cs/util/problem';

@Component({
  selector: 'app-inventory-admin',
  imports: [FormsModule],
  template: `
    <h1>Inventory</h1>
    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (message()) { <div class="alert success">{{ message() }}</div> }

    <div class="cols">
      <section class="card">
        <h2>Start tracking a SKU</h2>
        <div class="field"><label>SKU</label><input class="input" [(ngModel)]="newSku" /></div>
        <div class="row">
          <div class="field" style="flex:1"><label>Initial on-hand</label><input class="input" type="number" min="0" max="1000000" [(ngModel)]="newOnHand" /></div>
          <div class="field" style="flex:1"><label>Low-stock threshold</label><input class="input" type="number" min="0" max="1000000" [(ngModel)]="newThreshold" /></div>
        </div>
        <button class="btn sm" [disabled]="!newSku || busy()" (click)="createItem()">Create item</button>
      </section>

      <section class="card">
        <h2>Adjust stock</h2>
        <div class="field"><label>SKU</label><input class="input" [(ngModel)]="opSku" /></div>
        <div class="field"><label>Quantity / delta</label><input class="input" type="number" min="-1000000" max="1000000" [(ngModel)]="opQty" /></div>
        <div class="row">
          <button class="btn sm" [disabled]="!opSku || busy()" (click)="receive()">Receive (+)</button>
          <button class="btn secondary sm" [disabled]="!opSku || busy()" (click)="adjust()">Adjust (±)</button>
          <button class="btn secondary sm" [disabled]="!opSku || busy()" (click)="setThreshold()">Set threshold</button>
        </div>
      </section>
    </div>

    <section class="card">
      <div class="head"><h2>Stock levels</h2><label class="chk"><input type="checkbox" [(ngModel)]="lowOnly" (change)="load()" /> Low-stock only</label></div>
      @if (result(); as r) {
        <table>
          <thead><tr><th>SKU</th><th>On hand</th><th>Reserved</th><th>Available</th><th>Threshold</th><th></th></tr></thead>
          <tbody>
            @for (i of r.items; track i.id) {
              <tr>
                <td>{{ i.sku }}</td><td>{{ i.onHand }}</td><td>{{ i.reserved }}</td><td>{{ i.available }}</td><td>{{ i.lowStockThreshold }}</td>
                <td>@if (i.isLowStock) { <span class="badge bad">Low</span> } @else { <span class="badge ok">OK</span> }</td>
              </tr>
            } @empty { <tr><td colspan="6" class="muted">No items.</td></tr> }
          </tbody>
        </table>
      }
    </section>
  `,
  styles: [`
    .cols { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-bottom: 1.25rem; }
    .head { display: flex; justify-content: space-between; align-items: center; }
    .chk { font-size: 0.85rem; color: var(--muted); display: inline-flex; gap: 0.3rem; align-items: center; }
    @media (max-width: 760px) { .cols { grid-template-columns: 1fr; } }
  `],
})
export class InventoryAdmin implements OnInit {
  private readonly api = inject(InventoryAdminApi);

  newSku = ''; newOnHand = 0; newThreshold = 5;
  opSku = ''; opQty = 0; lowOnly = false;

  readonly result = signal<Paged<InventoryItem> | null>(null);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busy = signal(false);

  ngOnInit(): void { this.load(); }

  load(): void {
    this.api.list(this.lowOnly, 1, 100).subscribe({ next: (r) => this.result.set(r), error: (e) => this.error.set(problemMessage(e)) });
  }
  private ok(m: string): void { this.message.set(m); this.error.set(null); this.busy.set(false); this.load(); }
  private fail(e: unknown): void { this.error.set(problemMessage(e)); this.busy.set(false); }

  createItem(): void {
    this.busy.set(true);
    this.api.create({ sku: this.newSku.trim(), initialOnHand: Number(this.newOnHand), lowStockThreshold: Number(this.newThreshold) })
      .subscribe({ next: () => { this.ok('Item created.'); this.newSku = ''; }, error: (e) => this.fail(e) });
  }
  receive(): void {
    this.busy.set(true);
    this.api.receive(this.opSku.trim(), { quantity: Number(this.opQty), reason: 'Manual receive' }).subscribe({ next: () => this.ok('Stock received.'), error: (e) => this.fail(e) });
  }
  adjust(): void {
    this.busy.set(true);
    this.api.adjust(this.opSku.trim(), { delta: Number(this.opQty), reason: 'Manual adjustment' }).subscribe({ next: () => this.ok('Stock adjusted.'), error: (e) => this.fail(e) });
  }
  setThreshold(): void {
    this.busy.set(true);
    this.api.setThreshold(this.opSku.trim(), { threshold: Number(this.opQty) }).subscribe({ next: () => this.ok('Threshold updated.'), error: (e) => this.fail(e) });
  }
}
