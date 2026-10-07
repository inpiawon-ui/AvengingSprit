# -*- coding: utf-8 -*-
"""화염 분사구 불줄기 시트(마젠타, 세로 14칸)를 자르고 미리보기 GIF 를 만든다 (2026-10-07).

PD 「불 이미지 대충 올려 놓은 느낌 — 퀄이 똥」 → 코덱스 불줄기 전용 시트(붙기 3 · 뿜기 8 되풀이 · 꺼지기 3).
PD 「연출은 애니로 봐야 한다」 → 시트가 오면 게임 화면 위에서 돌린 GIF 부터 보여 준다.

쓰는 법:
  python flame_jet_cut.py preview   # _exchange/ref/flame_fx/preview_v3.gif
  python flame_jet_cut.py cut       # Assets/BaseResource/InGameMainUI/fx_flamejet_01~14.png (입이 왼쪽 가운데)
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from unit_up_cut import key_out  # noqa: E402

ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
D = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'flame_fx')
SRC = os.path.join(D, 'flame_jet_v3.png')
DST = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')
OUT_W = 256   # 게임 낱장 폭 — 입(왼쪽)에서 불 끝(오른쪽)까지
# 세로 배율 — 시트의 불은 높이:길이가 1:4 쯤이라 PD 가 본 화면 시안(1:2.7)보다 가늘다. 세로만 이만큼 키운다
TALL = 1.5


def frames(path=SRC):
    rgba = key_out(Image.open(path).convert('RGB'))
    a = np.asarray(rgba).copy()
    r, g, b = a[..., 0].astype(int), a[..., 1].astype(int), a[..., 2].astype(int)
    a[(b > g + 12) & (r > g + 30) & (b > 90), 3] = 0   # 마젠타가 섞인 테두리
    rgba = Image.fromarray(a)
    # 칸 경계 — 칸마다 왼쪽 끝 가운데에 **불 입**(뾰족한 끝)이 있다. 왼쪽 띠(입 쪽 40 px)에서 잉크 덩어리 열넷을 찾고,
    # 덩어리 가운데끼리의 중간을 경계로 삼는다. (시트 칸 높이가 주문과 달라 고르게 나누면 불 위쪽이 잘렸다)
    al = a[..., 3] > 40
    xs_any = np.where(al.any(axis=0))[0]
    x0 = xs_any.min()
    strip = al[:, x0:x0 + 40].any(axis=1)
    runs, inside, st = [], False, 0
    for y, v in enumerate(strip):
        if v and not inside:
            inside, st = True, y
        elif not v and inside:
            inside = False
            runs.append((st, y))
    if inside:
        runs.append((st, len(strip)))
    runs = sorted(runs, key=lambda r: r[1] - r[0], reverse=True)[:14]
    centers = sorted((r0 + r1) // 2 for r0, r1 in runs)
    cuts = [0] + [(centers[i] + centers[i + 1]) // 2 for i in range(len(centers) - 1)] + [rgba.height]
    bands = [(cuts[i], cuts[i + 1]) for i in range(len(centers))]
    h = 2 * max(max(c - s, e - c) for (s, e), c in zip(bands, centers))
    out = []
    for s, e in bands:
        crop = rgba.crop((0, s, rgba.width, e))
        xs = np.nonzero(np.asarray(crop)[..., 3] > 40)[1]
        crop = crop.crop((xs.min(), 0, rgba.width, e - s))
        canvas = Image.new('RGBA', (rgba.width, h), (0, 0, 0, 0))
        c = centers[len(out)] - s                                # 이 칸의 입 높이
        canvas.alpha_composite(crop, (0, h // 2 - c))          # 입을 왼쪽 가운데에
        out.append(canvas)
    return out


def full_len(fs):
    return max(np.nonzero(np.asarray(f)[..., 3] > 40)[1].max() for f in fs[3:11]) + 1


def preview():
    fs = frames()
    bg = Image.open(os.path.join(D, 'bg_jet_off.png')).convert('RGBA')   # 불이 꺼진 장면
    k = 290 / full_len(fs)   # 게임 화면에서 불이 닿는 길이(약 290 px)
    seq = [0, 1, 2] + list(range(3, 11)) * 3 + [11, 12, 13] + [None] * 6
    out = []
    for idx in seq:
        im = bg.copy()
        if idx is not None:
            f = fs[idx].resize((round(fs[idx].width * k), round(fs[idx].height * k * TALL)), Image.NEAREST)
            im.alpha_composite(f, (60, 363 - f.height // 2))
        out.append(im.crop((0, 200, 720, 620)).convert('RGB').quantize(255))
    path = os.path.join(D, 'preview_v3.gif')
    out[0].save(path, save_all=True, append_images=out[1:], duration=83, loop=0)
    print(path, len(fs), 'frames')


def cut():
    fs = frames()
    L = full_len(fs)
    for i, f in enumerate(fs):
        f = f.crop((0, 0, L, f.height))
        k = OUT_W / L
        f = f.resize((OUT_W, max(1, round(f.height * k * TALL))), Image.NEAREST)
        f.save(os.path.join(DST, f'fx_flamejet_{i + 1:02d}.png'))
    print('cut', len(fs), 'size', (OUT_W, round(fs[0].height * OUT_W / L)))


if __name__ == '__main__':
    {'preview': preview, 'cut': cut}[sys.argv[1] if len(sys.argv) > 1 else 'preview']()
