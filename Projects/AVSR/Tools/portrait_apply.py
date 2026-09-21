"""호스트 초상 납품(자홍 배경)을 뚫어 게임 그림으로 둔다.

쓰는 법: python portrait_apply.py <키> [<키> ...]
입력: Projects/AVSR/_exchange/ref/portrait/out_portrait_{키}.png
출력: Assets/BaseResource/Growth/Portraits/portrait_{키}.png  (잉크에 딱 맞춰 자르고 긴 변 512)
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
SRC = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'portrait')
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Growth', 'Portraits')


def key_out(im):
    a = np.asarray(im.convert('RGB')).astype(np.int16)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    dist = np.abs(a - np.array([255, 0, 255])).sum(axis=2)
    pink = (r > 120) & (b > 120) & (r - g > 45) & (b - g > 45)
    alpha = np.where((dist < 150) | pink, 0, 255).astype(np.uint8)
    rgb = a.copy()
    spill = (alpha > 0) & (r - g > 20) & (b - g > 20)   # 가장자리 분홍 기운
    rgb[spill, 0] = np.minimum(rgb[spill, 0], g[spill] + 20)
    rgb[spill, 2] = np.minimum(rgb[spill, 2], g[spill] + 20)
    return np.dstack([rgb.astype(np.uint8), alpha])


def main():
    os.makedirs(OUT, exist_ok=True)
    for key in sys.argv[1:]:
        rgba = key_out(Image.open(os.path.join(SRC, f'out_portrait_{key}.png')))
        ys, xs = np.nonzero(rgba[..., 3] > 8)
        im = Image.fromarray(rgba[ys.min():ys.max() + 1, xs.min():xs.max() + 1])
        s = 512 / max(im.size)
        if s < 1:
            im = im.resize((round(im.width * s), round(im.height * s)), Image.LANCZOS)
        im.save(os.path.join(OUT, f'portrait_{key}.png'))
        print(key, im.size)


if __name__ == '__main__':
    main()
