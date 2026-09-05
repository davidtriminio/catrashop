# ADR-026: CI/CD — GitHub Actions build/test + security pipeline

- **Status:** Accepted
- **Date:** 2026-09-05

## Context
Phase 21 gives the repository automated, reproducible verification on every push and pull request: build and
test both stacks, and run a security pipeline (static analysis, dependency scanning, secret detection). It must
follow the same principle as every phase since 15 — **no infrastructure required** — so the pipeline runs on a
clean GitHub-hosted runner with no Postgres/Redis/RabbitMQ, exactly like the local suites. The workflows are
authored ready; their first live run happens once the repo is pushed to GitHub.

## Decision

### Two workflows, least privilege, cancel-in-progress
`.github/workflows/ci.yml` (build + test) and `.github/workflows/security.yml` (scanning). Both default to
`permissions: contents: read`, grant `security-events: write` only to the CodeQL job, and use a
`concurrency` group per ref so superseded runs are cancelled. Triggers: push to `main`/`develop` and PRs into
them; the security workflow also runs on a weekly `schedule` so a newly disclosed advisory against an unchanged
dependency is still caught.

### CI — build and test both stacks, infra-free
- **Backend job:** `actions/setup-dotnet` pinned by `backend/global.json` (new — SDK `10.0.400`,
  `rollForward: latestFeature`), NuGet cache keyed on the project files + `Directory.Packages.props`,
  `dotnet restore` → `build --configuration Release` (warnings are already errors) → `test`. The Postgres-gated
  E2E skips without a database; everything else runs. TRX results are uploaded as an artifact.
- **Frontend job:** `pnpm/action-setup` (11.24.0) + `actions/setup-node` (Node 24, pnpm cache),
  `pnpm install --frozen-lockfile`, production builds of **both** apps (`commercehub-web`,
  `commercehub-admin`), Vitest for each, then Playwright (`install --with-deps chromium` + `pnpm run e2e`). The
  existing Playwright config already boots its own `ng serve`, so no service orchestration is needed.

### Security pipeline
- **CodeQL** (`csharp` + `javascript-typescript`) with `build-mode: none` and the `security-extended` query
  suite — no SDK setup or build orchestration in the analysis job; results land in the Security tab.
- **Dependency review** (`actions/dependency-review-action`) on PRs, failing on `high`+ severity introduced by
  the change.
- **NuGet vulnerability scan:** `dotnet list package --vulnerable --include-transitive`, failing the job if the
  report names any severity; `--deprecated` is reported non-blocking. (Complements the existing CPM pin that
  already cleared advisory NU1903.)
- **pnpm audit** (`--prod --audit-level high`) for the frontend.
- **Gitleaks** secret scan over full history, governed by a repo-root `.gitleaks.toml` that extends the default
  rules and **allowlists the intentional development placeholders** (the sample `.env.example`, the dev
  `appsettings.Development.json`, the test signing keys, the compose `commercehub` defaults). Phase 20's
  `SecretGuard` already blocks those values at boot outside Development, so allowlisting them here keeps the
  scan focused on genuine leaks rather than failing on documented dummies.

### Dependabot
`.github/dependabot.yml` opens weekly update PRs for **nuget** (`/backend`), **npm** (`/frontend`, Angular
grouped, dev deps grouped), and **github-actions** (`/`), with security updates landing as soon as an advisory
is published.

## Alternatives considered
- **CodeQL `autobuild` for C#.** Rejected in favour of `build-mode: none`: no SDK setup or infra in the
  analysis job, faster, and sufficient for this codebase. The CI workflow already proves the code compiles.
- **A single mega-workflow.** Split into `ci` and `security` so failures are legible, permissions are scoped
  per concern, and the security suite can run on its own weekly schedule.
- **Trivy / container image scanning.** Out of scope: the app has no Dockerfile (compose runs infrastructure
  only, the app runs on the host), so there is no image to scan. Revisit if the app is containerised.
- **Third-party secret scanners requiring a license.** Gitleaks is free and config-driven; the allowlist lives
  in-repo and is reviewed like any other file.

## Consequences
- New files: `.github/workflows/ci.yml`, `.github/workflows/security.yml`, `.github/dependabot.yml`,
  `.gitleaks.toml`, and `backend/global.json` (SDK pin — also makes local builds reproducible).
- Verified locally (the CI-equivalent commands): `dotnet build/test CommerceHub.slnx -c Release` →
  **195 pass / 2 skip / 0 fail**; `dotnet list package --vulnerable --include-transitive` → none; all workflow
  YAML and the Gitleaks TOML parse cleanly; the SDK pin resolves (`10.0.401` via `latestFeature`).
- Deferred to GitHub: the first live workflow run, CodeQL results in the Security tab, Dependabot PRs, and the
  Playwright run on the hosted runner. Enabling branch protection to require these checks is a repo setting, not
  code.
