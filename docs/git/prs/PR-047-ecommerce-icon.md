# PR-047 — App icon: a shopping bag in orbit instead of the "H"

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/ecommerce-icon` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Problem
The icon of PR-046 was a letter **H** (from "Hub"). The owner found that it does not say "online store" and does not like
the "Hub" part of the name. After looking at eight alternatives (bag, cart, shop front, box, price tag, basket, bag with a
sparkle, bag with an orbit) they chose the bag with an orbit ring and asked for it to be refined.

## Changes
- `branding/make-icons.py` now draws a **shopping bag wrapped by an orbit ring**, like a planet (a nod to the "Galactic"
  demo brand), on the same brand colours and with the same outputs as before (store favicons, admin favicons, Android
  legacy + adaptive + monochrome launcher icons, iOS icon set). Nothing else in the project needed to change.
- Refinements over the first sketch: the back half of the ring passes **behind** the bag and the front half **wraps** it,
  with a small gap (halo) that separates the ring from the bag (the two share one colour); the little eyelets that vanish at
  small sizes are gone; a sparkle is drawn only from 48 px up, so favicons at 16/32 px stay clean.
- `favicon.ico` is now assembled from one natively drawn PNG per size (16/32/48) instead of shrinking a single image.
- The monochrome (themed) Android layer cuts the gap out of the artwork instead of painting it, so it works on any wallpaper
  colour.
- `branding/README.md` and the previews were updated.

## Things to know
- It is still a mark drawn in code, not a designed logo; swapping in artwork only means replacing `draw_mark`/`svg`.
- The visible name ("CommerceHub") is **not** changed by this PR: the owner is still choosing one.
- Browsers cache favicons and some launchers keep the old icon: hard-reload / private window, reinstall the app.

## Verification
- Compared the raster and the vector (SVG) versions: the SVG, served over HTTP, renders the same drawing in the browser.
- Looked at the result at 16/32/64 px and large, in both colourways, the Android adaptive icon under a circular mask and the
  monochrome layer on a grey background.
- `favicon.ico` contains 16/32/48 px; iOS icons are RGB with no transparency; `ng build` of the store and the admin complete
  with the icon files; `flutter build apk --debug` succeeds with the adaptive/monochrome resources; `flutter analyze` clean and
  `flutter test` 40/40.
- **Not verified:** on a real phone launcher or iOS, and in the real browser tab or home-screen shortcut.
