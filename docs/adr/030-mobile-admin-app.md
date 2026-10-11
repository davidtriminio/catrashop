# ADR-030: Flutter admin app for order management

- **Status:** Accepted
- **Date:** 2026-10-07

## Context
Operators want to watch and advance orders from a phone. The app must stay narrow (orders only), target Android
and iOS, require a biometric/device unlock, and not depend on Firebase (a project cannot be created).

## Decision
- A Flutter app in `mobile/` (Riverpod, Dio, secure storage, `local_auth`) that reuses the **existing**
  permission-gated endpoints: `/api/v1/identity/login|refresh|logout` and `/api/v1/orders/admin/**`
  (list, detail, process, ship, deliver, cancel). No backend change was needed.
- **Admin-only:** after login the JWT must carry `orders.read_all` and `orders.manage`; otherwise the session is
  revoked immediately and never stored.
- **Biometric lock** on cold start and after 30 s in the background (device PIN accepted as fallback; devices with
  no screen lock are refused). Tokens live in Keystore/Keychain.
- **New-order alerts without push:** a foreground poll every 60 s plus a periodic background task
  (WorkManager / BGTaskScheduler, 15 min minimum) compares the newest order timestamp with a stored cursor and
  raises a local notification.

## Consequences
- ✅ No third-party push service, no new server component, no device-token storage.
- ⚠️ Alerts are not instant: up to ~15 min in the background on Android, best-effort on iOS. If real time is
  needed later, add a small device registry + FCM/APNs (needs a Firebase project / Apple account) behind the same
  `NewOrderWatcher` seam.
- ⚠️ Building and signing for iOS requires macOS and an Apple developer account.
