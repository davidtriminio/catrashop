# PR-043 — National shipping: delivery zones by department, pickup stores and a real shipping cost

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/national-shipping` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Problem
Shipping was never charged: checkout computed subtotal, discount and tax, and the "shipping" line in the web checkout was
only an estimate taken from a flat-rate method an operator attached to the order *after* payment. Addresses were free
text (any country, any region), so an order could be placed for anywhere, and there was no way to collect an order at
a store. The store ships within Honduras only, and its cost has to depend on the destination — never on how the
customer pays.

## Changes
- **Departments:** the 18 departments of Honduras as a fixed list with ISO 3166-2 codes (`HN-FM`, …). No municipality
  catalog.
- **Zones** (`ShippingZone`): a name, a cost, a lead time (min–max days), a **cash-on-delivery coverage flag** (used by
  the payment rules in PR-044) and the departments it serves. A department belongs to **one** zone (unique index).
  Admin CRUD + activate/deactivate: `GET/POST/PUT /api/v1/shipping/admin/zones`, `…/activate`, `…/deactivate`.
- **Pickup stores** (`PickupStore`): name, address, city, department, phone, hours; admin CRUD + enable/disable.
- **Public reads:** `GET /api/v1/shipping/zones` and `GET /api/v1/shipping/pickup-stores` (active ones only).
- **Quotation contract:** `IShippingQuotation` lives in `Orders.Contracts` and is implemented by Shipping (which already
  depends on Orders), so Orders never references Shipping.
- **Orders:** `DeliveryMode` (`Delivery` | `Pickup`), `DepartmentCode`, `PickupStoreId/Name` and `ShippingTotal` on the
  order. `GrandTotal = subtotal − discount + tax + shipping`; **shipping is not taxed**. The order list, detail, the
  customer view used by the AI tool and the amount the payment charges all include it.
- **Checkout validation (server):** a delivery needs an address with `country = HN` and a department that an active
  zone serves; a pickup needs an active store and the name of who collects it (no address). The cost is looked up by the
  server — a `shippingTotal` sent by the client is ignored. The department's name is stored in the order's `region`.
- **Migrations:** `AddNationalShipping` for Shipping (zones, zone departments, pickup stores) and Orders (five columns
  with defaults: existing orders become `Delivery` with a shipping total of 0).
- **Web:** checkout lets the customer choose home delivery or pickup, a department (with the zone's cost and days), the
  store and who collects; the summary shows the real shipping cost (or "Gratis"). Orders show the shipping line and
  "Recoger en" for pickups. Texts in es/en.
- **Admin:** the Shipping page now manages zones (with a department picker and a warning listing departments no
  active zone serves) and pickup stores; the order detail shows shipping and pickups.
- **Mobile:** the order detail reads `shippingTotal` and pickups so the lines add up to the total.
- **Seed:** `seed.ps1` creates 5 zones covering all 18 departments (Central L. 30, Norte 100, Occidente 130, Sur y
  oriente 130, Islas y Mosquitia 250 — example values, editable in the admin) and 2 pickup stores.

## Things to know
- **A fresh database has no zones**, so home delivery is impossible until an administrator creates them (or the seed
  runs). That is deliberate: a made-up price must never be charged by default.
- Zone costs are copied onto the order at checkout; editing a zone later does not change earlier orders.
- Saved customer addresses keep their free-text region; the checkout pre-selects the department only when the text
  matches a department name. The customer address book itself is unchanged.
- `ShippingMethod` (standard/express) is untouched: it only labels the shipment an operator creates for a paid order and
  no longer has any bearing on what the customer pays.
- Error messages that come from the server (`orders.delivery.not_available`, …) are in English until PR-049.
- Not in this PR: payment methods and their effect on the order state (PR-044/045).

## Verification
- Backend `dotnet test -c Release`: all unit and architecture tests pass (Shipping 33, Orders 23, architecture 68); the
  4 Postgres integration tests were skipped as usual (they need the fixed `commercehub` database).
- Against a temporary PostgreSQL database (`commercehub_verify`, migrations applied by `setup-db.ps1`, seeded) and a
  real API on port 5299, 30 HTTP scenarios passed: zones cover all 18 departments; delivery to a department costs its
  zone's amount and the total/list/payment amount include it; shipping is untaxed; a lower-case code works; a
  `shippingTotal` in the body is ignored; unknown department, missing department, country US, no address and unknown
  delivery mode are rejected; a deactivated zone refuses its departments; pickup is free, records the store and needs
  no address; pickup without a collector, at an unknown store or at a disabled store is rejected; a department cannot
  join two zones (409); zones in another currency or with no departments are rejected; a customer gets 403 and an
  anonymous caller 401 on the admin endpoints; editing a zone's cost affects only later orders. The temporary database
  was dropped afterwards.
- `ng test` web 46/46 and admin 6/6; `ng build` of both apps complete; `flutter test` 26/26 and `flutter analyze` clean.
- In the built-in browser (store on 4210 and admin on 4310 against that API) at 1280, 768 and 375 px: checkout for a
  delivery (department → zone cost → total LPS. 1,112.00 = 920 + 92 tax + 100) and for a pickup (store, hours, free
  shipping), both order pages, and the admin zones page creating/editing (a zone's cost edited from 30 to 35).
  This found and fixed a bug (the checkout button stayed disabled when the department was chosen before the other
  fields) and the placeholder postal code `-` showing on pickup orders.
- **Not verified:** the Flutter app on a device (only unit tests and the analyzer); the admin pickup-store form and
  the deactivate/activate buttons in the browser (covered by HTTP only); the Playwright e2e; checkout with a saved
  address pre-selecting its department; behaviour with the maximum number of zones/stores.
