# -*- coding: utf-8 -*-
"""위험 예고 그림 납품(_exchange/ref/danger_fx/fx2_*.png) → Assets/BundleResource/ParticleFx/Danger/fx_*.png.

2차(2026-10-07, PD 확정 「테두리는 E · 안쪽은 D」) — 얇은 이중선 테두리 + 안쪽으로 번지는 그라데이션.
  채움 · 차오름 4장은 그대로 옮긴다(거의 단색 — 게임이 아주 옅게 얹는다).
  테두리 띠 2장은 맨 아래 빈 줄을 잘라 낸다 — 띠 그림의 아래 끝이 곧 도형 경계라야 선이 경계에 붙는다.
그다음 Unity 에서 `Tools/Game/위험 예고 그림 들이기` 를 돌린다(주소 Fx/*).
"""
import os

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
SRC = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'danger_fx')
DST = os.path.join(ROOT, 'Assets', 'BundleResource', 'ParticleFx', 'Danger')

if __name__ == '__main__':
    os.makedirs(DST, exist_ok=True)
    for k in ['danger_fill', 'danger_core', 'safe_fill', 'safe_core']:
        Image.open(os.path.join(SRC, 'fx2_%s.png' % k)).convert('RGBA').save(os.path.join(DST, 'fx_%s.png' % k))
    for k in ['danger_edge', 'safe_edge']:
        im = Image.open(os.path.join(SRC, 'fx2_%s.png' % k)).convert('RGBA')
        a = np.asarray(im)
        rows = np.nonzero(a[..., 3].max(axis=1) > 40)[0]
        bottom = int(rows.max()) + 1 if len(rows) else im.size[1]
        im.crop((0, 0, im.size[0], bottom)).save(os.path.join(DST, 'fx_%s.png' % k))
        print(k, 'bottom', bottom)
    print('ok')
