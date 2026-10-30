# PR-021 — CI/CD (GitHub Actions build/test + security pipeline)

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/phase-21-cicd` → `develop`
- **Type:** `ci`
- **Status:** ✅ Merged

## Objective
Phase 21: automated, reproducible verification on every push and PR — build and test both stacks and run a
security pipeline (static analysis, dependency scanning, secret detection) — all **infrastructure-free**, so it
runs on a clean GitHub-hosted runner exactly like the local suites.

## Changes
- **`.github/workflows/ci.yml`** — two jobs:
  - *backend*: `setup-dotnet` pinned by `backend/global.json`, NuGet cache, restore → `build -c Release`
    (warnings are errors) → `test`; TRX artifact uploaded.
  - *frontend*: pnpm 11.24 + Node 24, `install --frozen-lockfile`, production builds of both apps, Vitest
    (web + admin), Playwright (`install --with-deps chromium` + `e2e`; the config boots its own `ng serve`).
- **`.github/workflows/security.yml`** — CodeQL (`csharp` + `javascript-typescript`, `build-mode: none`,
  `security-extended`), dependency-review on PRs (fail ≥ high), NuGet `--vulnerable` scan (fails on any
  severity) + `--deprecated` report, `pnpm audit --prod --audit-level high`, and Gitleaks over full history.
  Runs on push/PR and weekly.
- **`.gitleaks.toml`** — extends the default rules; allowlists the repo's intentional dev placeholders
  (`.env.example`, dev `appsettings`, test signing keys, compose `catrashop` defaults) so the scan targets
  real leaks. Phase 20's `SecretGuard` already blocks those values outside Development.
- **`.github/dependabot.yml`** — weekly updates for nuget (`/backend`), npm (`/frontend`, Angular + dev-deps
  grouped), and github-actions (`/`).
- **`backend/global.json`** — SDK pin (`10.0.400`, `rollForward: latestFeature`) for reproducible builds.

## Technical decisions
- **CodeQL `build-mode: none`** — no SDK/infra in the analysis job; CI already proves the code compiles.
- **Two workflows, least-privilege permissions, cancel-in-progress** — legible failures, scoped tokens, and a
  weekly security schedule that catches advisories against unchanged deps.
- **No container scanning** — the app has no Dockerfile (compose runs infra only), so there is no image to scan.
- **Gitleaks over a licensed scanner** — free, config lives in-repo and is reviewed like any other file.

## Testing / verification
Run the CI-equivalent commands locally (the workflows themselves execute on GitHub):
- `dotnet build/test Catrashop.slnx -c Release` → **195 passed, 2 skipped, 0 failed**.
- `dotnet list Catrashop.slnx package --vulnerable --include-transitive` → **no vulnerable packages**.
- SDK pin resolves: `dotnet --version` → `10.0.401` (via `latestFeature`).
- All three workflow YAML files and `.gitleaks.toml` parse cleanly.

## Deferred to GitHub
First live workflow run, CodeQL results in the Security tab, Dependabot PRs, and the hosted-runner Playwright
run. Requiring these checks via branch protection is a repository setting, not code.

