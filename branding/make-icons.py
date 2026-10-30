"""
Generates every app icon from one drawing: the favicons of the two web apps and the launcher icons of the mobile app.

    python branding/make-icons.py            # needs only Pillow:  pip install pillow

The mark is a geometric "H" (for Hub) on the brand colours from `frontend/shared/theme.scss` / `mobile/lib/core/theme.dart`:
  - store  : near-black tile, lime H      -> frontend/public
  - admin  : lime tile, near-black H      -> frontend/projects/commercehub-admin/public and the mobile app
To change the look, edit the constants below (or draw_glyph) and run the script again; nothing else needs touching.
Everything is drawn at 4x and scaled down, so edges are smooth at every size.
"""
import json
import os
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))

INK = (11, 11, 12)          # --primary
LIME = (200, 241, 105)      # --accent
VARIANTS = {
    'store': {'bg': INK, 'fg': LIME},
    'admin': {'bg': LIME, 'fg': INK},
}

# --- the drawing, in unit coordinates ----------------------------------------------------------------
TILE_RADIUS = 0.22          # corner radius of the rounded tile, as a fraction of its side
GLYPH_W, GLYPH_H = 0.46, 0.50   # the H as a fraction of the tile
STEM = 0.30                 # stem thickness, as a fraction of the H's width
BAR = 0.22                  # crossbar thickness, as a fraction of the H's height
ROUND = 0.05                # corner rounding of the H's strokes, as a fraction of its width
SS = 4                      # supersampling


def glyph_rects(cx, cy, w, h):
    """The three strokes of the H as (x0, y0, x1, y1) rectangles around the centre (cx, cy)."""
    x0, y0 = cx - w / 2, cy - h / 2
    stem, bar = w * STEM, h * BAR
    return [
        (x0, y0, x0 + stem, y0 + h),                       # left stem
        (x0 + w - stem, y0, x0 + w, y0 + h),               # right stem
        (x0 + stem * 0.9, cy - bar / 2, x0 + w - stem * 0.9, cy + bar / 2),  # crossbar
    ]


def draw_glyph(draw, size, color, scale=1.0):
    for r in glyph_rects(size / 2, size / 2, GLYPH_W * size * scale, GLYPH_H * size * scale):
        draw.rounded_rectangle(r, radius=GLYPH_W * size * scale * ROUND, fill=color)


def render(size, variant, mode):
    """mode: 'rounded' (tile with transparent corners), 'full' (opaque square, the OS rounds it),
    'foreground' (only the H, small, for Android adaptive icons) or 'mono' (that H in black, for themed icons)."""
    v = VARIANTS[variant]
    big = size * SS
    if mode in ('foreground', 'mono'):
        im = Image.new('RGBA', (big, big), (0, 0, 0, 0))
        # Adaptive icons keep ~66% of the canvas safe from masking: the H sits well inside it.
        draw_glyph(ImageDraw.Draw(im), big, v['fg'] if mode == 'foreground' else (0, 0, 0), scale=0.72)
    else:
        im = Image.new('RGBA', (big, big), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        if mode == 'rounded':
            d.rounded_rectangle((0, 0, big - 1, big - 1), radius=TILE_RADIUS * big, fill=v['bg'])
        else:
            d.rectangle((0, 0, big, big), fill=v['bg'])
        draw_glyph(d, big, v['fg'])
    im = im.resize((size, size), Image.LANCZOS)
    return im.convert('RGB') if mode == 'full' else im


def svg(variant, size=64):
    v = VARIANTS[variant]
    fill = lambda c: '#%02x%02x%02x' % c
    rects = ''.join(
        '<rect x="%.2f" y="%.2f" width="%.2f" height="%.2f" rx="%.2f" fill="%s"/>' % (
            x0, y0, x1 - x0, y1 - y0, GLYPH_W * size * ROUND, fill(v['fg']))
        for x0, y0, x1, y1 in glyph_rects(size / 2, size / 2, GLYPH_W * size, GLYPH_H * size))
    return ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 %d %d" width="%d" height="%d">'
            '<rect width="%d" height="%d" rx="%.2f" fill="%s"/>%s</svg>\n') % (
        size, size, size, size, size, size, TILE_RADIUS * size, fill(v['bg']), rects)


def save(im, *path):
    full = os.path.join(ROOT, *path)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    im.save(full)
    print('  wrote', os.path.relpath(full, ROOT))


def write(text, *path):
    full = os.path.join(ROOT, *path)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, 'w', encoding='utf-8', newline='\n') as f:
        f.write(text)
    print('  wrote', os.path.relpath(full, ROOT))


def web(variant, public):
    print(f'web ({variant}) -> {"/".join(public)}')
    ico = render(256, variant, 'rounded')
    full = os.path.join(ROOT, *public, 'favicon.ico')
    os.makedirs(os.path.dirname(full), exist_ok=True)
    ico.save(full, format='ICO', sizes=[(16, 16), (32, 32), (48, 48)])
    print('  wrote', os.path.relpath(full, ROOT))
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
