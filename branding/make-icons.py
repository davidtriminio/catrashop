"""
Generates every app icon from one drawing: the favicons of the two web apps and the launcher icons of the mobile app.

    python branding/make-icons.py            # needs only Pillow:  pip install pillow

The mark is a shopping bag wrapped by an orbit ring, like a planet ("Galactic"), on the brand colours from
`frontend/shared/theme.scss` / `mobile/lib/core/theme.dart`:
  - store  : near-black tile, lime mark   -> frontend/public
  - admin  : lime tile, near-black mark   -> frontend/projects/commercehub-admin/public and the mobile app
To change the look, edit the constants below (or draw_mark / svg) and run the script again; nothing else needs touching.
Everything is drawn at 4x and scaled down, so edges are smooth at every size.
"""
import io
import json
import math
import os
import struct
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))

INK = (11, 11, 12)          # --primary
LIME = (200, 241, 105)      # --accent
VARIANTS = {
    'store': {'bg': INK, 'fg': LIME},
    'admin': {'bg': LIME, 'fg': INK},
}

# --- the drawing, in unit coordinates (0..1 across the tile) ----------------------------------------------
TILE_RADIUS = 0.22                    # corner radius of the rounded tile
BAG = (0.26, 0.42, 0.74, 0.80)        # bag body: x0, y0, x1, y1
BAG_RADIUS = 0.07
HANDLE_BOX = (0.375, 0.22, 0.625, 0.56)   # ellipse the handle is the top half of
HANDLE_W = 0.06
HANDLE_X = (0.40, 0.60)               # where the handle meets the bag
RING_CENTER = (0.5, 0.58)
RING_RX, RING_RY = 0.43, 0.15         # the orbit
RING_W = 0.042
RING_HALO = 0.026                     # gap that separates the ring from the bag where it passes in front
RING_TILT = 22                        # degrees, the ring rises to the right
STAR = (0.80, 0.25, 0.075)            # sparkle: x, y, radius
STAR_MIN_SIZE = 48                    # smaller than this the sparkle is just noise, so it is left out
SS = 4                                # supersampling


def _ring(n, color, half, width, erase=False):
    """One half of the tilted ring on its own layer: 'back' is the upper arc, 'front' the lower one."""
    layer = Image.new('RGBA', (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    cx, cy = RING_CENTER[0] * n, RING_CENTER[1] * n
    box = (cx - RING_RX * n, cy - RING_RY * n, cx + RING_RX * n, cy + RING_RY * n)
    start, end = (180, 360) if half == 'back' else (0, 180)
    d.arc(box, start, end, fill=color, width=max(1, int(width)))
    return layer.rotate(RING_TILT, resample=Image.BICUBIC, center=(cx, cy))


def _sparkle(d, n, color):
    cx, cy, r = STAR[0] * n, STAR[1] * n, STAR[2] * n
    pts = []
    for i in range(8):
        a = math.radians(-90 + i * 45)
        rr = r if i % 2 == 0 else r * 0.26
        pts.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
    d.polygon(pts, fill=color)


def draw_mark(n, fg, bg, star, transparent):
    """The mark on an n x n transparent canvas. With `transparent` the gap around the front of the ring is cut out of
    the mark (for layers that sit over another background); otherwise it is painted in `bg` (for an opaque tile)."""
    im = Image.new('RGBA', (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    w = RING_W * n
    im.alpha_composite(_ring(n, fg, 'back', w))

    x0, y0, x1, y1 = (v * n for v in BAG)
    d.rounded_rectangle((x0, y0, x1, y1), radius=BAG_RADIUS * n, fill=fg)
    hw = int(HANDLE_W * n)
    d.arc(tuple(v * n for v in HANDLE_BOX), 180, 360, fill=fg, width=hw)
    for x in HANDLE_X:
        d.ellipse((x * n - hw / 2, y0 - hw / 2 + 0.004 * n, x * n + hw / 2, y0 + hw / 2 + 0.004 * n), fill=fg)

    halo = _ring(n, (0, 0, 0, 255) if transparent else bg + (255,), 'front', w + 2 * RING_HALO * n)
    if transparent:   # erase: keep the pixels of `im` only where the halo is empty
        alpha = Image.eval(halo.getchannel('A'), lambda a: 255 - a)
        im.putalpha(Image.composite(im.getchannel('A'), Image.new('L', (n, n), 0), alpha))
    else:
        im.alpha_composite(halo)
    im.alpha_composite(_ring(n, fg, 'front', w))
    if star:
        _sparkle(ImageDraw.Draw(im), n, fg)
    return im


def render(size, variant, mode):
    """mode: 'rounded' (tile with transparent corners), 'full' (opaque square, the OS rounds it),
    'foreground' (only the mark, smaller, for Android adaptive icons) or 'mono' (that mark in black, for themed icons)."""
    v = VARIANTS[variant]
    big = size * SS
    star = size >= STAR_MIN_SIZE
    if mode in ('foreground', 'mono'):
        # Adaptive icons keep ~66% of the canvas safe from masking: the mark sits well inside it.
        mark = draw_mark(big, v['fg'] if mode == 'foreground' else (0, 0, 0), v['bg'], star, transparent=True)
        inner = int(big * 0.64)
        mark = mark.resize((inner, inner), Image.LANCZOS)
        im = Image.new('RGBA', (big, big), (0, 0, 0, 0))
        im.alpha_composite(mark, ((big - inner) // 2, (big - inner) // 2))
    else:
        im = Image.new('RGBA', (big, big), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        if mode == 'rounded':
            d.rounded_rectangle((0, 0, big - 1, big - 1), radius=TILE_RADIUS * big, fill=v['bg'])
        else:
            d.rectangle((0, 0, big, big), fill=v['bg'])
        im.alpha_composite(draw_mark(big, v['fg'], v['bg'], star, transparent=False))
    im = im.resize((size, size), Image.LANCZOS)
    return im.convert('RGB') if mode == 'full' else im


def svg(variant, size=64):
    """The same drawing as a vector (used as favicon.svg)."""
    v = VARIANTS[variant]
    hexc = lambda c: '#%02x%02x%02x' % c
    fg, bg = hexc(v['fg']), hexc(v['bg'])
    s = size
    cx, cy = RING_CENTER[0] * s, RING_CENTER[1] * s
    rx, ry = RING_RX * s, RING_RY * s
    back = 'M%.2f %.2fA%.2f %.2f 0 0 1 %.2f %.2f' % (cx - rx, cy, rx, ry, cx + rx, cy)
    front = 'M%.2f %.2fA%.2f %.2f 0 0 0 %.2f %.2f' % (cx - rx, cy, rx, ry, cx + rx, cy)
    x0, y0, x1, y1 = (c * s for c in BAG)
    hx0, hy0, hx1, hy1 = (c * s for c in HANDLE_BOX)
    hrx, hry, hcx, hcy = (hx1 - hx0) / 2, (hy1 - hy0) / 2, (hx0 + hx1) / 2, (hy0 + hy1) / 2
    handle = 'M%.2f %.2fA%.2f %.2f 0 0 1 %.2f %.2f' % (hcx - hrx, hcy, hrx, hry, hcx + hrx, hcy)
    sx, sy, sr = STAR[0] * s, STAR[1] * s, STAR[2] * s
    star = ' '.join('%.2f,%.2f' % (sx + (sr if i % 2 == 0 else sr * 0.26) * math.cos(math.radians(-90 + i * 45)),
                                   sy + (sr if i % 2 == 0 else sr * 0.26) * math.sin(math.radians(-90 + i * 45))) for i in range(8))
    tilt = 'transform="rotate(%d %.2f %.2f)"' % (-RING_TILT, cx, cy)
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 %d %d" width="%d" height="%d">'
        '<rect width="%d" height="%d" rx="%.2f" fill="%s"/>'
        '<path d="%s" %s fill="none" stroke="%s" stroke-width="%.2f"/>'
        '<rect x="%.2f" y="%.2f" width="%.2f" height="%.2f" rx="%.2f" fill="%s"/>'
        '<path d="%s" fill="none" stroke="%s" stroke-width="%.2f" stroke-linecap="round"/>'
        '<path d="%s" %s fill="none" stroke="%s" stroke-width="%.2f"/>'
        '<path d="%s" %s fill="none" stroke="%s" stroke-width="%.2f"/>'
        '<polygon points="%s" fill="%s"/></svg>\n') % (
        s, s, s, s, s, s, TILE_RADIUS * s, bg,
        back, tilt, fg, RING_W * s,
        x0, y0, x1 - x0, y1 - y0, BAG_RADIUS * s, fg,
        handle, fg, HANDLE_W * s,
        front, tilt, bg, (RING_W + 2 * RING_HALO) * s,
        front, tilt, fg, RING_W * s,
        star, fg)


def ico_bytes(variant, sizes=(16, 32, 48)):
    """A .ico holding one natively drawn PNG per size (so the small ones are crisp and carry no sparkle)."""
    images = []
    for s in sizes:
        buf = io.BytesIO()
        render(s, variant, 'rounded').save(buf, format='PNG')
        images.append((s, buf.getvalue()))
    out = struct.pack('<HHH', 0, 1, len(images))
    offset = 6 + 16 * len(images)
    body = b''
    for s, data in images:
        out += struct.pack('<BBBBHHII', s, s, 0, 0, 1, 32, len(data), offset + len(body))
        body += data
    return out + body


def save(im, *path):
    full = os.path.join(ROOT, *path)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    im.save(full)
    print('  wrote', os.path.relpath(full, ROOT))


def write(text, *path, binary=False):
    full = os.path.join(ROOT, *path)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    if binary:
        with open(full, 'wb') as f:
            f.write(text)
    else:
        with open(full, 'w', encoding='utf-8', newline='\n') as f:
            f.write(text)
    print('  wrote', os.path.relpath(full, ROOT))


def web(variant, public):
    print(f'web ({variant}) -> {"/".join(public)}')
    write(ico_bytes(variant), *public, 'favicon.ico', binary=True)
    write(svg(variant), *public, 'favicon.svg')
    save(render(180, variant, 'full'), *public, 'apple-touch-icon.png')


def android(variant):
    res = ['mobile', 'android', 'app', 'src', 'main', 'res']
    print('android launcher icon')
    for name, px in {'mdpi': 48, 'hdpi': 72, 'xhdpi': 96, 'xxhdpi': 144, 'xxxhdpi': 192}.items():
        save(render(px, variant, 'rounded'), *res, f'mipmap-{name}', 'ic_launcher.png')       # Android 7 and older
    for name, px in {'mdpi': 108, 'hdpi': 162, 'xhdpi': 216, 'xxhdpi': 324, 'xxxhdpi': 432}.items():
        save(render(px, variant, 'foreground'), *res, f'mipmap-{name}', 'ic_launcher_foreground.png')
        save(render(px, variant, 'mono'), *res, f'mipmap-{name}', 'ic_launcher_monochrome.png')
    write('<?xml version="1.0" encoding="utf-8"?>\n'
          '<adaptive-icon xmlns:android="http://schemas.android.com/apk/res/android">\n'
          '    <background android:drawable="@color/ic_launcher_background"/>\n'
          '    <foreground android:drawable="@mipmap/ic_launcher_foreground"/>\n'
          '    <monochrome android:drawable="@mipmap/ic_launcher_monochrome"/>\n'
          '</adaptive-icon>\n', *res, 'mipmap-anydpi-v26', 'ic_launcher.xml')
    bg = '#%02X%02X%02X' % VARIANTS[variant]['bg']
    write('<?xml version="1.0" encoding="utf-8"?>\n<resources>\n'
          f'    <color name="ic_launcher_background">{bg}</color>\n</resources>\n', *res, 'values', 'ic_launcher_background.xml')


def ios(variant):
    print('ios app icon')
    folder = ['mobile', 'ios', 'Runner', 'Assets.xcassets', 'AppIcon.appiconset']
    with open(os.path.join(ROOT, *folder, 'Contents.json'), encoding='utf-8') as f:
        images = json.load(f)['images']
    for entry in images:
        side = float(entry['size'].split('x')[0]) * int(entry['scale'].rstrip('x'))
        save(render(round(side), variant, 'full'), *folder, entry['filename'])  # iOS icons must have no transparency


def previews():
    print('previews')
    for variant in VARIANTS:
        save(render(512, variant, 'rounded'), 'branding', f'{variant}.png')
        write(svg(variant), 'branding', f'{variant}.svg')


if __name__ == '__main__':
    web('store', ['frontend', 'public'])
    web('admin', ['frontend', 'projects', 'commercehub-admin', 'public'])
    android('admin')   # the mobile app is the admin app
    ios('admin')
    previews()
    print('done')
