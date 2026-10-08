"""C 「원혼 회로」 아이콘 시트(in/setc_icons_{시트}.png, 2x2 마젠타) → 게임 크기 PNG (2026-10-08).

카드: card_cXXX 160 px(레벨업 카드) + buffcard_cXXX 96 px(상점 칸 · 72 px 로 표시).
제단: shrine_* 96 px · 상점: shop_* 96 px. 그림 영역을 잘라 정사각 안에 비율 유지 · 가운데.
결과 _exchange/out/setc/icons/{이름}.png
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from cut_skill_parts import key_magenta  # noqa: E402
from order_set_c_icons import SHEETS  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
OUT = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'out', 'setc', 'icons')
CELL = 512


def square(rgba, size):
    box = rgba.getchannel('A').point(lambda a: 255 if a > 24 else 0).getbbox()
    art = rgba.crop(box)
    side = max(art.width, art.height)
    pad = Image.new('RGBA', (side, side), (0, 0, 0, 0))
    pad.alpha_composite(art, ((side - art.width) // 2, (side - art.height) // 2))
    inner = round(size * 0.94)
    out = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    out.alpha_composite(pad.resize((inner, inner), Image.LANCZOS), ((size - inner) // 2, (size - inner) // 2))
    return out


def main():
    os.makedirs(OUT, exist_ok=True)
    for sheet, (_, items) in SHEETS.items():
        path = os.path.join(IN, 'setc_icons_%s.png' % sheet)
        if not os.path.exists(path):
            print('없음', sheet)
            continue
        s = Image.open(path).convert('RGB').resize((CELL * 2, CELL * 2), Image.NEAREST)
        for i, (name, _) in enumerate(items):
            cell = s.crop(((i % 2) * CELL, (i // 2) * CELL, (i % 2 + 1) * CELL, (i // 2 + 1) * CELL))
            rgba = Image.fromarray(key_magenta(np.asarray(cell)), 'RGBA')
            if name.startswith('card_'):
                square(rgba, 160).save(os.path.join(OUT, name + '.png'))
                square(rgba, 96).save(os.path.join(OUT, 'buff' + name + '.png'))
            else:
                square(rgba, 96).save(os.path.join(OUT, name + '.png'))
            print(sheet, name)


if __name__ == '__main__':
    main()
