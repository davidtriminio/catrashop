# ADR-018: Frontend architecture (Angular customer app)

- **Status:** Accepted
- **Date:** 2026-07-08

## Context
Phase 13 introduces the customer storefront (Section 13) — the first frontend against the module APIs. It needs
authentication, catalog browsing, cart, checkout (with coupon and payment), orders, tracking, account, and
notifications, and must be structured so the admin app (Phase 14) can reuse the API layer.

## Decision
- **Angular 22 workspace** (`frontend/`), **standalone components**, **zoneless change detection**, and a
  **signals-first** state model. No NgModules, no third-party state or UI libraries — a small hand-rolled design
  system lives in one global stylesheet. This keeps the app modern and dependency-light (no over-engineering).
- **Typed API layer isolated under `src/app/core/api`** — one service per bounded context plus a single
  `models.ts` mirroring the backend DTOs (camelCase web-serialization). It depends only on `HttpClient` and an
  `API_BASE_URL` token, so Phase 14 can lift it into a shared library with no rewrites.
- **Auth via a functional HTTP interceptor.** It attaches the bearer token and, on a `401`, transparently
  refreshes once (rotating token), retries the original request, and clears the session if the refresh fails; a
  retry marker prevents loops and auth endpoints are exempt. Tokens persist in `localStorage`; a functional
  route guard redirects protected routes to login with a `returnUrl`.
- **App-wide singleton stores** (`AuthStore`, `CartStore`) expose signals (`isAuthenticated`, `itemCount`, …) so
  the header badge and pages share one source of truth. Feature state is component-local signals.
- **Lazy-loaded routes** — every feature is a `loadComponent` chunk, keeping the initial bundle small
  (~290 kB / ~79 kB transfer, well under the 1 MB budget).
- **Dev proxy, environment file-replacement for prod.** `proxy.conf.json` forwards `/api` to the backend
  (`http://localhost:5227`) so there is no CORS and no hard-coded host in dev; production uses relative `/api`
  behind a reverse proxy (`environment.ts`).
- **Server-authoritative money.** All prices, the coupon discount (quoted via `/promotions/quote`), tax, and the
  order total come from the API; the client never computes them. Payment uses the `Idempotency-Key` header.
- **Vitest** for unit tests (the Angular 22 default runner).

## Alternatives considered
- **A shared library up front (customer + admin + lib).** Deferred — with only one app today it would be
  premature structure. The API layer is deliberately isolated so the extraction in Phase 14 is mechanical.
- **A component/state library (Material, NgRx).** Rejected as unnecessary for this scope; signals + a small
  design system cover it without the weight.
- **SSR.** Not needed for an authenticated app behind login; can be added later if SEO on the catalog matters.

## Consequences
- The storefront covers the full journey: register/login → browse/search → product → cart → checkout (address,
  coupon, shipping estimate) → pay → orders/tracking, plus account (profile, addresses, wishlist) and
  notifications (inbox + marketing preferences).
- The API layer and auth plumbing are ready to be shared with the admin app.

## Known limitations (documented, not hidden)
- **Live end-to-end (SPA ↔ API ↔ Postgres) is deferred** until Docker is running — same constraint as the
  backend phases. This phase is verified by production build, unit test, and a running-app smoke test against the
  API-down path.
- **Notification preference writes send numeric enum values** because the backend serializes enums as numbers
  (web defaults, no string converter); the four marketing (type × channel) combos are mapped explicitly.
- **Wishlist shows product ids** — the API returns ids only; enriching with names needs a batch product lookup
  (future backend addition).
- **Test coverage is minimal this phase** (the money pipe); component and e2e tests are Phase 18.

## Verification
- `pnpm build` (production) → succeeds, **0 errors**; initial bundle 292 kB raw / 79 kB transfer; 16 lazy chunks.
- `ng test --watch=false` (vitest) → **2 passed**.
- **Running-app smoke test:** the dev server boots the SPA (zoneless bootstrap, DI, router, lazy chunks all OK);
  the shop page renders and handles the API-down case gracefully; the auth guard redirects `/account` →
  `/login`; the console shows only the expected proxy 502s (backend/Postgres not running), no Angular errors.
