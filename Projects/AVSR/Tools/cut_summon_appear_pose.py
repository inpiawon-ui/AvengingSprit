# -*- coding: utf-8 -*-
"""등장 자세 납품(appear_{키}.png, 분홍 바탕 · 크기 제각각) → 큰 그림 칸(384)에 맞춰 넣기.
분홍을 뚫고, 서기 그림(unitL_{키}_se)의 몸 높이 · 발 자리 · 가로 가운데에 맞춘다(같은 화면에서 자세만 바뀌게).
저장: Assets/BaseResource/UnitLarge/{키}/unitL_{키}_se_appear.png  (out_dir 인자를 주면 거기)
쓰는 법: python cut_summon_appear_pose.py <키> [out_dir]
"""
import os
import sys

import numpy as np
from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))


def ink(a):
    ys, xs = np.nonzero(a[..., 3] > 0)
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1


def main(key, out_dir=None):
    src = np.asarray(Image.open(os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in', f'appear_{key}.png')).convert('RGBA')).copy()
    r, g, b = (src[..., i].astype(int) for i in range(3))
    src[(r > 150) & (b > 150) & (g < 120) & (r - g > 80) & (b - g > 80), 3] = 0
    se = np.asarray(Image.open(os.path.join(ROOT, 'Assets', 'BaseResource', 'UnitLarge', key, f'unitL_{key}_se.png')).convert('RGBA'))
    sx0, sy0, sx1, sy1 = ink(se)
    x0, y0, x1, y1 = ink(src)
    body = Image.fromarray(src).crop((x0, y0, x1, y1))
    # 몸 높이를 서기와 같게(자세가 낮으면 조금 작아 보이는 게 맞다 — 서기 높이의 1.0 배를 넘기지 않게 높이 기준)
    s = (sy1 - sy0) / body.height
    body = body.resize((max(1, round(body.width * s)), max(1, round(body.height * s))), Image.NEAREST)
    a = np.asarray(body).copy()
    a[..., 3] = np.where(a[..., 3] > 110, 255, 0)
    body = Image.fromarray(a)
    out = Image.new('RGBA', se.shape[1::-1], (0, 0, 0, 0))
    cx = (sx0 + sx1) // 2
    out.alpha_composite(body, (max(0, min(out.width - body.width, cx - body.width // 2)), sy1 - body.height))
    d = out_dir or os.path.join(ROOT, 'Assets', 'BaseResource', 'UnitLarge', key)
    p = os.path.join(d, f'unitL_{key}_se_appear.png')
    out.save(p)
    print(p, '서기 잉크', (sx0, sy0, sx1, sy1), '자세 크기', body.size)


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else None)
