# Exercising CommerceHub end to end

There is **no automated seed script yet** — seeding is a planned follow-up. Until then, this page shows how to
drive the system through the API so you can see the full customer journey and the events it emits. Every path is
a real route (`/api/v1/...`); the exact request/response shapes are in the OpenAPI document the API serves in
Development at `/openapi/v1.json` (and the module endpoint files under `backend/src/Modules/**/Api`).

## Prerequisites

Infrastructure up and migrations applied — see [Quick start](../README.md#quick-start). The calls below assume
the API at `http://localhost:5000` (adjust to your launch profile). Capture the access token from login/register
and send it as `Authorization: Bearer <token>` on authenticated calls.

## 1. Create an account and sign in

```
POST /api/v1/identity/register     # → access token + refresh token
POST /api/v1/identity/login        # → same, for an existing user
```

Registering publishes `CustomerRegistered`, which the Customers module consumes to provision a profile and the
Notifications module reacts to with a welcome message (and the n8n welcome-email workflow, if running).

## 2. Seed catalog data (admin)

Product creation requires the `catalog.manage` permission, so use a user/role that has it. Products are created
as **Draft** and must be published before they are visible to customers.

```
POST /api/v1/catalog/categories
POST /api/v1/catalog/brands
POST /api/v1/catalog/products              # Draft, with variants/attributes/images
POST /api/v1/catalog/products/{id}/publish
```

Stock lives in Inventory, keyed by SKU — give each variant SKU some on-hand quantity through the inventory admin
endpoints (`/api/v1/inventory`) so checkout can reserve it.

## 3. Browse as a customer

```
GET /api/v1/catalog/products?q=...&category=...&sort=relevance&page=1
GET /api/v1/catalog/products/{slug}
GET /api/v1/catalog/categories
GET /api/v1/catalog/brands
```

These are anonymous and output-cached (Phase 22), so identical repeated reads are served from cache for ~60s.

## 4. Build a cart and check out

```
POST /api/v1/cart/items                    # add a variant (price is captured at add time)
PUT  /api/v1/cart/items/{itemId}           # change quantity
POST /api/v1/orders/checkout               # {shippingAddress, couponCode?}
```

Checkout reserves stock, optionally redeems a coupon, creates the order in `PaymentPending`, and enqueues
`OrderCreated`. See the [checkout sequence](architecture/README.md#checkout-sequence). A coupon can be previewed
first via `POST /api/v1/promotions/quote`.

## 5. Pay, fulfil, track

Payment drives the order to `Paid`/`PaymentFailed`; admins then move it through fulfilment, and the customer can
watch it progress and read tracking.

```
POST /api/v1/payments/...                  # capture (see the payments endpoints)
POST /api/v1/orders/admin/{id}/process     # Paid → Processing
POST /api/v1/orders/admin/{id}/ship        # Processing → Shipped   (creates a shipment)
POST /api/v1/orders/admin/{id}/deliver     # Shipped → Delivered
GET  /api/v1/orders/{id}                    # customer view
GET  /api/v1/orders                         # customer's order history
```

Each transition is guarded by the [order state machine](architecture/README.md#order-lifecycle-state-machine)
and emits events that fan out to notifications and the n8n workflows (order receipt, review request on delivery,
low-stock alert).

## 6. Ask the assistant

```
POST /api/v1/ai/chat                        # bounded tools + RAG; only ever reads your own data
```

---

*When automated seeding lands, this page will be updated with a one-command bootstrap and demo credentials.*
