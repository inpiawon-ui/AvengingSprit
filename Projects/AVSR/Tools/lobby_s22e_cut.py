# -*- coding: utf-8 -*-
"""로비 S22e — 받은 빈 판(_exchange/in/lobby_plate_s22e.png)에서 칸 부품을 오린다(2026-10-15).

그리지 않는다. 코덱스가 칸 좌표를 시안대로 못 옮겨서(챕터 칸 · 보스 칸이 옛 자리), 칸을 늘려 쓰는 부품으로 오려
유니티에서 S22e 좌표대로 놓는다. 배경은 칸 없는 판(lobby_backdrop_s22e.png)을 따로 받는다.

  ch_s22e_box       — 보상 칸 하나(테두리 바깥은 투명). 9칸 늘리기용(테두리 32)
  ch_s22e_tick      — 제목 줄 노란 막대(칸 속 남색과 함께 — 칸 위에 얹으면 이음매가 없다)
  ch_s22e_hline     — 제목 줄 아래 가로 구분선(남색 포함, 가로로 늘린다)
  ch_s22e_vline     — 설명 칸 세로 구분선
  ch_s22e_bossframe — 보스 칸 안 그림 액자
"""
import os

import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
SRC = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in', 'lobby_plate_s22e.png')
OUT = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'out', 'lobby_s22e')


def border_mask(rgb):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    return (b > 150) & (g > 110) & (r < 170)


def box(rgb, x0, y0, x1, y1):
    """테두리 바깥(가장자리와 이어진 비테두리 칸)을 투명하게."""
    c = rgb[y0:y1, x0:x1]
    wall = border_mask(c)
    wall = ndimage.binary_dilation(wall, iterations=1)
    free = ~wall
    lab, _ = ndimage.label(free)
    edge = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    outside = np.isin(lab, list(edge))
    a = np.where(outside, 0, 255).astype(np.uint8)
    # 테두리 밖 배경 반짝이가 점으로 딸려 온다 — 가장 큰 덩어리(칸)만 남긴다
    lab2, n2 = ndimage.label(a > 0)
    if n2 > 1:
        big = 1 + int(np.argmax(ndimage.sum(np.ones_like(lab2), lab2, range(1, n2 + 1))))
        a[lab2 != big] = 0
        outside = a == 0
    # 테두리 바로 바깥 1칸은 반투명으로 — 네온 번짐이 뚝 끊기지 않게
    soft = ndimage.binary_dilation(~outside, iterations=1) & outside
    a[soft] = 110
    return Image.fromarray(np.dstack([c, a]).astype(np.uint8))


def main():
    os.makedirs(OUT, exist_ok=True)
    rgb = np.array(Image.open(SRC).convert('RGB'))
    parts = {
        'ch_s22e_box': box(rgb, 567, 602, 1071, 798),
        'ch_s22e_tick': Image.fromarray(rgb[828:876, 208:236]),
        'ch_s22e_hline': Image.fromarray(rgb[874:885, 420:820]),
        'ch_s22e_bossframe': box(rgb, 200, 622, 386, 781),
    }
    # 설명 칸 세로선 — x 를 재서 오린다
    col = rgb[1360:1470, 380:470].astype(int)
    hot = np.where((col[..., 2] > 150).sum(axis=0) > 50)[0]
    vx = 380 + int(hot.mean()) if len(hot) else 425
    parts['ch_s22e_vline'] = Image.fromarray(rgb[1355:1475, vx - 6:vx + 7])
    for name, im in parts.items():
        im.save(os.path.join(OUT, name + '.png'))
        print(name, im.size)


if __name__ == '__main__':
    main()
