# Branding — app icons

One drawing, `make-icons.py`, produces every icon the project ships. Nothing else is edited by hand.

| App | Look | Files written |
|---|---|---|
| Store (`frontend`) | blue tile, white mark | `frontend/public/` `favicon.ico`, `favicon.svg`, `apple-touch-icon.png` |
| Admin (`frontend/projects/catrashop-admin`) | light-blue tile, blue mark | the same three files in its `public/` |
| Mobile app (it *is* the admin app) | light-blue tile, blue mark | Android: `mipmap-*/ic_launcher.png` (Android 7 and older), `ic_launcher_foreground.png` + `ic_launcher_monochrome.png`, `mipmap-anydpi-v26/ic_launcher.xml` (adaptive icon), `values/ic_launcher_background.xml`; iOS: every size in `AppIcon.appiconset` |

The mark is a **shopping cart with a ringed planet above the basket**: the cart says "online store" at a glance and the planet
nods to the "Galactic" demo brand. `store.png/.svg` and `admin.png/.svg` in this folder are previews of the two variants. The colours are the brand tokens
(`--primary #003399`, `--accent #dbe6ff`).

## Regenerate
```bash
pip install pillow
python branding/make-icons.py
```
Needs only Python and Pillow. To change the design edit the constants at the top of the script (colours, cart and planet geometry, tile
rounding) or `draw_mark`/`svg`, and run it again.

## Using a different logo
The mark is a placeholder drawn in code. To use a designed logo, replace `draw_mark` (and `svg`) with your artwork — or paste your own PNG in `render()` — and keep the rest: the script already
knows every size, the Android adaptive-icon safe zone (the mark stays inside the central 66%) and iOS's requirement that
icons have no transparency.

## Notes
- Web: the SVG is used by modern browsers, the `.ico` (16/32/48 px) by older ones and `apple-touch-icon.png` (180 px) by
  iOS "Add to Home Screen". Browsers cache favicons hard: after deploying, hard-reload or open the site in a private window.
- Android: after reinstalling, some launchers keep the old icon until the app is uninstalled or the launcher restarts.
  Android 13+ can show the themed (monochrome) icon.
- The notification icon still uses the launcher icon (`@mipmap/ic_launcher`); a dedicated white silhouette would look
  better in the status bar.
