# PR-048 — App icon: a shopping cart with a planet

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/cart-planet-icon` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Problem
The bag-with-an-orbit icon of PR-047 read as a **padlock**: a single arched handle over a square body is the silhouette of
a padlock, which is the opposite of "shop". The owner also wants a recruiter who sees the project to understand the concept
(an e-commerce) at first sight.

## Changes
- Six alternatives were drawn (tote bag with an orbit, with a planet, with a sparkle; cart with a planet or an orbit; paper
  bag). Two were dropped as confusing (the cart with an orbit through the basket, the paper bag whose handles looked like
  ears). The owner chose the **cart with a ringed planet** above the basket.
- `branding/make-icons.py` now draws that mark (cart handle/base, basket, two wheels, and a ringed planet whose ring passes
  behind the ball and in front of it with a small gap), in the same colourways and with the same outputs as before: store and
  admin favicons (`.ico` 16/32/48, `.svg`, apple-touch), Android legacy, adaptive and monochrome launcher icons, the iOS set.
- The vector (`favicon.svg`) is drawn from the same constants as the raster, so they stay identical.
- `branding/README.md` and the previews were updated.

## Things to know
- It is still a mark drawn in code, not a designed logo. Swapping in artwork only means replacing `draw_mark`/`svg`.
- The visible name ("CommerceHub") is **not** changed by this PR; the owner is still choosing one.
- Browsers cache favicons and some launchers keep the old icon: hard-reload / private window, reinstall the app.

## Verification
- Looked at both colourways at 16 to 256 px, the adaptive icon under a circular mask (the cart and planet stay inside the safe
  zone) and the monochrome layer on a grey background; the planet reads as a dot at 16 px and as a ringed planet from 32 px.
- The SVG served over HTTP renders the same drawing in the browser; `favicon.ico` holds 16/32/48 px; iOS icons are RGB with no
  transparency.
- `ng build` of the store and the admin complete with the icon files; `flutter build apk --debug` succeeds with the adaptive and
  monochrome resources; `flutter analyze` clean and `flutter test` 40/40.
- **Not verified:** on a real phone launcher or iOS, and in the real browser tab or home-screen shortcut.
