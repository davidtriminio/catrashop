# PR-009 — Payments module

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/payments` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Phase 9: process payments against orders through a swappable provider, without double-charging, and accept
provider webhooks safely (Sections 7, 9, 50).

## Changes
- **Domain:** `Payment` aggregate (idempotency key, amount, provider ref, guarded `PaymentStateMachine`),
  `ProcessedWebhookEvent` (webhook dedupe).
- **Provider abstraction:** `IPaymentProvider` + deterministic `FakePaymentProvider`; `IWebhookSignatureVerifier`
  + HMAC-SHA256 `HmacWebhookSignatureVerifier` (constant-time, fails closed).
- **Application:** `PaymentService` (initiate/get/refund, idempotent), `WebhookService` (verify → dedupe →
  reconcile); DTOs, validator, `PaymentsOptions`.
- **Infrastructure:** `PaymentsDbContext` (payments schema) with unique idempotency-key index, repository, outbox
  publisher, design-time factory, **initial migration** (3 tables).
- **Orders contract (extended):** `IOrderWorkflow` gains `GetPayableAsync` + `RefundAsync`; implemented in
  `OrderWorkflowService`.
- **API:** customer pay/get, admin refund (`orders.manage`), and an anonymous **signature-verified** webhook
  endpoint.
- **Tests:** `CommerceHub.Modules.Payments.Tests` — **10 unit tests**.
- ADR-014 (provider abstraction / idempotency / webhooks).

## Technical decisions
- **Provider behind an interface** — a real gateway drops in with no domain/application change (ADR-014).
- **Idempotency key (unique index)** so retries never double-charge; order reconciliation is idempotent too.
- **Webhooks**: HMAC signature (fails closed), event-id dedupe, idempotent reconciliation — an independent async
  confirmation path alongside the synchronous capture.
- Payments depends only on Orders' `IOrderWorkflow`; the money flow reflects onto the order, which commits/keeps
  inventory.

## Testing / verification
- `dotnet build` → **0/0**.
- **`dotnet test` → 10 passed / 0 failed** (payment transitions incl. rejected/idempotent; fake provider
  capture/decline; HMAC verifier accepts valid, rejects tampered, fails closed).
- Migration validates: `payments`/`processed_webhook_events`/`outbox_messages`; unique `ux_payments_idempotency_key`.
- API boot: 7 modules `[...,"Payments"]`; 4 payment endpoints; unauthenticated pay → **401**; **bad-signature
  webhook → 401**.
- ⚠️ **Deferred (no Postgres):** full capture → order Paid → inventory commit round-trip.

## Review cycle
**🟢 APPROVE (simulated)** — Provider is cleanly abstracted; idempotency is enforced at the schema; the webhook
path verifies signatures (fails closed) and dedupes — the money-handling risks are addressed.

## Merge result
Merged into `develop` with `--no-ff`.
