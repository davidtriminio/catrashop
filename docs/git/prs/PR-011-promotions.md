# PR-011 — Promotions module

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/promotions` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Phase 11: coupons with discount rules and usage limits, and a real discount wired into checkout — replacing the
`discountTotal = 0` placeholder Orders had carried since Phase 8 (Section 11).

## Changes
- **Domain:** `Coupon` aggregate (percentage or fixed amount, minimum subtotal, validity window, global +
  per-customer usage limits, optional percentage cap); `CouponRedemption` entity (one per order reference);
  `DiscountType`; all rules in `Coupon.CalculateDiscount` (pure, fail-closed).
- **Application:** `PromotionApplicationService` implementing the public `IPromotionApplication`
  (quote / redeem / release); `CouponAdminService` (CRUD + activate); DTOs, validators, mappers.
- **Infrastructure:** `PromotionsDbContext` (promotions schema), repositories, outbox publisher, design-time
  factory, **initial migration** (coupons / coupon_redemptions / outbox_messages).
- **Orders (wired):** `CheckoutRequest.CouponCode`; checkout quotes → reserves → redeems → persists, with the
  discount reducing the taxable base; cancel and refund now call `ReleaseAsync`. Orders references the Promotions
  contract (one-way, acyclic).
- **API:** customer `POST /promotions/quote` (preview); admin coupon CRUD/activate under `promotions.manage`.
- **Tests:** `CommerceHub.Modules.Promotions.Tests` — **17 unit tests**.
- ADR-016 (promotions & checkout discounts).

## Technical decisions
- Discount is **server-authoritative**, decided entirely inside `Coupon.CalculateDiscount`; Orders consumes a
  number and never re-derives it (ADR-016).
- Redemption is **idempotent on the order reference** (cart id) via a unique index — a retried checkout can't
  double-count; a denormalized counter enforces the global limit, per-customer limits count non-released rows.
- **Consistency ordering:** reserve inventory *before* redeeming the coupon, so an out-of-stock line never
  consumes a coupon use; `RedeemAsync` re-validates so a coupon exhausted mid-flight still fails closed.
- **Known limitations (called out, not hidden):** a true concurrent race on the very last global use can exceed
  the cap by one (needs a DB constraint / serializable txn — deferred); a redemption can dangle if the order
  insert fails after redeem (retry is idempotent-safe; cancel/refund release covers normal paths). Shipping cost
  still not in order totals (from ADR-015).

## Testing / verification
- `dotnet build` → **0/0**.
- **`dotnet test` → 78 passed / 0 failed** (17 new Promotions: rounding + cap, fixed ≤ subtotal, currency match,
  minimum-subtotal gate, inactive, validity window incl. exclusive end, global + per-customer limits, release
  frees a slot / floors at zero, release idempotent).
- Migration validates: `promotions` schema; `ux_coupons_code`, `ux_coupon_redemptions_reference`,
  `ix_coupon_redemptions_coupon_customer`.
- API boot: 9 modules `[...,"Promotions",...]`; admin coupon create → **401**; quote → **401** unauthenticated.
- ⚠️ **Deferred (no Postgres):** a live checkout applying a coupon end-to-end, and cancel/refund releasing it.

## Review cycle
**🟢 APPROVE (simulated)** — Discount logic is server-authoritative and concentrated in one testable domain
method; the checkout wiring reuses the established reserve-before-persist ordering and idempotency-by-cart-id, so
it composes cleanly with inventory and payments. The concurrency and dangling-redemption edges are documented
rather than hidden.

## Merge result
Merged into `develop` with `--no-ff`.
