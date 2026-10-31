# PR-029 — Local setup and seed scripts, Guid key persistence fix

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/local-dev` → `develop`
- **Type:** `fix`
- **Status:** ✅ Merged

## Objective
Make the project easy to run locally and fix a persistence bug found by doing it.

## Changes
- EF no longer treats domain-assigned Guid keys as database-generated: adding children to a loaded aggregate (refresh tokens, history, cart items) failed with a concurrency exception.
- `scripts/local`: `setup-db.ps1` and `seed.ps1` (admin, catalog, stock, customer, orders in several states) and `docs/local-development.md`.
- Android debug builds allow plain HTTP; release builds stay HTTPS-only.

## Verification
Full flow exercised against PostgreSQL: migrations, login, seed, order actions. No schema change.
