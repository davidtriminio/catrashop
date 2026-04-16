# PR-004 — Catalog module

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/catalog` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Phase 4: the product catalog — categories, brands, products with variants/attributes/images/SKUs — plus a
customer-facing search with PostgreSQL full-text search, filtering, sorting and pagination.

## Changes
- **Domain:** `Category` (self-referencing tree), `Brand`, `Product` aggregate owning `ProductVariant`
  (globally-unique SKU), `ProductAttribute`, `ProductImage`; slug generation; invariants (unique SKU in product,
  single primary image, "no publish without a variant").
- **Application:** `CategoryService`, `BrandService`, `ProductService` (create/update/add-variant/publish/archive),
  `CatalogQueryService`; repository + `ICatalogReadStore` abstractions; validators; `ICatalogUnitOfWork`.
- **Infrastructure:** `CatalogDbContext` (catalog schema), owned-collection configs with FKs + unique indexes,
  repositories, FTS read store, design-time factory, **initial migration** (7 tables).
- **BuildingBlocks:** `Slug` helper; `PermissionPolicy`/`RequirePermission` promoted here so any module can guard
  endpoints without referencing the API host.
- **API:** registers Catalog; public read endpoints (anonymous) + admin write endpoints (require
  `catalog.products.manage`).
- ADR-010 (search strategy).

## Technical decisions
- **Catalog owns structure/identity only** — price lives in Pricing, stock in Inventory (clean module boundaries).
- **Global SKU uniqueness** enforced by a unique index; slug uniqueness per aggregate type.
- **PostgreSQL FTS** (`websearch_to_tsquery` + `ts_rank`) — no separate search engine yet, isolated behind
  `ICatalogReadStore` (ADR-010).
- Read side returns projections (not aggregates) with correlated lookups — avoids loading full aggregates for lists.

## Testing / verification
- `dotnet build` → **0 warnings / 0 errors**.
- `dotnet ef migrations add` → succeeds; `catalog` schema with `categories`, `brands`, `products`,
  `product_variants`, `product_attributes`, `product_images`, `outbox_messages`; unique SKU + slug indexes; 6 FKs.
- API boot smoke test: root reports `modules: ["Identity","Catalog"]`; all **8** catalog endpoints in OpenAPI;
  admin `POST /products` → **401** without a token (permission authorization confirmed end-to-end).
- ⚠️ **Deferred (no Postgres running):** actual CRUD + FTS query execution. To run once Docker is up.

## Review cycle
**🟡 REQUEST CHANGES (simulated)** — Registering `IUnitOfWork` in both Identity and Catalog would make the last
registration win for *all* resolutions, silently saving through the wrong DbContext. Introduce per-module
unit-of-work markers.
**🛠 Corrective commit** — Added `IIdentityUnitOfWork` / `ICatalogUnitOfWork`; each context implements its own;
services depend on the module-specific marker.
**🟢 APPROVE (simulated)** — Boundaries clean, the UoW bug is fixed at the root, search isolated behind a seam.

## Merge result
Merged into `develop` with `--no-ff`.
