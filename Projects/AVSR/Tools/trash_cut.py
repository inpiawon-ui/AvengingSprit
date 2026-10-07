# -*- coding: utf-8 -*-
"""새 잡몹 납품 판(3 x 3, 마젠타)을 게임 규격 낱장으로 자른다 — 한 방향 아홉 칸을 **한 배율**로.

`unit_up_cut.py` 는 칸마다 잉크 높이를 76 에 맞춘다. 사람 몸은 그래도 되지만, 팔을 머리 위로 든 공격 칸 ·
웅크린 대기 칸처럼 칸마다 높이가 크게 다른 짐승은 칸마다 몸이 커졌다 작아졌다 했다(돌 고릴라 2026-10-07).
여기서는 서 있는 칸(대기 · 걷기 둘 · 피격)의 잉크 높이 중앙값으로 배율 하나를 정해 아홉 칸에 똑같이 쓴다.
판 가장자리에서 이어진 마젠타만 뚫고(옷 색 보호), 몸에서 떨어진 작은 부스러기는 버린다.

쓰는 법: python trash_cut.py <키> <방향> <납품 판>
결과  : Assets/BaseResource/Unit/<게임 키>/unit_<게임 키>_<방향>[_동작].png   (게임 키는 GAME_KEY 표)
"""
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from unit_up_cut import key_out  # noqa: E402

ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
CELL, INK_H, FOOT_UP = 96, 76, 8
ACTS = ['', '_walk1', '_walk2', '_atk1', '_atk2', '_hit', '_die1', '_die2']
# 아홉째 칸을 쓰는 몸 — 두더지의 땅속 흙더미(`_under`)
NINTH = {'mole': '_under'}
STANDING = [0, 1, 2, 5]
# 새 그림 → 게임이 부르는 키. 돌 고릴라는 집행자 자리에 들어간다(키를 바꾸면 방 표 · 챕터 표를 다 고쳐야 한다)
GAME_KEY = {'gorilla': 'actor_enforcer', 'scorpion': 'scrapgunner'}
# 몸 키(잉크 높이). 사람(호스트)은 76 이다. 덩치 큰 짐승은 폭이 넓어 같은 키여도 훨씬 커 보인다 —
# 돌 고릴라 76 은 「너무 크다」(PD 2026-10-07) → 68. 크게 보인 진짜 이유는 옛 집행자의 128 상자에 그려진 것이었다
#   (96 칸이 1.33 배로 커졌다) — 지금은 잡몹 상자(84)에 그린다. 사람보다 조금 낮고 폭이 넓다
BODY_H = {'gorilla': 68, 'boar': 60, 'mole': 58, 'mantis': 64, 'scorpion': 74}   # 멧돼지는 네발짐승 — 키가 낮고 길다(52 는 사람 몸의 반이라 안 보였다)


def cells(path):
    rgba = np.asarray(key_out(Image.open(path).convert('RGB')))
    h, w = rgba.shape[:2]
    out = []
    for i in range(9):
        r, c = divmod(i, 3)
        tile = rgba[r * h // 3:(r + 1) * h // 3, c * w // 3:(c + 1) * w // 3].copy()
        a = tile[..., 3] > 40
        lab, n = ndimage.label(a)
        if n:
            sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
            keep = np.isin(lab, [k + 1 for k, s in enumerate(sizes) if s >= max(30, sizes.max() * 0.02)])
            tile[~keep, 3] = 0
        out.append(tile)
    return out


def bbox(t):
    ys, xs = np.nonzero(t[..., 3] > 40)
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1


def main(key, d, path, out_dir=None):
    tiles = cells(path)
    heights = sorted(bbox(tiles[i])[3] - bbox(tiles[i])[1] for i in STANDING)
    scale = BODY_H.get(key, INK_H) / heights[len(heights) // 2]
    game = GAME_KEY.get(key, key)
    dst = out_dir or os.path.join(ROOT, 'Assets', 'BaseResource', 'Unit', game)
    os.makedirs(dst, exist_ok=True)
    acts = ACTS + ([NINTH[key]] if key in NINTH else [])
    for i, act in enumerate(acts):
        t = tiles[i]
        x0, y0, x1, y1 = bbox(t)
        crop = Image.fromarray(t[y0:y1, x0:x1])
        nw, nh = max(1, round((x1 - x0) * scale)), max(1, round((y1 - y0) * scale))
        if nw > CELL or nh > CELL - FOOT_UP:   # 칸을 넘으면 넘는 칸만 줄인다(드물다)
            k = min(CELL / nw, (CELL - FOOT_UP) / nh)
            nw, nh = int(nw * k), int(nh * k)
        crop = crop.resize((nw, nh), Image.NEAREST)
        canvas = Image.new('RGBA', (CELL, CELL), (0, 0, 0, 0))
        canvas.alpha_composite(crop, ((CELL - nw) // 2, CELL - FOOT_UP - nh))
        canvas.save(os.path.join(dst, f'unit_{game}_{d}{act}.png'))
    print(key, d, 'scale %.3f' % scale, '->', dst)


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4] if len(sys.argv) > 4 else None)
