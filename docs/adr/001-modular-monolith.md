# ADR-001: Modular Monolith over Microservices

- **Status:** Accepted
- **Date:** 2026-03-05
- **Deciders:** Architecture (Catrashop)

## Context

Catrashop must cover a broad commerce domain (identity, catalog, inventory, cart, orders, payments, shipping,
promotions, reviews, notifications, AI, administration, audit). We need clear internal boundaries, high
testability, and the ability to evolve quickly, while keeping operational complexity low enough for a small team to
run and debug on a single machine.

## Decision

Build a **modular monolith**: one deployable ASP.NET Core application composed of independent modules. Each module
owns its `Domain`, `Application`, and `Infrastructure` layers and exposes only a thin **public contract** (commands,
queries, integration events). Cross-module communication happens through:

1. **In-process contracts** for synchronous queries/commands (via a mediator), and
2. **Integration events via the Outbox pattern** for asynchronous side effects.

Module boundaries are enforced by **architecture tests** in CI, not by convention alone.

## Alternatives considered

- **Microservices from day one.** Rejected: pays full distributed-systems cost (network failure modes, eventual
  consistency, saga orchestration, distributed tracing, multi-service deploys) before there is any scaling or
  team-autonomy pressure to justify it. Highest risk to delivery and correctness.
- **Layered "big ball of mud" monolith** (horizontal layers only, no module seams). Rejected: no enforced
  boundaries → coupling creep, and no clean extraction path later.
- **Modular monolith** (chosen). Captures the one durable benefit of services — enforced boundaries — without the
  operational tax, and keeps a mechanical extraction path open.

## Consequences

**Positive**
- Consistency-critical flows (e.g. checkout) can use a single ACID transaction; only side effects go async.
- One deploy, one trace, one log stream → debuggable by one engineer.
- Boundaries enforced by compiler + architecture tests.
- Extraction to services later is mechanical (contracts + events already in place).

**Negative / risks**
- Requires discipline: it is *possible* to bypass boundaries in-process, so architecture tests are mandatory, not
  optional.
- A single process shares fate on deploy; mitigated by fast CI, feature flags where useful, and modular test
  isolation.

## Enforcement

- Each module lives under `backend/src/Modules/<Name>` with internal layers not referenced across modules.
- Public contracts live in a module's `*.Contracts` surface.
- `Architecture.Tests` assert no module depends on another module's non-contract internals.
