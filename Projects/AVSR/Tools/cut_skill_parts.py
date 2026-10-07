"""스킬 연출 부품 시트 자르기 (2026-10-07) — order_skill_parts.py 의 표(PARTS)대로.

in/skill_parts_{key}.png(마젠타, 4열 x 2줄 칸) → Assets/BaseResource/InGameMainUI/fx_{이름}_{n}.png(256 x 256).
그림은 고치지 않는다 — 바탕 빼기 · 나누기만.

바탕 빼기:
  · 보통 — 마젠타와 가까운 픽셀은 어디에 있든 투명(고리 안쪽처럼 갇힌 바탕도). 경계는 섞인 만큼 풀어 반투명으로.
    보라 · 자홍 그림(마법)은 순수 마젠타와 거리가 멀어 남는다.
  · 반투명 막(결계 · 얼음 결정) — 전체를 「빛 + 마젠타」로 풀어 비치게(glhx_parts_fit.unmix_magenta).
사용: python cut_skill_parts.py [key ...]   (비우면 시트가 있는 것 전부)
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from glhx_parts_fit import unmix_magenta  # noqa: E402
from order_skill_parts import PARTS  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')
CELL = 256
SHEER = {'gddome', 'isprison', 'vncloud'}   # 비치는 막 · 안개


def key_magenta(rgb):
    c = rgb.astype(np.float32)
    m = np.array([255.0, 0.0, 255.0])
    dist = np.abs(c - m).sum(axis=2)
    t = np.clip((dist - 70.0) / 150.0, 0, 1)
    safe = np.maximum(t, 1e-3)[..., None]
    f = np.where(t[..., None] < 1, (c - (1 - t)[..., None] * m) / safe, c)
    f = np.clip(f, 0, 255)
    t = np.where(t < 0.04, 0, t)
    return np.dstack([f, t * 255]).astype(np.uint8)


def cut(key):
    path = os.path.join(IN, 'skill_parts_%s.png' % key)
    if not os.path.exists(path):
        print('없음', key)
        return
    sheet = Image.open(path).convert('RGB').resize((CELL * 4, CELL * 2), Image.NEAREST)
    for r, row in enumerate(PARTS[key]):
        col = 0
        for name, n, _ in row:
            for i in range(n):
                cell = sheet.crop(((col + i) * CELL, r * CELL, (col + i + 1) * CELL, (r + 1) * CELL))
                rgba = unmix_magenta(cell) if name in SHEER else Image.fromarray(key_magenta(np.asarray(cell)), 'RGBA')
                rgba.save(os.path.join(OUT, 'fx_%s_%d.png' % (name, i + 1)))
            col += n
            print(key, name, n)


if __name__ == '__main__':
    for k in (sys.argv[1:] or list(PARTS)):
        cut(k)
