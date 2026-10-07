"""연쇄 방전 퀄업 부품 앉히기 (2026-10-07).

코덱스가 마젠타 바탕에 그려 온 부품 시트(in/lz_*.png)를 칸으로 나누고, 바탕을 빼고, 게임 크기로 줄여
Assets/BaseResource/InGameMainUI/fx_lz{이름}_{번호}.png 로 둔다 — ingamemainui 아틀라스(폴더째)에 들어가고
게임은 `FxFrames("lz{이름}")` 으로 꺼낸다. 그림은 고치지 않는다(바탕 빼기 · 나누기 · 줄이기만).

빛나는 그림이라 바탕 빼기를 「마젠타에서 얼마나 멀리 있나」로 한다 — 가장자리 반투명은 마젠타 기운을 걷는다.
"""
import os, sys
import numpy as np
from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')

# 이름 → (칸 수, 칸 하나의 출력 크기(가로, 세로))
PARTS = {
    'aim':    (1, (512, 32)),
    'bolt':   (4, (512, 96)),
    'hit':    (4, (192, 192)),
    'ring':   (4, (192, 192)),
    'arc':    (4, (192, 192)),
    'debris': (4, (192, 192)),
    'muzzle': (4, (96, 96)),
}


def key_out(im):
    a = np.asarray(im.convert('RGB')).astype(np.float32)
    h, w = a.shape[:2]
    bg = np.mean([a[2, 2], a[2, w - 3], a[h - 3, 2], a[h - 3, w - 3]], axis=0)
    d = np.sqrt(((a - bg) ** 2).sum(axis=2))
    t = np.clip((d - 40.0) / 110.0, 0, 1)
    # 반투명 가장자리 — 마젠타가 섞인 만큼(빨강 · 파랑이 초록보다 큰 몫)을 걷는다
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    m = np.minimum(r, b)
    spill = np.where(m > g, (m - g) * (1 - t), 0)
    r = np.clip(r - spill, 0, 255)
    b = np.clip(b - spill, 0, 255)
    return Image.fromarray(np.dstack([r, g, b, t * 255]).astype(np.uint8), 'RGBA')


def main(names):
    for name in names:
        n, size = PARTS[name]
        src = os.path.join(IN, f'lz_{name}.png')
        if not os.path.exists(src):
            print('없음', src)
            continue
        im = key_out(Image.open(src))
        w, h = im.size
        cw = w // n
        for i in range(n):
            cell = im.crop((i * cw, 0, (i + 1) * cw, h)).resize(size, Image.LANCZOS)
            out = os.path.join(OUT, f'fx_lz{name}_{i + 1}.png')
            cell.save(out)
        print(name, im.size, '→', n, '장', size)


if __name__ == '__main__':
    main(sys.argv[1:] or list(PARTS))
