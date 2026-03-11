# PR-001 — Project bootstrap & architecture baseline

> ⚠️ **Local workflow simulation.** No human reviewed this code. The review comments below document a
> real workflow (branch → commits → review → corrective commit → merge). The "requested change" describes an
> issue that genuinely surfaced during the build and was genuinely fixed.

- **Branch:** `feature/project-bootstrap` → `develop`
- **Type:** `chore` / `feat` / `docs`
- **Status:** ✅ Merged

## Objective
Establish the repository, architecture baseline, Git workflow, documentation scaffolding, and a **compiling,
runnable** modular-monolith backend skeleton so every subsequent phase has a stable foundation.

## Context
Greenfield repository. Section 57 fixes the phase order; this PR delivers Phase 0 (repo + architecture + Git +
docs) and Phase 1 (backend bootstrap).

## Changes
- **Repo meta:** professional `.gitignore` (.NET/Node/Angular/OS/secrets),
  `.gitattributes` (deterministic line endings), `.env.example` (no real secrets).
- **Docs:** `README.md` (incl. a substantive *"Why a modular monolith?"*), `docs/roadmap.md` (24-phase tracker),
  `docs/adr/001-modular-monolith.md`.
- **BuildingBlocks:** `Entity`/`AggregateRoot` (identity equality), domain events, `Result`/`Error` (functional
  error handling), `PageRequest`/`PagedResult`, and the `IModule` + `ModuleRegistry` modularity seam.
- **Api host:** composition root wiring an explicit module registry, `ProblemDetails`, OpenAPI, and
  liveness/readiness health endpoints.

## Technical decisions
1. **Modular monolith** with an explicit `IModule` seam instead of assembly scanning — the active module set is
   obvious and testable (ADR-001).
2. **Central Package Management** (`Directory.Packages.props`) — one source of truth for versions.
3. **`TreatWarningsAsErrors=true` + NuGet audit** on by default — supply-chain issues fail the build, not slip
   through.
4. **`.slnx`** (new .NET 10 solution format), `net10.0`, nullable + implicit usings repo-wide via
   `Directory.Build.props`.
5. Backend under `backend/`, cleanly separated from the forthcoming `frontend/`.

## Testing / verification
- `dotnet build` → **0 warnings, 0 errors**.
- API smoke test (manual, `--no-launch-profile`):
  - `GET /health/live` → `Healthy [200]`
  - `GET /` → `{"service":"CommerceHub API","status":"ok","modules":[]}`
  - `GET /openapi/v1.json` → OpenAPI 3.1 document served.

## Security considerations
- No secrets committed; `.env.example` only.
- Warnings-as-errors + NuGet audit enabled (see review cycle below).

## Risks
- Docker engine was not running at author time, so infra-dependent runtime paths (Postgres/Redis/RabbitMQ/n8n)
  are **not yet runtime-verified** — deferred to their phases with Docker up.

## Reviewer checklist
- [x] Builds clean (0 warnings with warnings-as-errors)
- [x] Runs; health + root + OpenAPI verified
- [x] No secrets committed
- [x] Module boundaries seam in place
- [x] Conventional Commits
- [x] Docs updated (README, ADR, roadmap)

---

## Review cycle

**🔴 REQUEST CHANGES — reviewer (simulated)**
> The first `dotnet build` failed with `NU1903`: `Microsoft.AspNetCore.OpenApi` 10.0.0 pulls a transitive
> `Microsoft.OpenApi` 2.0.0 that has a **known high-severity advisory** (GHSA-v5pm-xwqc-g5wc). With
> warnings-as-errors this correctly blocked the build. Do not lower the gate — pin a patched version instead.

**🛠 Corrective commit**
> Added a direct `Microsoft.OpenApi` `2.12.2` reference (same major as the `>=2.0.0` dependency → API-compatible)
> to override the vulnerable transitive. Rebuild: **0 warnings, 0 errors**. Security gate left intact.

**🟢 APPROVE — reviewer (simulated)**
> Gate did its job and the fix is minimal and correct. Foundation is clean. Approved for `develop`.

## Merge result
Merged into `develop` with a merge commit (`--no-ff`) to preserve the feature's history.
