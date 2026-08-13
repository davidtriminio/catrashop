# PR-018 — Testing (unit, integration, E2E, architecture)

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/phase-18-testing` → `develop`
- **Type:** `test`
- **Status:** ✅ Merged

## Objective
Phase 18: broaden the test pyramid beyond unit tests — real HTTP-pipeline **integration** tests,
**architecture** tests that enforce the modular-monolith boundaries, and **end-to-end** coverage on both
the API and the frontend — while staying deterministically green with **Docker down** (build now, verify
under Docker later).

## Changes

### Backend
- **`CommerceHub.ArchitectureTests` (NetArchTest) — 66 checks (11 modules × 6 rules).** Module isolation
  (no reach into another module's Domain/Application/Infrastructure/Api — only `Contracts`/events); no
  module depends on the API host; inward layering (Domain free of outer layers and of EF Core/Npgsql;
  Application free of the Npgsql driver); integration-event handlers only in the composition root.
- **`CommerceHub.IntegrationTests` (`WebApplicationFactory<Program>`) — 8 tests + 2 gated.** Boots the real
  host with no live infra (placeholder connection string; lazy EF). Covers health/readiness, the module
  banner (all 11), 404s, and the auth pipeline order on a real protected endpoint: **401 / 403 / 400**
  resolved before any DB call. A DB-backed Identity E2E (register→/me, unregistered login→401) is committed
  and **gated by `[PostgresFact]`** (real Npgsql `SELECT 1` probe), skipping cleanly without Postgres.
- Signing key injected via **env var** (deterministic override over `appsettings` in minimal hosting);
  `TestTokens` mints matching JWTs. New dev deps via CPM: `Microsoft.AspNetCore.Mvc.Testing`,
  `NetArchTest.Rules`, `Npgsql` (probe only).

### Frontend
- **Vitest 2 → 23** across both apps + shared lib: `problemMessage` parser; `AuthStore`
  (set/persist/restore/clear, permission checks) with a mocked `AuthApi`; `authGuard`/`permissionGuard`
  redirects; admin **route-guard drift** check; `Forbidden` component render.
- **Playwright e2e (3, headless Chromium vs. live `ng serve`)** — infra-free: protected route → client-side
  `/login` redirect with returnUrl; login form renders + validates; register link navigates. `test:web`,
  `test:admin`, `e2e` scripts; artifacts gitignored.

## Technical decisions
- **Architecture rules as executable tests, not review checklists** — a boundary violation fails the build.
  Verified the monolith is already clean (66/66).
- **Infra-less integration via lazy EF + resilient host.** Pipeline behavior (auth/authz/routing/
  ProblemDetails/health) is real and runs with no database; DB behavior is verified under real Postgres via
  the gated E2E — not a substitute engine (SQLite/InMemory would test a different mapping).
- **Honest deferral.** DB/browser-full journeys that need Docker are written and gated (skip), never faked
  green.

## Testing / verification
- Backend `dotnet test CommerceHub.slnx` → **183 passed, 2 skipped, 0 failed** (109 unit + 66 architecture
  + 8 integration; 2 Identity E2E skip — no Postgres).
- Frontend `ng test commercehub-web` → **17 passed**; `ng test commercehub-admin` → **6 passed**;
  `playwright test` → **3 passed** (real headless Chromium, live dev server).
- ⚠️ **Deferred (no Docker):** the 2 gated Identity E2E facts; a full checkout→pay→ship journey (API + DB).

## Review cycle
**🟢 APPROVE (simulated)** — The modular boundaries are now regression-proof, and the integration layer
exercises the real pipeline (auth 401/403/400) without pretending to have a database. DB- and browser-full
checks are gated and ready rather than skipped-by-omission or faked. Deferrals are explicit.

## Merge result
Merged into `develop` with `--no-ff`.
