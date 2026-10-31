# PR-034 — Mobile app redesign

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/mobile-redesign` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Bring the Flutter admin app up to the same design language as the web (PR-031) and make it faster to work with: see at
a glance what needs attention, filter and open orders, and change their state without losing context. No functional
change to authentication, the biometric lock or the alerts.

## Changes
- **Theme** (`core/theme.dart`): near-black primary, lime accent for "needs attention", pill buttons and chips,
  rounded cards, a 4 px spacing scale; light and dark follow the system.
- **Navigation:** bottom bar with three tabs (Pedidos, Resumen, Ajustes); the Pedidos icon carries a badge with the
  number of paid orders waiting. The new-order poller now lives in the shell so it runs on every tab.
- **Resumen** (new): a "Por atender" hero card (paid orders) and tappable counters per status, fed by count-only
  queries (`OrdersRepository.count`, page size 1); tapping one opens the Pedidos tab already filtered.
- **Pedidos:** horizontal filter chips, total count, cards with a status icon, relative time ("hace 12 min"), total and
  coloured status chip, skeleton placeholders, empty and error states.
- **Detalle:** summary card, shipping, items with totals, a vertical status timeline, and a sticky action bar. State
  changes and cancellation (with a reason) are confirmed in a bottom sheet that shows the status transition.
- **Ajustes** (new): account and roles, lock now, notification permission, API address, sign out with confirmation.
- Login and lock screens use the shared wordmark. Dates are formatted in Spanish (`initFormats`).
- Debug builds accept `--dart-define=SKIP_BIOMETRIC=true` so the app can run on an emulator with no screen lock; it is
  guarded by `kDebugMode` and compiled out of release builds.

## Verification
- `flutter analyze` clean; 17 unit/widget tests (state-machine helpers, JSON, JWT claims, relative time, list and filter
  behaviour, summary → filtered list, confirmation sheet, cancel-with-reason, no action bar on delivered orders).
- Rendered every screen (login, lock, orders light/dark, summary, settings, detail, confirm sheet) to images in the test
  environment and reviewed the layout; the debug APK builds.
- Not covered: running on a physical device or emulator against the API (the in-test icons use placeholder glyphs).
