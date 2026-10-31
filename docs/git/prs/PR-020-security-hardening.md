# PR-020 — Security hardening (headers, CORS, rate limiting, secret guard)

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/phase-20-security` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Phase 20: harden the HTTP edge — browser-trust boundary, abuse throttling, defence-in-depth headers, and a
fail-fast guarantee that development placeholder secrets can never reach production — while keeping the app
fully functional and resilient with **no infrastructure present**.

## Changes
- **Security module** (`Security/SecurityHardeningExtensions.cs`) wired at the composition root via
  `builder.AddCatrashopSecurity()` / `app.UseCatrashopSecurity()`, bound to a single `Security` config
  section (`SecurityOptions`) with safe defaults.
- **Response headers** (`SecurityHeadersMiddleware`): `X-Content-Type-Options`, `X-Frame-Options`,
  `Referrer-Policy`, `X-Permitted-Cross-Domain-Policies`, `Cross-Origin-Opener/Resource-Policy`, and a
  restrictive `Content-Security-Policy` for a JSON API. `Server` banner suppressed (`AddServerHeader = false`).
  Runs first, so rejections and error pages inherit the headers.
- **CORS**: one named policy trusting only the exact configured origins (dev: customer `:4200`, admin `:4300`),
  fixed method/header allow-list, credentials on, wildcards never honoured; empty list ⇒ no CORS headers.
- **Rate limiting**: global per-client (`sub` else IP) fixed-window limiter — generous for ordinary endpoints,
  tight for the unauthenticated credential endpoints (`/identity/login|register|refresh|password`); 429 +
  `Retry-After` + `problem+json`. Global, so no module opts in (isolation preserved).
- **Secret guard** (`SecretGuard`): boot-time, non-Development only — refuses to start with a missing, too
  short, or placeholder `Identity:Jwt:SigningKey` / `Payments:WebhookSecret`.
- **Optional HTTPS/HSTS** behind `Security:EnableHttpsRedirection` (off for dev/test).
- Dev `appsettings.Development.json` gains the `Security` section.

## Technical decisions
- **Global limiter over per-endpoint policies** — a per-endpoint policy name would couple a module to the
  host and break the architecture tests; the host recognises the auth path prefixes instead.
- **Custom headers + guard over a third-party package** — a few lines under our control, consistent with the
  Phase 19 stance (framework logger, custom health checks).
- **Guard complements, not replaces,** the Identity module's existing `ValidateOnStart` key-length check: it
  also catches a *placeholder* key of valid length and covers the webhook secret, and is environment-aware.
- **No new dependencies** — CORS and `Microsoft.AspNetCore.RateLimiting` ship in the ASP.NET Core framework.

## Testing / verification
- Backend `dotnet test Catrashop.slnx` → **195 passed, 2 skipped, 0 failed** (was 186; +9):
  - 4 × headers/CORS (headers present, no `Server`, allowed-origin preflight permitted, unknown-origin denied),
  - 1 × rate-limit burst on `/login` → at least one 429 with `Retry-After` + `problem+json`,
  - 4 × secret guard (Development tolerates placeholders; Production rejects placeholder and missing secrets,
    and accepts strong ones).
- Verified live (`dotnet run`, Development): `/` returns all six hardening headers + CSP and **no** `Server`
  banner; CORS preflight from `http://localhost:4200` → `Access-Control-Allow-Origin`, from
  `https://evil.example` → none; a 35-request burst on `/login` → 30 pass then **5 × 429**; `/metrics` and the
  `X-Correlation-ID` header unaffected.
- Deferred: HTTPS/HSTS under a TLS-terminating deployment; production rate-limit tuning against real traffic.

## Architecture / module isolation
Unchanged — all wiring lives in the API host; modules are untouched, so the 66 NetArchTest boundary rules
still pass. The rate limiter is global (no module references a host policy).

