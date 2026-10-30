# ADR-021: n8n automations over the event exchange

- **Status:** Accepted
- **Date:** 2026-07-28

## Context
Section 16 calls for n8n workflow automations (welcome email, review request, ops alerts, etc.). Phase 15 already
publishes every integration event to a durable RabbitMQ topic exchange (`catrashop.events`, ADR-020). The
question is how n8n should integrate without coupling it to the backend's internals.

## Decision
- **n8n subscribes to the event exchange — the exchange is the integration seam.** Each workflow owns a durable
  queue bound to the exchange with the routing key of the event it reacts to (routing key = the event's CLR full
  name, exactly as the backend publishes). A topic exchange fans a copy to every bound queue, so n8n consumes
  **independently of the in-process consumer** and **no backend code changes** are needed to add or change an
  automation.
- **Topology is declared, importable data.** [`automations/rabbitmq/definitions.json`](../../automations/rabbitmq/definitions.json)
  declares the exchange (same shape the backend declares at runtime, so it merges) and the four `n8n.*` queues +
  bindings; import it via the management UI or `rabbitmqadmin`.
- **Workflows are versioned exports**, one JSON per automation under `automations/n8n/`, each shaped as
  *RabbitMQ trigger → Code (normalize payload) → action* (email / delay+email / HTTP webhook). They are committed
  so the automation layer lives in source control alongside the code it reacts to.
- **No secrets in source.** Workflow JSON references credentials (RabbitMQ, SMTP) and endpoints (`OPS_WEBHOOK_URL`)
  by name/env only; the actual values are configured in n8n. Credential IDs in the exports are clearly-labeled
  placeholders to re-select on import.
- **Automations, not core logic.** These are side-effects (emails, alerts); the system is fully functional without
  them. Transactional, must-happen notifications remain the in-app Notifications module (ADR-017); n8n adds the
  outward, easily-changed touches marketing/ops iterate on.

## Workflows shipped
Welcome email (`CustomerRegistered`), order receipt (`OrderPaid`), review request (`ShipmentDelivered`, after a
3-day wait), low-stock ops alert (`LowInventoryDetected` → ops webhook).

## Alternatives considered
- **Backend calls n8n webhooks.** Rejected — it would couple the backend to n8n's URLs and duplicate the delivery
  concern the outbox/broker already solves. Consuming the exchange keeps the dependency one-way and additive.
- **n8n polling the API / database.** Rejected — wasteful and laggy versus event-driven triggers.

## Known limitations (documented, not hidden)
- **Address enrichment:** `OrderPaid` / `ShipmentDelivered` carry a `customerId`, not an email. The receipt and
  review workflows use a configurable fallback recipient; a production version adds an HTTP Request node to resolve
  the email (service token) or the backend enriches the event. Welcome (carries email) and low-stock (address-free)
  are complete as-is.
- **Idempotency:** delivery is at-least-once; each payload carries `EventId` for a dedupe step, not yet enforced
  in the workflows.
- **Live verification deferred to Docker:** importing/activating the workflows and observing end-to-end runs
  requires RabbitMQ + n8n running.

## Verification
- All five JSON files parse; the four workflow queues match the four declared queues/bindings; the four routing
  keys equal the backend event type full names registered in Phase 15 (`IntegrationSetup`). No secrets committed.
- Deferred (no Docker): import into n8n, wire credentials, publish a test event, observe execution.
