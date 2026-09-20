# -*- coding: utf-8 -*-
"""이펙트 납품 판(정사각 격자)을 게임이 쓰는 낱장 프레임으로 자른다.

게임은 `fx_{이름}_{n}.png` 를 n=1 부터 찾는다(`BattleDirector.FxFrames`). 시트를 슬라이스하는
코드가 없으므로 **낱장으로 넣어야** 한다.

쓰는 법:
  python fx_up_cut.py <이름> <납품 판> <가로칸> <세로칸> <결과 가로> <결과 세로> [--apply]

  예) 번개 줄기: python fx_up_cut.py boltbeam out_boltbeam_raw.png 2 2 192 64
  --apply 를 주면 Assets/BaseResource/InGameMainUI 에 바로 덮어쓴다(원본은 .bak 으로 남긴다).

⚠ 결과 세로가 칸 세로보다 작으면 **칸 가운데 띠**만 잘라 쓴다 — 가로로 긴 그림(번개 줄기)을
  정사각 칸에 그려 받기 때문이다.
"""
import shutil
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
GAME = ROOT / "Assets/BaseResource/InGameMainUI"
OUT = ROOT / "Projects/AVSR/_exchange/ref/fx_up/cut"


def key_out_black(im, floor=12.0):
    """검은 배경을 뚫는다. 밝기가 곧 알파다 — 빛은 어두운 데서 서서히 사라지기 때문.

    ⚠ <floor> 아래 밝기는 통째로 버린다. 칠해서 받은 판은 배경이 **완전한 검정이 아니라**
      옅은 후광이 깔려 있어, 기본값으로 뚫으면 알파가 5~8% 남아 화면에 **네모난 헤일로**로 뜬다.
    """
    a = np.asarray(im.convert("RGB")).astype(np.float32)
    lum = a.max(axis=2)
    alpha = np.clip((lum - floor) * 255.0 / 120.0, 0, 255).astype(np.uint8)
    return Image.fromarray(np.dstack([a.astype(np.uint8), alpha]))


def main():
    name, sheet = sys.argv[1], Path(sys.argv[2])
    cols, rows = int(sys.argv[3]), int(sys.argv[4])
    out_w, out_h = int(sys.argv[5]), int(sys.argv[6])
    apply = "--apply" in sys.argv
    floor = 12.0
    for a in sys.argv:
        if a.startswith("--floor="):
            floor = float(a.split("=", 1)[1])

    im = key_out_black(Image.open(sheet), floor)
    cw, ch = im.width // cols, im.height // rows
    OUT.mkdir(parents=True, exist_ok=True)

    n = 0
    for r in range(rows):
        for c in range(cols):
            cell = im.crop((c * cw, r * ch, (c + 1) * cw, (r + 1) * ch))
            if out_h < out_w:
                # 가운데 띠만 — 잉크의 세로 한가운데를 기준으로 잡는다
                a = np.asarray(cell)[..., 3]
                ys = np.nonzero((a > 8).any(axis=1))[0]
                mid = (ys.min() + ys.max()) // 2 if len(ys) else ch // 2
                band = max(1, round(ch * out_h / out_w))
                top = int(np.clip(mid - band // 2, 0, ch - band))
                cell = cell.crop((0, top, cw, top + band))
            frame = cell.resize((out_w, out_h), Image.LANCZOS)
            n += 1
            path = OUT / f"fx_{name}_{n}.png"
            frame.save(path)
            a = np.asarray(frame)[..., 3]
            print(f"fx_{name}_{n}.png  잉크 {int((a > 8).sum())}px  가장 진한 {int(a.max())}")

    if apply:
        for i in range(1, n + 1):
            dst = GAME / f"fx_{name}_{i}.png"
            if dst.exists() and not Path(str(dst) + ".bak").exists():
                shutil.copy2(dst, str(dst) + ".bak")   # 되돌릴 수 있게 원본을 남긴다
            shutil.copy2(OUT / f"fx_{name}_{i}.png", dst)
        print("게임에 반영:", GAME, f"({n}장, 원본은 .bak)")
    else:
        print("저장:", OUT, "— 게임에 넣으려면 --apply")


if __name__ == "__main__":
    main()
