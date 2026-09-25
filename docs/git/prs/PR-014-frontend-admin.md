# PR-014 — Administration console (Angular) + shared library

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/frontend-admin` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Phase 14: the staff administration console, and the shared frontend library that lets it and the storefront reuse
one API/auth layer (Section 14; realizes the extraction planned in ADR-018).

## Changes
- **Shared library** `frontend/shared/` (path alias `@ch/*`): DTO models, per-context API services (customer +
  new admin services: catalog/inventory/orders/promotions/shipping/notifications), auth store/interceptor/guards
  (+ `permissionGuard`), and utilities (money pipe, problem helper). The customer app was refactored onto `@ch/*`.
- **Shared design system** `shared/theme.scss` used by both apps via a `shared` SCSS include path; admin overrides
  a teal accent + wider layout.
- **New app** `commercehub-admin` (own dev proxy on port 4300, env replacement): sidebar shell filtered by
  permission, `/forbidden` page.
- **Admin screens:** dashboard; catalog (create categories/brands/products, publish/archive); inventory (track
  SKU, receive/adjust/threshold, levels); orders (search by status, process/ship/deliver/cancel); promotions
  (create coupons, activate/deactivate); shipping (create/deactivate methods); notifications (manual send).
- ADR-019 (admin app & shared library).

## Technical decisions
- **Two apps, one workspace, one shared library** (path-mapped source, not ng-packagr — same `@ch/*` surface,
  single-step build). One source of truth for API access and auth.
- **Permission-gated routes + sidebar**, with the **backend as the real authority** (guard is UX; API returns
  403). Guard lets routes through while the profile is still loading on refresh.
- **No `baseUrl`** (TS 6 deprecates it) — path alias resolves relative to the config.
- **Known limitations (ADR-019):** no admin "list-all" endpoints yet (admin product list = active catalog search;
  shipping list = active methods); coupon edit is create + activate/deactivate; live SPA↔API↔Postgres deferred to
  Docker.

## Testing / verification
- `ng build commercehub-web` and `ng build commercehub-admin` → **0 errors** (admin 288 kB raw / 81 kB transfer);
  the customer app builds unchanged after the `@ch/*` refactor.
- `ng test commercehub-web` + `ng test commercehub-admin` (vitest) → **2 + 2 passed**.
- **Running-app smoke test:** admin SPA boots (zoneless/DI/router/shared `@ch/*`); `/dashboard` unauthenticated →
  admin sign-in; console clean.
- ⚠️ **Deferred (no Postgres):** the live management flows against a running API + database.

## Review cycle
**🟢 APPROVE (simulated)** — The shared library removes duplication cleanly and the customer app is unchanged
behaviourally; admin routes are permission-gated with the backend as the real authority. The missing admin
list-all endpoints and deferred live run are called out rather than hidden.

## Merge result
Merged into `develop` with `--no-ff`.
