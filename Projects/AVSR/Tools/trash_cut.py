# -*- coding: utf-8 -*-
"""새 잡몹 납품 판(3 x 3, 마젠타)을 게임 규격 낱장으로 자른다 — 한 방향 아홉 칸을 **한 배율**로.

`unit_up_cut.py` 는 칸마다 잉크 높이를 76 에 맞춘다. 사람 몸은 그래도 되지만, 팔을 머리 위로 든 공격 칸 ·
웅크린 대기 칸처럼 칸마다 높이가 크게 다른 짐승은 칸마다 몸이 커졌다 작아졌다 했다(돌 고릴라 2026-10-07).
여기서는 서 있는 칸(대기 · 걷기 둘 · 피격)의 잉크 높이 중앙값으로 배율 하나를 정해 아홉 칸에 똑같이 쓴다.
판 가장자리에서 이어진 마젠타만 뚫고(옷 색 보호), 몸에서 떨어진 작은 부스러기는 버린다.

쓰는 법: python trash_cut.py <키> <방향> <납품 판>
결과  : Assets/BaseResource/Unit/<게임 키>/unit_<게임 키>_<방향>[_동작].png   (게임 키는 GAME_KEY 표)
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
CELL, INK_H, FOOT_UP = 96, 76, 8
ACTS = ['', '_walk1', '_walk2', '_atk1', '_atk2', '_hit', '_die1', '_die2']
# 아홉째 칸을 쓰는 몸 — 두더지의 땅속 흙더미(`_under`) · 살쾡이 사라짐 · 미믹 잠든 상자(`SetPose` 가 부른다)
NINTH = {'mole': '_under', 'shadow_cat': '_vanish', 'mimic': '_sleep', 'saw_monkey_m': '_empty'}
STANDING = [0, 1, 2, 5]
# 옆으로 넓은 몸 — 키로 맞추면 날개가 칸 밖으로 나가 잘린다(박쥐 2026-10-22 PD 「날개가 계속 짤려 있다」).
# 여덟 칸 중 가장 넓은 칸이 칸 폭(여백 2 px씩)에 들어오는 배율을 키 배율과 견줘 작은 쪽을 쓴다.
WIDE = {'bat', 'bat_poison', 'shadow_cat', 'mimic', 'boar_m', 'armadillo_m', 'saw_monkey_m'}
# 판에 그림자를 그려 온 떠 있는 몸 — 게임이 그림자를 따로 그리므로 몸 아래 납작한 덩어리를 뺀다(감시 눈 2026-10-22)
DROP_SHADOW = {'watcher_eye'}
# 새 그림 → 게임이 부르는 키. 돌 고릴라는 집행자 자리에 들어간다(키를 바꾸면 방 표 · 챕터 표를 다 고쳐야 한다)
GAME_KEY = {'gorilla': 'actor_enforcer', 'scorpion': 'scrapgunner',
            # 빨강 5종 기계 겉모습(PD 10-22) — 몸 키는 그대로 두고 그림만 바꾼다. 옛 그림은 자르기 전에 _archive 로
            'boar_m': 'boar', 'mantis_m': 'mantis', 'armadillo_m': 'armadillo', 'mushroom_m': 'mushroom',
            'saw_monkey_m': 'saw_monkey'}
# 몸 키(잉크 높이). 사람(호스트)은 76 이다. 덩치 큰 짐승은 폭이 넓어 같은 키여도 훨씬 커 보인다 —
# 돌 고릴라 76 은 「너무 크다」(PD 2026-10-07) → 68. 크게 보인 진짜 이유는 옛 집행자의 128 상자에 그려진 것이었다
#   (96 칸이 1.33 배로 커졌다) — 지금은 잡몹 상자(84)에 그린다. 사람보다 조금 낮고 폭이 넓다
BODY_H = {'gorilla': 68, 'boar': 60, 'mole': 58, 'mantis': 64, 'scorpion': 74,
          'armadillo': 52, 'mushroom': 56, 'mummy': 64, 'spider': 58, 'spiderling': 62,
          # 원소판은 원래 몸과 같은 키 · 2단계 새 몸(2026-10-22) — 떠 있는 둘은 해골보다 조금 작게, 살쾡이는 낮고 길게
          'gorilla_fire': 68, 'scrapgunner_ice': 74, 'watcher_eye': 58, 'lantern': 60, 'shadow_cat': 54, 'mimic': 72,
          # 기계 5종 — 발주 크기(쇄석기 · 방패 로봇은 낮고 길게, 저격기는 해골보다 조금 크게)
          'boar_m': 56, 'mantis_m': 74, 'armadillo_m': 54, 'mushroom_m': 62, 'saw_monkey_m': 68}   # 2026-10-07 묶음 넷 — 아르마딜로 · 거미는 낮고 넓다   # 멧돼지는 네발짐승 — 키가 낮고 길다(52 는 사람 몸의 반이라 안 보였다)


def drop_shadow(tile):
    """그려 온 그림자(어두운 자주색)를 지운다 — 분사 불꽃에 붙어 덩어리로는 안 떨어진다. 몸에는 자주색이 없다."""
    r, g, b = (tile[..., i].astype(int) for i in range(3))
    shade = (r > 18) & (b > 18) & (g < 0.6 * np.minimum(r, b)) & (np.maximum(r, b) < 170)
    tile[shade, 3] = 0
    # 남은 그림자 가장자리(거의 검정)는 몸의 가장 낮은 밝은 점보다 아래에 있다 — 그 아래를 비운다
    bright = (tile[..., 3] > 40) & (tile[..., :3].max(-1) >= 90)
    rows = np.nonzero(bright.any(1))[0]
    if len(rows):
        tile[rows[-1] + 2:, 3] = 0
        # 분사구 옆으로 번진 그림자 — 아래쪽 띠에서 밝은 점과 2 px 넘게 떨어진 어두운 점(외곽선은 밝은 점에 붙어 있다)
        near = ndimage.binary_dilation(bright, iterations=2)
        band = np.zeros_like(bright)
        band[max(0, rows[-1] - 10):] = True
        tile[band & ~near & (tile[..., :3].max(-1) < 90), 3] = 0


def cells(path, key=''):
    src = Image.open(path)
    # 가끔 마젠타 대신 진짜 투명 바탕으로 온다(얼음 코일 s 2026-10-22) — 그때는 그 알파를 그대로 쓴다
    if src.mode == 'RGBA' and np.asarray(src)[..., 3].min() == 0:
        rgba = np.asarray(src).copy()
    else:
        rgba = np.asarray(key_out(src.convert('RGB')))
    h, w = rgba.shape[:2]
    out = []
    for i in range(9):
        r, c = divmod(i, 3)
        tile = rgba[r * h // 3:(r + 1) * h // 3, c * w // 3:(c + 1) * w // 3].copy()
        a = tile[..., 3] > 40
        lab, n = ndimage.label(a)
        if n:
            sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
            keep = np.isin(lab, [k + 1 for k, s in enumerate(sizes) if s >= max(30, sizes.max() * 0.02)])
            # 옆 칸 그림이 칸 경계를 넘어 들어온 작은 조각(등불 탄 2026-10-22)
            th, tw = a.shape
            for k, sl in enumerate(ndimage.find_objects(lab)):
                edge = sl[1].start == 0 or sl[1].stop == tw or sl[0].start == 0 or sl[0].stop == th
                if edge and sizes[k] < sizes.max() * 0.15:
                    keep[lab == k + 1] = False
            tile[~keep, 3] = 0
        if key in DROP_SHADOW:
            drop_shadow(tile)
        out.append(tile)
    return out


def bbox(t):
    ys, xs = np.nonzero(t[..., 3] > 40)
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1


# 원소판 → 원래 몸 폴더. 원소판은 원래 몸과 **같은 크기**여야 같은 몹으로 읽힌다 —
# 표의 키(BODY_H)가 아니라 원래 몸 그림의 같은 방향 서 있는 칸을 재서 맞춘다(불 고릴라가 작게 나왔다 2026-10-22)
ELEMENT_BASE = {'gorilla_fire': 'actor_enforcer', 'scrapgunner_ice': 'scrapgunner', 'skeleton_poison': 'skeleton',
                'bat_poison': 'bat', 'coilwalker_ice': 'coilwalker', 'roadwarden_fire': 'roadwarden'}


# 방향마다 키를 따로 — 살쾡이 앞 · 뒷모습은 꼬리가 머리 위로 말려 같은 키면 몸이 옆모습보다 작아 보인다(2026-10-22)
DIR_H = {'shadow_cat': {'s': 68, 'n': 68}}


def base_height(base, d):
    hs = []
    for act in [ACTS[i] for i in STANDING]:
        p = os.path.join(ROOT, 'Assets', 'BaseResource', 'Unit', base, f'unit_{base}_{d}{act}.png')
        if os.path.exists(p):
            t = np.asarray(Image.open(p).convert('RGBA'))
            if (t[..., 3] > 40).any():
                hs.append(bbox(t)[3] - bbox(t)[1])
    return sorted(hs)[len(hs) // 2] if hs else None


def main(key, d, path, out_dir=None):
    tiles = cells(path, key)
    heights = sorted(bbox(tiles[i])[3] - bbox(tiles[i])[1] for i in STANDING)
    target = base_height(ELEMENT_BASE[key], d) if key in ELEMENT_BASE else None
    target = target or DIR_H.get(key, {}).get(d) or BODY_H.get(key, INK_H)
    scale = target / heights[len(heights) // 2]
    if key in WIDE:
        widest = max(bbox(t)[2] - bbox(t)[0] for t in tiles[:len(ACTS) + (key in NINTH)] if (t[..., 3] > 40).any())
        scale = min(scale, (CELL - 4) / widest)
    game = GAME_KEY.get(key, key)
    dst = out_dir or os.path.join(ROOT, 'Assets', 'BaseResource', 'Unit', game)
    os.makedirs(dst, exist_ok=True)
    acts = ACTS + ([NINTH[key]] if key in NINTH else [])
    for i, act in enumerate(acts):
        t = tiles[i]
        x0, y0, x1, y1 = bbox(t)
        crop = Image.fromarray(t[y0:y1, x0:x1])
        nw, nh = max(1, round((x1 - x0) * scale)), max(1, round((y1 - y0) * scale))
        if nw > CELL or nh > CELL - FOOT_UP:   # 칸을 넘으면 넘는 칸만 줄인다(드물다)
            k = min(CELL / nw, (CELL - FOOT_UP) / nh)
            nw, nh = int(nw * k), int(nh * k)
        crop = crop.resize((nw, nh), Image.NEAREST)
        canvas = Image.new('RGBA', (CELL, CELL), (0, 0, 0, 0))
        canvas.alpha_composite(crop, ((CELL - nw) // 2, CELL - FOOT_UP - nh))
        canvas.save(os.path.join(dst, f'unit_{game}_{d}{act}.png'))
    print(key, d, 'scale %.3f' % scale, '->', dst)


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4] if len(sys.argv) > 4 else None)
