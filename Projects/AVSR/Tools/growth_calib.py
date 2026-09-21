# -*- coding: utf-8 -*-
"""육성 화면 글자 보정 — 시안 글자와 게임 글자의 잉크를 재서 자리 · 크기 · 굵기 · 가로 비율을 고친다.

쓰는 법(스샷은 LobbyShotTool — 한국어 · 720×1280):
  python growth_calib.py [--apply]

  시안 잉크 = 글자 칸 안에서 바탕(칸 둘레 가운뎃값)과 다른 픽셀
  게임 잉크 = |글자 있는 스샷 − 글자 뺀 스샷|
보정은 **스펙 이름** 단위로 `Assets/BaseResource/Growth/growth_calib.json` 에 쌓인다.

⚠ 글자 내용이 시안과 다른 칸(레벨 숫자 · 비용 · 이름)은 **높이만** 맞춘다 — 폭 · 자리를 재면 틀린다.
"""
import json
import os
import sys

import cv2
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
REF = os.path.join(HERE, '..', '_exchange', 'ref', 'growth_v1')
CALIB = os.path.join(ROOT, 'Assets', 'BaseResource', 'Growth', 'growth_calib.json')
SHOT = os.path.join(os.environ['TEMP'], 'claude')
sys.path.insert(0, HERE)
from growth_build import text_mask  # noqa: E402

MOCK = {'g': 'growth_ghost_stats_v4.png', 'h': 'growth_host_stats_v4.png', 's': 'growth_host_skill_v4.png'}
GAME = {'g': ('growth_ghost_ko', 'growth_ghost_bare'), 'h': ('growth_host_ko', 'growth_host_bare')}

# (스펙 이름, 시안, 시안 칸, 게임 쪽, 게임 칸 세로 밀림, 방식)  방식: full · height · size(높이 · 굵기 · 세로)
ITEMS = [
    ('GoldText', 'g', (384, 20, 502, 52), 'g', 0, 'height'),
    ('TabOnText', 'g', (160, 160, 340, 208), 'g', 0, 'full'),
    ('CardNameEn', 'g', (288, 248, 470, 288), 'g', 0, 'full'),
    ('CardName', 'g', (288, 287, 420, 313), 'g', 0, 'full'),
    ('CardDesc', 'g', (288, 314, 520, 382), 'g', 0, 'width'),   # 세 줄 — 높이는 줄 간격이 정한다, 크기는 폭으로
    ('CardLvLabel', 'g', (288, 390, 334, 422), 'g', 0, 'full'),
    ('ExpLabel', 'g', (288, 434, 336, 460), 'g', 0, 'full'),
    ('StatHeader', 'h', (36, 538, 174, 574), 'h', 0, 'full'),
    ('StatTabOn', 'h', (100, 486, 280, 528), 'h', 0, 'full'),
    ('RowName', 'g', (120, 590, 240, 616), 'g', -50, 'full'),
    ('RowLvLabel', 'g', (272, 592, 300, 616), 'g', -50, 'full'),
    ('RowPct', 'g', (424, 590, 500, 616), 'g', -50, 'height'),
    ('ListHeader', 'h', (36, 794, 172, 830), 'h', 0, 'full'),
    ('SortText', 'h', (556, 800, 618, 826), 'h', 0, 'full'),
    ('PathTitle', 'g', (36, 804, 211, 842), 'g', 0, 'full'),   # 오른쪽 끝은 「?」 아이콘(212~) 직전까지
    ('PathLabelLock', 'g', (502, 1006, 566, 1034), 'g', 0, 'full'),
    ('BoxLvLabel', 'g', (360, 1066, 398, 1092), 'g', 0, 'full'),
    ('ExpValue', 'g', (560, 436, 690, 460), 'g', 0, 'height'),
    # 하단 바 부제는 빌더가 「보통」 스펙으로 세운다 — 선택된 HOST 칸에서 재도 글자 · 자리는 같다
    ('Nav_host_off', 'h', (116, 1232, 234, 1266), 'h', 0, 'full'),
    ('Nav_play_off', 'h', (352, 1232, 464, 1266), 'h', 0, 'full'),
    ('Nav_shop_off', 'h', (588, 1232, 662, 1266), 'h', 0, 'full'),
]
THRESH = 60


def load(p):
    return np.asarray(Image.open(p).convert('RGB').resize((720, 1280), Image.LANCZOS)).astype(np.int16)


def game_ink(a, b, box):
    x0, y0, x1, y1 = box
    d = np.abs(a[y0:y1, x0:x1] - b[y0:y1, x0:x1]).max(axis=2)
    m = d > THRESH
    cnt, lab, stats, _ = cv2.connectedComponentsWithStats(m.astype(np.uint8), connectivity=8)
    for i in range(1, cnt):
        if stats[i, cv2.CC_STAT_AREA] < 6:
            m[lab == i] = False
    ys, xs = np.nonzero(m)
    if len(xs) < 8:
        return None
    return dict(x0=x0 + xs.min(), x1=x0 + xs.max() + 1, y0=y0 + ys.min(), y1=y0 + ys.max() + 1, count=int(m.sum()))


def mock_ink(img, box):
    m, _ = text_mask(img.astype(np.uint8), box, 90)
    ys, xs = np.nonzero(m)
    if len(xs) < 8:
        return None
    x0, y0 = box[0], box[1]
    return dict(x0=x0 + xs.min(), x1=x0 + xs.max() + 1, y0=y0 + ys.min(), y1=y0 + ys.max() + 1, count=int(m.sum()))


def main():
    apply = '--apply' in sys.argv
    mocks = {k: np.asarray(Image.open(os.path.join(REF, v)).convert('RGB')).astype(np.int16) for k, v in MOCK.items()}
    games = {k: (load(os.path.join(SHOT, a + '.png')), load(os.path.join(SHOT, b + '.png'))) for k, (a, b) in GAME.items()}
    calib = {'items': []}
    if os.path.exists(CALIB):
        calib = json.load(open(CALIB, encoding='utf-8'))
    by = {c['name']: c for c in calib['items']}
    worst = 0.0
    for name, mk, mbox, gk, gdy, mode in ITEMS:
        m = mock_ink(mocks[mk], mbox)
        gbox = (mbox[0], mbox[1] + gdy, mbox[2], mbox[3] + gdy)
        o = game_ink(games[gk][0], games[gk][1], gbox)
        if m is None or o is None:
            print(f'{name:16s} 잉크 없음 m={m is not None} o={o is not None}')
            continue
        mw, mh = m['x1'] - m['x0'], m['y1'] - m['y0']
        ow, oh = o['x1'] - o['x0'], o['y1'] - o['y0']
        sh = mh / oh
        if mode == 'full':
            s = (mw / ow * mh / oh) ** 0.5
            dx = m['x0'] - o['x0']
        elif mode == 'width':
            s, dx = mw / ow, m['x0'] - o['x0']
            sh = s
        else:
            s, dx = sh, 0.0
        dy = (m['y0'] + m['y1']) / 2 - (o['y0'] + o['y1'] - 2 * gdy) / 2
        r = m['count'] / max(1, o['count'] * s * s) if mode in ('full', 'size') else 1.0
        err = max(abs(dx), abs(dy), abs(s - 1) * max(mw, mh), abs(r - 1) * 10 if mode != 'height' else 0)
        worst = max(worst, err)
        print(f'{name:16s} dx {dx:+5.1f} dy {dy:+5.1f} 크기 {s:5.3f} 굵기 {r:5.3f}  시안 {mw}x{mh} 게임 {ow}x{oh}  ({mode})')
        if apply:
            c = by.setdefault(name, {'name': name, 'dx': 0.0, 'dy': 0.0, 'scale': 1.0, 'dilate': 0.0, 'aspect': 1.0})
            c['scale'] = round(c['scale'] * sh ** 0.8, 4)
            if mode == 'full':
                c['aspect'] = round(c['aspect'] * ((mw / ow) / sh) ** 0.8, 4)
            if mode in ('full', 'width'):
                c['dx'] = round(c['dx'] + 0.8 * dx, 2)
            if mode != 'width':   # 여러 줄은 세로를 줄 간격이 정한다 — 가운데로 재면 틀린다
                c['dy'] = round(c['dy'] + 0.8 * dy, 2)
            if mode in ('full', 'size'):
                c['dilate'] = round(float(np.clip(c['dilate'] + 0.3 * (r - 1), -0.5, 0.5)), 3)
    print(f'가장 큰 어긋남 {worst:.1f}')
    if apply:
        # 경로 라벨은 모두 같은 글꼴 · 크기다 — 빛이 번지지 않는 「Lv. 40」 에서 잰 값을 나눠 쓴다
        # (「Lv. 10」 · 「Lv. 30」 은 시안 빛 번짐까지 잉크로 잡혀 키가 23 으로 나온다)
        lock = by.get('PathLabelLock')
        if lock:
            for n in ('PathLabelDone', 'PathLabelNext', 'PathLabelNow'):
                by[n] = dict(lock, name=n)
        calib['items'] = list(by.values())
        json.dump(calib, open(CALIB, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
        print('보정표 갱신:', CALIB)


if __name__ == '__main__':
    main()
