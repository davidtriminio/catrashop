# ADR-016: Promotions & checkout discounts

- **Status:** Accepted
- **Date:** 2026-06-26

## Context
Checkout needs to apply coupon discounts (Section 11): percentage or fixed-amount coupons with a minimum
subtotal, a validity window, a global usage limit, and a per-customer limit. Orders already computes totals and
until now hardcoded `discountTotal = 0`. The discount must be **server-authoritative** (never trusted from the
client), the rules must live in one place, and applying a coupon at checkout must stay consistent with the
rest of the cross-module checkout flow, which uses no distributed transaction.

## Decision
- **Promotions owns coupons and redemptions; Orders owns totals.** The discount amount is decided entirely inside
  the Promotions module and handed to Orders as a number. Orders never re-derives it and never reads the coupon
  rules.
- **All coupon rules live in `Coupon.CalculateDiscount`** (active flag, validity window, minimum subtotal, global
  and per-customer limits, currency match, percentage cap, never-below-zero). It is a pure method — fully
  unit-tested without a database — and every rule **fails closed** with a specific error code.
- **Public contract `IPromotionApplication`** (implemented by Promotions, consumed by Orders):
  - `QuoteAsync` — read-only evaluation used to price the discount before the order is built (and by a customer
    "preview" endpoint).
  - `RedeemAsync` — commits a redemption, **idempotent on the order reference** (the cart id, which is already
    the order's idempotency key). A retried checkout returns the recorded discount without counting again.
  - `ReleaseAsync` — reverses the redemption when an order is cancelled or refunded (idempotent).
  Promotions never references Orders — it only receives opaque identifiers, so the dependency stays one-way
  (**Orders → Promotions**), acyclic with the existing graph.
- **Redemption integrity.** Each redemption is a row keyed by a **unique `(reference)` index**; that index — not
  the counter — is the real guard that one order can never be counted twice. A denormalized `TimesRedeemed`
  counter on the coupon enforces the global limit cheaply; per-customer limits count non-released rows.
- **Consistency ordering at checkout** (mirrors ADR-013's reserve-before-persist): quote → build order → reserve
  inventory → **redeem coupon** → persist order. Reserving *before* redeeming means the common failure (a line
  gone out of stock) never consumes a coupon use. `RedeemAsync` re-validates under current usage, so a coupon
  exhausted between the quote and the redeem still fails closed and the reservation self-expires.
- **Discount reduces the taxable base**: tax is computed on `subtotal − discount`.

## Alternatives considered
- **Compute the discount inside Orders.** Rejected — it would duplicate coupon rules across modules and couple
  Orders to promotion internals. Orders stays a pure consumer of a number.
- **Two-phase reserve/confirm for coupons** (mirroring inventory's reserve-then-commit). Rejected as
  over-engineering for this scope: single-phase redeem-at-checkout with idempotency on the cart id plus
  release-on-cancel/refund is the standard, adequate behavior.
- **Enforce the global limit with a DB constraint / serializable transaction.** Deferred — see the known
  limitation below.

## Consequences
- Coupons are applied at checkout, released on cancel/refund, and previewable by customers before checkout.
- The discount is always decided server-side from persisted coupon state.
- Emits `CouponRedeemedIntegrationEvent` via the outbox (for future analytics / notifications once messaging lands).

## Known limitations (documented, not hidden)
- **Global-limit race:** two checkouts redeeming the very last remaining use concurrently can both pass the
  `TimesRedeemed < UsageLimit` check before either commits. The per-order unique index still prevents
  double-counting a single order, but the global cap can be exceeded by one under a true race. True enforcement
  needs a DB-level constraint or serializable isolation; deferred deliberately.
- **Dangling redemption:** if the order's own insert fails *after* the coupon was redeemed, the redemption row is
  left with no order. A retry of the same cart is safe (redeem is idempotent on the cart id and simply returns
  the existing row, which the created order then owns). Only permanent abandonment after that rare failure leaves
  a redemption consuming one slot; cancel/refund release covers the normal paths.
- **Shipping cost** is still not folded into the order total (carried over from ADR-015).

## Verification
- **17 unit tests pass** (percentage rounding + cap, fixed never exceeding subtotal, currency match, minimum
  subtotal gate, inactive, validity window incl. exclusive end, global + per-customer limits, release frees a
  slot and floors at zero, redemption release idempotent). Solution builds 0/0; full suite **78 pass**.
- Migration validates: `promotions` schema with `coupons` / `coupon_redemptions` / `outbox_messages`;
  `ux_coupons_code`, `ux_coupon_redemptions_reference`, `ix_coupon_redemptions_coupon_customer`.
- API boots with 9 modules `[...,"Promotions",...]`; admin coupon create → 401 unauthenticated; quote → 401
  unauthenticated.
- Deferred (no Postgres): a live checkout applying a coupon end-to-end and a cancel/refund releasing it.
