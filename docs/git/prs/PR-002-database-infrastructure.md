# PR-002 — Database & infrastructure foundation

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/database-infrastructure` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Phase 2: establish the shared persistence layer (EF Core 10 + PostgreSQL) and the transactional Outbox, plus the
local infrastructure stack, so modules can plug in a schema-isolated DbContext with event publishing built in.

## Changes
- `Catrashop.Infrastructure` project (EF Core 10, Npgsql 10, EFCore.NamingConventions).
- `ModuleDbContext` base: schema-per-module, snake_case, auto-applies entity configs + the outbox.
- Transactional **Outbox**: `OutboxMessage` (jsonb payload, retry/error/processed tracking, correlation id) +
  EF config with an unprocessed-rows index.
- Contracts: `IUnitOfWork`, `IIntegrationEvent`/`IntegrationEvent`.
- `AddModuleDbContext<T>` registration (Npgsql + per-schema `__ef_migrations_history`).
- `docker-compose.yml`: Postgres 17 / Redis 7 / RabbitMQ 4-mgmt / n8n, all with healthchecks.
- ADR-002 (PostgreSQL), ADR-005 (Outbox pattern, with sequence diagram + idempotency/retry/DLQ notes).

## Technical decisions
- **Schema-per-module** in one database — boundary visibility + independent migrations without multi-DB ops.
- **Outbox in each module's schema** so the event write joins that module's transaction (no dual-write).
- **snake_case** naming for idiomatic PostgreSQL.

## Testing / verification
- `dotnet build` → **0 warnings / 0 errors**.
- `docker compose config` → **valid** (parsed clean).
- ⚠️ **Deferred (Docker engine not running at author time):** bringing the stack up, applying migrations, and
  round-tripping the outbox. The first concrete migration lands with the Identity module (Phase 3); all deferred
  runtime checks will be run in a batch once Docker is up.

## Security considerations
- No secrets; compose reads from `.env` (git-ignored) with safe non-secret defaults.

## Reviewer checklist
- [x] Builds clean · [x] Compose valid · [x] No secrets · [x] Boundaries preserved (no cross-module refs)
- [x] ADRs added · [ ] Runtime migration/outbox verified — **deferred to Docker-up batch**

## Review cycle
**🟢 APPROVE (simulated)** — Clean foundation; correctly defers runtime checks rather than faking them. The
schema-per-module + outbox-per-schema choice is coherent. Approved for `develop`.

## Merge result
Merged into `develop` with `--no-ff`.
