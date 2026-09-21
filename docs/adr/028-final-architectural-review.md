# ADR-028: Final architectural review

- **Status:** Accepted
- **Date:** 2026-09-21

## Context
Phase 24 is the capstone: a single reviewing pass over the whole system to decide whether the architecture the
project set out to build (ADR-001) is actually what got built, whether every guard still holds, and whether the
result is release-ready. It records residual limitations honestly rather than papering over them. This ADR is the
verdict; it changes no code.

## What was reviewed, and the evidence

All guards were re-run fresh on this branch (not taken from memory):

| Guard | Result |
|---|---|
| Backend build, `-c Release` (warnings are errors) | **0 warnings / 0 errors** |
| Backend test suite (`dotnet test CommerceHub.slnx -c Release`) | **200 passed / 2 skipped / 0 failed** |
| Architecture tests (NetArchTest, 11 modules × 6 rules) | **66 / 66** |
| NuGet vulnerability scan (`--vulnerable --include-transitive`, all projects) | **none** |
| SDK pin resolves (`backend/global.json`) | **10.0.401** (latestFeature over 10.0.400) |
| Frontend production builds (`commercehub-web`, `commercehub-admin`) | **both clean** |
| Frontend unit tests (Vitest) | **23** (17 web + 6 admin) |
| Frontend e2e (Playwright, headless Chromium) | **3 passed** |

The 2 skipped tests are the Postgres-gated Identity E2E, which skip by design without a live database.

## Findings — the architecture holds

- **Module boundaries are real, not aspirational.** The 66 architecture tests fail the build if a module reaches
  into another module's `Application`/`Domain`/`Infrastructure` (only `Contracts` + integration events cross), if
  a module references the API host, if `Domain` touches EF Core/Npgsql or `Application` touches the Npgsql driver
  (inward layering), or if an integration-event handler lives outside the composition root. All green — the
  boundaries the design promised are enforced by the compiler + CI, exactly as ADR-001 intended.
- **Eleven modules, one deployable, schema-per-module.** Each module owns its PostgreSQL schema, migrations, and
  Outbox table; the composition root is the only place that knows all of them.
- **Consistency model is sound.** The consistency-critical path (checkout) is a single unit of work per module
  ordered so inventory is reserved before the order is persisted and keyed on the cart id for idempotency;
  cross-module side effects leave through the transactional Outbox. No distributed transaction, no saga on the
  happy path.
- **Security and performance are defence-in-depth and config-driven.** Headers, CORS, per-client rate limiting,
  a fail-fast secret guard (Phase 20); DbContext pooling, no-tracking reads, compression, output caching
  (Phase 22) — all with safe defaults, no new dependencies, and verified live on the pipeline.
- **The risky surface is bounded.** The AI assistant can only call an allow-list of read-only tools with
  ownership injected from the JWT; payment and LLM providers sit behind abstractions with deterministic fakes.

## Residual limitations & recommended follow-ups
Recorded deliberately — none block the release, each has an owner-facing next step.

1. **Live-database verification is Docker-deferred.** Repository round-trips and the gated E2E are validated by
   migration/model checks and an infra-less host, not live queries. *Next:* run the full suite with the compose
   stack up (Postgres/RabbitMQ) and the n8n import, once the Docker engine is available.
2. **Full-text search has no GIN index.** Catalog search computes `to_tsvector` at query time. A GIN expression
   index is the single biggest search win but needs a live Postgres to author and validate. *Top follow-up.*
3. **Redis is provisioned but unwired.** Rate limiting and output caching are in-process, which is correct for a
   single instance. *Next, for horizontal scale-out:* back both with Redis so limits and cache are shared.
4. **No automated seeding / migration bootstrap.** Migrations are applied per module by hand and there is no seed
   script. *Next:* a bootstrap command + demo seed (see `docs/demo-data.md`).
5. **CI has not had its first live run.** Workflows are authored and locally validated; the first GitHub run,
   CodeQL results, Dependabot PRs, and branch-protection required-checks are repo settings, pending the push.
6. **Payment and chat providers are fakes by design.** Swapping in real providers is a config/adapter change
   behind the existing abstractions, not a rewrite — the seam is proven by the tests.

## Decision
The system is **accepted as architecturally sound and release-ready (v1.0.0)**. It delivers the intended
modular-monolith: enforced boundaries, one transaction where it matters, event-driven side effects through the
Outbox, and a clear extraction path if a module ever needs to become a service. The residuals above are recorded
as post-1.0 work, not blockers. Tagging `v1.0.0` and pushing to enable the CI's first live run are the natural
next actions, to be taken by the maintainer.

## Consequences
- The 24-phase roadmap is complete; every phase left the system compiling and every guard is green at close.
- No code changed in this phase; this ADR plus PR-024 are the record.
- The follow-up list is the starting backlog for a hypothetical v1.1.
