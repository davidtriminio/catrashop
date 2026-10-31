# PR-012 — Notifications module

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/notifications` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Phase 12: in-app + email notifications with per-customer preferences and an idempotent, preference-aware dispatch
contract other modules (and the Phase 15 event consumers) can call (Section 12).

## Changes
- **Domain:** `Notification` (in-app inbox item, mark-read); `NotificationPreference` (per customer, owns muted
  `(type, channel)` pairs); `NotificationDispatch` (idempotency + per-channel audit); `NotificationCatalog`
  (transactional vs marketing split); `NotificationType` / `NotificationChannel` enums.
- **Application:** `NotificationDispatchService` implementing public `INotificationDispatcher`;
  `NotificationQueryService` (inbox), `NotificationPreferenceService`; `NotificationTemplateRenderer`; DTOs,
  validators, mappers.
- **Infrastructure:** `NotificationsDbContext` (notifications schema), repositories, design-time factory,
  `LoggingEmailSender` behind `IEmailSender`, **initial migration** (5 tables).
- **API:** me-scoped inbox (list / unread-count / read / read-all) and preferences (get / mute-unmute); admin
  `POST /notifications/admin/send` under `notifications.manage`.
- **Tests:** `Catrashop.Modules.Notifications.Tests` — **14 unit tests**.
- ADR-017 (notifications).

## Technical decisions
- Public `INotificationDispatcher.DispatchAsync`; the request carries the recipient's (optional) email so
  Notifications never reaches into Identity/Customers — dependency stays one-way/acyclic.
- **Idempotent on a dispatch key** (unique index) — a redelivered trigger never notifies twice; per-channel
  delivery outcome recorded.
- **Transactional notifications can't be muted** (receipts, shipment updates always deliver); only marketing types
  are mutable, per channel.
- Email behind an abstraction with a dev logging sender (swap for SMTP later, no caller change); a failed email
  does not fail the dispatch — the in-app row still lands.
- **Not wired to events yet** — that's the event-driven design's job in Phase 15; the contract is ready and the
  admin send exercises it now. Known limitations (dispatch race on same key; no email retry queue) documented in
  ADR-017.

## Testing / verification
- `dotnet build` → **0/0**.
- **`dotnet test` → 92 passed / 0 failed** (14 new Notifications: catalog split, unread→read idempotent, mute
  scoping, transactional-never-mutable-yet-always-allowed, mute dedup + unmute, template token fill / graceful
  missing token / caller-supplied marketing copy).
- Migration validates: `notifications` schema; `ux_notification_dispatches_key`, `ux_notification_mutes_topic`,
  `ix_notifications_recipient_read`.
- API boot: 10 modules `[...,"Notifications"]`; inbox, preferences, admin send → **401** unauthenticated.
- ⚠️ **Deferred (no Postgres):** a live dispatch creating an in-app row + logging an email; muting suppressing a
  marketing channel.

## Review cycle
**🟢 APPROVE (simulated)** — Clean sink module: idempotent dispatch keyed by a unique index, a sensible
transactional/marketing preference split that guarantees receipts, and email behind a swappable abstraction. It
deliberately stops at the contract rather than wiring events prematurely, matching the Phase 15 plan; the
concurrency and retry gaps are named, not hidden.

## Merge result
Merged into `develop` with `--no-ff`.
