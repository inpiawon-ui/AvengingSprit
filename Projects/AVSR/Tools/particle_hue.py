# -*- coding: utf-8 -*-
"""칠해진 파티클 그림의 **색만 돌려** 다른 속성 변형을 만든다.

런타임 색 입히기(곱셈)는 칠해진 그림을 죽인다 — 주황 불티에 파랑을 곱하면
밝은 데는 회색이 되고 어두운 데는 검게 죽어 «도형»으로 돌아간다.
색조만 돌리면 **명암과 결이 그대로 남는다.**

쓰는 법:
  python particle_hue.py <원본 이름> <새 이름>:<색조 각도> ... [--sat=1.0] [--apply]

  예) python particle_hue.py spark spark_ice:190 spark_venom:110 spark_bolt:165 --apply
      (주황 0도 기준으로 돌린다 — 190 이면 파랑, 110 이면 초록)
"""
import shutil
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
GAME = ROOT / "Assets/BaseResource/ParticleFx"
OUT = ROOT / "Projects/AVSR/_exchange/ref/particle/hue"


def rotate_hue(path, degrees, sat):
    im = Image.open(path).convert("RGBA")
    alpha = im.getchannel("A")
    hsv = np.asarray(im.convert("RGB").convert("HSV")).astype(np.int16)
    hsv[..., 0] = (hsv[..., 0] + round(degrees * 255 / 360)) % 256
    if sat != 1.0:
        hsv[..., 1] = np.clip(hsv[..., 1] * sat, 0, 255)
    out = Image.fromarray(hsv.astype(np.uint8), "HSV").convert("RGB").convert("RGBA")
    out.putalpha(alpha)
    return out


def main():
    src = sys.argv[1]
    jobs = [a for a in sys.argv[2:] if ":" in a and not a.startswith("--")]
    apply = "--apply" in sys.argv
    sat = 1.0
    for a in sys.argv:
        if a.startswith("--sat="):
            sat = float(a.split("=", 1)[1])

    path = GAME / f"{src}.png"
    if not path.exists():
        print("원본이 없다:", path)
        return
    OUT.mkdir(parents=True, exist_ok=True)

    for job in jobs:
        name, deg = job.split(":")
        img = rotate_hue(path, float(deg), sat)
        img.save(OUT / f"{name}.png")
        print(f"{name:14s} ← {src} 색조 {deg}도")
        if apply:
            shutil.copy2(OUT / f"{name}.png", GAME / f"{name}.png")
    if apply:
        print("게임에 반영:", GAME)


if __name__ == "__main__":
    main()
