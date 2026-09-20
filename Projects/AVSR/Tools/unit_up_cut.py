# -*- coding: utf-8 -*-
"""퀄업 납품 판(3×3)을 게임 규격 낱장으로 자른다.

규격(`.claude/project/constants.md`): 한 칸 96×96 · 잉크 높이 76 · 발밑은 아래에서 8px · 발 중심 x=48.
납품은 마젠타(#FF00FF) 배경에 정사각 한 장, 3×3 배치. 칸 차례는 발주서와 같다.

  1 대기   2 걷기1   3 걷기2
  4 공격1  5 공격2   6 피격
  7 쓰러짐1 8 쓰러짐2  9 빈 칸

쓰는 법:
  python unit_up_cut.py <키> <방향> <납품 판>      # 예: python unit_up_cut.py gangster s out_grid_s_raw.png
  결과: Projects/AVSR/_exchange/ref/char_up/cut/unit_{키}_{방향}[_{동작}].png

⚠ 잉크 높이를 76 으로 맞춘다. 납품마다 인물 키가 조금씩 다른데, 그대로 넣으면 걸을 때
  키가 들쭉날쭉해 보인다(방향이 바뀔 때마다 커졌다 작아진다).
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "Projects/AVSR/_exchange/ref/char_up/cut"
CELL = 96
INK_H = 76       # 잉크 높이(지금 게임 그림과 같게)
FOOT_UP = 8      # 발밑은 칸 아래에서 8px
FOOT_X = 48      # 발 중심
ACTS = ["", "_walk1", "_walk2", "_atk1", "_atk2", "_hit", "_die1", "_die2"]
# 쓰러진 자세는 키가 아니라 **누운 길이**라 76 으로 늘리면 거인이 된다 — 폭을 기준으로 맞춘다
LYING = {"_die2"}


def key_out(im):
    """마젠타 배경을 뚫고, 가장자리에 번진 분홍 기운을 뺀다."""
    a = np.asarray(im.convert("RGB")).astype(np.int16)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    dist = np.abs(a - np.array([255, 0, 255])).sum(axis=2)
    pink = (r > 120) & (b > 120) & (r - g > 45) & (b - g > 45)
    alpha = np.where((dist < 150) | pink, 0, 255).astype(np.uint8)
    rgb = a.copy()
    spill = (alpha > 0) & (r - g > 20) & (b - g > 20)
    rgb[spill, 0] = np.minimum(rgb[spill, 0], g[spill] + 20)
    rgb[spill, 2] = np.minimum(rgb[spill, 2], g[spill] + 20)
    return Image.fromarray(np.dstack([rgb.astype(np.uint8), alpha]))


def fit_cell(cell, lying):
    """잉크를 규격 크기로 줄이고 발 기준으로 칸에 앉힌다."""
    a = np.asarray(cell)[..., 3]
    ys, xs = np.nonzero(a > 8)
    if len(xs) == 0:
        return None
    ink = cell.crop((int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1))
    if lying:
        w = min(84, ink.width)
        h = max(1, round(ink.height * w / ink.width))
    else:
        h = INK_H
        w = max(1, round(ink.width * h / ink.height))
    small = ink.resize((w, h), Image.LANCZOS)
    # 반투명 가장자리는 도트답게 자른다(원본도 알파가 0 아니면 255 다)
    q = np.asarray(small).astype(np.uint8).copy()
    q[..., 3] = np.where(q[..., 3] > 110, 255, 0)
    small = Image.fromarray(q)

    aa = np.asarray(small)[..., 3]
    rows = np.nonzero(aa.any(axis=1))[0]
    fy = rows.max()
    foot = aa[max(0, fy - 4):fy + 1] > 8
    fx = np.nonzero(foot.any(axis=0))[0]
    fcx = (fx.min() + fx.max() + 1) / 2

    out = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    out.alpha_composite(small, (int(round(FOOT_X - fcx)), CELL - FOOT_UP - small.height))
    return out


def main():
    key, facing, sheet = sys.argv[1], sys.argv[2], Path(sys.argv[3])
    im = key_out(Image.open(sheet))
    saved = {}
    side = im.width // 3
    OUT.mkdir(parents=True, exist_ok=True)
    for i, act in enumerate(ACTS):
        r, c = divmod(i, 3)
        cell = im.crop((c * side, r * side, (c + 1) * side, (r + 1) * side)).resize((CELL, CELL), Image.LANCZOS)
        fitted = fit_cell(cell, act in LYING)
        if fitted is None:
            print(f"{i + 1}칸 비었다 — {act or 'idle'}")
            continue
        name = f"unit_{key}_{facing}{act}.png"
        fitted.save(OUT / name)
        a = np.asarray(fitted)[..., 3]
        ys, xs = np.nonzero(a > 8)
        print(f"{name}  잉크 {xs.max() - xs.min() + 1}×{ys.max() - ys.min() + 1}  발밑 {CELL - 1 - ys.max()}px")
        saved[act] = np.asarray(fitted)[..., 3] > 8

    # ── 자동 검수 ────────────────────────────────────────────
    # 지금 게임 그림이 내는 «움직인 양» 과 견준다. 너무 작으면 걸을 때 미끄러지듯 보인다.
    game = ROOT / "Assets/BaseResource/Unit" / key
    def diff(p, q):
        return int((p ^ q).sum()) if p is not None and q is not None else -1

    def load_game(act):
        f = game / f"unit_{key}_{facing}{act}.png"
        return np.asarray(Image.open(f).convert("RGBA"))[..., 3] > 8 if f.exists() else None

    checks = [("걷기1↔걷기2", "_walk1", "_walk2"), ("대기↔피격", "", "_hit"), ("대기↔공격", "", "_atk1")]
    bad = []
    for label, a1, a2 in checks:
        mine = diff(saved.get(a1), saved.get(a2))
        theirs = diff(load_game(a1), load_game(a2))
        mark = "" if theirs < 0 or mine >= theirs * 0.6 else "  ← 너무 작다(빠꾸)"
        if mark:
            bad.append(label)
        print(f"검수 {label}: 퀄업 {mine}px / 지금 {theirs}px{mark}")

    # 쓰러짐은 «뼈만 남는» 연출이라 흰 뼈가 있어야 한다 — 초록 양복만 남으면 사람이 넘어진 그림이다
    die2 = OUT / f"unit_{key}_{facing}_die2.png"
    if die2.exists():
        px = np.asarray(Image.open(die2).convert("RGBA"))
        ink = px[..., 3] > 8
        bone = ink & (px[..., 0] > 170) & (px[..., 1] > 150) & (px[..., 2] > 120)
        ratio = bone.sum() / max(1, ink.sum())
        ok = ratio > 0.12
        print(f"검수 쓰러짐2 뼈 비율 {ratio * 100:.0f}%{'' if ok else '  ← 뼈가 없다(빠꾸)'}")
        if not ok:
            bad.append("쓰러짐2")

    print("저장:", OUT)
    if bad:
        print("‼ 다시 받아야 할 칸:", ", ".join(bad))


if __name__ == "__main__":
    main()

