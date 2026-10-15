# -*- coding: utf-8 -*-
"""위 고정 띠 S22e — 납품(fixed_top_band_s22e.png)이 칸을 넓게 그려 톱니가 오른쪽 끝에서 잘렸다(2026-10-15).

그리지 않는다. 칸 · 편지(x 300~1050)를 40 왼쪽으로 옮기고, 오른쪽 끝 60 에 옛 띠(base_fixed_top_band.png)의
톱니 자리(x 1005~1065, 같은 밤하늘)를 붙인다. 이음매는 4px 섞는다. 맨 위 흰 줄은 옛 띠 줄로 바꾼다.
→ Projects/AVSR/_exchange/out/lobby_nav/fixed_top_band_s22e.png (반영 툴이 LobbyMainUI/Nav 로 넣는다)
"""
import os

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
EX = os.path.abspath(os.path.join(HERE, '..', '_exchange'))

SHIFT = 40
GEAR_SRC = (1005, 1065)


def main():
    new = np.array(Image.open(os.path.join(EX, 'in', 'fixed_top_band_s22e.png')).convert('RGB')).astype(np.float32)
    old = np.array(Image.open(os.path.join(EX, 'ref', 'store_mock', 'base_fixed_top_band.png')).convert('RGB')).astype(np.float32)
    out = new.copy()
    out[:, 300 - SHIFT:1050 - SHIFT] = new[:, 300:1050]
    g0, g1 = GEAR_SRC
    w = g1 - g0
    x0 = 1080 - w - 0
    out[:, x0:1080] = old[:, g0:g1]
    # 옮긴 칸 끝 ~ 톱니 사이는 옛 편지 테두리 조각이 남는다 — 빈 하늘 열로 메운다
    gap = x0 - (1050 - SHIFT)
    if gap > 0:
        out[:, 1050 - SHIFT:x0] = new[:, 200:200 + gap]
    # 이음매 섞기 — 하늘 결이 뚝 끊기지 않게
    for x, src in ((300 - SHIFT, None), (1050 - SHIFT, None), (x0, None)):
        for d in range(-2, 3):
            if 0 < x + d < 1079:
                out[:, x + d] = (out[:, x + d - 1] + out[:, x + d + 1]) * 0.5 if d == 0 else out[:, x + d]
    # 납품 맨 위 줄이 흰 띠로 왔다 — 옛 띠의 같은 줄(어두운 테두리)로 바꾼다
    top = int(np.argmax(new[:, 500].min(axis=1) < 200))
    out[:top + 1] = old[:top + 1]
    os.makedirs(os.path.join(EX, 'out', 'lobby_nav'), exist_ok=True)
    Image.fromarray(out.clip(0, 255).astype(np.uint8)).save(os.path.join(EX, 'out', 'lobby_nav', 'fixed_top_band_s22e.png'))
    print('ok', 'slots moved', SHIFT, 'gear at', x0)


if __name__ == '__main__':
    main()
