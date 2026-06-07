# ADR-014: Payments — provider abstraction, idempotency & webhooks

- **Status:** Accepted
- **Date:** 2026-06-06

## Context
Payments touch money, so the design must (a) never couple the domain to a specific gateway, (b) never double-charge
on a retry, and (c) accept asynchronous confirmations from a provider safely — including forged or redelivered
webhooks (Sections 7, 9, 50). No real financial system is integrated (Section 7): a simulated provider stands in.

## Decision

### Provider abstraction
`IPaymentProvider` (`AuthorizeAndCapture`, `Refund`) hides the gateway; the domain and application never reference
one. `FakePaymentProvider` is a deterministic stand-in (captures any positive amount, declines non-positive,
always refunds) returning a synthetic reference so the whole flow behaves as it would with a real gateway. A real
provider is a new implementation of the interface — nothing else changes.

### Idempotency
Each payment carries an **idempotency key** (client-supplied `Idempotency-Key` header, defaulting to `order:<id>`)
with a **unique index**. A repeated "pay this order" request returns the existing payment instead of charging
again. Order reconciliation is idempotent too: `IOrderWorkflow.ConfirmPayment` no-ops on an already-paid order.

### State machine
A payment moves only through `PaymentStateMachine`-validated states (Pending → Authorized → Captured → Refunded;
→ Failed), with same-status transitions treated as idempotent no-ops.

### Webhooks (Section 50)
Inbound webhooks are authenticated by an **HMAC-SHA256 signature** over the raw body (constant-time compare;
**fails closed** if no secret is configured), **deduplicated** by the provider's event id in a
`processed_webhook_events` table so redeliveries are safe, and then reconcile the payment and order. This is the
asynchronous confirmation path a real gateway uses; it is independent of and idempotent with the synchronous
capture, so the two can't corrupt each other.

## Alternatives considered
- **No idempotency key (rely on "one payment per order").** Rejected: retries during a failed attempt, or a
  captured-then-webhook race, need a stronger guarantee than a single order flag.
- **Trust webhook bodies without signatures.** Rejected outright — anyone could mark orders paid.
- **Only synchronous capture (no webhooks).** Works for the fake provider but wouldn't model real async gateways;
  the webhook path is built now so the seam is real.

## Consequences
- The gateway is swappable; charging is retry-safe; forged/duplicate webhooks are rejected/ignored.
- Cross-module: Payments depends only on the Orders `IOrderWorkflow` contract; it reflects outcomes onto orders
  (which in turn commit/keep inventory), keeping the money flow in one place.
- Refund flow drives the order Paid → RefundRequested → Refunded and emits `RefundCompleted` (n8n workflow 5).

## Verification
- **10 unit tests pass** (payment state machine incl. rejected/idempotent transitions; fake provider capture/
  decline; HMAC verifier accepts a valid signature, rejects a tampered body, and fails closed with no secret).
- Runtime: unauthenticated pay → 401 and a **bad-signature webhook → 401** verified against the running API.
- Deferred (no Postgres): the full capture → order Paid → inventory commit round-trip.
