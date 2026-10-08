"""C 「원혼 회로」 부품 납품(in/setc_{이름}.png, 마젠타 바탕) → 게임 크기 PNG (2026-10-08).

마젠타를 뚫고(cut_skill_parts.key_magenta) · 그림 영역만 잘라 · 게임 크기(order_set_c_parts.PARTS 의 w x h) 안에 비율 유지로 맞추고 · 가운데 둔다.
결과는 _exchange/out/setc/{이름}.png — 게임 반영은 유니티가 빌 때 따로(Assets 로 복사).
python fit_set_c_parts.py [이름 ...]
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from cut_skill_parts import key_magenta  # noqa: E402
from order_set_c_parts import PARTS  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
OUT = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'out', 'setc')


def fit(name):
    src = os.path.join(IN, 'setc_%s.png' % name)
    if not os.path.exists(src):
        print('없음', name)
        return None
    w, h = PARTS[name][2]
    rgba = Image.fromarray(key_magenta(np.asarray(Image.open(src).convert('RGB'))), 'RGBA')
    box = rgba.getchannel('A').point(lambda a: 255 if a > 24 else 0).getbbox()
    if box is None:
        print('빈 그림', name)
        return None
    art = rgba.crop(box)
    scale = min(w / art.width, h / art.height)
    size = (max(1, round(art.width * scale)), max(1, round(art.height * scale)))
    art = art.resize(size, Image.LANCZOS)
    canvas = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    canvas.alpha_composite(art, ((w - size[0]) // 2, (h - size[1]) // 2))
    os.makedirs(OUT, exist_ok=True)
    canvas.save(os.path.join(OUT, '%s.png' % name))
    print(name, 'art', box, '->', size, 'in', (w, h))
    return canvas


if __name__ == '__main__':
    for n in (sys.argv[1:] or list(PARTS)):
        fit(n)
