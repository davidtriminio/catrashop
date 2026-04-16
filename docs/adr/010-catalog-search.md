# ADR-010: Product search with PostgreSQL full-text search

- **Status:** Accepted
- **Date:** 2026-04-12

## Context
Customers need to search the catalog by free text and filter by category/brand, with sorting and pagination.
We want useful search without operating a separate search engine before scale justifies it (Section 15).

## Decision
Use **PostgreSQL full-text search** via Npgsql's EF Core functions: `to_tsvector('english', name || ' ' ||
description)` matched against `websearch_to_tsquery` (which gives users familiar quoted-phrase / `-exclude`
syntax), ranked with `ts_rank` for relevance ordering. Search runs against the `catalog.products` table, returns
**only Active products**, and is always paged (default 20, max 100). Category/brand filters resolve by slug.

## Alternatives considered
- **`LIKE '%term%'`.** Simple but no stemming/ranking and can't use an index for infix matches. Rejected — poor
  relevance and performance.
- **Elasticsearch / OpenSearch.** Powerful (facets, typo tolerance) but adds a service to run, sync, and secure,
  plus dual-write/consistency concerns. Not justified at this stage; the modular design lets us add it later behind
  the existing `ICatalogReadStore` seam without touching callers.

## Consequences
- Real stemmed, ranked search with zero extra infrastructure; filters/sort/pagination via the same query.
- The read side is isolated behind `ICatalogReadStore`, so swapping in a search engine later is a localized change.
- **Performance note / future optimization:** the current query computes `to_tsvector` per row at query time. For
  larger catalogs, add a **stored generated `tsvector` column with a GIN index** (a migration + shadow property) —
  deliberately deferred (Section 52: no premature optimization) since it doesn't change the API.

## Verification status
Query is compile-verified and written to Npgsql's documented FTS API; **runtime execution is deferred** until
PostgreSQL is running (per the project's "build now, verify later" mode).
