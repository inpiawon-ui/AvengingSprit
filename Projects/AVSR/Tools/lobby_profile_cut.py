# -*- coding: utf-8 -*-
"""로비 유령 프로필(A안, PD 확정 2026-10-06) — 납품 그림을 자르고 합친다. 그리지 않는다.

  _exchange/ref/lobby_profile/plate_empty.png (750 x 345 = 화면 250 x 115 의 3배) → LobbyV4/profile_plate.png (마젠타 뚫기)
  _exchange/ref/lobby_profile/plate_fill.png  (400 x 60, 채움 한 줄)            → LobbyV4/profile_fill.png (띠 가운데만)
  _exchange/ref/lobby_profile/base_top_nologo.png                              → _exchange/in/base_top.png
      로고 자리만 오려 지금 배경(now_base_top.png)에 합친다 — 나머지 픽셀은 원본 그대로.
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from hud3_cut import key_out  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
SRC = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'lobby_profile')
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'LobbyV4')
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
MAGENTA = np.array([255, 0, 255])


def main():
    # 판 — 캔버스 크기 그대로 둔다(화면 250 x 115 칸에 꽉 차게 놓는다)
    plate = key_out(np.asarray(Image.open(os.path.join(SRC, 'plate_empty.png')).convert('RGB')))
    Image.fromarray(plate).save(os.path.join(OUT, 'profile_plate.png'))

    # 채움 — 마젠타가 아닌 덩어리의 안쪽만(가장자리 번짐 없이)
    f = np.asarray(Image.open(os.path.join(SRC, 'plate_fill.png')).convert('RGB')).astype(int)
    ink = np.abs(f - MAGENTA).sum(axis=2) > 120
    sl = max(ndimage.find_objects(ndimage.label(ink)[0]), key=lambda s: (s[0].stop - s[0].start) * (s[1].stop - s[1].start))
    Image.fromarray(f[sl[0].start + 2:sl[0].stop - 2, sl[1].start + 4:sl[1].stop - 4].astype(np.uint8)).convert('RGBA') \
        .save(os.path.join(OUT, 'profile_fill.png'))

    # 배경 — 로고 자리만 합친다
    o = Image.open(os.path.join(SRC, 'now_base_top.png')).convert('RGB')
    n = Image.open(os.path.join(SRC, 'base_top_nologo.png')).convert('RGB')
    m = Image.new('L', o.size, 0)
    ImageDraw.Draw(m).rectangle((18, 6, 302, 178), fill=255)
    m = m.filter(ImageFilter.GaussianBlur(3))
    Image.composite(n, o, m).save(os.path.join(IN, 'base_top.png'))
    print('profile_plate.png · profile_fill.png · _exchange/in/base_top.png')


if __name__ == '__main__':
    main()
