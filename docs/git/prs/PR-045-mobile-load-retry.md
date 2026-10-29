# PR-045 — Mobile: a failed load explains itself and "Reintentar" visibly retries

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `fix/mobile-load-retry` → `develop`
- **Type:** `fix`
- **Status:** ✅ Merged

## Problem
After PR-043 the mobile order list failed with "No se pudieron cargar los pedidos / An error occurred while processing
your request." (the API answered **500** because the local database had not received PR-043's migrations). Two things in
the app made that hard to diagnose:

1. Pressing **Reintentar** looked inert. Riverpod keeps the *previous error* on screen while a retry is under way, and
   the server failed again within milliseconds, so the very same message came back with no animation and nothing to show
   that a new request had been made.
2. The message was only the server's generic title: nothing said it was a server error, or **which server** the phone
   was talking to (important now that the address can be changed, PR-044).

## Changes
- **Loading is shown while retrying** (orders list, summary, order detail): skeleton/spinner instead of the stale
  error. The loading state is held for ~0.6 s after a retry press, even if the server answers or fails at once.
- **Every failure now shows** the server's message, `Servidor: <address in use>`, for a 5xx a line saying it is a server
  error (check its database migrations), and — after a retry — `Intento N · HH:mm:ss`, so each press produces a visibly
  different message. The counter resets when a load succeeds.
- `LoadError` (shared widget) replaces three copies of the same error block; `StateMessage` scrolls instead of
  overflowing when the text is long, and the error areas are taller.
- **Riverpod's automatic retry is turned off** (`ProviderScope(retry: …)`): by default Riverpod 3 retries a failed
  provider up to 10 times with growing delays, keeping the screen in "loading" for a long time before any error appears.
  The app has an explicit Reintentar button, so errors surface immediately.

## Things to know
- This fixes how the app *reports* the failure; it does not and cannot fix the 500 itself. That one comes from the API's
  database lacking the PR-043 migrations (`orders.orders.shipping_total` and the `shipping_zones` tables).
- Same applies to the web store: a 500 from the API is shown with the server's generic message.

## Verification
- `flutter analyze`: no issues. `flutter test`: 40/40 (3 new): a failed load shows the message, the 5xx hint and the
  server address; Reintentar shows the loading skeleton, then the error numbered `Intento 1`, then `Intento 2` (and the
  previous number disappears); when the server recovers the retry shows the orders and clears the attempts.
- The test caught two real problems before shipping: the screen kept the old error during a retry (the reported
  symptom), and the longer message overflowed the fixed-height error area.
- **Not verified:** on a device/emulator; with the real API returning 500 from the phone.
