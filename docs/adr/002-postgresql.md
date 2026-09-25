# ADR-002: PostgreSQL as the primary datastore

- **Status:** Accepted
- **Date:** 2026-03-17

## Context
CommerceHub needs a relational store for strongly-consistent, transactional data (orders, inventory, payments),
plus JSON flexibility (outbox payloads, flexible product attributes) and decent full-text search — ideally without
adding a separate search engine early (Section 15).

## Decision
Use **PostgreSQL** as the single primary datastore, accessed via **EF Core 10** with the **Npgsql** provider.
Each module owns a dedicated **schema** in the same database; module tables never cross schemas. A shared
`ModuleDbContext` base applies platform conventions (snake_case naming, per-schema migrations history).

## Alternatives considered
- **SQL Server.** Capable, but Postgres is license-free, container-friendly, has first-class `jsonb` and built-in
  full-text search, and is the stronger default for a portable, cost-conscious stack.
- **A document database (e.g. Mongo).** Rejected as the *primary* store: the core domain is highly relational and
  transactional (money, stock). Document stores would push consistency work into the app.
- **Separate DB per module.** Overkill at monolith stage; a schema-per-module gives boundary visibility and
  independent migrations without the operational cost of many databases. Extraction later can promote a schema to
  its own database.

## Consequences
- Strong ACID guarantees for consistency-critical flows; `jsonb` for flexible payloads; native full-text search
  defers Elasticsearch until genuinely needed.
- Schema-per-module keeps boundaries visible in the database and enables independent migration history.
- One database to operate, back up, and reason about.
- Trade-off: schema-per-module requires each module to configure its own `MigrationsHistoryTable` schema (handled
  centrally in `AddModuleDbContext`).
