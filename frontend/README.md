# Catrashop — Frontend

An Angular 22 workspace with **two applications** and a **shared library**, all standalone, **zoneless**, and
signal-driven. See [ADR-018](../docs/adr/018-frontend-architecture.md) and
[ADR-019](../docs/adr/019-admin-app-shared-library.md) for the reasoning.

## Layout

| Path | What it is | Dev port |
|---|---|---|
| `src/` | **commercehub-web** — the customer storefront (workspace default project) | 4200 |
| `projects/commercehub-admin/` | **commercehub-admin** — the back-office console | 4300 |
| `shared/` | Code shared by both apps, imported via the `@ch/*` path alias | — |

The shared library holds the typed API models and services, the JWT auth store + interceptor (with rotating
refresh) and route guards, and small utilities — so neither app duplicates the API contract or the auth logic.
The customer app uses `authGuard`; the admin app adds `permissionGuard` over the same auth foundation.

## Prerequisites

Node **24+** and **pnpm 11+**. Install once with `pnpm install`.

## Common commands

```bash
# Serve
pnpm start                      # customer app  → http://localhost:4200
pnpm ng serve commercehub-admin # admin app     → http://localhost:4300

# Production builds (output in dist/)
pnpm build                      # customer app
pnpm ng build commercehub-admin # admin app

# Unit tests (Vitest)
pnpm test                       # customer app (watch)
pnpm run test:web               # customer app (single run)
pnpm run test:admin             # admin app (single run)

# End-to-end (Playwright — boots its own ng serve)
pnpm run e2e
```

## Configuration

Each app has its own `environments/` — `environment.ts` is replaced by `environment.development.ts` for dev
builds. The API base URL is provided through the `API_BASE_URL` injection token in each app's `app.config.ts`;
`HttpClient` runs on the Fetch backend (`withFetch()`) with the `authInterceptor` attaching the bearer token.

For the wider picture — how the SPAs sit in front of the modular-monolith API — see the
[architecture overview](../docs/architecture/README.md).
