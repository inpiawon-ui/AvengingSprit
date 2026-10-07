# -*- coding: utf-8 -*-
"""돌진 예고 부품 판(마젠타)을 게임 낱장으로 자른다 (2026-10-07 — 시안 A 꺾쇠 + B 흙먼지 통과).

판 : 위 줄 왼쪽 칸에 꺾쇠 하나, 아래 줄 256 x 256 칸 넷에 흙먼지 4컷.
결과 : Assets/BaseResource/InGameMainUI/fx_rush_chevron.png · fx_rush_dust_1~4.png
  · 꺾쇠는 잉크만 잘라 64 x 64 칸 가운데에 — 게임이 띠 폭에 맞춰 키운다
  · 흙먼지 넷은 **같은 칸(256)을 같은 배율로** 줄인다 — 컷마다 크기가 커졌다 작아지는 것이 곧 움직임이다
  · 먼지 알갱이 사이 분홍 테(마젠타가 섞인 픽셀)도 지운다

쓰는 법: python rush_fx_cut.py [납품 판]
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from unit_up_cut import key_out  # noqa: E402

ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
SRC = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'rush_fx', 'rush_parts_raw.png')
DST = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')
CHEV = 64
DUST = 96


def unpink(im):
    a = np.asarray(im).copy()
    r, g, b = a[..., 0].astype(int), a[..., 1].astype(int), a[..., 2].astype(int)
    # 마젠타가 섞인 알갱이 — 흙(r > g > b)과 꺾쇠(빨강 · 노랑)는 파랑이 초록보다 낮다. 파랑이 초록을 넘으면 분홍 테다
    pink = (b > g + 12) & (r > g + 30)
    a[pink, 3] = 0
    return Image.fromarray(a)


def main(path=SRC):
    src = Image.open(path).convert('RGB')
    w, h = src.size
    rgba = unpink(key_out(src))

    top = rgba.crop((0, 0, w // 4, h // 2))
    a = np.asarray(top)[..., 3] > 40
    ys, xs = np.nonzero(a)
    ink = top.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
    k = (CHEV - 4) / max(ink.size)
    ink = ink.resize((max(1, round(ink.width * k)), max(1, round(ink.height * k))), Image.NEAREST)
    out = Image.new('RGBA', (CHEV, CHEV), (0, 0, 0, 0))
    out.alpha_composite(ink, ((CHEV - ink.width) // 2, (CHEV - ink.height) // 2))
    out.save(os.path.join(DST, 'fx_rush_chevron.png'))
    print('fx_rush_chevron', out.size)

    for i in range(4):
        cell = rgba.crop((i * w // 4, h // 2, (i + 1) * w // 4, h))
        cell = cell.resize((DUST, DUST), Image.NEAREST)
        cell.save(os.path.join(DST, f'fx_rush_dust_{i + 1}.png'))
        print(f'fx_rush_dust_{i + 1}', cell.size)


if __name__ == '__main__':
    main(*sys.argv[1:])
