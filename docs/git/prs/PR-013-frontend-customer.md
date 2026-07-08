# PR-013 — Customer storefront (Angular)

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/frontend-customer` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Phase 13: the customer-facing Angular storefront over the module APIs — auth, catalog, cart, checkout (coupon +
payment), orders, tracking, account, and notifications (Section 13).

## Changes
- **Workspace:** Angular 22 in `frontend/` — standalone components, zoneless change detection, signals; vitest;
  dev proxy to the backend; environment file-replacement for prod.
- **Core:** typed API services per bounded context + `models.ts` (DTO mirrors) under `core/api`; `AuthStore` and
  `CartStore` (signal stores); functional JWT interceptor (attach bearer, transparent single refresh on 401,
  retry, clear on failure); `authGuard`.
- **Features (lazy routes):** login/register; catalog product list (search/filter/sort/paging) and detail
  (variants, add-to-cart, wishlist); cart; checkout (saved/new address, coupon quote, shipping estimate, place
  order, pay); orders list + detail (pay/cancel/tracking + history); account (profile, addresses, wishlist);
  notifications (inbox + marketing preferences); public shipment tracking.
- **Shared:** header/footer layout, money pipe, order-status badge, problem-details message helper.
- ADR-018 (frontend architecture).

## Technical decisions
- **Signals-first, zoneless, no state/UI library** — modern and dependency-light (no over-engineering); a small
  design system in one stylesheet.
- **API layer isolated under core/api** so Phase 14 (admin) can extract it to a shared library mechanically.
- **Server-authoritative money** — prices, coupon discount (`/promotions/quote`), tax and totals come from the
  API; payment uses the `Idempotency-Key` header.
- **Auth**: rotating refresh handled entirely in the interceptor; tokens in localStorage; guard with returnUrl.
- **Known limitations (documented in ADR-018):** live SPA↔API↔Postgres deferred to Docker; notification
  preference writes send numeric enum values (backend serializes enums as numbers); wishlist shows product ids;
  test coverage minimal this phase (Phase 18 adds component/e2e).

## Testing / verification
- `pnpm build` (production) → **0 errors**; initial bundle 292 kB raw / 79 kB transfer; 16 lazy chunks
  (all features code-split).
- `ng test --watch=false` (vitest) → **2 passed**.
- **Running-app smoke test** (dev server, backend down): SPA bootstraps (zoneless/DI/router/lazy chunks OK); shop
  page renders and shows a graceful error on the API-down path; the auth guard redirects `/account` → `/login`;
  console shows only the expected proxy 502s — no Angular runtime errors.
- ⚠️ **Deferred (no Postgres):** the live storefront journey against a running API + database.

## Review cycle
**🟢 APPROVE (simulated)** — Clean, modern Angular: isolated typed API layer, signal stores, a robust
refresh-on-401 interceptor, and fully lazy features under budget. Money stays server-authoritative end to end.
The deferred live run and the minor enum/wishlist gaps are called out rather than hidden.

## Merge result
Merged into `develop` with `--no-ff`.
