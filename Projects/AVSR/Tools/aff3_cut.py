# -*- coding: utf-8 -*-
"""상성 3속성 개편(파워 · 무기 · 마법, PD 시안 2026-10-06) — 납품 그림을 자르고 줄여 게임 그림 자리에 넣는다.

그리지 않는다. 마젠타를 뚫고(hud3_cut.key_out 와 같은 규칙), 칸을 자르고, 규격으로 줄일 뿐이다.

  _exchange/ref/affinity3/aff3_icons_sheet.png (768 x 256, 3칸)  → rps_gem_force · rps_gem_blade · rps_gem_magic (128 x 128)
                                                                    + 로비 사본 ChapterHost/ch_kind_{...}
  _exchange/ref/affinity3/rps_tri_{force,blade,magic}_new.png      → rps_tri_{...}.png (480 x 440)

파일 이름은 예전 그대로 둔다 — 코드(AffinityRule.GemName · TriangleName)와 프리팹 연결(GUID)이 그대로 산다.
  force = 파워(옛 「힘」) · blade = 무기(옛 「날」) · magic = 마법(옛 「술」)
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from hud3_cut import key_out  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
SRC = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'affinity3')
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')
LOBBY = os.path.join(ROOT, 'Assets', 'BaseResource', 'ChapterHost')


def clear_pockets(rgba):
    """칼과 권총 사이처럼 **안에 갇힌** 마젠타 틈을 뚫는다.

    key_out 은 판 가장자리에서 이어진 마젠타만 배경으로 본다(분홍 옷을 지우지 않으려고). 그런데 이 아이콘 · 삼각판에는
    원래 분홍 · 자주가 없다(파워 빨강 · 무기 금색 · 마법 파랑 · 화살표 초록/빨강/회청) — 그래서 갇힌 마젠타도 배경이다.
    틈 가장자리의 섞인 픽셀은 마젠타 기운을 빼서 남긴다.
    """
    a = rgba.astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    mag = (r > 150) & (b > 150) & (g < 110) & (np.abs(r - b) < 90)
    rgba = rgba.copy()
    rgba[mag, 3] = 0
    tint = (rgba[..., 3] > 0) & (r - g > 40) & (b - g > 40) & (np.abs(r - b) < 70)
    rgba[tint, 0] = np.minimum(rgba[tint, 0], (g[tint] + 40)).astype(np.uint8)
    rgba[tint, 2] = np.minimum(rgba[tint, 2], (g[tint] + 40)).astype(np.uint8)
    return rgba


def fit(rgba, size):
    """잉크를 칸 가운데에 두고 정사각/지정 크기로 줄인다(비율 유지)."""
    al = rgba[..., 3]
    ys, xs = np.nonzero(al > 8)
    crop = Image.fromarray(rgba[ys.min():ys.max() + 1, xs.min():xs.max() + 1])
    w, h = size
    s = min(w * 0.92 / crop.width, h * 0.92 / crop.height)
    # 알파를 곱한 채로 줄인다 — 그냥 줄이면 투명 픽셀에 남은 마젠타 색이 가장자리로 번진다
    crop = crop.convert('RGBa').resize((max(1, round(crop.width * s)), max(1, round(crop.height * s))), Image.LANCZOS).convert('RGBA')
    out = Image.new('RGBA', size, (0, 0, 0, 0))
    out.paste(crop, ((w - crop.width) // 2, (h - crop.height) // 2), crop)
    return out


def main():
    sheet = np.asarray(Image.open(os.path.join(SRC, 'aff3_icons_sheet.png')).convert('RGB'))
    for i, key in enumerate(['force', 'blade', 'magic']):
        cell = clear_pockets(key_out(sheet[:, i * 256:(i + 1) * 256]))
        icon = fit(cell, (128, 128))
        icon.save(os.path.join(OUT, 'rps_gem_%s.png' % key))
        # 로비 챕터 · 호스트 창은 같은 그림의 사본(ch_kind_*)을 쓴다 — 둘 다 바꿔야 로비와 전투가 같다
        icon.save(os.path.join(LOBBY, 'ch_kind_%s.png' % key))
        print('rps_gem_%s.png · ch_kind_%s.png' % (key, key))
    for key in ['force', 'blade', 'magic']:
        rgba = clear_pockets(key_out(np.asarray(Image.open(os.path.join(SRC, 'rps_tri_%s_new.png' % key)).convert('RGB'))))
        # 삼각판은 세 장의 아이콘 자리가 같아야 한다 — 잉크로 다시 맞추지 않고 판을 통째로 줄인다
        Image.fromarray(rgba).convert('RGBa').resize((480, 440), Image.LANCZOS).convert('RGBA').save(os.path.join(OUT, 'rps_tri_%s.png' % key))
        print('rps_tri_%s.png' % key)


if __name__ == '__main__':
    main()
