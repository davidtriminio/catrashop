# PR-017 — AI shopping assistant

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/ai-assistant` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Phase 17: an AI assistant with strictly bounded tools, RAG, and guardrails — safety properties enforced by
architecture, not by trusting the model (Section 17).

## Changes
- **New `AI` module:** `AgentOrchestrator` (bounded model↔tool loop); `IChatModel` + deterministic offline
  `FakeChatModel`; `IKnowledgeRetriever` + lexical retriever over a seeded non-PII knowledge base; three tools —
  `knowledge_search` (RAG), `search_products` (public catalog), `order_status` (caller's own order);
  `PromptInjectionGuard`; `SystemPrompt`; `AgentOptions`; `POST /api/v1/ai/chat` (authenticated).
- **Minimal public read contracts for the tools:** `IProductCatalogQuery.SearchProductsAsync` (Catalog) and
  `IOrderQueries.GetForCustomerByNumberAsync` (Orders, ownership-scoped).
- **Tests:** `CommerceHub.Modules.AI.Tests` — **11 unit tests**.
- ADR-022.

## Technical decisions (security)
- **Tools are an allow-list = the boundary.** No SQL/shell/HTTP/write tool exists; the assistant can only do what
  a tool exists for.
- **Ownership is injected from the JWT**, never from the model — `order_status` can only read the caller's own
  order (foreign/unknown number → "not found").
- **Layered guardrails:** scoping system prompt; a guard that hard-refuses prompt-exfiltration/override before the
  model runs; tool output fed back as untrusted data; client history filtered so it can't inject a system/tool
  role; bounded iterations + message cap + auth.
- **Provider-agnostic:** a real LLM and a vector retriever swap in behind `IChatModel` / `IKnowledgeRetriever`
  with no orchestrator/tool/guardrail changes.
- **Known limitations (ADR-022):** `FakeChatModel` is a deterministic stand-in; lexical (not embedding) RAG; no
  conversation persistence or per-user rate limiting yet; live run deferred to Docker.

## Testing / verification
- `dotnet build` → **0/0**; `dotnet test` → **109 passed** (11 new AI): injection guard flags
  exfiltration/override and passes benign input; retriever returns the right policy / nothing for gibberish;
  orchestrator injects the authed user into the order tool (not the model), refuses injection without running a
  tool or leaking the prompt, and falls back helpfully.
- API boots with 11 modules `[…,"AI"]`; `POST /api/v1/ai/chat` → **401** unauthenticated.
- ⚠️ **Deferred (no Docker):** a live chat against a real model + seeded data.

## Review cycle
**🟢 APPROVE (simulated)** — Safety is architectural: an allow-list of read-only tools over public contracts,
ownership taken from the JWT (proven by test), and layered injection defenses including a hard pre-model refusal.
The model and retriever are cleanly swappable. Limitations (deterministic model, lexical RAG, no rate limiting)
are called out, not hidden.

## Merge result
Merged into `develop` with `--no-ff`.
