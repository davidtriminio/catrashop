# ADR-025: Security hardening — headers, CORS, rate limiting, secret guard

- **Status:** Accepted
- **Date:** 2026-08-30

## Context
Phase 20 hardens the HTTP edge of the platform. The domain already enforces authentication (JWT bearer),
permission-based authorization, idempotency, and HMAC-verified webhooks; what was missing were the
transport-level controls every internet-facing API needs: browser-trust boundaries (CORS), abuse throttling
(rate limiting), defence-in-depth response headers, and a guarantee that the convenient development
placeholder secrets can never reach a real environment. As with every phase since 15, nothing may require
infrastructure to start, and the in-memory integration host (which runs as `Development`) must be unaffected.

## Decision

### One composition-root module, config-driven with safe defaults
`CommerceHub.Api.Security.SecurityHardeningExtensions` owns the wiring, bound to a single `Security`
configuration section (`SecurityOptions`). Each control has a safe default, so the app runs locally with no
configuration and production tightens each knob. Two entry points mirror the observability module:
`builder.AddCommerceHubSecurity()` (services + fail-fast guard) and `app.UseCommerceHubSecurity()` (pipeline).

### Response hardening headers
`SecurityHeadersMiddleware` sets `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`,
`Referrer-Policy: no-referrer`, `X-Permitted-Cross-Domain-Policies: none`, the cross-origin isolation pair
(`Cross-Origin-Opener-Policy` / `Cross-Origin-Resource-Policy: same-origin`), and a restrictive
`Content-Security-Policy` (`default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'`)
suited to a JSON API. The `Server` banner is suppressed at the Kestrel level (`AddServerHeader = false`). The
middleware runs first so the headers also cover error pages and rate-limit rejections.

### CORS restricted to configured origins
A single named policy (`commercehub`) trusts only the exact origins in `Security:Cors:AllowedOrigins`
(dev: the customer SPA `http://localhost:4200` and admin SPA `http://localhost:4300`), with a fixed method and
header allow-list and credentials enabled. Wildcards are never honoured. An empty list emits no CORS headers
at all — the correct posture when the SPA is served same-origin or behind a reverse proxy.

### Per-client rate limiting, tighter on credentials
A global `PartitionedRateLimiter` (ASP.NET Core, no package) partitions by client (authenticated subject when
present, else remote IP) using a fixed window. Ordinary endpoints get a generous budget; the unauthenticated
credential endpoints (`/identity/login|register|refresh|password`) get a much tighter one — that is the brute
force and account-enumeration surface. Rejections return **429** with a `Retry-After` header and a
`application/problem+json` body. The limiter is global, so no module opts in and module isolation is preserved.

### Fail-fast secret guard
`SecretGuard.Validate` runs at boot and, in any non-`Development` environment, refuses to start if
`Identity:Jwt:SigningKey` or `Payments:WebhookSecret` is missing, too short, or still contains a checked-in
placeholder fragment (`change-me`, `dev-only`, …). A known signing key forges any identity; a known webhook
secret forges payment callbacks — both must be impossible to ship by accident. `Development` keeps the
placeholders, which is how the app (and the integration host) runs offline.

### Optional HTTPS/HSTS
`EnableHttpsRedirection` (off by default) turns on `UseHsts()` + `UseHttpsRedirection()`. It stays off for the
plain-HTTP dev and in-memory hosts; a TLS-terminating deployment enables it.

## Alternatives considered
- **Per-endpoint rate-limit policies** (`RequireRateLimiting("auth")` on the Identity group). Rejected: it
  leaks a host-defined policy name into a module, breaking the isolation the architecture tests enforce. A
  global limiter that recognises the auth path prefixes keeps all knowledge in the host.
- **A third-party security-headers package** (e.g. NetEscapades). The header set is a dozen lines and must stay
  under our control; a custom middleware avoids a dependency, matching the Phase 19 stance on health checks.
- **Relying only on the Identity module's existing `ValidateOnStart` key-length check.** That catches a short
  key but not a shipped *placeholder* key of valid length, and says nothing about the webhook secret. The guard
  is the environment-aware complement.
- **CORS `AllowAnyOrigin`.** Never — it is incompatible with credentials and defeats the browser trust
  boundary. Explicit origins only.

## Consequences
- No new dependencies: CORS and `Microsoft.AspNetCore.RateLimiting` are part of the ASP.NET Core shared
  framework; headers and the guard are a few lines of our own code.
- Dev `appsettings.Development.json` gains a `Security` section (CORS origins, rate-limit budgets, HTTPS off).
- Backend tests: **195 pass, 2 skipped, 0 failed** (was 186); 9 new tests — 4 header/CORS assertions, 1
  rate-limit burst (→ 429 + `Retry-After`), 4 secret-guard cases (dev tolerates placeholders; production
  rejects placeholder/missing and accepts strong secrets).
- Verified live: `/` carries all hardening headers and no `Server` banner; a preflight from
  `http://localhost:4200` is allowed while `https://evil.example` gets no allow-origin; a burst past the auth
  budget returns 429; `/metrics` and the correlation id are unaffected.
- Deferred: HTTPS/HSTS verification under a TLS-terminating deployment, and tuning the production rate-limit
  budgets against real traffic.
