# PR-026 — In-process event transport and forwarded headers

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/in-process-transport` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Run the API on a 1 GB host without RabbitMQ and behind a reverse proxy.

## Changes
- `Messaging:Transport=InProcess` selects `InProcessEventBus`: the outbox publisher dispatches straight to the handlers (ADR-029).
- RabbitMQ consumer and readiness check are skipped in that mode.
- Forwarded headers (`X-Forwarded-For/Proto`) are honoured so rate limiting sees the real client IP.

## Verification
Unit tests cover transport selection, direct dispatch and failure propagation (the outbox retries).
