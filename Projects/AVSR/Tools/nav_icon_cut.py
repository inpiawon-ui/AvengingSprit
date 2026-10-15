# -*- coding: utf-8 -*-
"""하단 바 4칸(S22e) — 지금 탭 통그림에서 아이콘만 오린다(2026-10-15). 그리지 않는다.

nav_host_off · nav_play_off · nav_shop_off(359 x 146) 의 왼쪽 아이콘 칸에서, 탭 속 남색과 이어진 칸을 투명하게.
→ Projects/AVSR/_exchange/out/lobby_nav/nav_icon_{host,play,shop}.png (반영 툴이 LobbyMainUI/Nav 로 넣는다)
"""
import os

import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
SRC = os.path.join(ROOT, 'Assets', 'BaseResource', 'Growth')
OUT = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'out', 'lobby_nav')

BOX = (36, 32, 178, 122)   # 아이콘 칸 — 탭 네온 테두리(x<30 · y<30 · y>126) 안쪽, 글자 왼쪽


def cut(name):
    im = np.array(Image.open(os.path.join(SRC, f'nav_{name}_off.png')).convert('RGBA')).astype(np.int32)
    x0, y0, x1, y1 = BOX
    c = im[y0:y1, x0:x1].copy()
    navy = np.array([1, 34, 78])   # 탭 속 남색(실측) — 테두리 쪽은 안쪽 빛으로 푸르게 번져 있다
    d = np.sqrt(((c[..., :3] - navy) ** 2).sum(axis=-1))
    bg = d < 52
    lab, _ = ndimage.label(bg)
    edge = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    outside = np.isin(lab, list(edge))
    c[..., 3] = np.where(outside, 0, c[..., 3])
    img = Image.fromarray(c.astype(np.uint8))
    img = img.crop(img.getbbox())
    os.makedirs(OUT, exist_ok=True)
    img.save(os.path.join(OUT, f'nav_icon_{name}.png'))
    print(name, img.size, 'navy', navy)


if __name__ == '__main__':
    for n in ('host', 'play', 'shop'):
        cut(n)
