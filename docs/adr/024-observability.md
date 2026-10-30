# ADR-024: Observability — OpenTelemetry, structured logs, health probes

- **Status:** Accepted
- **Date:** 2026-08-22

## Context
Phase 19 makes the platform operable: the three telemetry pillars (logs, traces, metrics) and health probes,
wired so they are useful in production yet self-contained with **no infrastructure present** (Docker down).
Nothing may crash or block startup because a collector, database, or broker is missing — the same resilience
principle used since Phase 15.

## Decision

### One telemetry source, subscribed at the composition root
`Catrashop.BuildingBlocks.Observability.CatrashopTelemetry` owns a single `ActivitySource` and `Meter`
(both named `Catrashop`). Any layer emits spans/metrics on these without referencing the host; the API
subscribes OpenTelemetry to them by name. This keeps instrumentation decoupled from exporter wiring and does
not create a new cross-module dependency (BuildingBlocks is shared by all).

### OpenTelemetry for traces and metrics
Configured in the API (`Observability/ObservabilityExtensions.cs`):
- **Tracing:** ASP.NET Core + HttpClient instrumentation, plus the `Catrashop` and `Npgsql` sources.
- **Metrics:** ASP.NET Core + HttpClient + .NET runtime instrumentation, plus the `Catrashop` and `Npgsql`
  meters.
- **Exporters are opt-in and config-driven** (`ObservabilityOptions`): the in-process **Prometheus** scrape
  endpoint (`/metrics`) is on by default (no external dependency); **OTLP** is enabled only when
  `Observability:OtlpEndpoint` is set; a **console** exporter is a dev convenience. With none configured the
  pipeline still records — it simply has nowhere to push, so the app is fully functional offline.

### A first custom metric + span (proof the domain wiring works)
The outbox processor emits `catrashop.messaging.outbox.published` / `...failed` counters (tagged by event
type) and an `outbox.drain` span. This exercises the shared source end to end and appears on `/metrics`,
rather than shipping only third-party instrumentation.

### Structured logging with correlation
The API owns its logging providers: scopes and `TraceId`/`SpanId` are always included; **JSON console** in
non-development, human-readable console in development; logs also flow to **OTLP** when configured. A
`CorrelationIdMiddleware` honours an inbound `X-Correlation-ID` (or falls back to the trace id / a new GUID),
pushes it into a logging scope so every line for the request carries it, tags it on the active span, and echoes
it in the response header — one id ties a request's logs, trace, and client report together.

### Health probes that respect the resilience model
Two endpoints with a rich JSON writer (per-check status + duration):
- **`/health/live`** — liveness: no checks, healthy whenever the process can serve. A broker/DB outage never
  fails liveness (so an orchestrator won't kill a pod that is merely waiting on a dependency).
- **`/health/ready`** — readiness: custom `NpgsqlHealthCheck` (`SELECT 1`) and `RabbitMqHealthCheck` (short-lived
  connection), tagged `ready`. Returns **503** while a dependency is down, **200** when both are reachable.

### An opt-in observability stack in compose
A `docker compose --profile observability up` brings up **Jaeger** (OTLP in, trace UI) and **Prometheus**
(scraping the host's `/metrics`). It is not part of the default `up`, and the API (running on the host) points
at it by setting `Observability:OtlpEndpoint=http://localhost:4317`. No backend code depends on it.

## Alternatives considered
- **Serilog.** A fine choice, but `Microsoft.Extensions.Logging` + OpenTelemetry logs already give structured
  output and OTLP export with one fewer third-party abstraction; the OTel `Meter`/`ActivitySource` model
  covers metrics/traces uniformly. Kept the framework logger.
- **Third-party health-check packages** (`AspNetCore.HealthChecks.*`). The two checks needed are a few lines
  each and must stay lightweight and non-fatal; custom checks avoid a dependency and give exact control over
  timeouts and the live/ready split.
- **EF Core instrumentation package** (beta). Skipped — Npgsql emits its own spans/metrics via its
  `ActivitySource`/`Meter`, which we subscribe to by name, avoiding a prerelease dependency.
- **Prometheus-only vs OTLP-only.** Kept both: Prometheus for a zero-dependency, offline-verifiable scrape;
  OTLP for the real collector path. Both are cheap to wire and serve different audiences.

## Consequences
- New API dependencies: `OpenTelemetry.Extensions.Hosting`, `.Instrumentation.AspNetCore/.Http/.Runtime`,
  `.Exporter.OpenTelemetryProtocol/.Console`, `.Exporter.Prometheus.AspNetCore` (beta), plus direct `Npgsql`
  and `RabbitMQ.Client` for the health checks — all pinned via CPM.
- Backend tests: **186 pass, 2 skipped, 0 failed** (was 183); the 3 new integration tests assert the
  correlation-id header (generated + echoed), the readiness 503 + per-check JSON while infra is down, and that
  `/metrics` serves the Prometheus exposition format. The existing readiness test was updated (it now reflects
  infra state rather than always-200).
- Verified live: the API boots infra-less; `/health/live` → 200, `/health/ready` → 503 (postgres + rabbitmq
  reported), `/` carries `X-Correlation-ID`, `/metrics` serves runtime/ASP.NET/domain metrics, logs are
  structured with scopes.
- Deferred to Docker: exporting to the compose Jaeger/Prometheus stack, and readiness turning 200 with the
  real database and broker up.
