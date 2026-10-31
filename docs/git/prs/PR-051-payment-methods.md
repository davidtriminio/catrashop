# PR-051 — Payment methods: cash on delivery, bank transfer and pay at the store

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/payment-methods` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Problem
Paying an order meant one click that the simulated gateway always accepted. The store works in Honduras with manual,
local flows, and the payment must depend on how the order is delivered (never the other way round).

## Rules (decided with the owner)
| Code | Delivery | Receipt | What happens |
|---|---|---|---|
| `CONTRA_ENTREGA` | yes, only where the zone covers cash on delivery | no | order is confirmed now; money is registered as collected after delivery |
| `TRANSFERENCIA` | yes, and pickup | yes (JPG/PNG/PDF ≤ 5 MB) + bank account + reference | order stays awaiting payment until an operator verifies the receipt |
| `PAGO_EN_TIENDA` | pickup only | no | order is confirmed now; money is registered as collected at the store |
| `LINKPAGO` | — | — | **not built**: the owner will use "another way" later |

Assumption to confirm: a pickup order may also pay by transfer. Shipping cost never depends on the method.

## Changes
- **Orders:** `Order.PaymentMethod`; checkout requires one and the server re-checks it against the delivery mode and the zone's
  cash-on-delivery flag (`PaymentMethodCodes.AllowedFor`). New state **`Confirmed`** (`PaymentPending → Confirmed →
  Processing/Cancelled`): stock is committed but the order is *not* marked paid. `GET /orders/payment-methods` lists what a
  delivery option allows. Shipping accepts `Confirmed` like `Paid`. Migration `AddPaymentMethod`.
- **Payments:** `Payment.Method`, transfer data (bank account, reference, receipt key, review note) and status `UnderReview`.
  Customer: `GET /payments/bank-accounts`, `GET /payments/by-order/{id}`, `POST /payments/{id}/transfer-proof` (multipart).
  Operator (`orders.manage`): `GET /payments/admin`, `…/by-order/{id}`, `…/{id}/receipt`, `…/approve`, `…/reject`, `…/collect`
  (only once the order is delivered). Approve marks the order paid; a cancelled order cannot be paid. Receipts are checked by
  their first bytes (not the file name), stored under `Payments:ReceiptsPath` on the server disk with generated names.
  Orders without a method keep the gateway path. Migration `AddManualPayments`. Bank accounts come from `Payments:BankAccounts`.
- **Web:** checkout offers the allowed methods and starts the payment right after the order; the order page has a payment
  panel (bank accounts, receipt upload, review state). **Admin:** Payments page (receipts to verify, awaiting transfer, to
  collect, done) and a payment card on the order. **Mobile:** knows `Confirmed`.
- **Deploy/seed:** receipts folder and bank-account settings in systemd, Docker and env examples; `seed.ps1` creates three
  cash-on-delivery orders and one transfer with a receipt to verify.

## Things to know
- Existing local databases need the two new migrations (`scripts/local/setup-db.ps1`). Old orders have no method and keep working.
- Set real `Payments:BankAccounts` and back up `Payments:ReceiptsPath` in production.
- Refunding a manual payment only updates the system; the money is returned by hand.

## Verification
- Backend Release tests; new unit tests for the method rules, the `Confirmed` state and every payment flow (fakes in memory).
- Web and admin production builds, Vitest, Flutter analyze/test.
- **Not verified:** a real API + PostgreSQL run of the upload and approve flow, the browser UI, Docker/Oracle deployment.
