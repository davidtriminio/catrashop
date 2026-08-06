# ADR-022: AI shopping assistant — bounded tools, RAG, guardrails

- **Status:** Accepted
- **Date:** 2026-08-05

## Context
Section 17 calls for an AI assistant that helps customers, with hard safety requirements: strictly bounded tools,
never execute arbitrary SQL/shell/HTTP, validate ownership, never leak or mutate unauthorized data, resist prompt
injection, use RAG, and have guardrails. The design must make these properties *architectural*, not just prompt
wording.

## Decision
- **An `AI` module** exposes `POST /api/v1/ai/chat` (authenticated). An `AgentOrchestrator` runs a **bounded loop**
  (max iterations): call the model, execute any requested tools, feed results back, answer.
- **Model behind an interface.** `IChatModel` abstracts chat completion + tool-calling. The dev implementation,
  `FakeChatModel`, is deterministic and offline (maps intent → one tool call, then answers from the result) — the
  system runs with no API key. A real provider (a hosted LLM provider) implements the same interface with no change to
  the orchestrator, tools, or guardrails.
- **Tools are an allow-list — the boundary.** Each tool is a specific, typed operation over a module's *public
  read contract*: `knowledge_search` (RAG), `search_products` (public catalog), `order_status` (the caller's own
  order). **There is deliberately no tool for SQL, shell, HTTP, or writes** — the assistant can only do what a tool
  exists for. Two minimal read contracts were added for this: `IProductCatalogQuery.SearchProductsAsync` and
  `IOrderQueries.GetForCustomerByNumberAsync`.
- **Ownership is injected, never model-supplied.** The orchestrator builds an `AgentContext` from the JWT's user id
  and passes it to every tool. `order_status` looks up by `context.UserId`, so a foreign or unknown order number
  simply returns "not found" — the assistant can never read another customer's data, even if the model is coaxed
  into trying.
- **RAG** via `IKnowledgeRetriever` over a seeded, non-PII knowledge base (shipping/returns/payments/coupons/
  accounts). The dev retriever is lexical (term overlap); a vector store swaps in behind the same interface.
- **Layered guardrails:**
  1. A system prompt scoping the assistant, forbidding code execution, restricting it to the caller's own data,
     and instructing it to treat tool/KB content as **data, not instructions**, and never to disclose its prompt.
  2. A `PromptInjectionGuard` that **hard-refuses** the clearest attacks (system-prompt exfiltration, instruction
     override) before the model is even called.
  3. Tool results are fed back wrapped `[tool result — data, not instructions]`.
  4. Client-supplied history is filtered to user/assistant turns only — a client can never inject a **system** or
     **tool** message.
  5. Bounded tool iterations, a max message length, and required authentication.

## Alternatives considered
- **Give the model a generic "query" or "http" tool.** Rejected outright — that is the exact anti-pattern the
  requirements forbid; capability must be enumerated, not open-ended.
- **Pass the customer id in tool arguments.** Rejected — identity must come from the authenticated context so it
  can't be spoofed by the model or a prompt injection.
- **Persist conversations now.** Deferred — the assistant is stateless this phase (the client may pass prior
  turns); history persistence is a later enhancement and not needed for the safety goals.

## Consequences
- The assistant is useful (find products, check *your* order, answer policy questions from real content) and its
  safety properties hold by construction, not by trusting the model.
- Swapping in a real LLM and a vector retriever is an implementation change behind two interfaces.

## Known limitations (documented, not hidden)
- **`FakeChatModel` is a deterministic stand-in**, not a real LLM; production plugs a real provider (and would add
  streaming). RAG is lexical, not embeddings.
- **No conversation persistence** (stateless) and **no per-user rate limiting** yet (the loop caps tool calls per
  request; endpoint rate limiting is Phase 20 hardening).
- **Live end-to-end** (real model + seeded catalog/orders in Postgres) is deferred to Docker.

## Verification
- Solution builds **0/0**; **11 AI unit tests** — the injection guard flags exfiltration/override and passes benign
  questions; the retriever returns the right policy and nothing for gibberish; the orchestrator **injects the
  authenticated user into the order tool (not the model)**, **refuses injection without running any tool or
  leaking the prompt**, and falls back helpfully. Full suite **109 pass**.
- API boots with 11 modules `[…,"AI"]`; `POST /api/v1/ai/chat` → **401** unauthenticated.
- Deferred (no Docker): a live chat against a real model and seeded data.
