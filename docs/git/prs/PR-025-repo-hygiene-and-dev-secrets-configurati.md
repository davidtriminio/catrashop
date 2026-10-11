# PR-025 — Repo hygiene and dev-secrets configuration

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/dev-config` → `develop`
- **Type:** `chore`
- **Status:** ✅ Merged

## Objective
Housekeeping before the first post-1.0 work: ignore release bundles, force LF for shell/service/conf files, and move all development secrets out of the repository.

## Changes
- `.gitignore` / `.gitattributes`: release bundles ignored, LF enforced for `*.sh`, `*.service`, `*.conf`.
- `appsettings.Development.json` no longer contains secrets; they live in .NET user-secrets (`docs/deployment.md`).
- `docker-compose.yml` requires explicit passwords; design-time DbContext factories read `COMMERCEHUB_CONNECTION`.

## Verification
Build is green; no behaviour change.
