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
| [PR-025](PR-025-repo-hygiene-and-dev-secrets-configurati.md) | Repo hygiene and dev-secrets configuration | `feature/dev-config` | `develop` |
| [PR-026](PR-026-in-process-event-transport-and-forwarded.md) | In-process event transport and forwarded headers | `feature/in-process-transport` | `develop` |
| [PR-027](PR-027-oracle-vm-release-systemd-apache-cloudfl.md) | Oracle VM release (systemd + Apache + Cloudflare) and Vercel config | `feature/oracle-deploy` | `develop` |
| [PR-028](PR-028-flutter-admin-app-for-order-management.md) | Flutter admin app for order management | `feature/mobile-admin` | `develop` |
| [PR-029](PR-029-local-setup-and-seed-scripts-guid-key-pe.md) | Local setup and seed scripts, Guid key persistence fix | `feature/local-dev` | `develop` |
| [PR-030](PR-030-profile-race-and-dev-fixes.md) | Profile provisioning race and local-dev fixes | `feature/fix-profile-race` | `develop` |
| [PR-031](PR-031-design-foundation.md) | Design foundation (tokens and primitives) | `feature/design-foundation` | `develop` |
| [PR-032](PR-032-catalog-price-filters.md) | Catalog prices, price filter and price sorting | `feature/catalog-filters` | `develop` |
| [PR-033](PR-033-storefront-redesign.md) | Storefront redesign: header, catalog, product page and cart | `feature/storefront-catalog` | `develop` |
| [PR-034](PR-034-mobile-redesign.md) | Mobile app redesign | `feature/mobile-redesign` | `develop` |
| [PR-035](PR-035-orders-and-shared-ui.md) | Storefront orders and shared UI components | `feature/storefront-account` | `develop` |
| [PR-036](PR-036-admin-console-redesign.md) | Admin console redesign | `feature/admin-redesign` | `develop` |
| [PR-037](PR-037-session-stability.md) | Session stability: refresh race and transient failures | `feature/session-stability` | `develop` |
| [PR-038](PR-038-validation-limits.md) | Minimum/maximum validations audit | `feature/validation-limits` | `develop` |
| [PR-039](PR-039-product-detail-layout.md) | Product detail page layout | `feature/product-detail-layout` | `develop` |
| [PR-040](PR-040-currency-hnl.md) | Store currency HNL and es-HN money formats | `feature/currency-hnl` | `develop` |
| [PR-041](PR-041-admin-permissions.md) | Administrator gets promotions and notifications permissions | `fix/admin-permissions` | `develop` |
| [PR-042](PR-042-i18n-web-spanish.md) | i18n phase 1: Transloco base and the storefront in Spanish | `feature/i18n-web-es` | `develop` |
| [PR-043](PR-043-national-shipping.md) | National shipping: zones by department, pickup stores and shipping cost | `feature/national-shipping` | `develop` |
| [PR-044](PR-044-mobile-api-url.md) | Mobile: change and remember the API address from Settings | `feature/mobile-api-url` | `develop` |
| [PR-045](PR-045-mobile-load-retry.md) | Mobile: failed loads explain themselves and Reintentar visibly retries | `fix/mobile-load-retry` | `develop` |
| [PR-046](PR-046-app-icons.md) | New app icons: web favicons and the mobile launcher icon | `feature/app-icons` | `develop` |
| [PR-047](PR-047-ecommerce-icon.md) | App icon: a shopping bag in orbit instead of the H | `feature/ecommerce-icon` | `develop` |
| [PR-048](PR-048-cart-planet-icon.md) | App icon: a shopping cart with a planet | `feature/cart-planet-icon` | `develop` |
