# ADR-019: Admin application & shared frontend library

- **Status:** Accepted
- **Date:** 2026-07-14

## Context
Phase 14 adds the staff administration console (Section 14). It manages the same domain the customer app reads,
so it must reuse the API layer and auth plumbing rather than duplicate them — the extraction ADR-018 anticipated.

## Decision
- **Two applications in one workspace.** `commercehub-web` (customer, Phase 13) and a new `commercehub-admin`
  live in the same Angular workspace. Both are standalone, zoneless, signals-first, built by the same CLI.
- **Shared code promoted to `frontend/shared/`** and imported through a TypeScript path alias **`@ch/*`** (no
  `baseUrl`, which TS 6 deprecates — the mapping is resolved relative to the config). It holds the DTO models, the
  per-context API services (customer *and* admin), the auth store/interceptor/guards, and small utilities
  (money pipe, problem-details helper). The customer app was refactored onto `@ch/*`; the admin app consumes the
  same modules. This is the shared library ADR-018 promised — as path-mapped source rather than an ng-packagr
  package, which keeps the build single-step (no library pre-build) with no loss of isolation.
- **Shared design system** as `shared/theme.scss`, pulled in by each app's `styles.scss` via a `shared` SCSS
  include path (`@use 'theme';`). The admin app overrides a few tokens (a teal accent, a wider max width) so the
  console is visually distinct from the storefront.
- **Permission-gated routes.** A `permissionGuard(permission)` factory guards each admin section against the
  matching backend permission (`catalog.products.manage`, `inventory.manage`, `orders.read_all`/`orders.manage`,
  `promotions.manage`, `notifications.manage`); the sidebar shows only the sections the user's permissions allow;
  a `/forbidden` page catches the rest. The guard is UX: **the backend remains the real authority** and returns
  403, so while a profile is still loading on a hard refresh the guard lets the route through rather than
  false-denying.
- **Separate dev server** (port 4300) with its own proxy to the API, and production environment file-replacement,
  mirroring the customer app.

## Admin capabilities
Dashboard (shows the sections and permissions the account has); Catalog (create categories/brands/products as
drafts, publish/archive); Inventory (start tracking a SKU, receive/adjust stock, set thresholds, view levels);
Orders (search by status, drive Processing → Shipped → Delivered, cancel); Promotions (create coupons,
activate/deactivate); Shipping (create/deactivate methods); Notifications (manual/operational send).

## Alternatives considered
- **An ng-packagr library** (`ng generate library`). Rejected for now — it adds a separate build target and
  ordering; path-mapped source gives the same import surface (`@ch/*`) with a simpler pipeline. It can be promoted
  to a packaged library later without changing import sites.
- **Admin as a guarded area of the customer app.** Rejected — a distinct app keeps the storefront bundle lean and
  the two audiences cleanly separated, while the shared library prevents duplication.

## Consequences
- One source of truth for API access and auth across both apps; the admin console is permission-aware end to end.
- Adding the admin app did not change the customer app's behaviour (only its import paths).

## Known limitations (documented, not hidden)
- **No "list everything" admin endpoints yet:** the admin product list reuses the public catalog search (active
  products only) and the shipping list shows active methods only — a backend admin-list endpoint (incl. drafts /
  inactive) is a future addition. Create/publish/archive/deactivate all work regardless.
- **Coupon editing** is create + activate/deactivate (no full edit form yet); **order status** renders as a plain
  badge.
- **`permissionGuard` allows through while the profile is still loading** on a hard refresh (the API enforces).
- **Live end-to-end (SPA ↔ API ↔ Postgres) is deferred** to Docker, as in every prior phase.

## Verification
- `ng build commercehub-web` and `ng build commercehub-admin` → **0 errors** (admin initial 288 kB raw / 81 kB
  transfer; customer unchanged); the customer app builds unchanged after being refactored onto `@ch/*`.
- `ng test commercehub-web` and `ng test commercehub-admin` (vitest) → **2 + 2 passed**.
- **Running-app smoke test:** the admin dev server boots the SPA (zoneless, DI, router, shared `@ch/*` all OK);
  navigating to `/dashboard` unauthenticated redirects to the admin sign-in; console is clean (no errors, no
  network calls until sign-in).
