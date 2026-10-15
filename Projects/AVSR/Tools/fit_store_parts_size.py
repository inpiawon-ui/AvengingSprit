# -*- coding: utf-8 -*-
"""크게 줄여 쓰는 부품을 화면 크기의 약 2배로 미리 줄인다(2026-10-15).

코덱스 납품은 크기가 제각각이다 — 등급 배지 147px 를 화면 22px 로 쓰면 점 필터에서 도트가 깨지고(반짝임)
아틀라스 · 메모리만 먹는다. 알파를 곱해 줄여(premultiplied) 가장자리가 검게 번지지 않게 한다.
그리지 않는다 — 크기만 바꾼다. 원본은 `_exchange/in/` 시트에 그대로 있다.
"""
import os

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.abspath(os.path.join(HERE, '..', '_exchange', 'out'))

# 파일 → 줄일 폭(px). 화면 폭의 약 2배
TARGET = {
    'parts1/ch_card_frame_b.png': 200, 'parts1/ch_card_frame_a.png': 200, 'parts1/ch_card_frame_s.png': 200,
    'parts1/ch_card_sel_overlay.png': 216,
    'parts1/ch_grade_b.png': 48, 'parts1/ch_grade_a.png': 48, 'parts1/ch_grade_s.png': 48,
    'parts1/ch_today_band.png': 192,
    'parts1/ch_star_half.png': 32,
    'parts1/ch_lock.png': 48,
    'parts2/topbar/icon_stamina.png': 40,
    'parts2/topbar/icon_season_crown.png': 40,
    'parts2/topbar/icon_reddot.png': 24,
    'parts2/bottombar/nav_icon_summon.png': 120,
}


def shrink(path, width):
    im = Image.open(path).convert('RGBA')
    if im.width <= width:
        return None
    h = max(1, round(im.height * width / im.width))
    a = np.asarray(im).astype(np.float32) / 255.0
    pm = a.copy()
    pm[..., :3] *= pm[..., 3:4]
    small = np.stack([np.asarray(Image.fromarray((pm[..., c] * 255).astype(np.uint8)).resize((width, h), Image.LANCZOS),
                                 dtype=np.float32) / 255.0 for c in range(4)], axis=-1)
    alpha = np.clip(small[..., 3:4], 1e-4, 1)
    rgb = np.where(small[..., 3:4] > 0.004, small[..., :3] / alpha, 0)
    out = np.dstack([np.clip(rgb, 0, 1), np.clip(small[..., 3:4], 0, 1)])
    Image.fromarray((out * 255).round().astype(np.uint8)).save(path)
    return im.size, (width, h)


if __name__ == '__main__':
    for rel, w in TARGET.items():
        p = os.path.join(OUT, rel)
        if not os.path.exists(p):
            print('없음', rel)
            continue
        r = shrink(p, w)
        print(rel, r if r else '이미 작음')
