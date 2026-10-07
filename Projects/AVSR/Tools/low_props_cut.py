# -*- coding: utf-8 -*-
"""낮은 장애물 8종 판(4 x 2 칸, 마젠타)을 게임 규격 낱장으로 자른다 (2026-10-07).

PD 「위로 긴 것들은 2D 라 애매하다 · 다른 오브젝트로」 — 키 큰 기둥 · 덩어리를 대신할 낮은 엄폐.
그림 캔버스 = 발자국(72 px/m) + 솟음(`BattleDirector.ObstacleRise`). 아래를 발자국에 맞물려 놓으므로
물건을 캔버스 바닥에 붙이고, 폭을 캔버스 폭에 맞춘다(비율 유지).
바닥에 눕는 것(구덩이)은 판정 칸(정사각)을 그림이 채우게 세로를 늘린다 — 보이는 곳 = 막히는 곳.

쓰는 법: python low_props_cut.py [납품 판]
결과  : Assets/BaseResource/InGameMainUI/obj_<종류>.png
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from unit_up_cut import key_out  # noqa: E402

ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
SRC = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'room_layout', 'low_props_raw.png')
DST = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')

# 칸 번호(위 줄 왼쪽부터 0) → (파일 이름, 캔버스 폭, 캔버스 높이, 눕는가)
CELLS = {
    0: ('obj_sandbag', 144, 102, False),
    1: ('obj_barrel_pile', 144, 108, False),
    2: ('obj_pit', 144, 144, True),
    3: ('obj_fallen_pillar', 144, 102, False),
    4: ('obj_scrap_pile', 144, 96, False),
    5: ('obj_drain', 144, 72, True),          # 배수구 철망 — 아직 쓰는 종류가 없다(바닥 장식)
    6: ('obj_jersey_row', 216, 102, False),
    7: ('obj_wreck_car', 144, 102, False),
}


def main(path=SRC):
    rgba = key_out(Image.open(path).convert('RGB'))
    w, h = rgba.size
    for i, (name, cw, ch, flat) in CELLS.items():
        r, c = divmod(i, 4)
        tile = rgba.crop((c * w // 4, r * h // 2, (c + 1) * w // 4, (r + 1) * h // 2))
        a = np.asarray(tile)[..., 3] > 40
        ys, xs = np.nonzero(a)
        crop = tile.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
        if flat:
            nw, nh = cw - 4, ch - 8               # 눕는 것은 칸을 채운다
        else:
            k = min(cw / crop.width, ch / crop.height)
            nw, nh = max(1, round(crop.width * k)), max(1, round(crop.height * k))
        crop = crop.resize((nw, nh), Image.NEAREST)
        out = Image.new('RGBA', (cw, ch), (0, 0, 0, 0))
        y = (ch - nh) // 2 if flat else ch - nh  # 서 있는 것은 바닥에 붙인다
        out.alpha_composite(crop, ((cw - nw) // 2, y))
        out.save(os.path.join(DST, name + '.png'))
        print(name, out.size, 'ink', (nw, nh))


if __name__ == '__main__':
    main(*sys.argv[1:])
