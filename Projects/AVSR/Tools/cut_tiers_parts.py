"""등급 연출 부품 → Assets/BaseResource/PopupFx (2026-10-12, 시안 mock_tiers_*).

- in/tiers_part_ring.png (검은 바탕 바닥 고리) → fx_tier_ring_1.png
- in/tiers_part_beam.png (검은 바탕 세로 빛기둥) → fx_tier_beam_1.png
  검은 바탕 → 투명 바탕(알파 = 가장 밝은 채널, 색 = 원래 색 / 알파). 부드러운 빛이라 밝기 하한은 낮게(번짐을 살린다).
- 이미 있는 도트 그림을 연출 아틀라스에도 둔다(새로 그리지 않는다):
  HUD 금화 InGameMainUI/goldicon → fx_result_coin_1 · 챕터 별 ChapterHost/ch_star_on → fx_result_tierstar_1
python cut_tiers_parts.py
"""
import os
import shutil

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
BASE = os.path.join(ROOT, 'Assets', 'BaseResource')
DST = os.path.join(BASE, 'PopupFx')
FLOOR = 8

PARTS = {
    'ring': ('tiers_part_ring.png', 'fx_tier_ring_1.png', (256, 80)),
    'beam': ('tiers_part_beam.png', 'fx_tier_beam_1.png', (64, 256)),
}
COPIES = {
    'fx_result_coin_1.png': os.path.join(BASE, 'InGameMainUI', 'goldicon.png'),
    'fx_result_tierstar_1.png': os.path.join(BASE, 'ChapterHost', 'ch_star_on.png'),
}


def black_to_alpha(rgb):
    a = np.asarray(rgb).astype(float)
    a = np.clip((a - FLOOR) / (255 - FLOOR), 0, 1)
    alpha = a.max(axis=2, keepdims=True)
    color = np.where(alpha > 0, a / np.maximum(alpha, 1e-6), 0)
    return Image.fromarray(np.concatenate([color * 255, alpha * 255], axis=2).astype(np.uint8), 'RGBA')


def main():
    for src, dst, size in PARTS.values():
        im = Image.open(os.path.join(EX, src)).convert('RGB').resize(size, Image.LANCZOS)
        black_to_alpha(im).save(os.path.join(DST, dst))
        print(dst, size)
    for dst, src in COPIES.items():
        shutil.copyfile(src, os.path.join(DST, dst))
        print(dst, '<-', os.path.relpath(src, ROOT))


if __name__ == '__main__':
    main()
