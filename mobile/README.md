# CommerceHub Admin (Flutter)

Android + iOS app for **order management only**: sign in as an admin, browse/filter orders, open one, and move it
through the fulfilment states (process → ship → deliver) or cancel it. See
[ADR-030](../docs/adr/030-mobile-admin-app.md).

- Auth: `/api/v1/identity/login` (accounts need `orders.read_all` + `orders.manage`, i.e. OrderManager or Administrator)
- Orders: `/api/v1/orders/admin` list/detail and `/{id}/process|ship|deliver|cancel`
- Biometric (or device PIN) unlock on start and after 30 s in the background
- New-order alerts: 60 s polling while open + 15 min background task (local notifications, no Firebase)

## Run
```bash
cd mobile
flutter pub get
flutter run                      # talks to https://api.davidtriminio.dev
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5227   # Android emulator -> local API
flutter test && flutter analyze
```
Plain-HTTP local APIs also need `android:usesCleartextTraffic="true"` temporarily in the Android manifest.

## Release builds
**Android** — create a keystore once and keep it out of git:
```bash
keytool -genkey -v -keystore commercehub-release.jks -keyalg RSA -keysize 2048 -validity 10000 -alias commercehub
```
Create `android/key.properties` (git-ignored):
```
storeFile=C:/path/to/commercehub-release.jks
storePassword=...
keyAlias=commercehub
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
