# ADR-005: Transactional Outbox for integration events

- **Status:** Accepted
- **Date:** 2026-03-17

## Context
When a module changes state and must also tell other modules/systems (e.g. `OrderCreated` → notifications,
analytics, fulfillment), publishing directly to the broker inside the request is unsafe: the DB commit and the
broker publish are two resources with no shared transaction. A crash between them either loses the event (committed
change, no message) or emits a phantom event (message sent, change rolled back).

## Decision
Use the **Transactional Outbox pattern**. In the same DB transaction as the domain change, write an
`outbox_messages` row (the serialized integration event). A background publisher (Phase 15) polls unprocessed rows,
publishes them to RabbitMQ, and stamps `processed_on_utc`. The outbox table lives in **each module's schema**, so
the write always joins that module's transaction.

```mermaid
sequenceDiagram
    participant App as Module use-case
    participant DB as PostgreSQL (module schema)
    participant Pub as Outbox publisher (worker)
    participant MQ as RabbitMQ
    App->>DB: BEGIN; domain change + INSERT outbox_messages; COMMIT
    loop poll
        Pub->>DB: SELECT unprocessed ORDER BY occurred_on
        Pub->>MQ: publish(event)
        MQ-->>Pub: ack
        Pub->>DB: UPDATE processed_on_utc
    end
```

## Idempotency, retries, dead-lettering
- **At-least-once** delivery: a crash after publish but before the `processed_on_utc` update re-publishes on the
  next poll. Consumers must therefore be **idempotent** (keyed on `EventId`).
- **Retries:** failures increment `retry_count` and record `error`; the row stays unprocessed and is retried.
- **Dead-letter:** rows exceeding a retry threshold are routed to a dead-letter path for inspection (Phase 15),
  never silently dropped.

## Alternatives considered
- **Direct publish in the request.** Rejected: dual-write inconsistency (see Context).
- **Distributed transaction (2PC) across DB + broker.** Rejected: operationally heavy, poorly supported, kills
  throughput.
- **CDC (log-based capture).** Powerful but adds infrastructure and operational surface not justified yet; the
  outbox is simpler and sufficient at this scale.

## Consequences
- No lost or phantom events; the broker can be down at commit time without data loss.
- Requires idempotent consumers and a publisher worker.
- Slight write amplification (one extra row per event) — negligible versus the correctness guarantee.
