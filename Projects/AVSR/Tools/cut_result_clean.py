"""결과창 빛 뗀 그림 · 연출 부품(in/rc_{이름}.png) → out/rc/ (PD 2026-10-08 2차).

그림(금화 더미 · 상자)은 불투명이라 거리 키로 뚫고 가장자리 분홍을 지운다.
빛 부품(반짝임 · 아우라)은 마젠타를 빼서 푼다(unmix — 가산으로 그리므로 밝기가 곧 빛).
반짝임은 6장 뒤에 빈 장 6장을 붙인다 — 반복할 때 「반짝 → 쉼」이 되게(연출 표 층은 쉼 칸이 없다).
python cut_result_clean.py
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from cut_skill_parts import key_magenta  # noqa: E402
from glhx_parts_fit import unmix_magenta  # noqa: E402
from order_result_clean import PARTS  # noqa: E402

EX = os.path.join(HERE, '..', '_exchange')
OUT = os.path.join(EX, 'out', 'rc')
ART = {'goldpile_clean', 'chest_silver_clean', 'chest_gold_clean', 'chest_platinum_clean'}
REST_FRAMES = {'gold_twinkle': 6}
PIXEL = {'pixel_mote'}   # 2배로 그려 받은 도트 — 반으로 줄여(가장 가까운 점) 진짜 도트 크기로


def on_magenta(path):
    """투명 칸이 섞여 온 시트도 마젠타 바탕으로 맞춘다 — 키는 마젠타만 안다."""
    im = Image.open(path).convert('RGBA')
    bg = Image.new('RGBA', im.size, (255, 0, 255, 255))
    bg.alpha_composite(im)
    return bg.convert('RGB')


def clean_art(rgb):
    a = key_magenta(np.asarray(rgb)).copy()
    r, g, b, al = (a[:, :, i].astype(int) for i in range(4))
    # 가장자리에 남은 분홍(빨강 · 파랑이 초록보다 훨씬 큼) — 바탕이 번진 픽셀이라 지운다
    pink = (r - g > 90) & (b - g > 90) & (al > 0)
    a[pink, 3] = 0
    return Image.fromarray(a, 'RGBA')


# 금화 더미 단계 1~3(fx_present_goldpile_stages)의 바닥 줄 — 마지막 장이 이보다 뜨면 바뀌는 순간 들썩인다
PILE_FLOOR = 138


def align_bottom(img, floor):
    box = img.getbbox()
    if box is None:
        return img
    out = Image.new('RGBA', img.size, (0, 0, 0, 0))
    out.alpha_composite(img, (0, floor - box[3]))
    return out


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, (w, h, n, cols, _, _) in PARTS.items():
        src = os.path.join(EX, 'in', 'rc_%s.png' % name)
        if not os.path.exists(src):
            print('없음', name)
            continue
        rows = (n + cols - 1) // cols
        sheet = on_magenta(src).resize((w * cols, h * rows), Image.NEAREST)
        frames = []
        for i in range(n):
            c = sheet.crop(((i % cols) * w, (i // cols) * h, (i % cols + 1) * w, (i // cols + 1) * h))
            f = clean_art(c) if name in ART or name in PIXEL else unmix_magenta(c)
            if name in PIXEL:
                f = f.resize((w // 2, h // 2), Image.NEAREST)
            frames.append(f)
        frames += [Image.new('RGBA', (w, h), (0, 0, 0, 0))] * REST_FRAMES.get(name, 0)
        if name == 'goldpile_clean':
            frames[0] = align_bottom(frames[0], PILE_FLOOR)
        if name in ART:
            frames[0].save(os.path.join(OUT, '%s.png' % name))
        else:
            for i, f in enumerate(frames):
                f.save(os.path.join(OUT, 'fx_result_%s_%d.png' % (name, i + 1)))
        print(name, len(frames))


if __name__ == '__main__':
    main()
