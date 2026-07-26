# CommerceHub automations (n8n)

These are **event-driven** automations. They subscribe to the same RabbitMQ topic exchange the backend publishes
to (`commercehub.events`, ADR-020), so **no backend change is needed** — the exchange is the integration seam.
Each workflow has its own durable queue bound to the integration-event routing key it reacts to; a topic exchange
fans a copy to every bound queue, so n8n receives events independently of the in-process consumer.

> Nothing here contains secrets. Credentials (RabbitMQ, SMTP) and endpoint URLs are configured in n8n / via
> environment variables — the workflow JSON only references them by name.

## Workflows

| File | Trigger event (routing key) | Queue | Action |
|------|-----------------------------|-------|--------|
| `n8n/01-welcome-email.json` | `…Identity.Contracts.CustomerRegisteredIntegrationEvent` | `n8n.welcome` | Send a welcome email (event carries the address). |
| `n8n/02-order-receipt.json` | `…Orders.Contracts.OrderPaidIntegrationEvent` | `n8n.receipts` | Email an order receipt. |
| `n8n/03-shipment-review-request.json` | `…Shipping.Contracts.ShipmentDeliveredIntegrationEvent` | `n8n.reviews` | Wait 3 days, then request a review. |
| `n8n/04-low-stock-alert.json` | `…Inventory.Contracts.LowInventoryDetectedIntegrationEvent` | `n8n.ops` | POST a low-stock alert to an ops webhook (`OPS_WEBHOOK_URL`). |

## Setup

1. **Start the stack** (`docker compose up -d`) — brings up RabbitMQ (`:5672` / mgmt `:15672`) and n8n (`:5678`).
2. **Create the topology.** In the RabbitMQ management UI → *Overview → Import definitions*, import
   [`rabbitmq/definitions.json`](rabbitmq/definitions.json). It declares the `commercehub.events` exchange (same
   shape the backend declares, so it merges) and the four `n8n.*` queues with their bindings. *(Or create the
   queues + bindings by hand / via `rabbitmqadmin`.)*
3. **Add n8n credentials** (n8n → Credentials):
   - **RabbitMQ** named `CommerceHub RabbitMQ` → host `rabbitmq`, port `5672`, user/pass from `.env`.
   - **SMTP** named `CommerceHub SMTP` → your mail provider (e.g. Mailhog/Mailpit for local testing).
4. **Import the workflows** (n8n → *Import from File*), one per file. After import, open each workflow and
   re-select its credentials (the exported IDs are placeholders), then **Activate**.
5. For the ops alert, set `OPS_WEBHOOK_URL` in n8n's environment (e.g. a Slack incoming-webhook URL).

## Notes & limitations

- **Message shape:** event payloads are JSON with PascalCase fields (as the backend serializes them). Each
  workflow's *Code* node parses `content` (string) or the object directly, so it is tolerant of the RabbitMQ
  node's "content is JSON" setting.
- **Idempotency:** RabbitMQ delivery is at-least-once. Each payload includes `EventId`; add a dedupe step (or
  enable n8n execution de-duplication) if exactly-once side effects matter.
- **Address lookup:** `OrderPaid` and `ShipmentDelivered` carry a `customerId`, not an email. The receipt and
  review workflows use a configurable fallback recipient; a production version would add an HTTP Request node to
  resolve the customer's email from the API (with a service token), or the backend would enrich those events.
  The welcome and low-stock workflows are complete as-is (welcome carries the email; low-stock is address-free).
