"""화면별 연출 부품 시트(in/fxs_{화면}_{부품}.png) → fx_{화면}_{부품}_{n}.png (2026-10-08).

칸 크기 · 장 수 · 열 수는 order_fx_screen_parts.SCREENS 와 같은 표. 마젠타는 거리 키(key_magenta)로 뚫는다(제 색 그대로).
결과 _exchange/out/fxs/{화면}/ — 게임 반영은 유니티가 빌 때 Assets/BaseResource/InGameMainUI/ 로.
python cut_fx_screen_parts.py {화면}
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from cut_skill_parts import key_magenta  # noqa: E402
from order_fx_screen_parts import SCREENS  # noqa: E402
from glhx_parts_fit import unmix_magenta  # noqa: E402

# 반투명 빛이 마젠타와 섞여 오는 부품 — 거리 키로 뚫으면 가장자리가 분홍으로 남는다. 마젠타를 빼서 푼다(2026-10-08 확인)
UNMIX = {'clear_title_burst', 'clear_coin_rain'}

ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')


def cut(screen):
    out = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'out', 'fxs', screen)
    os.makedirs(out, exist_ok=True)
    for name, (_, w, h, n, cols, _, _) in SCREENS[screen].items():
        path = os.path.join(IN, 'fxs_%s_%s.png' % (screen, name))
        if not os.path.exists(path):
            print('없음', name)
            continue
        rows = (n + cols - 1) // cols
        sheet = Image.open(path).convert('RGB').resize((w * cols, h * rows), Image.NEAREST)
        for i in range(n):
            c = sheet.crop(((i % cols) * w, (i // cols) * h, (i % cols + 1) * w, (i // cols + 1) * h))
            cut_img = unmix_magenta(c) if '%s_%s' % (screen, name) in UNMIX else Image.fromarray(key_magenta(np.asarray(c)), 'RGBA')
            cut_img.save(os.path.join(out, 'fx_%s_%s_%d.png' % (screen, name, i + 1)))
        print(screen, name, n)


if __name__ == '__main__':
    cut(sys.argv[1])
