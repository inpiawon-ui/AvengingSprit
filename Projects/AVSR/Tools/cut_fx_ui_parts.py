"""공통 UI 이펙트 부품 시트(in/fxui_{이름}.png) → fx_ui_{이름}_{1..8}.png (256 x 256) · fx_ui_beam_soft_1.png (2026-10-08).

색 곱하기 부품은 마젠타를 풀어(unmix) 반투명을 살리고 **무채색으로** 만든다 — 게임에서 SetTint 로 금 · 청록 · 진홍 · 호박을 곱한다.
(풀고 나면 가장자리에 분홍 · 연두 기가 남아 색을 곱하면 얼룩진다 — 밝기만 남긴다.)
제 색이 있는 부품(chain_snap · coin_orbit)은 거리 키(key_magenta)로 뚫고, 남은 분홍 기를 빼 금색 쪽으로 돌린다.
결과는 _exchange/out/fxui/ — 게임 반영은 유니티가 빌 때 Assets/BaseResource/InGameMainUI/ 로.
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from cut_skill_parts import key_magenta  # noqa: E402
from glhx_parts_fit import unmix_magenta  # noqa: E402
from order_fx_ui_parts import PARTS, BEAM  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
OUT = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'out', 'fxui')
CELL = 256


def neutral(rgba):
    a = np.asarray(rgba.convert('RGBA')).astype(np.float32)
    lum = a[..., :3].max(axis=2)
    a[..., 0] = a[..., 1] = a[..., 2] = lum
    return Image.fromarray(a.clip(0, 255).astype(np.uint8), 'RGBA')


def unpink(rgba):
    a = np.asarray(rgba).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    pink = (b > g + 30) & (r > g + 30)
    a[..., 2] = np.where(pink, g * 0.4, b)            # 분홍 → 주황 · 금 쪽
    a[..., 3] = np.where(pink & (a[..., 3] < 200), a[..., 3] * 0.5, a[..., 3])
    return Image.fromarray(a.clip(0, 255).astype(np.uint8), 'RGBA')


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, (_, mode) in PARTS.items():
        sheet = Image.open(os.path.join(IN, 'fxui_%s.png' % name)).convert('RGB').resize((CELL * 4, CELL * 2), Image.NEAREST)
        for i in range(8):
            cell = sheet.crop(((i % 4) * CELL, (i // 4) * CELL, (i % 4 + 1) * CELL, (i // 4 + 1) * CELL))
            if mode == 'mul':
                rgba = neutral(unmix_magenta(cell))
            else:
                rgba = unpink(Image.fromarray(key_magenta(np.asarray(cell)), 'RGBA'))
            rgba.save(os.path.join(OUT, 'fx_ui_%s_%d.png' % (name, i + 1)))
        print(name, mode)
    beam = Image.open(os.path.join(IN, 'fxui_%s.png' % BEAM[0])).convert('RGB').resize((CELL * 4, CELL), Image.NEAREST)
    neutral(unmix_magenta(beam)).resize((CELL, 64), Image.LANCZOS).save(os.path.join(OUT, 'fx_ui_%s_1.png' % BEAM[0]))
    print(BEAM[0])


if __name__ == '__main__':
    main()
