# PR-044 — Mobile: change and remember the API address from Settings

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/mobile-api-url` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Problem
The mobile app's API address was fixed at build time (`--dart-define=API_BASE_URL`) and Settings only displayed it.
Pointing a device at another server (the emulator's `10.0.2.2`, a PC on the LAN, the real API) meant rebuilding. While
testing PR-043 this also made it hard to tell *which* server a device was talking to when "orders could not be loaded".

## Changes
- **Settings → Servidor → Cambiar** opens a dialog to edit the address: it must be an absolute `http(s)://` URL with a
  host (no credentials/query); trailing slashes are removed. **Probar** calls `/health/live` and says whether the server
  answers (it saves nothing). **Guardar** saves it; **Restablecer** (shown only when a custom address is in use) goes
  back to the address the app was built with. Settings shows whether the address is the built-in one or "changed on this
  device".
- The address is saved in the Keychain/Keystore (`ApiUrlStore`, same package as the session) and read once before the
  app starts, so it survives closing the app. A corrupt value or unavailable storage falls back to the built-in address.
- Every request goes to the current address (`apiUrlProvider` feeds `apiClientProvider`); the **background order check**
  (WorkManager isolate) resolves the saved address too.
- Changing the address **signs the admin out**: the old server is asked to revoke the refresh token (best effort), the
  tokens and the "last seen order" cursor are cleared, background polling stops and cached orders/summary are dropped.
- `mobile/README.md` documents it.

## Things to know
- Release builds are HTTPS-only (Android blocks cleartext), so a plain `http://` address only works in debug builds; the
  dialog says so.
- The change is per device and is not shared with the web apps.
- Numbering: this took **PR-044**, so the rest of the roadmap moves one up (payment methods backend is now PR-045, and
  so on).

## Verification
- `flutter analyze`: no issues. `flutter test`: 37/37 (11 new): URL normalization (valid, trailing slashes, rejected
  inputs), resolving the saved/built-in address (corrupt value, storage failure), changing the address (saves it, signs
  out and clears the tokens), an invalid address changing nothing, resetting, and the Settings dialog (change, invalid
  input, "Probar" success/failure without saving, reset).
- **Not verified:** on a device or emulator (no Android build was made), the real Keychain/Keystore persistence across
  an app restart, the background isolate using the saved address, and "Probar" against a real server; only unit and
  widget tests with an in-memory store and a stubbed reachability check.
