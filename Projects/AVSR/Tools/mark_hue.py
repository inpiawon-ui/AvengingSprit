# -*- coding: utf-8 -*-
"""표적 4프레임의 색조를 돌려 저주·보스 조준용 한 벌을 굽는다.

색을 런타임에 곱하면 칠해진 불길이 죽는다 — 색조만 돌리면 명암과 결이 남는다.
"""
import numpy as np
from PIL import Image

SRC = 'Assets/BaseResource/InGameMainUI/fx_mark_%d.png'
JOBS = [('markcurse', 232), ('markboss', 178)]   # 보라 · 청록


def rotate(path, degrees):
    im = Image.open(path).convert('RGBA')
    alpha = im.getchannel('A')
    hsv = np.asarray(im.convert('RGB').convert('HSV')).astype(np.int16)
    hsv[..., 0] = (hsv[..., 0] + round(degrees * 255 / 360)) % 256
    out = Image.fromarray(hsv.astype(np.uint8), 'HSV').convert('RGB').convert('RGBA')
    out.putalpha(alpha)
    return out


for name, deg in JOBS:
    for i in range(1, 5):
        img = rotate(SRC % i, deg)
        dst = 'Assets/BaseResource/InGameMainUI/fx_%s_%d.png' % (name, i)
        img.save(dst)
        print(dst, '색조', deg)
