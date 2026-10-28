# PR-037 — Session stability (refresh-token race and transient failures)

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/session-stability` → `develop`
- **Type:** `fix`
- **Status:** ✅ Merged

## Objective
Stop administrators being signed out when they change a product status or refresh the Orders page, on the web
console and in the mobile app.

## Diagnosis
Refresh tokens are single-use and rotate; replaying a rotated token is treated as theft and revokes the whole chain.

| # | Cause | Where |
|---|---|---|
| 1 | Parallel requests with an expired access token each called `refresh` with the same token; the first rotated it, the rest looked like a replay and revoked every session. | Angular `authInterceptor` |
| 2 | Any refresh error (network, 429, 5xx) or profile-load error cleared the session. | Angular `authInterceptor`, `AuthStore` |
| 3 | `_doRefresh` returned `false` for any `DioException`, and the interceptor then wiped the tokens. | Flutter `ApiClient` |
| 4 | The WorkManager isolate built its own `ApiClient` and could refresh at the same time as the foreground app. | Flutter `backgroundDispatcher` |

The web token was already persisted in `localStorage`, so a reload was not the cause; no cookie change was made.

## Changes
- **Backend:** `Identity:Security:RefreshReuseGraceSeconds` (default 10). A token rotated within that window and
  presented again gets a sibling refresh token instead of revoking the chain. Replays outside the window, after
  logout, or of expired tokens are still rejected and still revoke everything.
- **Web:** one shared in-flight refresh for parallel 401s; the session is cleared only when the server rejects the
  credentials (400/401/403), never for network errors, 429 or 5xx; same rule when restoring the profile at start-up.
- **Mobile:** `RefreshResult { ok, rejected, transient }`: only `rejected` signs out; a transient failure surfaces the
  original error and keeps the session. A request that raced another client's refresh retries with the stored token.
  The background isolate uses `canRefresh: false` and never spends the refresh token.

## Tests (rule → test)
| Rule | Test |
|---|---|
| Parallel 401s refresh once and all retry | `auth-interceptor.spec.ts` |
| Offline / 429 / 503 on refresh keeps the session | `auth-interceptor.spec.ts`, `api_client_session_test.dart` |
| Rejected refresh token signs out | both |
| Reload restores the session; logout clears it | `auth-interceptor.spec.ts` |
| Background client never refreshes; raced refresh is reused | `api_client_session_test.dart` |
| Concurrent refresh does not revoke the chain; logout revokes | `EndToEndJourneyTests` (PostgreSQL-gated) |

## Verification
- Web Vitest 27/27 (the two behaviour tests fail against the previous interceptor); Flutter 23/23 and
  `flutter analyze` clean; backend builds and architecture tests 66/66.
- Not verified: the PostgreSQL-gated integration tests were skipped (no database available to them), so the grace
  window has not been exercised against a real API; no real-device or browser check of the original symptom.
