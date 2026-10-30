# Catrashop Admin (Flutter)

Android + iOS app for **order management only**: sign in as an admin, browse/filter orders, open one, and move it
through the fulfilment states (process → ship → deliver) or cancel it. See
[ADR-030](../docs/adr/030-mobile-admin-app.md).

- Auth: `/api/v1/identity/login` (accounts need `orders.read_all` + `orders.manage`, i.e. OrderManager or Administrator)
- Orders: `/api/v1/orders/admin` list/detail and `/{id}/process|ship|deliver|cancel`
- Three tabs: **Pedidos** (filter chips, order cards, detail with timeline and a sticky action bar), **Resumen** (count per
  status; "Por atender" = paid orders waiting) and **Ajustes** (account, lock, notifications, sign out)
- Biometric (or device PIN) unlock on start and after 30 s in the background
- New-order alerts: 60 s polling while open + 15 min background task (local notifications, no Firebase)
- Same design language as the web apps (near-black + lime accent, `lib/core/theme.dart`), light and dark

## Run
```bash
cd mobile
flutter pub get
flutter run                      # talks to https://api.davidtriminio.dev
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5227   # Android emulator -> local API
flutter test && flutter analyze
```
Debug builds allow plain HTTP (for a local API); release builds are HTTPS-only.

### Changing the API address without rebuilding
**Ajustes → Servidor → Cambiar** edits the address this device talks to (an absolute `http(s)://` URL; **Probar**
checks `/health/live`). It is saved in the Keychain/Keystore, so it survives closing the app, and **Restablecer** goes
back to the address the app was built with (`--dart-define=API_BASE_URL`, default `https://api.davidtriminio.dev`).
Changing it signs the admin out, because the session belongs to the previous server. The background order check uses
the saved address too.

Emulator without a screen lock? Debug builds can skip the biometric gate (compiled out of release builds):
`flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5227 --dart-define=SKIP_BIOMETRIC=true`

## Release builds
**Android** — create a keystore once and keep it out of git:
```bash
keytool -genkey -v -keystore catrashop-release.jks -keyalg RSA -keysize 2048 -validity 10000 -alias catrashop
```
Create `android/key.properties` (git-ignored):
```
storeFile=C:/path/to/catrashop-release.jks
storePassword=...
keyAlias=catrashop
keyPassword=...
```
Then `flutter build apk --release` (sideload) or `flutter build appbundle --release` (Play Store). Without
`key.properties` the release build falls back to debug signing (local testing only).

**iOS** — needs macOS + Xcode + an Apple developer account: `flutter build ipa`. The Face ID text and background
modes are already in `ios/Runner/Info.plist`. Background refresh on iOS is best-effort (the OS decides when).

## Layout
```
lib/core/             config, secure token store, Dio client (auto refresh), providers, notifications/background
lib/features/auth/    login + biometric lock screen
lib/features/orders/  models, repository (+ NewOrderWatcher), list and detail pages
```
