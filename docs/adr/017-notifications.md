# ADR-017: Notifications (channels, preferences, idempotent dispatch)

- **Status:** Accepted
- **Date:** 2026-07-02

## Context
The platform needs to notify customers (Section 12): in-app notifications with an inbox, email, and per-customer
preferences. Notifications should eventually be driven by domain events (order placed/paid/shipped/delivered,
payment failed, password reset), but the messaging backbone (RabbitMQ + outbox consumers) is Phase 15. This phase
delivers the module itself — its channels, preferences, delivery, and idempotency — without prematurely coupling
it to the event flow.

## Decision
- **Public contract `INotificationDispatcher.DispatchAsync(NotificationRequest)`**, implemented by the
  Notifications module and consumed by others (and, from Phase 15, by the messaging consumers). Notifications
  depends on nothing but BuildingBlocks + Infrastructure and receives only opaque identifiers, so the dependency
  graph stays one-way and acyclic.
- **The request carries the recipient's email** (optional) rather than Notifications reaching into
  Identity/Customers. Email is attempted only when an address is present and the channel is allowed; otherwise
  only the in-app channel is used. This keeps the module decoupled — the event consumers that fire later already
  have (or resolve) the address.
- **Idempotent dispatch.** Every request carries an `IdempotencyKey`; a `notification_dispatches` row with a
  unique index on it is the guard, so a redelivered trigger (an at-least-once event, a retried call) never
  notifies twice. The dispatch row also records which channels actually delivered.
- **Channel preferences with a transactional/marketing split.** A `NotificationPreference` per customer holds the
  set of muted `(type, channel)` pairs. Only **marketing** types (promotions, back-in-stock) are mutable;
  **transactional** types (order, payment, account) are always delivered and cannot be silenced — a customer must
  not be able to mute a receipt or a shipment update. The catalog of which types are marketing lives in one place
  (`NotificationCatalog`).
- **Email behind an abstraction.** `IEmailSender` with a dev `LoggingEmailSender` (logs instead of sending),
  mirroring the payments-provider approach — a real SMTP/transactional-email provider is a later registration
  swap with no caller changes. **A failed email does not fail the dispatch**: the in-app notification still lands
  and the per-channel outcome is recorded.
- **Templates in code.** A `NotificationTemplateRenderer` maps each type + its data tokens to a title/body via a
  switch, degrading gracefully when a token is missing. A full template engine would be over-engineering now.
- **Inbox** endpoints are strictly me-scoped (list, unread count, mark-one-read, mark-all-read); preference
  endpoints are me-scoped; a `notifications.manage` admin endpoint sends a notification manually (also the way
  this phase exercises dispatch without the event flow).

## Alternatives considered
- **Wire dispatch synchronously into Orders/Identity now** (e.g. call the dispatcher from checkout). Rejected —
  that is the event-driven design's job (Phase 15); doing it synchronously now would create coupling that would
  be torn out. The contract is in place; consumers arrive with messaging.
- **Resolve the recipient's email inside Notifications via a Customers/Identity contract.** Rejected for now to
  keep the module a decoupled sink; the caller supplies the address.
- **Store preferences as per-type/-channel opt-in rows.** Rejected in favor of storing only mutes over a
  sensible "allowed by default" baseline — less data, simpler defaults.

## Consequences
- Notifications is independently useful and testable today (inbox, preferences, manual send) and ready for
  event consumers to call `DispatchAsync` in Phase 15.
- Delivery is idempotent and preference-aware; transactional messages are guaranteed.

## Known limitations (documented, not hidden)
- **Dispatch race:** two concurrent dispatches with the *same* key can both pass the existence check before either
  commits; the unique index still prevents a duplicate row (the second commit fails). True single-flight would
  need a caught unique-violation or upsert — deferred, consistent with the webhook-dedupe approach (ADR-014).
- **No retin/retry queue for email** yet — a failed send is logged and recorded, not retried; retry belongs with
  the Phase 15 messaging/outbox worker.

## Verification
- **14 unit tests pass** (catalog mutable split; notification unread → read incl. idempotent timestamp; mute
  blocks only its channel; transactional cannot be muted yet is always allowed; mute dedup + unmute; template
  token fill, graceful missing-token, caller-supplied marketing copy). Solution builds 0/0; full suite **92 pass**.
- Migration validates: `notifications` schema with `notifications` / `notification_preferences` /
  `notification_mutes` / `notification_dispatches` / `outbox_messages`; `ux_notification_dispatches_key`,
  `ux_notification_mutes_topic`, `ix_notifications_recipient_read`.
- API boots with 10 modules `[...,"Notifications"]`; inbox, preferences, and admin send → 401 unauthenticated.
- Deferred (no Postgres): a live dispatch creating an in-app row + logging an email, and preference-muting
  suppressing a marketing channel.
