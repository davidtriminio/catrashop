# PR-046 — New app icons: web favicons and the mobile launcher icon

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/app-icons` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Problem
The web apps shipped the default Angular favicon and the mobile app the default Flutter launcher icon, so a browser tab,
a home screen and the app drawer all looked unbranded.

## Changes
- **One generator, `branding/make-icons.py`** (Python + Pillow, nothing else) draws a geometric **H** (for Hub) on the
  brand colours and writes every icon, so the design lives in one place and every size is derived from it:
  - **Store:** near-black tile with a lime H → `frontend/public/` (`favicon.ico` with 16/32/48 px, `favicon.svg`,
    `apple-touch-icon.png`).
  - **Admin web:** lime tile with a near-black H (so the two tabs are told apart) → its own `public/` folder.
  - **Mobile app** (it is the admin app, so it uses the admin look): Android legacy `ic_launcher.png` in the five
    densities, an **adaptive icon** (`mipmap-anydpi-v26/ic_launcher.xml` + foreground layers + background colour), a
    **monochrome layer** for Android 13 themed icons, and the full iOS `AppIcon.appiconset` (opaque, as iOS requires).
- Both `index.html` link the SVG, the ICO and the apple-touch icon, and set `theme-color`; the admin tab title is now
  "CommerceHub Admin" (it said "CommercehubAdmin").
- `branding/README.md` explains how it works, how to regenerate and how to swap in a designed logo; `store`/`admin`
  `.png`/`.svg` previews are kept in `branding/`.

## Things to know
- **The H is a placeholder mark I designed** to match the wordmark (no logo file was provided). Replacing it with a real
  logo only means changing `draw_glyph`/`svg` in the script and running it again.
- Browsers cache favicons aggressively and some Android launchers keep the old icon: hard-reload / private window, and
  uninstall + reinstall the app (or restart the launcher) to see the change.
- The notification small icon still uses the launcher icon; a white silhouette would look better in the status bar.

## Verification
- Generated 41 files; sizes and modes checked (iOS icons are RGB with no transparency; `favicon.ico` contains 16/32/48;
  the 1024 px marketing icon and the 167 px iPad Pro icon are the right size).
- Looked at the result: both variants, the 16 px favicon enlarged (the H stays readable) and the adaptive icon under a
  circular mask (the H stays inside the safe zone).
- `ng build` of the store and the admin complete; the built output contains the three icon files and the links; served
  statically, `favicon.svg` is delivered as `image/svg+xml` and renders correctly in the browser.
- `flutter build apk --debug` succeeds and the APK contains `mipmap-anydpi-v26/ic_launcher.xml` and the monochrome and
  foreground layers; `flutter analyze` clean and `flutter test` 40/40.
- **Not verified:** how it looks on a real phone launcher (round/squircle masks, themed icon), on iOS (no build), in the
  actual browser tab or in a home-screen shortcut.
