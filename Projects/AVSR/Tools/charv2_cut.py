# -*- coding: utf-8 -*-
"""char_v2(원작 기준 다시 그리기) 납품 판을 게임 규격으로 자른다 — 인게임 128 칸 + 로비용 큰 칸.

납품(마젠타 바탕):
  charv2_{키}_{방향}.png        3×3 판, 8자세 (발주서 칸 차례)
  charv2_{키}_float_{방향}.png  가로 2칸 — 떠서 이동하는 몸(구루 등)의 걷기 두 칸을 갈아 낀다(있을 때만)

  1 대기   2 걷기1   3 걷기2
  4 공격1  5 공격2   6 맞음
  7 쓰러짐1 8 쓰러짐2  9 빈 칸

결과:
  인게임  Assets/BaseResource/Unit/{키}/unit_{키}_{방향}{동작}.png   128 칸 — 96 칸 규격(잉크 76 · 발 아래 8 · 가운데 48)에 사방 16 여백
          (몸 크기는 96 칸 그림과 같다 · Unit.FitBodyCell 이 몸 칸을 4/3 로 키운다)
  로비    Assets/BaseResource/UnitLarge/{키}/unitL_{키}_se{동작}.png  같은 배치를 BIG 배(기본 3)로 · se 방향만 — 납품 원본에서 바로 줄인다(확대 아님)

쓰는 법:
  python charv2_cut.py <키> [--dry]        # 다섯 방향 전부 · --dry 면 Assets 에 안 쓰고 미리보기 폴더에만
미리보기: Projects/AVSR/_exchange/ref/char_v2/{키}_cells/ (인게임 칸) · {키}_compare.png
"""
import os
import sys

import numpy as np
from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
REF = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'char_v2')
UNIT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Unit')
LARGE = os.path.join(ROOT, 'Assets', 'BaseResource', 'UnitLarge')

STATES = ['', '_walk1', '_walk2', '_atk1', '_atk2', '_hit', '_die1', '_die2']
DIRS = ['s', 'se', 'e', 'ne', 'n']
CELL, PAD = 128, 16
INK_H = 76          # 96 칸 규격 잉크 높이(대기 기준)
FOOT = 96 - 8       # 96 칸 안 발밑 y
MID_X = 48
BIG = 3             # 로비 칸 = 128 × 3 = 384
LARGE_DIRS = ['se']  # 로비 진열은 3/4 오른쪽 아래만 — 한 몸 아틀라스를 한 장(2048)에 담는다
FLOAT_LIFT = 3      # 떠서 이동 — 대기보다 이만큼 떠 있다(96 칸 px)


def key_out(im):
    """마젠타 바탕 뚫기 — 판 가장자리에서 이어진 마젠타만 바탕(분홍 옷이 뚫리지 않게, unit_up_cut 과 같은 규칙)."""
    from scipy import ndimage
    a = np.asarray(im.convert('RGB')).astype(np.int16)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    dist = np.abs(a - np.array([255, 0, 255])).sum(axis=2)
    pink = (r > 110) & (b > 100) & (r - g > 55) & (b - g > 45)
    like_bg = (dist < 150) | pink
    labels, _ = ndimage.label(like_bg)
    edge = np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))
    bg = np.isin(labels, edge[edge > 0]) | (dist < 60)
    return Image.fromarray(np.dstack([a.astype(np.uint8), np.where(bg, 0, 255).astype(np.uint8)]), 'RGBA')


def bbox(im):
    m = np.asarray(im)[..., 3] > 16
    ys, xs = np.where(m)
    return xs.min(), ys.min(), xs.max(), ys.max()


def place(src, s, ox, oy, size):
    """src 를 s 배로 줄여 (ox, oy) 에 둔 size 칸 · 칸 밖으로 나간 px 수."""
    sm = src.resize((max(1, round(src.width * s)), max(1, round(src.height * s))), Image.LANCZOS)
    a = np.asarray(sm).copy()
    a[..., 3] = np.where(a[..., 3] > 110, 255, 0)
    sm = Image.fromarray(a, 'RGBA')
    big = Image.new('RGBA', (size * 3, size * 3), (0, 0, 0, 0))
    big.alpha_composite(sm, (ox + size, oy + size))
    out = big.crop((size, size, size * 2, size * 2))
    lost = int((np.asarray(big)[..., 3] > 0).sum() - (np.asarray(out)[..., 3] > 0).sum())
    return out, lost


def jobs_for(key, d):
    sheet = key_out(Image.open(os.path.join(IN, f'charv2_{key}_{d}.png')))
    W = sheet.width // 3
    cells = [sheet.crop(((i % 3) * W, (i // 3) * W, (i % 3 + 1) * W, (i // 3 + 1) * W)) for i in range(8)]
    ib = bbox(cells[0])
    s = INK_H / (ib[3] - ib[1] + 1)                  # 대기 키 = 76 — 모든 자세 같은 배율(같은 사람 · 같은 키)
    cx = (ib[0] + ib[2]) / 2
    jobs = {st: (c, s, MID_X - cx * s, FOOT - ib[3] * s) for st, c in zip(STATES, cells)}
    fp = os.path.join(IN, f'charv2_{key}_float_{d}.png')
    if os.path.exists(fp):
        F = key_out(Image.open(fp))
        FW = F.width // 2
        fc = [F.crop((0, 0, FW, F.height)), F.crop((FW, 0, 2 * FW, F.height))]
        fb = [bbox(c) for c in fc]
        sf = INK_H / (fb[0][3] - fb[0][1] + 1)
        drop = min(3, max(1, round((fb[1][3] - fb[0][3]) * sf)))
        for k, st in enumerate(['_walk1', '_walk2']):
            b = fb[k]
            foot = FOOT - FLOAT_LIFT + (drop if k else 0)
            jobs[st] = (fc[k], sf, MID_X - (b[0] + b[2]) / 2 * sf, foot - b[3] * sf)
    return jobs


def main():
    key = sys.argv[1]
    dry = '--dry' in sys.argv
    cells_dir = os.path.join(REF, f'{key}_cells')
    os.makedirs(cells_dir, exist_ok=True)
    if not dry:
        os.makedirs(os.path.join(UNIT, key), exist_ok=True)
        os.makedirs(os.path.join(LARGE, key), exist_ok=True)
    problems = []
    for d in DIRS:
        if not os.path.exists(os.path.join(IN, f'charv2_{key}_{d}.png')):
            problems.append(f'{d}: 납품 없음')
            continue
        for st, (c, s, ox, oy) in jobs_for(key, d).items():
            name = f'{key}_{d}{st}'
            small, lost = place(c, s, round(ox + PAD), round(oy + PAD), CELL)
            large, lost_l = place(c, s * BIG, round((ox + PAD) * BIG), round((oy + PAD) * BIG), CELL * BIG)
            if lost or lost_l:
                problems.append(f'{d}{st}: 칸 밖 {lost}px')
            small.save(os.path.join(cells_dir, f'unit_{name}.png'))
            if not dry:
                small.save(os.path.join(UNIT, key, f'unit_{name}.png'))
                if d in LARGE_DIRS:
                    large.save(os.path.join(LARGE, key, f'unitL_{name}.png'))
    print(key, '잘림 없음' if not problems else ' · '.join(problems))


if __name__ == '__main__':
    main()
