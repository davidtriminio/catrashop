"""
Generates every app icon from one drawing: the favicons of the two web apps and the launcher icons of the mobile app.

    python branding/make-icons.py            # needs only Pillow:  pip install pillow

The mark is a shopping cart with a ringed planet floating above the basket ("Galactic" e-commerce), on the brand colours
from `frontend/shared/theme.scss` / `mobile/lib/core/theme.dart`:
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
TILE_RADIUS = 0.22                                                   # corner radius of the rounded tile
CART_LINE = [(0.15, 0.34), (0.26, 0.34), (0.36, 0.66), (0.78, 0.66)]  # handle, down the side, along the base
CART_LINE_W = 0.055
BASKET = [(0.29, 0.42), (0.84, 0.42), (0.76, 0.60), (0.35, 0.60)]
WHEELS = [(0.43, 0.78), (0.70, 0.78)]
WHEEL_R = 0.06
PLANET = (0.60, 0.24, 0.09)           # centre x, y and radius of the ball
PLANET_RING = (0.17, 0.05, 20)        # ring half-width, half-height, tilt in degrees (rises to the right)
PLANET_RING_W = 0.026
PLANET_HALO = 0.014                   # gap that separates the front of the ring from the ball
SS = 4                                # supersampling


def _ring(n, color, half, width):
    """One half of the planet's tilted ring on its own layer: 'back' is the upper arc, 'front' the lower one."""
    cx, cy, _ = PLANET
    rx, ry, tilt = PLANET_RING
    layer = Image.new('RGBA', (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    box = ((cx - rx) * n, (cy - ry) * n, (cx + rx) * n, (cy + ry) * n)
    start, end = (180, 360) if half == 'back' else (0, 180)
    d.arc(box, start, end, fill=color, width=max(1, int(width)))
    return layer.rotate(tilt, resample=Image.BICUBIC, center=(cx * n, cy * n))


def draw_mark(n, fg, bg, transparent):
    """The mark on an n x n transparent canvas. With `transparent` the gap around the front of the ring is cut out of
    the mark (for layers that sit over another background); otherwise it is painted in `bg` (for an opaque tile)."""
    im = Image.new('RGBA', (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)

    # the cart
    w = int(CART_LINE_W * n)
    pts = [(x * n, y * n) for x, y in CART_LINE]
    d.line(pts, fill=fg, width=w, joint='curve')
    for x, y in (pts[0], pts[-1]):
        d.ellipse((x - w / 2, y - w / 2, x + w / 2, y + w / 2), fill=fg)
    d.polygon([(x * n, y * n) for x, y in BASKET], fill=fg)
    for x, y in WHEELS:
        r = WHEEL_R * n
        d.ellipse((x * n - r, y * n - r, x * n + r, y * n + r), fill=fg)

    # the planet: back of the ring, the ball, then the front of the ring with a small gap around it
    px, py, pr = PLANET
    rw = PLANET_RING_W * n
    im.alpha_composite(_ring(n, fg, 'back', rw))
    d.ellipse(((px - pr) * n, (py - pr) * n, (px + pr) * n, (py + pr) * n), fill=fg)
    halo = _ring(n, (0, 0, 0, 255) if transparent else bg + (255,), 'front', rw + 2 * PLANET_HALO * n)
    if transparent:   # erase: keep the pixels of `im` only where the halo is empty
        alpha = Image.eval(halo.getchannel('A'), lambda a: 255 - a)
        im.putalpha(Image.composite(im.getchannel('A'), Image.new('L', (n, n), 0), alpha))
    else:
        im.alpha_composite(halo)
    im.alpha_composite(_ring(n, fg, 'front', rw))
    return im


def render(size, variant, mode):
    """mode: 'rounded' (tile with transparent corners), 'full' (opaque square, the OS rounds it),
    'foreground' (only the mark, smaller, for Android adaptive icons) or 'mono' (that mark in black, for themed icons)."""
    v = VARIANTS[variant]
    big = size * SS
    if mode in ('foreground', 'mono'):
        # Adaptive icons keep ~66% of the canvas safe from masking: the mark sits well inside it.
        mark = draw_mark(big, v['fg'] if mode == 'foreground' else (0, 0, 0), v['bg'], transparent=True)
        inner = int(big * 0.70)
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
        im.alpha_composite(draw_mark(big, v['fg'], v['bg'], transparent=False))
    im = im.resize((size, size), Image.LANCZOS)
    return im.convert('RGB') if mode == 'full' else im


def svg(variant, size=64):
    """The same drawing as a vector (used as favicon.svg)."""
    v = VARIANTS[variant]
    hexc = lambda c: '#%02x%02x%02x' % c
    fg, bg = hexc(v['fg']), hexc(v['bg'])
    s = size
    line = ' '.join('%.2f,%.2f' % (x * s, y * s) for x, y in CART_LINE)
    basket = ' '.join('%.2f,%.2f' % (x * s, y * s) for x, y in BASKET)
    wheels = ''.join('<circle cx="%.2f" cy="%.2f" r="%.2f" fill="%s"/>' % (x * s, y * s, WHEEL_R * s, fg) for x, y in WHEELS)
    px, py, pr = (c * s for c in PLANET)
    rx, ry = PLANET_RING[0] * s, PLANET_RING[1] * s
    back = 'M%.2f %.2fA%.2f %.2f 0 0 1 %.2f %.2f' % (px - rx, py, rx, ry, px + rx, py)
    front = 'M%.2f %.2fA%.2f %.2f 0 0 0 %.2f %.2f' % (px - rx, py, rx, ry, px + rx, py)
    tilt = 'transform="rotate(%d %.2f %.2f)"' % (-PLANET_RING[2], px, py)
    rw = PLANET_RING_W * s
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 %d %d" width="%d" height="%d">'
        '<rect width="%d" height="%d" rx="%.2f" fill="%s"/>'
        '<polyline points="%s" fill="none" stroke="%s" stroke-width="%.2f" stroke-linecap="round" stroke-linejoin="round"/>'
        '<polygon points="%s" fill="%s"/>%s'
        '<path d="%s" %s fill="none" stroke="%s" stroke-width="%.2f"/>'
        '<circle cx="%.2f" cy="%.2f" r="%.2f" fill="%s"/>'
        '<path d="%s" %s fill="none" stroke="%s" stroke-width="%.2f"/>'
        '<path d="%s" %s fill="none" stroke="%s" stroke-width="%.2f"/></svg>\n') % (
        s, s, s, s, s, s, TILE_RADIUS * s, bg,
        line, fg, CART_LINE_W * s,
        basket, fg, wheels,
        back, tilt, fg, rw,
        px, py, pr, fg,
        front, tilt, bg, rw + 2 * PLANET_HALO * s,
        front, tilt, fg, rw)


def ico_bytes(variant, sizes=(16, 32, 48)):
    """A .ico holding one natively drawn PNG per size, so the small ones stay crisp."""
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
