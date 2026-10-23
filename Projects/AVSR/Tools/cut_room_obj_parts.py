# -*- coding: utf-8 -*-
"""방 물건 부품 판(마젠타)을 게임 규격 낱장으로 자른다 — 3단계 쓰레기장(2026-10-23).

판은 칸 크기가 주문과 조금 다르게 온다(코덱스). 그래서 판을 열 x 줄로 나눠 칸마다 **그림 덩어리 상자**를 잰 뒤,
게임 규격 칸에 맞춰 줄인다 — 아래 맞춤 · 가운데 · 한 배율(가장 가까운 이웃).
  막는 물건 · 울타리 : 72 x 102 (아래 72 x 72 발자국 + 위 30 윗면) — 폭을 72 에, 키는 102 를 넘지 않게
  가시 판           : 72 x 72 (바닥에 눕는다)

쓰는 법: python cut_room_obj_parts.py <무대> [내보낼 폴더]
  기본 내보낼 폴더 = _exchange/ref/room_obj_parts/cut/<무대>/ (게임에 넣기 전 확인용). 게임에 넣을 때는 Assets/BaseResource/InGameMainUI
"""
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from unit_up_cut import key_out  # noqa: E402

ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

# 판 이름 → (열, 줄, [(칸 차례, 내보낼 이름, 칸 폭, 칸 높이)])
SHEETS = {
    'block': (3, 2, [(i, f'block_{i + 1}', 72, 102) for i in range(6)]),
    'spike_fence': (4, 1, [(0, 'spike_1', 72, 72), (1, 'spike_2', 72, 72), (2, 'fence_1', 72, 102), (3, 'fence_2', 72, 102)]),
}


def cells(path, cols, rows):
    rgba = np.asarray(key_out(Image.open(path).convert('RGB'))).copy()
    # 울타리 철망 · 쇠창살 사이처럼 **안에 갇힌 마젠타**도 뚫는다 — 방 물건에는 마젠타 색이 없다
    #   (key_out 은 사람 옷 색을 지키려고 가장자리와 이어진 마젠타만 뚫는다)
    r, g, b = (rgba[..., i].astype(int) for i in range(3))
    rgba[((r > 180) & (b > 180) & (g < 110)) | ((r - g > 60) & (b - g > 60) & (b > 110)), 3] = 0   # 마젠타 + 둘레에 남는 분홍 번짐
    h, w = rgba.shape[:2]
    out = []
    for i in range(cols * rows):
        r, c = divmod(i, cols)
        t = rgba[r * h // rows:(r + 1) * h // rows, c * w // cols:(c + 1) * w // cols].copy()
        a = t[..., 3] > 40
        lab, n = ndimage.label(a)
        if n:   # 떨어진 작은 부스러기는 버린다(옆 칸에서 넘어온 것 포함)
            sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
            keep = np.isin(lab, [k + 1 for k, s in enumerate(sizes) if s >= max(40, sizes.max() * 0.03)])
            t[~keep, 3] = 0
        out.append(t)
    return out


# 물 타일 — 칸 안의 **자리를 지켜야** 한다(둑은 변 · 모서리에 붙어 있다). 덩어리 상자로 자르면 자리가 없어지므로
# 판의 마젠타 여백으로 칸 줄 · 칸 열을 찾아 칸을 통째로 잘라 72 x 72 로 줄인다.
WATER_ORDER = ['water_1', 'water_2', 'water_edge_n', 'water_edge_s', 'water_edge_w', 'water_edge_e',
               'water_corner_nw', 'water_corner_ne', 'water_corner_sw', 'water_corner_se']


def bands(mask, axis):
    """마젠타가 아닌 칸이 있는 줄(열) 구간들 — 칸 사이 여백으로 끊긴다."""
    on = mask.any(axis=axis)
    out, start = [], None
    for i, v in enumerate(on):
        if v and start is None:
            start = i
        elif not v and start is not None:
            out.append((start, i)); start = None
    if start is not None:
        out.append((start, len(on)))
    return [b for b in out if b[1] - b[0] > 40]


def cut_water(stage, dst):
    path = os.path.join(EX, 'in', f'parts_{stage}_water.png')
    if not os.path.exists(path):
        print('없음', path)
        return
    rgba = np.asarray(key_out(Image.open(path).convert('RGB'))).copy()
    r, g, b = (rgba[..., i].astype(int) for i in range(3))
    rgba[((r > 180) & (b > 180) & (g < 110)) | ((r - g > 60) & (b - g > 60) & (b > 110)), 3] = 0   # 마젠타 + 둘레에 남는 분홍 번짐
    mask = rgba[..., 3] > 40
    cols, rows = bands(mask, 0), bands(mask, 1)
    k = 0
    for ry in rows:
        for cx in cols:
            if k >= len(WATER_ORDER):
                break
            tile = rgba[ry[0]:ry[1], cx[0]:cx[1]]
            if not (tile[..., 3] > 40).any():
                continue   # 3 줄째 빈 칸
            if WATER_ORDER[k].startswith('water_') and WATER_ORDER[k][6:].isdigit():
                # 수면은 바깥 테두리 몇 줄이 조금 밝게 그려져 와서 이어 붙이면 칸 금이 보인다 — 테두리를 깎는다
                th, tw = tile.shape[:2]
                m = max(3, int(min(th, tw) * 0.04))
                tile = tile[m:th - m, m:tw - m]
            out = os.path.join(dst, f'obj_{stage}_{WATER_ORDER[k]}.png')
            Image.fromarray(tile).resize((72, 72), Image.NEAREST).save(out)
            print(out)
            k += 1
    if k != len(WATER_ORDER):
        print(f'⚠ 물 타일 {k}장 — {len(WATER_ORDER)}장이어야 한다(칸 {len(cols)} 열 x {len(rows)} 줄)')


def fit(tile, cw, ch):
    ys, xs = np.nonzero(tile[..., 3] > 40)
    crop = Image.fromarray(tile[ys.min():ys.max() + 1, xs.min():xs.max() + 1])
    k = min(cw / crop.width, ch / crop.height)
    nw, nh = max(1, round(crop.width * k)), max(1, round(crop.height * k))
    crop = crop.resize((nw, nh), Image.NEAREST)
    canvas = Image.new('RGBA', (cw, ch), (0, 0, 0, 0))
    canvas.alpha_composite(crop, ((cw - nw) // 2, ch - nh))
    return canvas


def main(stage, dst=None):
    dst = dst or os.path.join(EX, 'ref', 'room_obj_parts', 'cut', stage)
    os.makedirs(dst, exist_ok=True)
    for sheet, (cols, rows, items) in SHEETS.items():
        path = os.path.join(EX, 'in', f'parts_{stage}_{sheet}.png')
        if not os.path.exists(path):
            print('없음', path)
            continue
        tiles = cells(path, cols, rows)
        for i, name, cw, ch in items:
            out = os.path.join(dst, f'obj_{stage}_{name}.png')
            fit(tiles[i], cw, ch).save(out)
            print(out)
    cut_water(stage, dst)


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else None)
