# PR-030 — Profile provisioning race and local-dev fixes

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/fix-profile-race` → `develop`
- **Type:** `fix`
- **Status:** ✅ Merged

## Objective
Opening the account page failed with "An error occurred while processing your request" for a user whose customer
profile did not exist yet. Fix the root cause and two developer-experience problems found while diagnosing it.

## What was wrong
- **Race in `EnsureCustomerAsync`.** The account page fires `/customers/me` and `/customers/me/wishlist` at the same
  time. Both check "does the profile exist?", both see no, both insert, and the loser hits
  `23505 duplicate key … pk_customers` (a 500). The `CustomerRegistered` handler can lose the same race.
- **`setup-db.ps1` rebuilt in Debug**, which fails while a Debug API is running (locked output files).
- **`pnpm start` failed on pnpm 11** (`ERR_PNPM_IGNORED_BUILDS`): the four packages with install scripts
  (`esbuild`, `lmdb`, `@parcel/watcher`, `msgpackr-extract`) were never approved.

## Changes
- `ICustomerRepository.TryAddAsync` replaces `Add`: it inserts and returns `false` on a unique violation, detaching
  its copy so later queries in the same scope see the committed row. `EnsureCustomerAsync` uses it, so provisioning
  is idempotent under concurrency. The Application layer stays free of EF/Npgsql types.
- `setup-db.ps1` builds once in Release and runs `dotnet ef --no-build --configuration Release`.
- `frontend/pnpm-workspace.yaml` approves the four build scripts.

## Verification
- Three unit tests (existing profile, lost race, new profile); the Customers suite is 11/11.
- Against a real PostgreSQL: registered a user and fired 8 simultaneous profile/wishlist requests → all `200`, exactly
  one row in `customers.customers`.
- `pnpm install --frozen-lockfile` exits 0 and both Angular apps build.
