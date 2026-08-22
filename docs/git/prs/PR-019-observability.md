# PR-019 — Observability (logging, tracing, metrics, health)

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/phase-19-observability` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Phase 19: make the platform operable — structured logs, distributed traces, metrics, and health probes —
wired to be production-useful yet fully functional and resilient with **no infrastructure present**.

## Changes
- **Shared telemetry source** (`CommerceHubTelemetry` in BuildingBlocks): one `ActivitySource` + `Meter`
  named `CommerceHub`, subscribed by the API — instrumentation stays decoupled from exporters, no new
  cross-module dependency.
- **OpenTelemetry** in the API: tracing (ASP.NET Core, HttpClient, `CommerceHub`, `Npgsql`) and metrics
  (ASP.NET Core, HttpClient, .NET runtime, `CommerceHub`, `Npgsql`). Exporters opt-in via `ObservabilityOptions`:
  in-process **Prometheus** `/metrics` on by default, **OTLP** when an endpoint is configured, **console** for
  dev. First domain instrumentation: outbox `published`/`failed` counters + an `outbox.drain` span.
- **Structured logging + correlation:** API owns its providers (scopes + trace/span ids always on; JSON console
  out of dev); `CorrelationIdMiddleware` honours/echoes `X-Correlation-ID`, scopes it onto every log line, and
  tags it on the active span.
- **Health probes:** rich JSON writer; `/health/live` (no infra dependency, always healthy while up) vs
  `/health/ready` (custom `NpgsqlHealthCheck` + `RabbitMqHealthCheck`, 503 while a dependency is down).
- **Opt-in compose profile** `observability`: Jaeger (OTLP + trace UI) + Prometheus (scrapes host `/metrics`),
  with `observability/prometheus.yml`. Not started by default; no backend code depends on it.
- Dev `appsettings` gains an `Observability` section (OTLP disabled by default). New deps via CPM (OpenTelemetry
  set + Prometheus.AspNetCore beta + direct Npgsql/RabbitMQ.Client for the checks).

## Technical decisions
- **Resilience first:** exporters are opt-in; with none configured the pipeline records but pushes nowhere, so
  the app runs offline. Readiness reflects infra (503 down); liveness never does — an orchestrator won't kill a
  pod that is only waiting on a dependency.
- **Framework logger over Serilog**, **custom health checks over third-party packages**, **Npgsql's own
  source over the beta EF Core instrumentation** — fewer dependencies, exact control, same outcome.
- **Prometheus + OTLP both:** a zero-dependency offline scrape and the real collector path.

## Testing / verification
- Backend `dotnet test CommerceHub.slnx` → **186 passed, 2 skipped, 0 failed** (was 183; +3 integration:
  correlation header generated + echoed, readiness 503 with per-check JSON, `/metrics` exposition format). The
  prior always-200 readiness test now asserts infra-reflective 503.
- Verified live (API on host, Docker down): `/health/live` → **200** `{"status":"Healthy","checks":[]}`;
  `/health/ready` → **503** listing `postgres` + `rabbitmq` Unhealthy; `/` returns an `X-Correlation-ID`;
  `/metrics` serves runtime/ASP.NET/domain metrics; console logs are structured with scopes.
- ⚠️ **Deferred (no Docker):** exporting to the compose Jaeger/Prometheus stack; readiness flipping to 200 with
  a real database + broker up.

## Review cycle
**🟢 APPROVE (simulated)** — Three pillars plus health, wired the resilient way: opt-in exporters, a live/ready
split that respects the platform's infra-independence, and a correlation id threaded through logs, traces, and
the response. Custom instrumentation proves the shared source end to end. Dependencies and the compose stack are
justified and optional; deferrals are explicit.

## Merge result
Merged into `develop` with `--no-ff`.
