# PR-040 — Store currency HNL and `es-HN` money formats

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/currency-hnl` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
The store sells in Honduran lempiras. Make HNL the single store currency end to end, so the payment and shipping
work that follows (PR-042 onwards) starts from the right money model.

## Changes
- **Backend:** new `Store:Currency` setting (`StoreOptions`, default `HNL`) shared by all modules. The validators for
  product variants, shipping methods and fixed-amount coupons (create/update/quote) now require that currency
  instead of any 3-letter code. Carts, orders and payments already take the currency from the variants, so they
  follow.
- **Web (store and admin):** `STORE_CURRENCY` / `STORE_LOCALE` constants; `MoneyPipe` formats with `es-HN`
  (`L 1,234.50`, default HNL); the catalog list now uses `MoneyPipe` instead of Angular's `currency` pipe with a USD
  fallback; the admin forms default to HNL and the currency field is read-only.
- **Mobile:** `formatMoney` shows lempiras as `L 1,234.50`; the new-order notification uses it too.
- **Seed:** the three demo products are priced in HNL (L 320, L 1,050, L 460).
- **Docs:** "Store currency" section in `docs/deployment.md`.

## Not in this PR
Dates and numbers outside money still use the browser/default locale; the `es-HN` `LOCALE_ID` and Spanish texts
arrive with the i18n work (PR-041).

## Existing data
Rows already stored in another currency are not converted. A deployment with real USD prices needs a one-off data
script; a deployment with only test data just needs re-seeding.

## Tests (rule → test)
| Rule | Test |
|---|---|
| Variants accept only the store currency (case-insensitive), configurable | `CatalogRulesTests` |
| Fixed-amount coupons need the store currency | `CouponValidationTests` |
| Money shown as `L 1,234.50`, defaults to HNL, tolerates null | `money.spec.ts` |
| Mobile formats lempiras as `L` and keeps other codes | `format_test.dart` |

## Verification
- Backend: Catalog (24), Promotions (27) and architecture (66) tests pass; the API builds.
- Web Vitest 33/33 and admin 6/6; both builds succeed; Flutter 24/24 and `flutter analyze` clean.
- **Not verified:** nothing was run against PostgreSQL for this PR (the temporary role was removed), so the seed in
  HNL and the validators over HTTP are untested end to end; no visual check in the browser or on a device.
