"""결과창 연출 부품(in/rcfx_{이름}.png, 검은 바탕 · 2배 도트) → Assets/BaseResource/PopupFx/fx_result_{이름}_{n}.png (2026-10-09).

검은 바탕 빛 → 투명 바탕 빛: 밝기 하한(FLOOR)을 깎아 바깥의 어두운 판을 지우고, 알파 = 가장 밝은 채널, 색 = 원래 색 / 알파.
게임은 가산 재질로 그리므로 결과가 검은 바탕 그대로 더한 것과 같다(어두운 판만 빠진다).
2배 도트를 반으로(가장 가까운 점) 줄여 게임 도트 크기로.
칸 크기 · 장 수는 order_result_fx_parts.py 표와 같다.
python cut_result_fx_parts.py [이름 ...]
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from order_result_fx_parts import PARTS  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
DST = os.path.join(ROOT, 'Assets', 'BaseResource', 'PopupFx')
FLOOR = 62   # 바깥 어두운 판(구름 모양 테두리)이 이 아래 — 42 는 게임에서 테두리가 남았다(녹화 vE)


def black_to_alpha(rgb):
    a = np.asarray(rgb).astype(float)
    a = np.clip((a - FLOOR) / (255 - FLOOR), 0, 1)
    alpha = a.max(axis=2, keepdims=True)
    color = np.where(alpha > 0, a / np.maximum(alpha, 1e-6), 0)
    out = np.concatenate([color * 255, alpha * 255], axis=2).astype(np.uint8)
    return Image.fromarray(out, 'RGBA')


def main(only=None):
    for name, (w, h, n, cols, *_rest) in PARTS.items():
        if only and name not in only:
            continue
        src = os.path.join(EX, 'in', 'rcfx_%s.png' % name)
        if not os.path.exists(src):
            print('없음', name)
            continue
        rows = (n + cols - 1) // cols
        sheet = Image.open(src).convert('RGB').resize((w * cols, h * rows), Image.NEAREST)
        for i in range(n):
            c = sheet.crop(((i % cols) * w, (i // cols) * h, (i % cols + 1) * w, (i // cols + 1) * h))
            f = black_to_alpha(c).resize((w // 2, h // 2), Image.NEAREST)
            f.save(os.path.join(DST, 'fx_result_%s_%d.png' % (name, i + 1)))
        print(name, n)


if __name__ == '__main__':
    main(sys.argv[1:] or None)
