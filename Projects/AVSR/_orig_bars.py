"""원작 HUD 게이지 → 우리 UI 의 배경/채움 스프라이트.

원작 바는 **한 장에 테두리·채운 부분·빈 부분이 함께** 그려져 있다. 우리 UI 는
`{이름}bg` 위에 `{이름}fill` 을 얹고 fill 의 가로를 줄여 게이지를 표현하므로
두 장으로 나눠야 한다.

  bg    원작 바에서 채운 부분을 **빈 색으로 덮은** 것 — 테두리가 남는다
  fill  테두리 안쪽 높이의 **채움 색 단색 띠** — 가로로 늘어나도 안 뭉개진다

fill 에 테두리를 남기면 가로를 줄일 때 끝 테두리가 같이 눌려 찌그러진다.

사용: python _orig_bars.py
"""
import io
import os
import sys
from collections import Counter

from PIL import Image

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SRC = os.path.join(HERE, '_exchange', 'out', '16_hud')
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')

# 조각 → 우리 UI 이름. 원작 ENERGY 가 우리 고스트 HP, BOSS 가 보스 HP 다.
BARS = [
    ('part_04', 'ghosthpbar'),   # ENERGY  주황
    ('part_06', 'bosshpbar'),    # BOSS    초록
    ('part_08', 'hosthpbar'),    # 보라 — 호스트 HP 로 쓴다
]

# 게이지가 더 필요한 곳(런 EXP·유지 훅)은 원작에 대응이 없다.
# 원작 바의 테두리를 그대로 쓰고 색만 바꿔 같은 언어로 만든다.
DERIVED = [
    ('part_08', 'expbar', (0x5A, 0xC8, 0xF0)),        # 고스트 색
    ('part_08', 'maintainbar', (0xF0, 0xB4, 0x28)),   # 호스트 색
]


def analyse(im):
    """(테두리 제외 위·아래 y, 채움색, 빈색)"""
    px = im.load()
    w, h = im.size

    # 세로로 색이 가장 다양한 열 = 안쪽. 위아래 한 줄씩은 테두리다.
    inner_top, inner_bottom = 1, h - 2

    # 채움 색은 **안쪽 가운데 줄**에서 고른다. 안쪽 전체에서 최빈색을 고르면
    # 위아래 음영 줄이 이겨서 실제보다 어두운 색이 잡힌다.
    mid = (inner_top + inner_bottom) // 2
    left = Counter(px[x, mid][:3] for x in range(2, w // 3))
    right = Counter(px[x, mid][:3] for x in range(w * 2 // 3, w - 2))
    return inner_top, inner_bottom, left.most_common(1)[0][0], right.most_common(1)[0][0]


def build(src_name, out_name, tint=None):
    """
    원작 바를 두 장으로 나눈다.

    색을 하나 골라 단색으로 칠하면 위아래 음영이 사라져 원작 느낌이 죽는다.
    **원작의 세로 그라데이션을 그대로 오려** 쓴다 — 가로로 늘어나도 세로 음영은
    유지된다. `tint` 를 주면 밝기를 유지한 채 색상만 옮긴다(원작에 대응이 없는 게이지).
    """
    im = Image.open(os.path.join(SRC, src_name + '.png')).convert('RGBA')
    w, h = im.size
    top, bottom, _, _ = analyse(im)
    px = im.load()

    # 채움 띠 — 왼쪽(채워진 쪽) 안쪽을 오린다
    fill = im.crop((6, top, 14, bottom + 1))
    if tint:
        fp = fill.load()
        for y in range(fill.height):
            for x in range(fill.width):
                r, g, b, al = fp[x, y]
                lum = (r * 299 + g * 587 + b * 114) / 255000.0     # 0~1
                fp[x, y] = (int(tint[0] * lum), int(tint[1] * lum),
                            int(tint[2] * lum), al)
    fill.save(os.path.join(OUT, out_name + 'fill.png'))

    # 배경 — 안쪽을 오른쪽(빈 쪽) 무늬로 덮는다. 테두리는 그대로.
    bg = im.copy()
    bp = bg.load()
    src_x = w - 8
    for y in range(top, bottom + 1):
        for x in range(2, w - 2):
            if bp[x, y][3] > 8:
                bp[x, y] = px[src_x, y]
    bg.save(os.path.join(OUT, out_name + 'bg.png'))

    print(f'{out_name:<16} {w}x{h}  안쪽 {bottom-top+1}px'
          + (f'  색조 #{tint[0]:02X}{tint[1]:02X}{tint[2]:02X}' if tint else '  원작 색'))


def main():
    os.makedirs(OUT, exist_ok=True)
    for src, name in BARS:
        build(src, name)
    for src, name, rgb in DERIVED:
        build(src, name, rgb)
    print(f'\n→ {OUT}')


if __name__ == '__main__':
    main()
