# Simulated Pull Request history

> **These PRs are local workflow simulations.** No human reviewer approved this code. They exist to demonstrate a
> professional branch → commit → review → merge workflow, including a few deliberate `REQUEST CHANGES` cycles
> followed by corrective commits. Every review comment below was authored as part of the simulation.

Index:

| PR | Title | Branch | Merged to |
|----|-------|--------|-----------|
| [PR-001](PR-001-project-bootstrap.md) | Project bootstrap & architecture baseline | `feature/project-bootstrap` | `develop` |
| [PR-002](PR-002-database-infrastructure.md) | Database & infrastructure foundation | `feature/database-infrastructure` | `develop` |
| [PR-003](PR-003-identity.md) | Identity module (auth, roles, permissions, sessions) | `feature/identity` | `develop` |
| [PR-004](PR-004-catalog.md) | Catalog module (products, categories, brands, FTS) | `feature/catalog` | `develop` |
| [PR-005](PR-005-inventory.md) | Inventory module (stock, reservations, idempotency) | `feature/inventory` | `develop` |
| [PR-006](PR-006-customers.md) | Customers module (profile, addresses, wishlist) | `feature/customers` | `develop` |
| [PR-007](PR-007-cart.md) | Cart module + Catalog variant pricing | `feature/cart` | `develop` |
| [PR-008](PR-008-orders.md) | Orders module (checkout + state machine) | `feature/orders` | `develop` |
| [PR-009](PR-009-payments.md) | Payments module (provider, idempotency, webhooks) | `feature/payments` | `develop` |
| [PR-010](PR-010-shipping.md) | Shipping module (methods, shipments, tracking) | `feature/shipping` | `develop` |
| [PR-011](PR-011-promotions.md) | Promotions module (coupons, discounts, checkout wiring) | `feature/promotions` | `develop` |
| [PR-012](PR-012-notifications.md) | Notifications module (in-app + email, preferences, dispatch) | `feature/notifications` | `develop` |
| [PR-013](PR-013-frontend-customer.md) | Customer storefront (Angular 22) | `feature/frontend-customer` | `develop` |
| [PR-014](PR-014-frontend-admin.md) | Administration console + shared library | `feature/frontend-admin` | `develop` |
| [PR-015](PR-015-messaging.md) | Messaging (Outbox → RabbitMQ → handlers) | `feature/messaging` | `develop` |
| [PR-016](PR-016-n8n-automations.md) | n8n automations over the event exchange | `feature/n8n-automations` | `develop` |
| [PR-017](PR-017-ai-assistant.md) | AI shopping assistant (bounded tools, RAG, guardrails) | `feature/ai-assistant` | `develop` |
| [PR-018](PR-018-testing.md) | Testing (architecture, integration, E2E; frontend unit + Playwright) | `feature/testing` | `develop` |
| [PR-019](PR-019-observability.md) | Observability (OpenTelemetry, structured logs, correlation, health) | `feature/observability` | `develop` |
| [PR-020](PR-020-security-hardening.md) | Security hardening (headers, CORS, rate limiting, secret guard) | `feature/phase-20-security` | `develop` |
| [PR-021](PR-021-cicd-pipeline.md) | CI/CD (GitHub Actions build/test + security pipeline) | `feature/phase-21-cicd` | `develop` |
| [PR-022](PR-022-performance-review.md) | Performance review (DbContext pooling, no-tracking reads, compression, output cache) | `feature/phase-22-performance` | `develop` |
| [PR-023](PR-023-documentation-polish.md) | Documentation polish (README accuracy, architecture docs, ADR & docs indexes, changelog) | `feature/phase-23-docs` | `develop` |
| [PR-024](PR-024-final-architectural-review.md) | Final architectural review (evidence-based verdict; release-ready v1.0.0) | `feature/phase-24-final-review` | `develop` |
