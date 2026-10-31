# PR-050 — Rename the code, configuration and docs to Catrashop

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/rename-internal-catrashop` → `develop`
- **Type:** `refactor` (breaking for existing local setups)
- **Status:** ✅ Merged

## Problem
PR-049 changed only what people see ("Catrashop") and kept `CommerceHub` as the internal code name. The owner does not want a
half-finished rename: the project has not been deployed and has no real data, so the risk of renaming everything is small.

## Changes
Done with one deterministic script (paths with `git mv`, so history follows the files; then a byte-for-byte replace in text files, so
line endings are untouched; binaries are skipped). Roughly 410 tracked paths and 520 files with ~1,500 mentions were touched.

| Was | Now |
|---|---|
| `CommerceHub.slnx`, `CommerceHub.Api`, `CommerceHub.BuildingBlocks`, `CommerceHub.Modules.*`, tests | `Catrashop.*` (folders, projects, assemblies, namespaces) |
| `commercehub-web`, `commercehub-admin` (Angular projects, `dist/`, CI, Vercel config) | `catrashop-web`, `catrashop-admin` |
| `@ch/*` import alias, `ch-status-badge`, `ch-timeline`, `ch-language-switcher`, `ch_auth_tokens`, `ch_lang` | `@cs/*`, `cs-…`, `cs_auth_tokens`, `cs_lang` |
| Flutter package `commercehub_admin`; Android `applicationId`/namespace and Kotlin package; iOS bundle ids | `catrashop_admin`; `dev.davidtriminio.catrashop_admin`; `…catrashopAdmin` |
| `COMMERCEHUB_CONNECTION`, JWT issuer/audience, RabbitMQ exchange/queues/user, default DB/user names, Docker/Apache/systemd names | `CATRASHOP_*`, `catrashop`, `catrashop.events`, … |
| `*@commercehub.local` seed accounts | `admin@catrashop.local`, `cliente@catrashop.local` (same passwords) |
| n8n workflow and credential names, ADRs, docs, PR history, deployment files | all say Catrashop |

## Things to know — what breaks for you
- **Start the API with** `dotnet run --project backend/src/Api/Catrashop.Api`. The project kept its `UserSecretsId`, so your user-secrets
  (connection string, signing key) still apply.
- **Sign in again everywhere**: the token issuer/audience changed. Old tokens are rejected.
- **The mobile app is a new app**: the Android `applicationId` and iOS bundle id changed, so uninstall the old one.
- **Seed accounts** are now `@catrashop.local`. Accounts already in your database keep their old e-mail (`@commercehub.local`); the
  seed would create the new ones next to them.
- Environment variable `COMMERCEHUB_CONNECTION` is now `CATRASHOP_CONNECTION`; RabbitMQ names changed, so re-import
  `automations/rabbitmq/definitions.json` if you use it.
- Old compiled output (`backend/.art`, `frontend/dist/commercehub-*`) is ignored by git and left behind; it is harmless and can be deleted.
- Not renamed on purpose: the demo brand **"Galactic"** of the sample products, and your own Docker/database names
  (`galactic-ecommerce`). The repository folders are renamed by hand at the end (see below).
- Folder rename (by hand, with the IDE and Claude closed): `Rename-Item galactic-ecommerce catrashop` and
  `Rename-Item galactic-ecommerce-real catrashop-real` in `V:\dev`. Neither repo has a git remote and GitHub has no repo for this project.

## Verification
- Backend `dotnet test` (Release): every test project passes (architecture 68/68, 4 Postgres integration tests skipped as usual); all ten EF
  contexts report *no changes since the last migration*, i.e. the namespaces moved without altering the model or the migrations.
- `pnpm install --frozen-lockfile` works with the renamed workspace; web 46/46 and admin 6/6; `ng build` of both in production mode.
- `flutter analyze` clean, `flutter test` 40/40, `flutter build apk --debug` succeeds and the APK contains `catrashop_admin/MainActivity`
  and no trace of the old package.
- End to end on a temporary PostgreSQL database with a real API on port 5299 started from `Catrashop.Api`: `setup-db.ps1` applied the ten
  migrations, `seed.ps1` created the new accounts, zones and stores, and my 30 HTTP scenarios (login, catalogue, checkout by delivery and
  pickup, payments, shipping rules, permissions) all passed. The database was dropped afterwards.
- Searched every tracked path and file for the old name in any case: none left, except the PR-049 document, kept as history.
- **Not verified:** the Playwright e2e (needs browsers), iOS, a real phone, the Docker and Oracle deployment files (renamed consistently but
  never run), the n8n and RabbitMQ topology (renamed, not imported), and the CI workflows on a runner.
