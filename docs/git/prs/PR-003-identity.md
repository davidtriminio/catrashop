# PR-003 — Identity module

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/identity` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Phase 3: a complete Identity module — registration, JWT authentication, refresh-token sessions with rotation,
password reset, role + permission authorization, and a security audit trail.

## Changes
- **Domain:** `User` aggregate owning role assignments, refresh tokens and reset tokens; `Role` enum, `Permissions`
  catalog, `RolePermissions` map; typed `IdentityErrors`; append-only `SecurityAuditLog`.
- **Application:** `AuthenticationService` (register/login/refresh/logout), `PasswordRecoveryService`,
  `UserQueryService`; request/response DTOs; FluentValidation validators; options (`JwtOptions`,
  `IdentitySecurityOptions`).
- **Infrastructure:** `IdentityDbContext` (identity schema), owned-collection EF configs, `UserRepository`,
  bcrypt hasher, SHA-256 token hasher, secure token generator, HS256 `JwtTokenGenerator`, outbox publisher,
  audit logger, design-time factory, **initial migration** (6 tables).
- **API host:** JwtBearer validation, on-demand permission policy provider + handler, `ICurrentUser`, module
  registration; `ResultHttpExtensions` (ProblemDetails) and `ICurrentUser` promoted to BuildingBlocks.
- ADR-009 (authentication strategy).

## Technical decisions
- Refresh tokens hashed at rest, single-use with rotation + **reuse detection** (revoke-all on replay).
- Login **equalizes timing** and returns generic errors → no account enumeration.
- **Permission-based** authorization via claims; roles→permissions code-defined (ADR-009), DB-driven noted as
  future evolution.
- Guid keys (deliberate simplicity over strongly-typed IDs for clean EF migrations).
- MediatR intentionally **not** used (now commercially licensed) — direct application services suffice here.

## Testing / verification
- `dotnet build` → **0 warnings / 0 errors** (with warnings-as-errors).
- `dotnet ef migrations add` → succeeds; migration creates the `identity` schema with `users`, `user_roles`,
  `refresh_tokens`, `password_reset_tokens`, `security_audit_logs`, `outbox_messages` — snake_case, jsonb outbox,
  unique email index.
- API boot smoke test: root reports `modules: ["Identity"]`; all **7** endpoints in OpenAPI; `/me` → **401**
  unauthenticated.
- ⚠️ **Deferred (no Postgres running):** register/login/refresh/reset DB round-trips. To run once Docker is up.

## Security considerations
- bcrypt (wf 12); signing key from config/env, validated at startup, never committed (dev key clearly labeled).
- Only token **hashes** stored; reset token leaves the system solely via a transient integration event.
- Security audit log for login success/failure, refresh, reuse detection, resets.

## Reviewer checklist
- [x] Builds clean · [x] Migration valid · [x] API boots, auth enforced on /me · [x] No secrets committed
- [x] Module boundaries respected (Api ↔ module only via IModule + public contracts)
- [ ] Credential DB round-trips — **deferred to Docker-up batch**

## Review cycle
**🟡 REQUEST CHANGES (simulated)** — Initial `ICurrentUser` lived in `Identity.Application.Abstractions`; other
modules needing the current user would then depend on the whole Identity assembly. Move it to BuildingBlocks.
**🛠 Corrective commit** — `ICurrentUser` promoted to `Catrashop.BuildingBlocks.Security`; endpoints updated.
**🟢 APPROVE (simulated)** — Boundaries clean, auth model solid, verification honest about the deferred DB paths.

## Merge result
Merged into `develop` with `--no-ff`.
