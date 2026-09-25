# Architecture Decision Records

Each ADR captures one significant decision — its context, the decision, alternatives considered, and
consequences. They are immutable once **Accepted**: a later decision that changes course gets a new ADR that
supersedes the old one rather than editing history.

> **On the numbering.** ADRs are numbered by decision, not by phase, and the sequence has intentional gaps
> (003, 004, 006, 007, 008 were reserved during planning and folded into other records). The list below is the
> complete set that exists.

| ADR | Title | Status |
|---|---|---|
| [001](001-modular-monolith.md) | Modular monolith | Accepted |
| [002](002-postgresql.md) | PostgreSQL as the primary datastore | Accepted |
| [005](005-outbox-pattern.md) | Transactional Outbox for reliable events | Accepted |
| [009](009-authentication-strategy.md) | Authentication strategy (JWT + refresh, permissions) | Accepted |
| [010](010-catalog-search.md) | Catalog full-text search | Accepted |
| [011](011-inventory-reservations-idempotency.md) | Inventory reservations & idempotency | Accepted |
| [012](012-pricing-placement-and-cart-capture.md) | Pricing placement & cart price capture | Accepted |
| [013](013-order-lifecycle-and-checkout.md) | Order lifecycle & checkout orchestration | Accepted |
| [014](014-payments-provider-idempotency-webhooks.md) | Payments — provider abstraction, idempotency, webhooks | Accepted |
| [015](015-shipping-fulfillment-integration.md) | Shipping & fulfilment integration | Accepted |
| [016](016-promotions-discounts.md) | Promotions & discounts | Accepted |
| [017](017-notifications.md) | Notifications (in-app + email, preferences) | Accepted |
| [018](018-frontend-architecture.md) | Frontend architecture (Angular 22, zoneless, signals) | Accepted |
| [019](019-admin-app-shared-library.md) | Admin app + shared library | Accepted |
| [020](020-messaging-rabbitmq-outbox.md) | Messaging — RabbitMQ + Outbox publisher/consumers | Accepted |
| [021](021-n8n-automations.md) | n8n automations over the event exchange | Accepted |
| [022](022-ai-assistant.md) | AI assistant — bounded tools, RAG, guardrails | Accepted |
| [023](023-testing-strategy.md) | Testing strategy | Accepted |
| [024](024-observability.md) | Observability | Accepted |
| [025](025-security-hardening.md) | Security hardening | Accepted |
| [026](026-cicd-pipeline.md) | CI/CD pipeline | Accepted |
| [027](027-performance-review.md) | Performance review | Accepted |

For how these decisions fit together, see the [architecture overview](../architecture/README.md).
