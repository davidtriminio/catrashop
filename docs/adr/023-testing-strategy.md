# ADR-023: Testing strategy — integration, architecture, and E2E under an infra-less policy

- **Status:** Accepted
- **Date:** 2026-08-13

## Context
Phases 0–17 built the platform with a solid base of **unit tests** (109, one suite per module plus
Infrastructure). Phase 18 broadens the test pyramid with the layers that unit tests cannot cover:
**integration tests** over the real HTTP pipeline, **architecture tests** that make the modular-monolith
boundaries executable, and **end-to-end** coverage on both the API and the frontend.

The binding constraint is the standing one for this project: **Docker is not running**, so PostgreSQL,
RabbitMQ, and n8n are unavailable. Tests must be deterministic and green in an infra-less environment,
yet the DB-backed and browser-backed checks must be real and ready to run once the stack is up — the
"build now, verify under Docker later" discipline used since Phase 2.

## Decision

### Backend — architecture tests (`CommerceHub.ArchitectureTests`, NetArchTest)
Encode the rules that define the modular monolith so a violation fails the build, not a code review:
- **Module isolation:** no module assembly may depend on another module's `Domain`, `Application`,
  `Infrastructure`, or `Api` namespaces — cross-module access goes only through published `Contracts`
  (in-process) or integration events. `Contracts` is deliberately excluded from the forbidden set.
- **No module depends on the API composition root** (`CommerceHub.Api`).
- **Inward layering:** `Domain` depends on no outer layer and on no persistence framework (EF Core /
  Npgsql); `Application` never takes a direct dependency on the Npgsql driver.
- **Handlers live in the composition root:** no business module implements `IIntegrationEventHandler<>` —
  handlers stay in `CommerceHub.Api.Integration` so modules never couple to one another. (66 checks:
  11 modules × 6 rules, driven by `[Theory]` data.)

### Backend — integration tests (`CommerceHub.IntegrationTests`, `WebApplicationFactory<Program>`)
Boot the **real** API host in-memory over the full middleware pipeline. The host is configured **without
live infrastructure**: a placeholder PostgreSQL connection string satisfies each module's registration
(EF Core connects lazily, so the app still starts — the same resilience that lets the app boot with the
broker down). These tests cover everything that does **not** require a database round-trip:
- health/readiness probes, the service banner enumerating all 11 modules, unknown-route 404s;
- the **auth pipeline order** on a real permission-protected endpoint — 401 (no/garbage token),
  403 (authenticated but missing the permission), and a 400 ProblemDetails from request validation once
  the permission is present — each resolved by the pipeline before any endpoint body / DB call.

Configuration override: the minimal-hosting `ConfigurationManager` materializes before
`WebApplicationFactory`'s `ConfigureAppConfiguration` delegates run, so an in-memory source does **not**
reliably override the app's own `appsettings`. The factory therefore sets the JWT signing key via an
**environment variable** (added after the JSON sources by the default builder, so it wins deterministically),
and mints test tokens against that same key.

### Backend — DB-backed E2E, gated (deferred to Docker)
A real end-to-end slice through Identity (register → read own profile with the issued token; unregistered
login → 401) is committed and compiled, guarded by a custom `[PostgresFact]`. The gate opens a real Npgsql
connection to the dev database (`SELECT 1`) once per run — an open TCP port is **not** sufficient, the
database and credentials must actually be usable — and **skips cleanly** when it is not. The tests light up
automatically under `docker compose up` with migrations applied.

### Frontend — component/unit (Vitest via `@angular/build:unit-test`) and E2E (Playwright)
- **Vitest** coverage grows from 2 to **23** across both apps and the shared library: the `problemMessage`
  RFC-7807 parser; the `AuthStore` (session set/persist/restore/clear, permission checks) with a mocked
  `AuthApi`; the `authGuard` / `permissionGuard` redirect logic; the admin **route-guard configuration**
  (drift guard between nav and routes); and a component render smoke test (`Forbidden`).
- **Playwright** drives the customer web app in a real headless Chromium against a live `ng serve`. The
  specs are **infrastructure-free by design** — a fresh browser has no session, so a protected route
  redirects to `/login` purely client-side, and the login form renders and validates without the backend.
  Full journeys needing the API + database are part of the Docker-up pass.

## Alternatives considered
- **Testcontainers / SQLite for integration tests.** Rejected for now: Testcontainers needs Docker (the
  very thing that is down), and SQLite/EF-InMemory do not honor the PostgreSQL-specific mapping the modules
  rely on (schema-per-module, snake_case, FTS), so they would test a different system. The pipeline-level
  factory gives real, infra-less coverage today; DB behavior is verified under real Postgres via the gated
  E2E, not a substitute engine.
- **Reflection-only boundary checks instead of NetArchTest.** NetArchTest expresses namespace/dependency
  rules far more legibly; reflection is used only where it reads better (scanning for handler interfaces).
- **A monolithic full-journey E2E as the headline test.** Deferred: it needs Docker and would be red today,
  which violates the "never claim green without running it" rule. It is written and gated instead.

## Consequences
- Backend: **183 tests pass, 2 skipped, 0 failed** (109 unit + 66 architecture + 8 integration; 2 gated E2E
  skip without Postgres). The module boundaries are now regression-proof.
- Frontend: **23 Vitest tests** (both apps + shared) and **3 Playwright e2e** pass headless.
- New dev dependencies: `NetArchTest.Rules`, `Microsoft.AspNetCore.Mvc.Testing`, `Npgsql` (probe only) on
  the backend; `@playwright/test` on the frontend. All pinned via Central Package Management / the lockfile.
- Deferred to Docker: the 2 gated Identity E2E facts, and any full checkout→pay→ship journey (API + DB).
