# PR-041 — Administrator role gets the promotions and notifications permissions

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `fix/admin-permissions` → `develop`
- **Type:** `fix`
- **Status:** ✅ Merged

## Problem
The Administrator role is built from `Identity.Domain.Permissions.All`, but that list never learned about the
permissions that the Promotions (`promotions.manage`) and Notifications (`notifications.manage`) modules added later.
An administrator therefore got **403** on every coupon and manual-notification endpoint, and the matching admin
pages could not work. Found while seeding coupons through the API for the PR-040 conversion test.

## Changes
- `Permissions.PromotionsManage` and `Permissions.NotificationsManage` added to the Identity catalog and to
  `Permissions.All` (so only the Administrator role receives them; no other role changes).
- New architecture test `PermissionCatalogTests`: every `*Permissions` constant declared in a module's `Domain`
  namespace must be granted to Administrators, and catalog strings must be unique and dotted. A module that adds a
  permission without registering it now fails the build instead of producing a silent 403.

## Things to know
- Permissions travel inside the access token, so an administrator already signed in must **sign in again** (or wait
  for the next refresh) to receive the two new permissions.
- No role other than Administrator has a way to manage coupons or notifications; if a "Marketing" role is wanted
  it is a separate decision.

## Verification
- Architecture tests 68/68. With the old `Permissions.cs` the new test fails and names both missing permissions;
  with the fix it passes.
- Against a temporary PostgreSQL database and a real API: the administrator's token carries
  `promotions.manage` and `notifications.manage`; creating a coupon returns 201, listing coupons 200, and
  `notifications/admin/send` with an empty body 400 (authorised, then rejected by validation). A customer still gets
  403 on both.
- Not verified: the admin pages in a browser, and sending a real notification.
