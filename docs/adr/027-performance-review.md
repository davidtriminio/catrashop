# ADR-027: Performance review

- **Status:** Accepted
- **Date:** 2026-09-13

## Context
Phase 22 is a dedicated performance pass over the running system rather than a new feature. The goal is to
remove avoidable work on the hot read paths and add the standard HTTP-layer optimisations an e-commerce API
wants, while keeping the phase's two constraints: **no new dependencies** and **no infrastructure required**
(the app and the in-memory integration host must still boot and pass the suite without Postgres/RabbitMQ; a
live database round-trip is verified under Docker).

A review of the code found the data model and indexing already in good shape — every hot filter/lookup column
already carries an index (order number/customer/cart/status, notification recipient+read, shipment
order/tracking, payment idempotency/order, inventory SKU, cart customer, catalog slug/status/SKU), and the
customer-facing search and list endpoints already project to DTOs (server-side paging, no over-fetch). The
gaps were narrower and specific:

1. **Change tracking on read paths.** No query in the codebase used `AsNoTracking`. Endpoints that materialise
   an entity only to map it to a response were still paying for the change-tracker snapshot and identity map.
2. **Per-request DbContext construction.** Every module registered its context with `AddDbContext`, allocating
   a fresh context and internal service provider on each request.
3. **No HTTP-layer optimisation.** Responses were neither compressed nor cached, so identical anonymous
   catalog reads re-ran the full query + serialisation every time and shipped uncompressed JSON.

## Decision

### Data layer
- **DbContext pooling.** `AddModuleDbContext` now uses `AddDbContextPool` instead of `AddDbContext`. The
  context is reset and reused between requests, removing per-request context/internal-provider allocation. Every
  module context is pooling-safe — its constructor takes only `DbContextOptions` and it holds no request-scoped
  state — so the default pool size (1024) stands. One change, all eleven modules.
- **No-tracking reads, opt-in.** Tracking stays the **default** (a missed tracked load would silently fail to
  persist, and most DB round-trips are Docker-deferred so the suite could not catch that). Instead
  `AsNoTracking()` is added only to the four read paths that materialise an entity purely for display and never
  mutate it: the catalog product-detail read, the order-detail read, the notification inbox listing, and the
  shipping-methods listing. Every repository `GetBy…` that loads an aggregate to mutate it is left tracked.

### HTTP layer — a composition-root `Performance` concern
Mirrors the Phase 20 `Security` module (`AddCommerceHubPerformance` / `UseCommerceHubPerformance`), config-driven
under section `Performance`, safe defaults, framework-only (`Microsoft.AspNetCore.ResponseCompression` and
`Microsoft.AspNetCore.OutputCaching` ship in the shared framework — no package added).

- **Response compression.** Brotli + Gzip (both `Optimal`) over the framework's default MIME set plus
  `application/problem+json`. `EnableForHttps` is **off by default**: compressing a TLS response that reflects
  request input alongside a secret is the BREACH oracle, and a TLS-terminating proxy usually compresses anyway.
  It is a config knob for deployments that terminate TLS at the app and accept the trade-off.
- **Output caching.** An in-memory `catalog-reads` policy (60 s TTL, vary-by full query string, tagged
  `catalog`) applied to the four anonymous catalog GETs (search, detail, categories, brands). The policy name
  and the `.CacheCatalogReads()` endpoint helper live in **BuildingBlocks** — the same seam
  `RequirePermission` uses — so the Catalog module opts in without referencing the API host (architecture tests
  still pass). Compression sits **outside** caching in the pipeline, so the cache stores one uncompressed body
  and each client still gets its own negotiated encoding; both run **before** authentication so an anonymous
  hit short-circuits early.

### Frontend
Already at the modern performance baseline — both apps are zoneless + signals, routes are lazy, every `@for`
carries `track` (compiler-enforced), production budgets are set, and there are no `<img>` tags to optimise. The
one applicable win: `provideHttpClient(withFetch())` on both apps, moving `HttpClient` onto the Fetch backend
(lighter, the modern default, hydration-ready). No behaviour change; interceptors are unaffected.

## Alternatives considered
- **Flip the global default to `NoTracking` and opt into `AsTracking()` in repositories.** The cleaner
  end-state, but a single missed `AsTracking()` silently drops a write, and with DB round-trips deferred to
  Docker the offline suite could not catch it. Rejected in favour of opt-in no-tracking on verified read paths.
- **New indexes / a GIN index for full-text search.** Indexing is already complete for the current queries, so
  no new B-tree index was justified. A GIN expression index over `to_tsvector(name || ' ' || description)` is
  the single biggest catalog-search win, but it is an expression/functional index that needs a live Postgres to
  author and validate correctly and is an architectural change to the search path — recorded as the top
  follow-up rather than authored blind.
- **App-level HTTPS compression on by default.** Rejected on BREACH grounds; left as an explicit opt-in.
- **`PreloadAllModules` for the SPA router.** Rejected: it trades idle bandwidth for faster navigation, not a
  clear win for a storefront with many lazy routes; left lazy.

## Consequences
- New files: `Performance/PerformanceOptions.cs`, `Performance/PerformanceExtensions.cs` (API host),
  `Caching/OutputCachePolicies.cs` (BuildingBlocks), `PerformanceTests.cs` (integration).
- Verified: `dotnet build/test -c Release` → **200 pass / 2 skip / 0 fail** (+5 integration: Brotli & Gzip
  negotiated on the live pipeline, no compression when unrequested, output-cache store registered, options bind
  with safe defaults). Frontend: both apps build in production, Vitest **23** (17 web + 6 admin).
- Deferred to Docker: measuring the output-cache HIT and the pooling/no-tracking gains against a live database
  (the offline suite proves the wiring; the numbers need real infrastructure). The FTS GIN index is the top
  follow-up.
