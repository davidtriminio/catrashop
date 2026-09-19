# PR-023 — Documentation polish

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/phase-23-docs` → `develop`
- **Type:** `docs`
- **Status:** ✅ Merged

## Objective
Phase 23: bring the documentation up to the standard of the code. This phase writes no production code — it
makes the docs **accurate** (the README had drifted from what was actually built), **complete** (it referenced
files that did not exist), and **navigable** (indexes + an architecture overview). No ADR: this phase records no
architectural decision, it corrects and completes the record of decisions already made.

## What was wrong
An accuracy pass against the source found the README claiming things the code does not do:
- **Non-existent modules** in the architecture diagram — `Reviews`, `Administration`, and `Audit` are not
  backend modules (there are 11: Identity, Catalog, Inventory, Customers, Cart, Promotions, Orders, Payments,
  Shipping, Notifications, AI). Administration is the `commercehub-admin` SPA; auditing is a log inside Identity.
- **Wrong stack entries** — *Serilog* (the app uses `Microsoft.Extensions.Logging` + OpenTelemetry) and
  *Testcontainers* (it uses `WebApplicationFactory` + a Postgres-gated E2E). Neither package is referenced.
- **Redis listed as the cache/rate-limiter** — it is provisioned in compose but no backend code references it;
  rate limiting and output caching are in-process.
- **Dead links** — `docs/architecture`, `docs/demo-data.md`, and `CHANGELOG.md` were all linked but missing.
- **A false runtime claim** — "applies migrations on startup"; the app does not auto-migrate.
- **Stale prerequisites / paths** — Node 20 (CI uses 24) and `dotnet run --project backend/src/Api` (the
  project is `backend/src/Api/CommerceHub.Api`).

## Changes
- **`README.md`** — corrected architecture diagram (11 real modules, admin as a SPA, Redis marked
  provisioned-but-unwired, Jaeger/Prometheus shown), corrected stack table and prerequisites, fixed the quick
  start (real project path, no false auto-migrate, both SPAs with ports), refreshed status (23/24), and repointed
  the documentation map at the now-existing files.
- **`docs/architecture/README.md`** (new) — the overview the README promised: system context, module map,
  boundary-enforcement diagram, checkout sequence, order-lifecycle state machine, Outbox flow, and the AI
  boundary. Every diagram is grounded in current code (e.g. the state machine mirrors `OrderStateMachine`; the
  sequence mirrors `OrderCheckoutService`).
- **`docs/adr/README.md`** (new) — an ADR index with statuses and a note explaining the intentional numbering
  gaps.
- **`docs/README.md`** (new) — a documentation hub.
- **`CHANGELOG.md`** (new) — Keep-a-Changelog, phase by phase, each entry linking its ADR + PR.
- **`docs/demo-data.md`** (new) — an honest end-to-end walkthrough over real routes (no seed script yet; that is
  called out, not implied).
- **`frontend/README.md`** — replaced the default Angular CLI boilerplate with a real workspace README (two apps,
  the `@ch/*` shared library, ports, commands).

## Verification
Documentation only — no source changed, so the suites are unaffected (backend **200 pass / 2 skip / 0 fail**,
frontend Vitest **23** as of Phase 22). Every factual claim was checked against the code before writing: module
list, absence of Serilog/Testcontainers/Redis usage, the order state table, checkout ordering, route prefixes
(`/api/v1/...`), the admin dev port (4300), and the `@ch/*` alias. All internal doc links resolve to existing
files.

