"""보상 연출 부품 시트(in/fxp_{이름}.png) → out/fxp/fx_present_{이름}_{n}.png (2026-10-08).

칸 크기 · 장 수는 order_fx_present.py 표와 같다. 빛 부품은 마젠타를 빼서 풀고(unmix — 가장자리가 분홍으로 안 남게),
불투명한 그림(저주 연기 · 금화 더미 · 구슬)은 거리 키로 뚫는다.
python cut_fx_present.py
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from cut_skill_parts import key_magenta  # noqa: E402
from glhx_parts_fit import unmix_magenta  # noqa: E402
from order_fx_present import items  # noqa: E402

EX = os.path.join(HERE, '..', '_exchange')
OPAQUE = set()   # 전부 마젠타 빼기로 — 거리 키는 금화 · 연기 가장자리에 분홍 테를 남겼다(10-08 실측)


def main():
    out = os.path.join(EX, 'out', 'fxp')
    os.makedirs(out, exist_ok=True)
    for name, w, h, n, cols, _ in items():
        src = os.path.join(EX, 'in', 'fxp_%s.png' % name)
        if not os.path.exists(src):
            print('없음', name)
            continue
        rows = (n + cols - 1) // cols
        sheet = Image.open(src).convert('RGB').resize((w * cols, h * rows), Image.NEAREST)
        for i in range(n):
            c = sheet.crop(((i % cols) * w, (i // cols) * h, (i % cols + 1) * w, (i // cols + 1) * h))
            img = Image.fromarray(key_magenta(np.asarray(c)), 'RGBA') if name in OPAQUE else unmix_magenta(c)
            img.save(os.path.join(out, 'fx_present_%s_%d.png' % (name, i + 1)))
        print(name, n)


if __name__ == '__main__':
    main()
