"""원작 시트 → 워크오더에 붙일 앵커 이미지.

지금까지 워크오더는 **설명문**이었다. "원작 갱스터는 정장에 권총" 같은 문장으로
그리게 하니 류가 나오기도 했고, 나온 그림이 원작과 얼마나 닮았는지 대조할
기준도 없었다.

원작 시트가 생겼으니 워크오더를 **그림 + 색표**로 바꾼다. 각 캐릭터마다
  · 배경을 지운 시트 2배   — 인물·복장·비율·동작 구성을 그대로 본다
  · 팔레트 띠              — 쓸 수 있는 색이 이것뿐임을 못박는다
두 장을 만들어 둔다.

배경 키는 시트마다 다르다(초록·회청·베이지). 네 모서리에서 가장 흔한 색을 잡는다.
오른쪽 아래 "Ripped by" 표는 캐릭터가 아니므로 지운다.

사용: python _orig_anchor.py [이름조각 ...]
"""
import io
import os
import sys
from collections import Counter

from PIL import Image

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
SHEETS = os.path.join(HERE, 'Reference', 'Original')
OUT = os.path.join(HERE, '_exchange', 'out', '15_original')

ZOOM = 2
WATERMARK_BOTTOM = 0.22
WATERMARK_RIGHT = 0.60
MIN_PIXELS = 8          # 이보다 드문 색은 압축 잔색이지 팔레트가 아니다


def bg_key(im):
    px = im.load()
    w, h = im.size
    c = Counter()
    for x, y in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1),
                 (1, 1), (w - 2, 1), (1, h - 2), (w - 2, h - 2)):
        c[px[x, y]] += 1
    return c.most_common(1)[0][0]


def build(path, name):
    im = Image.open(path).convert('RGB')
    px = im.load()
    w, h = im.size
    key = bg_key(im)
    y0 = int(h * (1 - WATERMARK_BOTTOM))
    x0 = int(w * WATERMARK_RIGHT)

    # 배경 → 투명, 워터마크 → 투명
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    op = out.load()
    counts = Counter()
    for y in range(h):
        for x in range(w):
            if y >= y0 and x >= x0:
                continue
            p = px[x, y]
            if p == key:
                continue
            op[x, y] = p + (255,)
            counts[p] += 1

    # 남은 영역만 잘라 낸다 — 워터마크를 지우고 나면 아래가 비어 있다
    box = out.getbbox()
    if box:
        out = out.crop(box)
    big = out.resize((out.width * ZOOM, out.height * ZOOM), Image.NEAREST)
    big.save(os.path.join(OUT, f'{name}__sheet.png'))

    colors = [rgb for rgb, n in counts.most_common() if n >= MIN_PIXELS]
    cell, per = 20, 12
    rows = (len(colors) + per - 1) // per
    strip = Image.new('RGB', (cell * per, cell * max(1, rows)), (24, 24, 32))
    for i, rgb in enumerate(colors):
        for yy in range(cell):
            for xx in range(cell):
                strip.putpixel(((i % per) * cell + xx, (i // per) * cell + yy), rgb)
    strip.save(os.path.join(OUT, f'{name}__palette.png'))
    return len(colors), big.size


def main():
    os.makedirs(OUT, exist_ok=True)
    want = sys.argv[1:]
    files = sorted(f for f in os.listdir(SHEETS) if f.endswith('.png'))
    if want:
        files = [f for f in files if any(w.lower() in f.lower() for w in want)]

    for f in files:
        n, size = build(os.path.join(SHEETS, f), f[:-4])
        print(f'{f[:-4]:<44} 팔레트 {n:>3}색  시트 {size[0]}x{size[1]}')
    print(f'\n→ {OUT}')


if __name__ == '__main__':
    main()
