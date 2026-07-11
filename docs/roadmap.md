# CommerceHub — Build Roadmap (24 phases)

Built incrementally. Each phase must leave the system compiling; infra-dependent runtime checks require Docker.
Legend: ✅ done · 🚧 in progress · ⬜ pending · 🔷 partially scaffolded

| # | Phase | Status | Notes |
|---|-------|--------|-------|
| 0 | Repo + architecture + Git + docs baseline | ✅ | gitflow, .gitignore, README, ADRs, docs tree |
| 1 | Backend bootstrap (solution, BuildingBlocks, Api) | ✅ | Builds clean; API health/OpenAPI verified running |
| 2 | Database + infrastructure (EF, Postgres, migrations) | ✅ | EF10+Npgsql infra, Outbox, ModuleDbContext, compose (first migration lands with Identity) |
| 3 | Identity (auth, roles, permissions, refresh tokens) | ✅ | Full module; builds 0/0; migration validates; API boots, 7 endpoints, /me→401. DB round-trips deferred to Docker |
| 4 | Catalog (products, categories, variants, SKUs) | ✅ | Full module + FTS search; builds 0/0; migration validates (7 tables, unique SKU/slug, FKs); API boots, 8 endpoints, admin→401. DB queries deferred to Docker |
| 5 | Inventory (available/reserved/sold, movements) | ✅ | Module + idempotent reservations, overselling prevention, low-stock events; **12 unit tests pass**; migration validates (4 tables); API boots (3 modules), admin→401. ADR-011 |
| 6 | Customers (profile, addresses, wishlist) | ✅ | Module (id=userId, IDOR-safe); default-address & wishlist rules; **8 unit tests pass**; migration validates (4 tables); API boots (4 modules), /me→401 |
| 7 | Cart (persistence, captured prices, expiry) | ✅ | Module + Catalog variant pricing/price-query; captured prices, currency guard, sliding expiry, one-active-cart index; **8 unit tests**; migrations validate; API boots (5 modules), /cart→401. ADR-012 |
| 8 | Orders (state machine, totals, history) | ✅ | Checkout orchestration (cart→reserve→order→events), guarded state machine, totals+sim.tax, cancel/refund paths, IOrderWorkflow for Payments; **13 unit tests**; migration validates (unique cart idempotency); API boots (6 modules), 10 endpoints. ADR-013 |
| 9 | Payments (provider abstraction, idempotency) | ✅ | FakePaymentProvider, payment state machine, idempotency key, HMAC+dedup webhooks, refunds→order; drives IOrderWorkflow; **10 unit tests**; migration validates; API boots (7 modules), unauth→401, bad-sig webhook→401. ADR-014 |
| 10 | Shipping (methods, shipments, tracking) | ✅ | Methods + shipments (one/order), tracking, shipment state machine; drives orders via IOrderFulfillment (Processing→Shipped→Delivered); **10 unit tests**; migration validates; API boots (8 modules), admin→401. ADR-015 |
| 11 | Promotions (coupons, rules, usage limits) | ✅ | Coupon aggregate (%/fixed, min subtotal, window, global + per-customer limits, cap); rules in `Coupon.CalculateDiscount`; `IPromotionApplication` (quote/redeem/release) wired into checkout (discount now real, reduces taxable base) + release on cancel/refund; **17 unit tests**; migration validates (unique code + per-order redemption); API boots (9 modules), admin→401, quote→401. ADR-016 |
| 12 | Notifications (email/in-app, preferences) | ✅ | In-app inbox + email (LoggingEmailSender behind IEmailSender), per-customer preferences (transactional always-on / marketing mutable per channel), idempotent `INotificationDispatcher` (unique dispatch key), code templates; me-scoped inbox/prefs + admin send under `notifications.manage`; **14 unit tests**; migration validates (5 tables); API boots (10 modules), all→401. ADR-017 |
| 13 | Angular customer experience | ✅ | Angular 22 workspace (`frontend/`), standalone + zoneless + signals; typed API layer (core/api) + JWT interceptor w/ rotating refresh + guard; lazy routes: auth, catalog (list/detail), cart, checkout (address+coupon+pay), orders (list/detail/tracking), account, notifications, public tracking; prod build 0 errors (292kB init), vitest 2/2, running-app smoke test (bootstrap/routing/guard OK). ADR-018. Live SPA↔API↔DB deferred to Docker |
| 14 | Angular administration | ⬜ | |
| 15 | Messaging (RabbitMQ, Outbox publisher/consumers) | ⬜ | |
| 16 | n8n automations | ⬜ | |
| 17 | AI Agent (bounded tools, RAG, guardrails) | ⬜ | |
| 18 | Testing (unit, integration, E2E, architecture) | ⬜ | |
| 19 | Observability (logging, tracing, metrics, health) | ⬜ | |
| 20 | Security hardening | ⬜ | |
| 21 | CI/CD (GitHub Actions, security pipeline) | ⬜ | |
| 22 | Performance review | ⬜ | |
| 23 | Documentation polish | ⬜ | |
| 24 | Final architectural review | ⬜ | |

## Environment constraints recorded at kickoff (2026-03-05)
- .NET SDK 10.0.400 ✅, Node 24.18 ✅, pnpm 11.24 ✅, Angular CLI 22.1.6 ✅
- Docker installed (28.4.0) but **Docker Desktop engine not running** → containers cannot start until the user
  launches Docker Desktop. Code and compose files are authored ready; runtime verification of
  Postgres/Redis/RabbitMQ/n8n is deferred until the engine is up.
- No local `psql`/`redis-cli` clients (not required; access is via containers/EF).
