# PR-028 — Flutter admin app for order management

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/mobile-admin` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Flutter app (Android + iOS) for operators: admin-only login, orders list/detail, process/ship/deliver/cancel (ADR-030).

## Changes
- Biometric (or device PIN) lock; tokens in Keystore/Keychain; automatic refresh on 401.
- New-order alerts without Firebase: 60 s polling while open plus a periodic background task.
- Deployment guide, ADR-029/030 and the mobile README.

## Verification
`flutter analyze` clean, unit tests for models/state helpers/JWT claims, debug APK builds.
