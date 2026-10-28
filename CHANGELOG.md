# Changelog

All notable changes to CommerceHub. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); this repository is built **incrementally, one phase at
a time** (see [`docs/roadmap.md`](docs/roadmap.md)), so releases are tracked as phases. Every phase left the
system compiling; infrastructure-dependent runtime verification is deferred to Docker.

Each entry links the Architecture Decision Record and the simulated Pull Request that introduced it. The PRs are
**local workflow simulations** — no human reviewed the code.

## [Unreleased]
- **Fix:** the account page no longer fails when the customer profile is created by concurrent requests
  (idempotent provisioning); `setup-db.ps1` no longer clashes with a running API; `pnpm start` works on pnpm 11
  → [PR-030](docs/git/prs/PR-030-profile-race-and-dev-fixes.md)
- **Design foundation:** new shared design tokens and primitives (neutral palette with a lime accent, spacing scale,
  chips, badges, skeletons) for the storefront and admin console; cards and notifications no longer crowd each other
  → [PR-031](docs/git/prs/PR-031-design-foundation.md)
- **Catalog:** product list items carry their price; price filter (`minPrice`/`maxPrice`), `price_asc`/`price_desc`
  sorting and a `price-range` endpoint → [PR-032](docs/git/prs/PR-032-catalog-price-filters.md)
- **Storefront redesign:** responsive header, catalog with filter panel, active-filter chips, URL-driven state and
  pager, redesigned product page and cart → [PR-033](docs/git/prs/PR-033-storefront-redesign.md)
- **Mobile redesign:** shared design language, bottom navigation, new Summary and Settings tabs, order cards, detail
  with timeline and sticky action bar, confirmation sheets, light/dark → [PR-034](docs/git/prs/PR-034-mobile-redesign.md)
- **Orders and shared UI:** one status badge and one timeline component for both web apps; redesigned orders list,
  order detail, tracking page and footer → [PR-035](docs/git/prs/PR-035-orders-and-shared-ui.md)
- **Admin console redesign:** dashboard with order KPIs, status chips and badges in the orders list, order detail
  with a single next-step action and timeline, new sign-in → [PR-036](docs/git/prs/PR-036-admin-console-redesign.md)

## v1.1.0 — Oracle deployment & mobile admin app
- **Flutter admin app** (`mobile/`): order list/detail, state actions, biometric lock, local new-order alerts
  → [ADR-030](docs/adr/030-mobile-admin-app.md)
- **In-process event transport** (`Messaging:Transport=InProcess`) so the API runs without RabbitMQ on a 1 GB host
  → [ADR-029](docs/adr/029-in-process-event-transport.md)
- **Oracle VM deployment** (systemd + Apache + Cloudflare, SQL-script migrations, ~4 MB release bundle) in
  `deploy/oracle/`; frontends deploy to Vercel (`frontend/vercel.json`); the Docker stack moved to `deploy/docker/`
- API honours `X-Forwarded-*` from the local reverse proxy (real client IP for rate limiting)
- Repository hygiene: tooling attributions removed from history and docs
- Further changes belong to the post-1.0 backlog (residuals in [ADR-028](docs/adr/028-final-architectural-review.md)).
→ [PR-025](docs/git/prs/PR-025-repo-hygiene-and-dev-secrets-configurati.md) · [PR-026](docs/git/prs/PR-026-in-process-event-transport-and-forwarded.md) · [PR-027](docs/git/prs/PR-027-oracle-vm-release-systemd-apache-cloudfl.md) · [PR-028](docs/git/prs/PR-028-flutter-admin-app-for-order-management.md) · [PR-029](docs/git/prs/PR-029-local-setup-and-seed-scripts-guid-key-pe.md)

## Phase 24 — Final architectural review
Capstone review; no production code. Every guard re-run as evidence — Release build 0/0, backend 200 pass / 2
skip / 0 fail, architecture 66/66, no vulnerable packages, frontend both apps build + Vitest 23 + Playwright 3.
Verdict: architecture matches ADR-001, **accepted as release-ready (v1.0.0)**; residual follow-ups recorded.
→ [ADR-028](docs/adr/028-final-architectural-review.md) · [PR-024](docs/git/prs/PR-024-final-architectural-review.md)

## Phase 23 — Documentation polish
Accuracy + completeness pass over all docs: corrected README (real 11-module diagram, stack, quick start), new
architecture overview + ADR index + docs hub + this changelog + demo walkthrough, real frontend README.
→ [PR-023](docs/git/prs/PR-023-documentation-polish.md)

## Phase 22 — Performance review
DbContext pooling (all 11 modules), opt-in `AsNoTracking()` on display-only reads, response compression
(Brotli/Gzip) and output caching for anonymous catalog reads; frontend `withFetch()`. No new dependencies.
→ [ADR-027](docs/adr/027-performance-review.md) · [PR-022](docs/git/prs/PR-022-performance-review.md)

## Phase 21 — CI/CD
GitHub Actions build/test for both stacks + a security pipeline (CodeQL, dependency review, NuGet/pnpm audit,
Gitleaks) and Dependabot. Infrastructure-free.
→ [ADR-026](docs/adr/026-cicd-pipeline.md) · [PR-021](docs/git/prs/PR-021-cicd-pipeline.md)

## Phase 20 — Security hardening
Response headers, CORS restricted to configured origins, per-client rate limiting, fail-fast secret guard.
→ [ADR-025](docs/adr/025-security-hardening.md) · [PR-020](docs/git/prs/PR-020-security-hardening.md)

## Phase 19 — Observability
OpenTelemetry traces/metrics with opt-in exporters (Prometheus/OTLP/console), structured JSON logging,
correlation id, and live/ready health probes.
→ [ADR-024](docs/adr/024-observability.md) · [PR-019](docs/git/prs/PR-019-observability.md)

## Phase 18 — Testing
Architecture tests (NetArchTest), integration tests (WebApplicationFactory), a Postgres-gated E2E, expanded
Vitest specs, and Playwright e2e.
→ [ADR-023](docs/adr/023-testing-strategy.md) · [PR-018](docs/git/prs/PR-018-testing.md)

## Phase 17 — AI shopping assistant
Bounded agent loop over an allow-list of read-only tools, RAG over a seeded knowledge base, and guardrails
(injection guard, ownership from JWT, bounded iterations).
→ [ADR-022](docs/adr/022-ai-assistant.md) · [PR-017](docs/git/prs/PR-017-ai-assistant.md)

## Phase 16 — n8n automations
Four event-driven workflows over the `commercehub.events` exchange (welcome email, order receipt, review
request, low-stock alert) as importable JSON. No backend change.
→ [ADR-021](docs/adr/021-n8n-automations.md) · [PR-016](docs/git/prs/PR-016-n8n-automations.md)

## Phase 15 — Messaging
Outbox → RabbitMQ topic exchange → durable consumer queue → composition-root handlers; idempotent consumers;
resilient hosted services.
→ [ADR-020](docs/adr/020-messaging-rabbitmq-outbox.md) · [PR-015](docs/git/prs/PR-015-messaging.md)

## Phase 14 — Administration console
Second Angular app (`commercehub-admin`) with permission-gated routes; shared code extracted to `frontend/shared`
(`@ch/*`) and consumed by both apps.
→ [ADR-019](docs/adr/019-admin-app-shared-library.md) · [PR-014](docs/git/prs/PR-014-frontend-admin.md)

## Phase 13 — Customer storefront
Angular 22 workspace (standalone, zoneless, signals), typed API layer, JWT interceptor with rotating refresh,
guarded lazy routes across the customer journey.
→ [ADR-018](docs/adr/018-frontend-architecture.md) · [PR-013](docs/git/prs/PR-013-frontend-customer.md)

## Phase 12 — Notifications
In-app inbox + email behind `IEmailSender`, per-customer preferences, idempotent dispatch.
→ [ADR-017](docs/adr/017-notifications.md) · [PR-012](docs/git/prs/PR-012-notifications.md)

## Phase 11 — Promotions
Coupon aggregate (%/fixed, limits, window, caps), rules, redemption ledger, wired into checkout and released on
cancel/refund.
→ [ADR-016](docs/adr/016-promotions-discounts.md) · [PR-011](docs/git/prs/PR-011-promotions.md)

## Phase 10 — Shipping
Methods, shipments (one per order), tracking, shipment state machine driving order fulfilment.
→ [ADR-015](docs/adr/015-shipping-fulfillment-integration.md) · [PR-010](docs/git/prs/PR-010-shipping.md)

## Phase 9 — Payments
Fake provider, payment state machine, idempotency key, HMAC + dedup webhooks, refunds driving the order.
→ [ADR-014](docs/adr/014-payments-provider-idempotency-webhooks.md) · [PR-009](docs/git/prs/PR-009-payments.md)

## Phase 8 — Orders
Checkout orchestration (cart → reserve → order → events), guarded state machine, totals + simulated tax, cancel/
refund paths.
→ [ADR-013](docs/adr/013-order-lifecycle-and-checkout.md) · [PR-008](docs/git/prs/PR-008-orders.md)

## Phase 7 — Cart
Persistent cart with captured prices, currency guard, sliding expiry, one-active-cart guarantee.
→ [ADR-012](docs/adr/012-pricing-placement-and-cart-capture.md) · [PR-007](docs/git/prs/PR-007-cart.md)

## Phase 6 — Customers
Profile, addresses, wishlist (id = user id, IDOR-safe); provisioned on `CustomerRegistered`.
→ [PR-006](docs/git/prs/PR-006-customers.md)

## Phase 5 — Inventory
Available/reserved/sold quantities, movements, idempotent reservations, overselling prevention, low-stock events.
→ [ADR-011](docs/adr/011-inventory-reservations-idempotency.md) · [PR-005](docs/git/prs/PR-005-inventory.md)

## Phase 4 — Catalog
Products, variants/SKUs, categories, brands, and PostgreSQL full-text search.
→ [ADR-010](docs/adr/010-catalog-search.md) · [PR-004](docs/git/prs/PR-004-catalog.md)

## Phase 3 — Identity
Auth, roles, permissions, refresh tokens, password reset, security audit log.
→ [ADR-009](docs/adr/009-authentication-strategy.md) · [PR-003](docs/git/prs/PR-003-identity.md)

## Phase 2 — Database & infrastructure
EF Core + Npgsql, schema-per-module `ModuleDbContext`, Outbox table, docker-compose.
→ [ADR-002](docs/adr/002-postgresql.md), [ADR-005](docs/adr/005-outbox-pattern.md) · [PR-002](docs/git/prs/PR-002-database-infrastructure.md)

## Phase 1 — Backend bootstrap
Solution, BuildingBlocks, API host with health + OpenAPI.
→ [PR-001](docs/git/prs/PR-001-project-bootstrap.md)

## Phase 0 — Repository & architecture baseline
Gitflow, `.gitignore`, README, ADR baseline, docs tree.
→ [ADR-001](docs/adr/001-modular-monolith.md)
