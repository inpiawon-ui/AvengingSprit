# -*- coding: utf-8 -*-
"""챕터 · 호스트 선택 — 위 · 아래 고정 시안(_exchange/ref/lobby_profile/fixed_chapter.png, 2026-10-06) 납품을 자른다.

그리지 않는다. 마젠타를 뚫고, 칸을 자르고, 시안에서 오린 선 하나를 판에 합친다.

  fx_chapter_tall.png (941 x 1790) → ChapterHost/ch_screen_fixed.png
      가운데 941 x 1372(209 줄부터)가 시안 y 120~1170 과 픽셀로 같다. 위아래 209 줄은 키 큰 폰에서 보일 몫.
      랜덤 칸의 세로 구분선은 빈 판에 빠져 있어 시안에서 오려 붙인다.
  fx_card_parts.png (1200 x 400)   → ch_card_frame_fx · ch_card_frame_sel_fx · ch_check_fx · ch_icon_dice_fx
"""
import os
import sys

import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from hud3_cut import key_out  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
SRC = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'lobby_profile')
CH = os.path.join(ROOT, 'Assets', 'BaseResource', 'ChapterHost')

S = 941 / 720.0     # 시안(720) → 판(941)
TOP = 209           # 판에서 시안 y 120 이 놓인 줄
DIVIDER = (200, 912, 214, 1036)   # 시안 좌표 — 랜덤 칸 주사위 옆 세로선


def to_plate(x, y):
    return int(round(x * S)), int(round(TOP + (y - 120) * S))


def main():
    plate = Image.open(os.path.join(SRC, 'fx_chapter_tall.png')).convert('RGBA')
    mock = Image.open(os.path.join(SRC, 'fixed_chapter.png')).convert('RGBA')
    x0, y0, x1, y1 = DIVIDER
    px0, py0 = to_plate(x0, y0)
    px1, py1 = to_plate(x1, y1)
    piece = mock.crop(DIVIDER).resize((px1 - px0, py1 - py0), Image.LANCZOS)
    mask = Image.new('L', piece.size, 0)
    mask.paste(255, (3, 3, piece.size[0] - 3, piece.size[1] - 3))
    plate.paste(piece, (px0, py0), mask.filter(ImageFilter.GaussianBlur(2)))
    plate.convert('RGB').save(os.path.join(CH, 'ch_screen_fixed.png'))

    a = key_out(np.asarray(Image.open(os.path.join(SRC, 'fx_card_parts.png')).convert('RGB')))
    lab, _ = ndimage.label(a[..., 3] > 40)
    parts = [sl for sl in ndimage.find_objects(lab) if (sl[0].stop - sl[0].start) * (sl[1].stop - sl[1].start) > 500]
    parts.sort(key=lambda sl: sl[1].start)   # 왼쪽부터 : 보통 틀 · 고른 틀 · 체크 · 주사위
    for name, sl in zip(['ch_card_frame_fx', 'ch_card_frame_sel_fx', 'ch_check_fx', 'ch_icon_dice_fx'], parts):
        ys, xs = slice(max(0, sl[0].start - 2), sl[0].stop + 2), slice(max(0, sl[1].start - 2), sl[1].stop + 2)
        Image.fromarray(a[ys, xs]).save(os.path.join(CH, name + '.png'))
        print(name, xs.stop - xs.start, ys.stop - ys.start)


def wide():
    """fx_chapter_wide.png (1255 x 1790, 태블릿용 좌우 157 연장) → ChapterHost/ch_screen_wide.png

    가운데 941 폭은 지금 판(ch_screen_fixed.png — 구분선까지 붙인 것)을 그대로 덮는다.
    납품이 가운데를 조금이라도 바꿨어도 칸 · 테두리는 원본 그대로 남는다. 이음매만 3px 섞는다.
    """
    out = Image.open(os.path.join(SRC, 'fx_chapter_wide.png')).convert('RGBA')
    core = Image.open(os.path.join(CH, 'ch_screen_fixed.png')).convert('RGBA')
    x0 = (out.size[0] - core.size[0]) // 2
    mask = Image.new('L', core.size, 0)
    mask.paste(255, (4, 0, core.size[0] - 4, core.size[1]))
    out.paste(core, (x0, 0), mask.filter(ImageFilter.GaussianBlur(2)))
    out.convert('RGB').save(os.path.join(CH, 'ch_screen_wide.png'))
    print('ch_screen_wide', out.size)


if __name__ == '__main__':
    if sys.argv[1:] == ['wide']:
        wide()
    else:
        main()
