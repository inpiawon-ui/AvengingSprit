"""결과창 상자 빛 부품(in/c2_{이름}.png, 검은 바탕 · 2배 도트) → Assets/BaseResource/PopupFx (2026-10-09).

  chest_rise_a / _b → fx_result_chest_rise_1 / _2   (번갈아 깜빡이는 바닥 빛살)
  mote_star / mote_dot → fx_result_mote_1 / _2      (파티클 입자 — 한 시스템이 두 그림을 섞어 쓴다)
검은 바탕 → 투명: 알파 = 가장 밝은 채널, 색 = 색 / 알파(게임은 가산으로 그려 검은 바탕을 더한 것과 같다).
이번 발주는 「바깥 어두운 판 금지」를 지시했다 — 밝기 하한은 잡티만 지우는 정도(FLOOR).
python cut_chest2_parts.py
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from order_chest2_parts import PARTS  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
DST = os.path.join(ROOT, 'Assets', 'BaseResource', 'PopupFx')
FLOOR = 18
OUT_NAME = {'chest_rise_a': 'chest_rise_1', 'chest_rise_b': 'chest_rise_2', 'chest_halo': 'chest_halo_1', 'mote_star': 'mote_1', 'mote_dot': 'mote_2'}


def black_to_alpha(rgb):
    a = np.asarray(rgb).astype(float)
    a = np.clip((a - FLOOR) / (255 - FLOOR), 0, 1)
    alpha = a.max(axis=2, keepdims=True)
    color = np.where(alpha > 0, a / np.maximum(alpha, 1e-6), 0)
    return Image.fromarray(np.concatenate([color * 255, alpha * 255], axis=2).astype(np.uint8), 'RGBA')


def main():
    for name, (w, h, _) in PARTS.items():
        src = os.path.join(EX, 'in', 'c2_%s.png' % name)
        if not os.path.exists(src):
            print('없음', name)
            continue
        img = Image.open(src).convert('RGB').resize((w, h), Image.NEAREST)
        if name.startswith('chest_rise') or name == 'chest_halo':
            # 흰색으로 그리라 했는데 chest_rise_b 는 파랗게 왔다 — 밝기(가장 밝은 채널)만 남겨 흰 그림으로. 색은 게임이 등급색으로 곱한다
            v = np.asarray(img).max(axis=2)
            img = Image.fromarray(np.stack([v, v, v], axis=2).astype(np.uint8), 'RGB')
        out = black_to_alpha(img).resize((w // 2, h // 2), Image.NEAREST)
        if name.startswith('chest_rise'):
            # chest_rise_b 는 빛살 묶음이 칸 왼쪽으로 치우쳐 그려져 왔다 — 두 장을 번갈아 깜빡이면 빛이 왼쪽으로 쏠린다.
            # 가로 가운데를 칸 가운데에 맞춘다(세로 바닥 줄은 그대로)
            box = out.getbbox()
            if box:
                dx = out.width // 2 - (box[0] + box[2]) // 2
                moved = Image.new('RGBA', out.size, (0, 0, 0, 0))
                moved.alpha_composite(out, (dx, 0)) if dx >= 0 else moved.alpha_composite(out.crop((-dx, 0, out.width, out.height)), (0, 0))
                out = moved
        if name.startswith('mote'):
            # 입자는 칸 안에 작게 그려져 왔다 — 빈 여백을 잘라 정사각으로(파티클 크기 = 보이는 크기)
            box = out.getbbox()
            if box:
                cx, cy = (box[0] + box[2]) / 2, (box[1] + box[3]) / 2
                half = max(box[2] - box[0], box[3] - box[1]) / 2 + 1
                out = out.crop((int(cx - half), int(cy - half), int(cx + half + 0.999), int(cy + half + 0.999)))
        out.save(os.path.join(DST, 'fx_result_%s.png' % OUT_NAME[name]))
        print(name, '→', OUT_NAME[name], out.size)


if __name__ == '__main__':
    main()
