# -*- coding: utf-8 -*-
"""퀄업 유닛 그림 검수 — 납품 시트를 규격대로 자르고, 지금 그림과 나란히 붙여 보여 준다.

규격(`.claude/project/constants.md`): 한 칸 96×96 · 발밑은 아래에서 8px · 발 중심 x=48 · 마젠타 배경.

쓰는 법:
  python unit_up_check.py <납품 시트> <칸 수> [--apply]

  검수만: 칸마다 잉크 크기 · 발밑 · 발 중심 · 헤일로(반투명 테두리)를 재서 표로 찍는다.
  --apply: 마젠타를 뚫고 발 기준으로 맞춰 `char_up/cut/` 에 낱장으로 저장한다.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "Projects/AVSR/_exchange/ref/char_up"
CELL = 96
FOOT_UP = 8      # 발밑은 칸 아래에서 8px 위
FOOT_X = 48      # 발 중심은 칸 가로 48px


def key_out_magenta(im):
    """마젠타(#FF00FF) 배경을 뚫는다. 가장자리의 반투명 자국(헤일로)도 함께 깎는다.

    이미 투명한 그림(지금 게임 그림)은 그 알파를 그대로 쓴다 — 다시 뚫으면 온 칸이 잉크가 된다.
    """
    if im.mode == "RGBA" and np.asarray(im)[..., 3].min() < 250:
        return im
    a = np.asarray(im.convert("RGB")).astype(np.int16)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    dist = np.abs(a - np.array([255, 0, 255])).sum(axis=2)
    # 배경 = 순수 마젠타에 가깝거나, 분홍으로 섞인 가장자리(빨강 · 파랑이 초록보다 훨씬 높다)
    pink = (r > 120) & (b > 120) & (r - g > 45) & (b - g > 45)
    alpha = np.where((dist < 150) | pink, 0, 255).astype(np.uint8)
    # 남은 가장자리의 마젠타 기운을 뺀다(초록을 넘지 않게 눌러 준다)
    rgb = a.copy()
    spill = (alpha > 0) & (r - g > 20) & (b - g > 20)
    rgb[spill, 0] = np.minimum(rgb[spill, 0], g[spill] + 20)
    rgb[spill, 2] = np.minimum(rgb[spill, 2], g[spill] + 20)
    return Image.fromarray(np.dstack([rgb.astype(np.uint8), alpha]))


def measure(cell):
    a = np.asarray(cell)[..., 3]
    ys, xs = np.nonzero(a > 8)
    if len(xs) == 0:
        return None
    # 발 중심은 **맨 아래 6줄**(발)로 잰다 — 몸 전체로 재면 뻗은 팔 · 총이 중심을 끌고 간다
    foot_rows = (a > 8)[max(0, ys.max() - 5):ys.max() + 1]
    fxs = np.nonzero(foot_rows.any(axis=0))[0]
    return dict(w=int(xs.max() - xs.min() + 1), h=int(ys.max() - ys.min() + 1),
                foot=int(CELL - 1 - ys.max()), cx=float((fxs.min() + fxs.max() + 1) / 2),
                halo=int(((a > 8) & (a < 200)).sum()))


def main():
    sheet = Path(sys.argv[1])
    cells = int(sys.argv[2])
    apply = "--apply" in sys.argv
    im = Image.open(sheet)
    if im.size != (CELL * cells, CELL):
        im = im.resize((CELL * cells, CELL), Image.NEAREST)
        print(f"※ 시트 크기가 {Image.open(sheet).size} 라 {CELL * cells}×{CELL} 로 맞췄다")
    keyed = key_out_magenta(im)

    for i in range(cells):
        c = keyed.crop((i * CELL, 0, (i + 1) * CELL, CELL))
        m = measure(c)
        if m is None:
            print(f"{i + 1}칸: 비었다")
            continue
        print(f"{i + 1}칸: 잉크 {m['w']}×{m['h']}  발밑 {m['foot']}px(기준 {FOOT_UP})  "
              f"발중심 {m['cx']:.1f}(기준 {FOOT_X})  반투명 {m['halo']}px")
        if apply:
            (OUT / "cut").mkdir(parents=True, exist_ok=True)
            c.save(OUT / "cut" / f"{sheet.stem}_{i + 1}.png")
    if apply:
        print("저장:", OUT / "cut")


if __name__ == "__main__":
    main()
