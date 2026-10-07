"""유령 빛 · 피격 부품 앉히기 (2026-10-07) — 설계 Projects/AVSR/_exchange/spec_hitfx_ghostlight.md.

코덱스가 마젠타 바탕에 그려 온 부품 시트(in/gl_*.png · in/hx_*.png)를 칸으로 나누고, 바탕을 빼고, 게임 크기로 줄여
Assets/BaseResource/InGameMainUI/fx_{이름}_{번호}.png 로 둔다 — ingamemainui 아틀라스(폴더째)에 들어가고
게임은 `FxFrames("{이름}")` 으로 꺼낸다. 그림은 고치지 않는다(바탕 빼기 · 나누기 · 줄이기만).

⚠ 마법 겹(hxmagic)은 자홍이 그림 색이다 — 가장자리 마젠타 걷기(despill)를 하면 그림이 지워진다. 그 부품만 뺀다.
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lz_parts_fit import key_out  # noqa: E402
from wp_parts_fit import despill  # noqa: E402

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')

# 이름 → (원본 파일, 칸 수, 칸 하나의 출력 크기(가로, 세로), 마젠타 걷기)
# ⚠ 유령 빛(gl*)은 2026-10-07 부터 그림을 쓰지 않는다 — 셰이더(`UI/AdditiveLight`) + 파티클로 그린다.
#   알갱이 별 하나만 gl_mote.png 에서 잘라 파티클 원본으로 쓴다(Assets/BaseResource/ParticleFx/ghostmote.png).
PARTS = {
    'hxcore':  ('hx_core.png', 4, (192, 192), True),
    'hxcrit':  ('hx_crit.png', 4, (192, 192), True),
    'hxweap':  ('hx_weap.png', 4, (192, 192), True),
    'hxpow':   ('hx_pow.png', 4, (192, 192), True),
    'hxmagic': ('hx_magic.png', 4, (192, 192), False),
    'hxline':  ('hx_line.png', 3, (192, 192), True),
    'hxslash': ('hx_slash.png', 4, (192, 192), True),
    'hxblunt': ('hx_blunt.png', 4, (192, 192), True),
}


def unmix_magenta(im):
    """반투명 빛을 마젠타 위에 그려 왔다 — 픽셀 = a·빛 + (1−a)·마젠타 를 풀어 빛 색과 투명도를 되돌린다.

    그냥 마젠타만 빼면 광막 안쪽이 마젠타와 섞인 보라 · 갈색으로 남는다(2026-10-07 실측).
    a 는 빛 색이 0~255 안에 들어오는 가장 작은 값(= 가장 투명하게)이다.
    """
    import numpy as np
    c = np.asarray(im.convert('RGB')).astype(np.float32)
    r, g, b = c[..., 0], c[..., 1], c[..., 2]
    a = np.maximum.reduce([(255 - r) / 255.0, g / 255.0, (255 - b) / 255.0])
    a = np.clip(a, 0, 1)
    safe = np.maximum(a, 1e-3)[..., None]
    m = np.array([255.0, 0.0, 255.0])
    f = (c - (1 - a)[..., None] * m) / safe
    f = np.clip(f, 0, 255)
    # ⚠ 마젠타에서 **멀리 떨어진** 진한 부분은 섞인 게 아니라 그림 그대로다 — 원래 색 · 불투명으로 둔다.
    #   전부 풀었더니 파란 흰빛(200,221,253)이 청록(165,253,251)으로 틀어졌다(시안과 색이 달라짐, 2026-10-07).
    #   마젠타와의 거리 180 → 300 사이에서 「푼 값」과 「원래 값」을 섞는다.
    dist = np.abs(c - m).sum(axis=2)
    keep = np.clip((dist - 180.0) / 120.0, 0, 1)
    f = f * (1 - keep)[..., None] + c * keep[..., None]
    a = a * (1 - keep) + 1.0 * keep
    a = np.where(a < 0.03, 0, a)   # 바탕 잡티
    return Image.fromarray(np.dstack([f, a * 255]).astype(np.uint8), 'RGBA')


def main(names):
    for name in names:
        src, n, size, spill = PARTS[name]
        path = os.path.join(IN, src)
        if not os.path.exists(path):
            print('없음', name)
            continue
        if name.startswith('gl'):
            im = unmix_magenta(Image.open(path))   # 유령 빛은 전부 반투명 빛
        else:
            im = key_out(Image.open(path))
            if spill:
                im = despill(im)
        w, h = im.size
        cw = w // n
        for i in range(n):
            cell = im.crop((i * cw, 0, (i + 1) * cw, h)).resize(size, Image.LANCZOS)
            cell.save(os.path.join(OUT, f'fx_{name}_{i + 1}.png'))
        print(name, im.size, '→', n, '장', size)


if __name__ == '__main__':
    main(sys.argv[1:] or list(PARTS))
