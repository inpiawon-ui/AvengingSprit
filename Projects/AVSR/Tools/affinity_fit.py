# -*- coding: utf-8 -*-
"""상성 시험판 납품 그림을 자르고 규격에 맞춘다 (그리지 않는다 — 자르기·배치만).

  _affinity_icons_sheet.png (320x64, 5칸) -> affinity_{strike,rapid,pierce,blast,element}.png (64x64)
  _fx_weakhit_sheet.png     (512x128, 4칸) -> fx_weakhit_{1..4}.png (128x128)
  affinity_adv_mark.png / affinity_adv_ring.png -> 그대로 (배경 검사만)

  결과는 _exchange/in/ 에 쓴다. 게임 폴더로는 「납품 반영」 툴이 옮긴다.
"""
import os, sys
from PIL import Image

IN = os.path.join(os.path.dirname(__file__), '..', '_exchange', 'in')
ICONS = ['strike', 'rapid', 'pierce', 'blast', 'element']


def opaque_ratio(im):
    a = im.getchannel('A')
    n = sum(1 for v in a.getdata() if v > 0)
    return n / (im.width * im.height)


def magenta_left(im):
    return sum(1 for r, g, b, a in im.getdata() if a > 0 and r > 200 and b > 200 and g < 70)


def cut(sheet, names, cell):
    src = os.path.join(IN, sheet)
    if not os.path.exists(src):
        print('없음', sheet); return
    im = Image.open(src).convert('RGBA')
    w, h = cell
    assert im.size == (w * len(names), h), f'{sheet} 크기 {im.size}'
    for i, n in enumerate(names):
        c = im.crop((i * w, 0, (i + 1) * w, h))
        out = os.path.join(IN, n + '.png')
        c.save(out)
        print(f'{n}: 채움 {opaque_ratio(c):.0%} · 마젠타 {magenta_left(c)}')


def check(name):
    p = os.path.join(IN, name)
    if not os.path.exists(p):
        print('없음', name); return
    im = Image.open(p).convert('RGBA')
    print(f'{name}: {im.size} · 채움 {opaque_ratio(im):.0%} · 마젠타 {magenta_left(im)} · bbox {im.getbbox()}')


if __name__ == '__main__':
    cut('_affinity_icons_sheet.png', ['affinity_' + n for n in ICONS], (64, 64))
    cut('_fx_weakhit_sheet.png', [f'fx_weakhit_{i}' for i in range(1, 5)], (128, 128))
    check('affinity_adv_mark.png')
    check('affinity_adv_ring.png')

    # ── 가위바위보(날 · 힘 · 술) 부품 (2026-10-02) ──
    cut('_rps_gems_sheet.png', ['rps_gem_blade', 'rps_gem_force', 'rps_gem_magic'], (64, 64))
    cut('_rps_arrows_sheet.png', ['rps_up', 'rps_down'], (64, 64))
    cut('_rps_triangle_sheet.png', ['rps_tri_blade', 'rps_tri_force', 'rps_tri_magic'], (240, 220))
    check('rps_ring.png')
