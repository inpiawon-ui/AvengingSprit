"""원작 시트에서 캐릭터별 팔레트를 뽑는다.

**"원작에 충실하다"를 만드는 것은 사실상 팔레트 하나다.** 실루엣이 조금 달라도
색이 같으면 같은 게임으로 읽히고, 색이 다르면 아무리 잘 그려도 남의 게임이 된다.
지금 우리 캐릭터는 21색·74색·84색으로 제각각이라 한 화면에 살지 않는 것처럼 보인다.

원작은 캐릭터마다 33~53색이다. 그 색을 그대로 워크오더에 못박는다.

배경은 단색 키다(시트마다 다르다 — 초록·회색·베이지). 네 모서리에서 가장 흔한
색을 키로 잡는다. 오른쪽 아래 "Ripped by" 표는 캐릭터 색이 아니므로 잘라 낸다.

사용: python _orig_palette.py            모든 시트
      python _orig_palette.py Vampire    이름에 그 말이 든 시트만
"""
import io
import os
import sys
from collections import Counter

from PIL import Image

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
SHEETS = os.path.join(HERE, 'Reference', 'Original')
OUT = os.path.join(HERE, 'Reference', 'Palette')

# "Ripped by" 표는 오른쪽 아래에 붙는다. 시트마다 크기가 조금씩 다르지만
# 아래 22% · 오른쪽 60% 안쪽이면 대부분 걸린다. 캐릭터가 거기까지 오는 시트는 없다.
WATERMARK_BOTTOM = 0.22
WATERMARK_RIGHT = 0.60


def bg_key(im):
    """네 모서리에서 가장 흔한 색 = 배경 키."""
    px = im.load()
    w, h = im.size
    c = Counter()
    for x, y in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1),
                 (1, 1), (w - 2, 1), (1, h - 2), (w - 2, h - 2)):
        c[px[x, y]] += 1
    return c.most_common(1)[0][0]


def palette(path):
    im = Image.open(path).convert('RGB')
    px = im.load()
    w, h = im.size
    key = bg_key(im)

    y0 = int(h * (1 - WATERMARK_BOTTOM))
    x0 = int(w * WATERMARK_RIGHT)

    c = Counter()
    for y in range(h):
        for x in range(w):
            if y >= y0 and x >= x0:
                continue                    # 워터마크 영역
            p = px[x, y]
            if p == key:
                continue
            c[p] += 1
    return key, c


def strip(name, colors, key):
    """팔레트 띠 이미지. 워크오더에 그대로 붙일 수 있게."""
    os.makedirs(OUT, exist_ok=True)
    cell, per = 24, 12
    rows = (len(colors) + per - 1) // per
    img = Image.new('RGB', (cell * per, cell * rows), key)
    for i, (rgb, _) in enumerate(colors):
        for yy in range(cell):
            for xx in range(cell):
                img.putpixel(((i % per) * cell + xx, (i // per) * cell + yy), rgb)
    p = os.path.join(OUT, f'{name}.png')
    img.save(p)
    return p


def main():
    if not os.path.isdir(SHEETS):
        print(f'시트 폴더가 없다: {SHEETS}')
        return
    want = sys.argv[1:]
    files = sorted(f for f in os.listdir(SHEETS) if f.endswith('.png'))
    if want:
        files = [f for f in files if any(w.lower() in f.lower() for w in want)]

    print(f'{"시트":<44}{"배경키":<18}{"색수":>5}  주요 색')
    for f in files:
        key, c = palette(os.path.join(SHEETS, f))
        # 1픽셀짜리 잔색(압축·경계 노이즈)은 팔레트가 아니다
        colors = [(rgb, n) for rgb, n in c.most_common() if n >= 8]
        name = f[:-4]
        strip(name, colors, key)
        top = ' '.join('#%02X%02X%02X' % rgb for rgb, _ in colors[:5])
        print(f'{name:<44}{str(key):<18}{len(colors):>5}  {top}')

    print(f'\n팔레트 띠 → {OUT}')


if __name__ == '__main__':
    main()
