# ADR-009: Authentication & authorization strategy

- **Status:** Accepted
- **Date:** 2026-03-29

## Context
CommerceHub needs stateless API auth for a SPA + future mobile clients, revocable sessions, and
fine-grained back-office access control across many modules.

## Decision
- **Access tokens:** short-lived (15 min) **HS256 JWTs** carrying `sub`, `email`, one `role` claim per role,
  and one `permission` claim per effective permission. Authorization reads claims — no per-request DB hit.
- **Refresh tokens:** long-lived (14 d) opaque random tokens, **stored only as SHA-256 hashes**, single-use with
  **rotation**. Replay of a rotated/revoked token triggers **reuse detection** → all sessions revoked.
- **Passwords:** hashed with **bcrypt** (work factor 12). Login equalizes timing on the unknown-user path to
  avoid account enumeration; all credential errors are generic.
- **Password reset:** hashed single-use tokens (2 h); the raw token leaves the system only via an integration
  event to Notifications/n8n. Completing a reset revokes all sessions.
- **Permission-based authorization:** endpoints require a permission string (`module.resource.action`); a custom
  `IAuthorizationPolicyProvider` materializes a policy per permission on demand, and a handler checks the claim.
  Roles → permissions is a **code-defined** map (`RolePermissions`).

## Alternatives considered
- **Server-side sessions/cookies.** Simpler revocation but stateful and awkward for mobile/cross-origin; rejected
  for the API (the SPA still stores tokens carefully).
- **ASP.NET Core Identity (full).** Brings a large schema and opinions we don't need; we use a lean domain model
  and only borrow proven primitives (bcrypt).
- **DB-driven permissions.** More flexible (edit permissions without deploy) but adds a join on every token issue
  and admin surface we don't need yet. **Future evolution:** move `RolePermissions` into the database behind the
  same claim mechanism — no API change required.

## Consequences
- Fast, stateless authorization; revocation is handled through refresh-token rotation + reuse detection rather
  than access-token blacklists (acceptable given the 15-min access lifetime).
- Secrets (signing key) come from configuration/env, validated at startup (`ValidateOnStart`); never committed.
- Security-relevant events are written to an append-only `security_audit_logs` table.
