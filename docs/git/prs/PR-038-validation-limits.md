# PR-038 — Minimum/maximum validations audit

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/validation-limits` → `develop`
- **Type:** `fix`
- **Status:** ✅ Merged

## Objective
Audit every minimum/maximum rule, close the gaps (cart quantity ceiling and stock, zero or over-precise prices,
unbounded stock figures) and put a test behind each rule.

## Gaps found and fixed
- Cart quantity had only a fixed 1000 cap in the request validator and **no stock check** on add or update.
- Variant price accepted `0` and any number of decimals.
- Stock figures (initial, received, adjustment, threshold) had no upper bound.
- The price-filter normalisation lived inline in the endpoint and had no test; neither did the page-size cap.
- The product detail UI had a minimum of 1 but no maximum.

## Changes
- **Cart:** `Cart:MaxQuantityPerLine` (default 20, merged adds count together) and a stock check against the new
  public `IStockQuery` of Inventory (`Cart` now references Inventory contracts only). Same rule on add and update;
  clear messages (`Only 3 unit(s) available.`, `This product is out of stock.`, `You can add at most 20 units…`).
  Products Inventory does not track are limited only by the line cap.
- **Checkout:** unchanged. It already reserves every line atomically (`inventory.insufficient_stock`), which stays
  the final guard against races between add-to-cart and checkout.
- **Catalog:** variant price `> 0`, at most 2 decimals, `≤ 1 000 000`.
- **Inventory:** every stock figure within `0..1 000 000` (adjustment delta `±1 000 000`, non-zero).
- **Web:** quantity stepper clamps to 1..20 (detail and cart; `+` disables at the limit with a hint); admin number
  inputs carry `min`/`max`.
- `PriceFilter.Normalize` extracted from the endpoint so it can be tested.

## Rule → where enforced → test
| Rule | Enforced in | Test |
|---|---|---|
| Cart quantity ≥ 1 | `CartQuantityPolicy`, `AddCartItemRequestValidator`, `Cart.AddItem` | `CartQuantityPolicyTests.Below_one_is_rejected`, `CartTests` |
| Cart quantity ≤ configured cap (merged adds included) | `CartQuantityPolicy` in `CartService` add/update; UI clamp | `CartQuantityPolicyTests`, `quantity.spec.ts` |
| Cart quantity ≤ available stock | `CartQuantityPolicy` + `IStockQuery`; checkout reservation | `CartQuantityPolicyTests.Never_more_than_the_available_stock`, `…Out_of_stock…` |
| Untracked stock → only the cap | `CartQuantityPolicy` | `…Untracked_stock_only_applies_the_line_cap` |
| Variant price > 0, ≤ 2 decimals, ≤ 1 000 000 | `CatalogValidators` | `ProductVariantValidationTests` |
| SKU ≤ 64, variant name ≤ 150, product name ≤ 200, description ≤ 8000 | `CatalogValidators` | `ProductVariantValidationTests` |
| Initial stock / threshold in 0..1 000 000, SKU ≤ 64 | `InventoryValidators` | `InventoryValidationTests` |
| Received qty 1..1 000 000; adjustment non-zero, ±1 000 000 | `InventoryValidators` | `InventoryValidationTests` |
| Stock never negative | `InventoryItem.Adjust` | `InventoryValidationTests.Stock_never_goes_negative` |
| Coupon percentage in (0, 100], fixed amount > 0 + currency | `CouponRules` | `CouponValidationTests` |
| Coupon end after start; usage/per-customer limits ≥ 1; minimum ≥ 0; cap > 0 | `CouponRules` | `CouponValidationTests` |
| Coupon code ≤ 40, name ≤ 120, description ≤ 500 | `PromotionValidators` | `CouponValidationTests` |
| API page size clamped to 1..100 (default 20), page ≥ 1 | `PageRequest` | `PageRequestTests` |
| Catalog price filter drops negatives and swaps inverted bounds | `PriceFilter` (used by `/catalog/products`) | `PriceFilterTests` |

## Verification
- Backend: new `Catrashop.Modules.Catalog.Tests` (19) plus additions to Cart (15), Inventory (34) and Promotions
  (26) all pass; the API builds.
- Web Vitest 30/30; web and admin builds succeed.
- Not verified: the stock check through the real HTTP API and a database (no PostgreSQL available to me), and the
  new stepper behaviour in a browser.
