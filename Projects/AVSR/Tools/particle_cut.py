# -*- coding: utf-8 -*-
"""파티클 텍스처 납품 판을 낱장 PNG 로 자른다.

⚠ 배경을 **밝기로 뚫으면 안 된다.** 빛 알갱이(glow)는 한가운데가 흰색이라 같이 지워진다.
  대신 **테두리에서 흘려 채운다**(flood fill) — 칸 가장자리에 닿아 있는 배경색 덩어리만
  지우므로, 그림 안쪽의 같은 색은 그대로 남는다.

쓰는 법:
  python particle_cut.py <납품 판> <가로칸> <세로칸> <결과 한 변> <이름1> <이름2> ... [--apply]

  예) python particle_cut.py out_particles_raw.png 2 2 64 spark smoke glow shard --apply
  --apply 를 주면 Assets/BaseResource/ParticleFx/ 에 넣는다.

⚠ 결과는 **정사각 캔버스에 가운데 정렬**이다. 파티클 판은 정사각이라 그림이 가로로 길면
  늘어나 버린다 — 여백째로 넣어야 비율이 산다.
"""
import shutil
import sys
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
GAME = ROOT / "Assets/BaseResource/ParticleFx"
OUT = ROOT / "Projects/AVSR/_exchange/ref/particle/cut"

TOL = 30          # 배경색에서 이만큼 안쪽이면 같은 배경으로 본다
HARD = 110        # 줄인 뒤 알파를 굳히는 문턱


def background_mask(rgb):
    """칸 테두리에 닿은 배경 덩어리를 찾는다."""
    h, w = rgb.shape[:2]
    seed = rgb[0, 0].astype(np.int16)          # 모서리 색을 배경색으로 본다
    close = (np.abs(rgb.astype(np.int16) - seed).sum(axis=2) <= TOL)

    out = np.zeros((h, w), bool)
    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            if close[y, x] and not out[y, x]:
                out[y, x] = True
                q.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if close[y, x] and not out[y, x]:
                out[y, x] = True
                q.append((y, x))
    while q:
        y, x = q.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and close[ny, nx] and not out[ny, nx]:
                out[ny, nx] = True
                q.append((ny, nx))
    return out


def cut_cell(cell, size):
    rgb = np.asarray(cell.convert("RGB"))
    alpha = np.where(background_mask(rgb), 0, 255).astype(np.uint8)
    px = np.dstack([rgb, alpha])

    ys, xs = np.nonzero(alpha > 0)
    if len(xs) == 0:
        return Image.new("RGBA", (size, size), (0, 0, 0, 0)), 0, 0
    ink = px[ys.min():ys.max() + 1, xs.min():xs.max() + 1]

    # 알파를 곱해 줄인다 — 배경색이 가장자리로 번지지 않게
    pm = ink.astype(np.float32)
    pm[..., :3] *= pm[..., 3:4] / 255.0
    h, w = ink.shape[:2]
    long_side = max(w, h)
    scale = size / long_side
    small = np.asarray(Image.fromarray(pm.astype(np.uint8), "RGBA")
                       .resize((max(1, round(w * scale)), max(1, round(h * scale))), Image.BOX)).astype(np.float32)
    a = small[..., 3:4]
    col = np.where(a > 0, small[..., :3] * 255.0 / np.maximum(a, 1e-3), 0)
    hard = (a[..., 0] >= HARD) * 255
    img = Image.fromarray(np.dstack([np.clip(col, 0, 255).astype(np.uint8), hard.astype(np.uint8)]), "RGBA")

    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    out.paste(img, ((size - img.width) // 2, (size - img.height) // 2))
    return out, img.width, img.height


def main():
    sheet = Path(sys.argv[1])
    cols, rows, size = int(sys.argv[2]), int(sys.argv[3]), int(sys.argv[4])
    names = [a for a in sys.argv[5:] if not a.startswith("--")]
    apply = "--apply" in sys.argv

    im = Image.open(sheet).convert("RGB")
    cw, ch = im.width // cols, im.height // rows
    OUT.mkdir(parents=True, exist_ok=True)

    made = []
    for i, name in enumerate(names):
        if name == "-":
            continue
        r, c = divmod(i, cols)
        out, w, h = cut_cell(im.crop((c * cw, r * ch, (c + 1) * cw, (r + 1) * ch)), size)
        out.save(OUT / f"{name}.png")
        made.append(name)
        print(f"{name:8s} {size}x{size} 캔버스 · 잉크 {w}x{h}")

    if apply:
        GAME.mkdir(parents=True, exist_ok=True)
        for name in made:
            shutil.copy2(OUT / f"{name}.png", GAME / f"{name}.png")
        print("게임에 반영:", GAME, f"({len(made)}장)")
    else:
        print("저장:", OUT, "— 게임에 넣으려면 --apply")


if __name__ == "__main__":
    main()
