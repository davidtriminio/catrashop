# PR-022 — Performance review

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/phase-22-performance` → `develop`
- **Type:** `perf`
- **Status:** ✅ Merged

## Objective
Phase 22: a dedicated performance pass over the running system — remove avoidable work on the hot read paths
and add the standard HTTP-layer optimisations — under the same constraints as every phase since 15: **no new
dependencies** and **no infrastructure required** (app + integration host boot and pass without Postgres/
RabbitMQ; live DB round-trips verified under Docker).

The review found the data model and indexing already solid — every hot filter/lookup column is indexed and the
customer-facing reads already project + page server-side. The gaps were narrower: change tracking on read
paths, per-request DbContext construction, and no compression or caching at the HTTP layer.

## Changes
- **DbContext pooling** — `AddModuleDbContext` now uses `AddDbContextPool` (was `AddDbContext`); one change,
  all eleven modules. Every module context is pooling-safe (options-only constructor, no request-scoped state).
- **No-tracking reads (opt-in)** — `AsNoTracking()` on the four display-only entity reads (catalog product
  detail, order detail, notification inbox listing, shipping-methods listing). Tracking stays the default;
  every aggregate load that mutates is left tracked, so no write can silently stop persisting.
- **`Performance` composition-root concern** (`AddCatrashopPerformance` / `UseCatrashopPerformance`,
  section `Performance`, framework-only, mirrors the Phase 20 `Security` module):
  - *Response compression* — Brotli + Gzip (`Optimal`) over the default MIME set + `application/problem+json`;
    `EnableForHttps` **off by default** (BREACH; a TLS proxy usually compresses).
  - *Output caching* — in-memory `catalog-reads` policy (60 s, vary-by full query string, tag `catalog`) on the
    four anonymous catalog GETs. Policy name + `.CacheCatalogReads()` helper live in **BuildingBlocks** so the
    Catalog module opts in without referencing the API host (architecture tests still green). Compression sits
    outside caching; both run before auth so an anonymous hit short-circuits early.
- **Frontend** — `provideHttpClient(withFetch())` on both apps (lighter, modern default, hydration-ready). The
  rest was already optimal: zoneless + signals, lazy routes, every `@for` tracked, budgets set, no `<img>`.

## Technical decisions
- **Opt-in no-tracking, not a global flip** — a missed `AsTracking()` would silently drop a write, and the
  offline suite can't catch that with DB round-trips deferred to Docker. So no-tracking is applied only to
  verified read-only paths.
- **Shared cache convention in BuildingBlocks** — keeps modules decoupled from the host, the same seam
  `RequirePermission` already uses.
- **No fabricated indexes** — indexing is already complete for the current queries. The one real gap, a GIN
  expression index for full-text search, needs a live Postgres to author/validate and is an architectural
  change to search — recorded as the top follow-up, not authored blind.

## Testing / verification
- `dotnet build/test Catrashop.slnx -c Release` → **200 passed, 2 skipped, 0 failed** (+5 integration):
  Brotli and Gzip negotiated on the live pipeline, no compression when the client doesn't ask, output-cache
  store registered, `PerformanceOptions` bind with safe defaults. Build is warnings-as-errors clean.
- Architecture tests (66) still pass — the caching seam did not introduce a module→host dependency.
- Frontend: both apps build in production; Vitest **23** (17 web + 6 admin).

## Deferred to Docker
Measuring the output-cache HIT and the pooling / no-tracking gains against a live database (the suite proves
the wiring; the numbers need real infrastructure). The FTS GIN index is the top follow-up.

