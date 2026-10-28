# PR-031 — Design foundation (tokens and primitives)

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/design-foundation` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
First step of the UI redesign. The previous theme was a single generic indigo palette with ad-hoc spacing, which is why
pages looked basic and crowded (alerts and notifications touching each other). Replace it with a small, deliberate
design system that both Angular apps share, without rewriting any template.

## Direction
Neutral and editorial: near-black primary, one lime accent reserved for highlights (badges, unread marks, the active
admin menu item), soft borders instead of heavy shadows, pill buttons and chips. It works without product photos, which
the seed data does not have.

## Changes
- `frontend/shared/theme.scss`: new tokens (colour, a 4 px spacing scale `--space-1…8`, radii, focus ring, status tints
  that meet WCAG AA) and primitives (`.btn` incl. `.accent`, `.chip`, `.badge` variants, `.card` incl. `.flush`,
  `.page-head`, `.list-row`, `.empty`, `.skeleton`, `.stack`). Every existing class name and CSS variable is kept.
- Cards and alerts now space themselves (`.card + .card`), which fixes the crowded pages globally.
- Admin console: dropped its teal override, sidebar rebuilt on the tokens (active item uses the accent so it stays
  visible on the dark sidebar).
- Notifications page: uses `page-head`, `list-row` and `empty`; unread rows get a lime marker; buttons no longer wrap.

## Verification
- Both Angular apps build; Vitest web 17/17 and admin 6/6.
- Checked in the browser against a real API with seeded data: sign-in, account and notifications at desktop and phone
  width. Known gap, handled in the next phase: the storefront header does not collapse on narrow screens.
