# -*- coding: utf-8 -*-
"""탄·명중·총구 납품 판(정사각 격자)을 게임이 쓰는 낱장으로 자른다.

이펙트(`fx_up_cut.py`)는 검은 배경을 **밝기로** 뚫는다 — 빛은 어두운 데서 서서히 사라지므로
그 편이 맞다. 탄은 다르다. 원작 스프라이트는 **알파 단계가 없고 테두리가 딱 끊긴다.**
밝기로 뚫으면 탄의 어두운 붉은 테두리가 통째로 먹힌다. 그래서 **자홍(#FF00FF) 배경**을
색으로 도려내고 알파를 0/255 로 굳힌다(캐릭터 납품과 같은 방식).

쓰는 법:
  python shot_up_cut.py <납품 판> <가로칸> <세로칸> <칸당 축소배율> <이름1> <이름2> ... [--apply]

  이름은 칸 순서(왼→오, 위→아래)대로 **게임 파일명:잉크 긴 쪽 크기** 로 적는다.
  예) python shot_up_cut.py out_bullet_raw.png 3 3 \
        shot_bullet_1:8 shot_bullet_2:14 shot_bullet_3:20 \
        impact_bullet_1:16 impact_bullet_2:24 impact_bullet_3:22 \
        fx_muzzle_1:30 fx_muzzle_2:40 fx_muzzle_3:26

  ⚠ 배율(1/8 등)을 쓰지 않고 **잉크를 목표 크기에 맞춘다.** 납품 판의 칸 크기가
    요청과 다르게 오기 때문이다(1152 를 달라 해도 1254 로 온다). 잉크 기준으로 맞추면
    칸이 몇 픽셀로 오든 게임에서 보이는 크기가 같다.

  칸을 건너뛰려면 이름 자리에 `-` 를 적는다.
  --apply 를 주면 Assets/BaseResource/InGameMainUI 에 덮어쓴다(원본은 ref/shot_up/old/ 에 남긴다).

⚠ 결과 캔버스 크기는 **지금 게임 파일의 크기를 그대로 따른다.** 파일이 없으면 48×48.
  탄 그림의 캔버스가 바뀌면 화면에서 크기가 튄다 — 게임은 캔버스째로 배치한다.
"""
import shutil
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
GAME = ROOT / "Assets/BaseResource/InGameMainUI"
OUT = ROOT / "Projects/AVSR/_exchange/ref/shot_up/cut"
OLD = ROOT / "Projects/AVSR/_exchange/ref/shot_up/old"

KEY = np.array([255, 0, 255], dtype=np.int16)   # 납품 배경색
KEY_TOL = 120                                   # 자홍에서 이만큼 떨어지면 그림으로 본다


def key_out_magenta(cell):
    """자홍 배경 → 알파 0. 가장자리 반투명은 만들지 않는다(원작은 알파 단계가 없다)."""
    rgb = np.asarray(cell.convert("RGB")).astype(np.int16)
    dist = np.abs(rgb - KEY).sum(axis=2)
    alpha = np.where(dist > KEY_TOL, 255, 0).astype(np.uint8)
    return np.dstack([rgb.astype(np.uint8), alpha])


def shrink(px, w, h):
    """알파를 곱해서 줄인다 — 배경색이 가장자리로 번지지 않게."""
    im = Image.fromarray(px, "RGBA")
    pm = np.asarray(im).astype(np.float32)
    pm[..., :3] *= pm[..., 3:4] / 255.0
    small = np.asarray(Image.fromarray(pm.astype(np.uint8), "RGBA").resize((w, h), Image.BOX)).astype(np.float32)
    a = small[..., 3:4]
    rgb = np.where(a > 0, small[..., :3] * 255.0 / np.maximum(a, 1e-3), 0)
    hard = (a[..., 0] >= 110) * 255        # 알파를 굳힌다 — 픽셀 그림은 반투명 테두리가 없다
    return np.dstack([np.clip(rgb, 0, 255).astype(np.uint8), hard.astype(np.uint8)])


def canvas_of(name):
    p = GAME / f"{name}.png"
    if p.exists():
        im = Image.open(p)
        return im.width, im.height
    return 48, 48


def cut_ink(px):
    """자홍을 걷어낸 칸에서 그림이 있는 네모만 도려낸다."""
    ys, xs = np.nonzero(px[..., 3] > 0)
    if len(xs) == 0:
        return None
    return px[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def place(px, size, longest):
    """잉크의 긴 쪽을 <longest> 로 맞춰 줄이고 캔버스 한가운데에 놓는다."""
    ink = cut_ink(px)
    if ink is None:
        return Image.new("RGBA", size, (0, 0, 0, 0)), 0, 0
    h, w = ink.shape[:2]
    s = longest / max(w, h)
    small = shrink(ink, max(1, round(w * s)), max(1, round(h * s)))
    small = cut_ink(small)                       # 줄이면서 생긴 빈 줄을 다시 턴다
    img = Image.fromarray(small, "RGBA")
    if img.width > size[0] or img.height > size[1]:
        f = min(size[0] / img.width, size[1] / img.height)
        img = img.resize((max(1, int(img.width * f)), max(1, int(img.height * f))), Image.NEAREST)
    out = Image.new("RGBA", size, (0, 0, 0, 0))
    out.paste(img, ((size[0] - img.width) // 2, (size[1] - img.height) // 2))
    return out, img.width, img.height


def main():
    sheet = Path(sys.argv[1])
    cols, rows = int(sys.argv[2]), int(sys.argv[3])
    names = [a for a in sys.argv[4:] if not a.startswith("--")]
    apply = "--apply" in sys.argv

    im = Image.open(sheet).convert("RGB")
    cw, ch = im.width // cols, im.height // rows
    OUT.mkdir(parents=True, exist_ok=True)

    made = []
    for i, entry in enumerate(names):
        if entry == "-":
            continue
        name, longest, canvas = (entry.split(":") + ["", ""])[:3]
        longest = int(longest) if longest else 16
        r, c = divmod(i, cols)
        cell = im.crop((c * cw, r * ch, (c + 1) * cw, (r + 1) * ch))
        # ⚠ 같은 탄의 프레임은 **캔버스가 같아야 한다.** 게임은 캔버스를 한 변 size 인
        #   정사각으로 늘여 그리므로, 캔버스가 다르면 프레임마다 크기가 튄다.
        size = (int(canvas), int(canvas)) if canvas else canvas_of(name)
        out, iw, ih = place(key_out_magenta(cell), size, longest)
        out.save(OUT / f"{name}.png")
        made.append(name)
        print(f"{name:20s} 캔버스 {size[0]}x{size[1]}  잉크 {iw}x{ih}")

    if apply:
        OLD.mkdir(parents=True, exist_ok=True)
        for name in made:
            dst = GAME / f"{name}.png"
            if dst.exists() and not (OLD / f"{name}.png").exists():
                shutil.copy2(dst, OLD / f"{name}.png")   # 되돌릴 수 있게 (Assets 밖에 둔다)
            shutil.copy2(OUT / f"{name}.png", dst)
        print("게임에 반영:", GAME, f"({len(made)}장, 원본은 {OLD})")
    else:
        print("저장:", OUT, "— 게임에 넣으려면 --apply")


if __name__ == "__main__":
    main()
