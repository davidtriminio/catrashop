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
| 14 | Angular administration | ✅ | 2nd app `commercehub-admin` in the workspace; shared code extracted to `frontend/shared` (path alias `@ch/*`) — models, API services (customer+admin), auth, util — consumed by both apps; permission-gated routes (`permissionGuard`) + sidebar; screens: dashboard, catalog, inventory, orders fulfilment, promotions, shipping methods, notifications send; both apps build 0 errors, vitest 2+2, admin smoke test (bootstrap/guard/shared-lib OK). ADR-019 |
| 15 | Messaging (RabbitMQ, Outbox publisher/consumers) | ✅ | Outbox→RabbitMQ topic exchange (per-module `OutboxProcessor`, routing key=event full name); durable consumer queue + manual-ack dispatcher → composition-root handlers (modules stay decoupled); idempotent consumers (no inbox table); reactions: CustomerRegistered→provision+welcome, Order/Payment/Shipment→notifications, LowInventory/CouponRedeemed→logging; resilient hosted services (app boots w/o infra); **6 unit tests** (98 total); API boots w/ messaging, health 200, no crash. ADR-020 |
| 16 | n8n automations | ✅ | 4 event-driven workflows over the `commercehub.events` exchange (welcome email, order receipt, review request w/ 3-day wait, low-stock ops alert) — importable n8n JSON + RabbitMQ `definitions.json` (exchange+queues+bindings, routing keys = event full names); no backend change (exchange is the seam); no secrets (creds/URLs by name/env). JSON validated + queues/keys cross-checked. ADR-021. Live import/run deferred to Docker |
| 17 | AI Agent (bounded tools, RAG, guardrails) | ✅ | AI module + `POST /ai/chat`; bounded agent loop; `IChatModel` (deterministic `FakeChatModel`, real LLM swaps in); tools = allow-list over public read contracts (knowledge/RAG, product search, own-order status) — NO arbitrary SQL/shell/HTTP; ownership injected from JWT (not model); RAG via `IKnowledgeRetriever` (lexical over seeded KB); guardrails (system prompt, injection guard hard-refusal, tool output=data, client can't inject system/tool msgs, bounded iters); **11 unit tests** (109 total); boots (11 modules), /ai/chat→401. ADR-022 |
| 18 | Testing (unit, integration, E2E, architecture) | ✅ | **Architecture tests** (NetArchTest, 66 = 11 modules × 6 rules): module isolation (only `Contracts`/events cross boundaries), no dependency on the API host, inward layering (Domain free of outer layers + EF/Npgsql; Application free of the Npgsql driver), handlers only in the composition root. **Integration tests** (`WebApplicationFactory`, 8 + 2 gated): infra-less host (lazy EF, placeholder conn string) covering health, module banner (11), 404, and the auth pipeline order 401/403/400 before any DB call; DB-backed Identity E2E gated by `[PostgresFact]` (real Npgsql probe → skip w/o Postgres). **Frontend:** Vitest 2→**23** (both apps + shared: `problemMessage`, `AuthStore`, guards, admin route-drift, component render) + **Playwright** 3 e2e (headless Chromium vs live `ng serve`, infra-free guard/redirect/form). Backend **183 pass / 2 skip / 0 fail**. ADR-023. Full DB/browser journeys deferred to Docker |
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
