# -*- coding: utf-8 -*-
"""위 · 아래 고정 막대(PD 확정 시안 _exchange/ref/lobby_profile/fixed_*.png, 2026-10-06) — 납품을 자르고 합친다.

그리지 않는다. 마젠타를 뚫고, 칸을 자르고, 바뀐 자리만 원본에 합친다.

  fx_top_band.png (1080 x 180)  → LobbyV4/fixed_top_band.png (+ 4:3 옆을 메울 가장자리 한 줄 band_edge_l/r)
  fx_nav_sheet.png (1080 x 324) → Growth/nav_{host,play,shop}_{on,off}.png   (여섯 칸 틀이 같다 — 누를 때 출렁이지 않게)
  base_mid 의 점 셋              → LobbyV4/nav_dot_on.png · nav_dot_off.png (지우기 전에 잘라 둔다)
  fx_base_mid.png                → _exchange/in/base_mid.png   (점 자리만 합친다)
  fx_bg_{ghost,host,skill}.png   → _exchange/in/bg_*.png       (맨 위 로고 · 재화 띠만 합친다)
"""
import os
import shutil
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from hud3_cut import key_out  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
SRC = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'lobby_profile')
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
LOBBY = os.path.join(ROOT, 'Assets', 'BaseResource', 'LobbyV4')
GROWTH = os.path.join(ROOT, 'Assets', 'BaseResource', 'Growth')


def composite(orig, new, boxes, out, feather=3):
    o = Image.open(orig).convert('RGBA')
    n = Image.open(new).convert('RGBA')
    m = Image.new('L', o.size, 0)
    d = ImageDraw.Draw(m)
    for b in boxes:
        d.rectangle(b, fill=255)
    m = m.filter(ImageFilter.GaussianBlur(feather))
    Image.composite(n, o, m).save(out)


def cut_dot(mid, box):
    """점 하나 — 둘레 남색에서 색 거리로 떼어 낸다."""
    a = mid[box[1]:box[3], box[0]:box[2]].astype(np.float32)
    bg = np.median(np.concatenate([a[0], a[-1], a[:, 0], a[:, -1]]), axis=0)
    dist = np.abs(a - bg).sum(axis=2)
    alpha = np.clip((dist - 40) / 80.0, 0, 1) * 255
    return Image.fromarray(np.dstack([a, alpha]).astype(np.uint8))


def main():
    # 위 막대
    shutil.copy(os.path.join(SRC, 'fx_top_band.png'), os.path.join(LOBBY, 'fixed_top_band.png'))
    band = Image.open(os.path.join(SRC, 'fx_top_band.png')).convert('RGB')
    band.crop((0, 0, 4, 180)).save(os.path.join(LOBBY, 'band_edge_l.png'))
    band.crop((1076, 0, 1080, 180)).save(os.path.join(LOBBY, 'band_edge_r.png'))

    # 하단 바 여섯 칸 — 칸 크기 그대로(360 x 162 = 화면 240 x 108)
    # ⚠ 납품은 칸(360 x 162) 안에서 줄마다 틀 높이가 달랐다(켜짐 줄 y 30 · 꺼짐 줄 y 167). 틀 여섯 개는 모두 343 x 130 —
    #   **틀을 찾아 같은 크기로** 잘라야 켜짐 · 꺼짐이 겹친다(그래야 누를 때 칸이 출렁이지 않는다).
    from scipy import ndimage
    sheet = key_out(np.asarray(Image.open(os.path.join(SRC, 'fx_nav_sheet.png')).convert('RGB')))
    lab, _ = ndimage.label(ndimage.binary_closing(sheet[..., 3] > 40, iterations=3))
    frames = [(i + 1, sl) for i, sl in enumerate(ndimage.find_objects(lab))
              if (sl[0].stop - sl[0].start) * (sl[1].stop - sl[1].start) > 20000]
    frames.sort(key=lambda f: (f[1][0].start // 100, f[1][1].start))
    names = ['nav_host_on', 'nav_play_on', 'nav_shop_on', 'nav_host_off', 'nav_play_off', 'nav_shop_off']
    for name, (idx, sl) in zip(names, frames):
        y0, x0 = sl[0].start - 8, sl[1].start - 8
        cell = sheet[y0:y0 + 146, x0:x0 + 359].copy()
        # 이웃 칸의 빛 번짐이 끝에 걸린다 — 이 칸 덩어리(조금 넓힌 것)만 남긴다
        own = ndimage.binary_dilation(lab[y0:y0 + 146, x0:x0 + 359] == idx, iterations=6)
        cell[~own, 3] = 0
        Image.fromarray(cell).save(os.path.join(GROWTH, name + '.png'))

    # 점 — base_mid 에서 지우기 전에 잘라 둔다(노랑 = 켜짐, 파랑 = 꺼짐)
    mid = np.asarray(Image.open(os.path.join(LOBBY, 'base_mid.png')).convert('RGB'))
    cut_dot(mid, (420, 780, 446, 806)).save(os.path.join(LOBBY, 'nav_dot_on.png'))
    cut_dot(mid, (458, 780, 484, 806)).save(os.path.join(LOBBY, 'nav_dot_off.png'))
    composite(os.path.join(LOBBY, 'base_mid.png'), os.path.join(SRC, 'fx_base_mid.png'),
              [(415, 776, 526, 808)], os.path.join(IN, 'base_mid.png'), feather=2)

    # 육성 배경 — 맨 위 띠만
    for k in ['ghost', 'host', 'skill']:
        composite(os.path.join(GROWTH, 'bg_%s.png' % k), os.path.join(SRC, 'fx_bg_%s.png' % k),
                  [(0, 0, 720, 158)], os.path.join(IN, 'bg_%s.png' % k), feather=4)
    # 호스트 육성 v5 판 — 맨 위 로고 · 금화 칸 · 우편 · 설정이 구워져 있다. 같은 밤 도시인 정리된 bg_host 에서
    #   그 자리만 가져온다(그 아래 탭 판은 v5 것 그대로). 정리된 bg_host 는 위에서 _exchange/in 에 막 만든 것.
    composite(os.path.join(GROWTH, 'v5', 'bg_host_v5.png'), os.path.join(IN, 'bg_host.png'),
              [(0, 0, 232, 136), (316, 0, 720, 74)], os.path.join(IN, 'bg_host_v5.png'), feather=3)
    print('ok')


def profile_plate():
    """fx_profile_plate.png (375 x 180, 시안 왼쪽 위 250 x 120 의 1.5배) → LobbyV4/profile_plate_fx.png"""
    p = key_out(np.asarray(Image.open(os.path.join(SRC, 'fx_profile_plate.png')).convert('RGB')))
    Image.fromarray(p).save(os.path.join(LOBBY, 'profile_plate_fx.png'))


def growth_strip():
    """육성 배경 맨 위 띠 — 위 막대 밑으로 보이는 밤 도시(화면 y 112~198)를 시안 fixed_growth.png 그대로 옮긴다.

    육성 판은 화면에 y' = 58 + 0.9745 y 로 놓인다(FixedBarsBinder.GrowthShift · GrowthScaleY).
    시안 화면 줄을 배경 줄로 되돌려(÷0.9745) 붙인다. 아래 끝(탭 판 바로 위)은 4px 섞는다.
    시안은 화면 해상도(720)라 배경(720)과 배율이 같다 — 늘리지 않는다(세로 2.6% 만).
    """
    mock = Image.open(os.path.join(SRC, 'fixed_growth.png')).convert('RGBA')
    y0, y1 = 112, 198
    b0, b1 = int(round((y0 - 58) / 0.9745)), int(round((y1 - 58) / 0.9745))
    strip = mock.crop((0, y0, 720, y1)).resize((720, b1 - b0), Image.LANCZOS)
    mask = Image.new('L', strip.size, 255)
    d = ImageDraw.Draw(mask)
    for i in range(4):
        d.line([(0, strip.size[1] - 1 - i), (720, strip.size[1] - 1 - i)], fill=int(255 * i / 4))
    for name, sub in [('bg_host_v5.png', 'v5'), ('bg_ghost.png', ''), ('bg_skill.png', '')]:
        src = os.path.join(GROWTH, sub, name)
        im = Image.open(src).convert('RGBA')
        im.paste(strip, (0, b0), mask)
        im.save(os.path.join(IN, name))


if __name__ == '__main__':
    # ⚠ 그냥 돌리면 점을 base_mid 에서 다시 자른다 — base_mid 는 이미 점을 지운 판이라 빈 점이 된다.
    #   판 하나만 다시 자를 때는 이름을 준다 : python fixed_bars_cut.py profile | strip
    if sys.argv[1:] == ['profile']:
        profile_plate()
    elif sys.argv[1:] == ['strip']:
        growth_strip()
    else:
        main()
