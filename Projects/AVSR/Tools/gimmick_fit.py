# -*- coding: utf-8 -*-
"""기믹 그림(통·포탑·바위·되튕기는 벽·불바닥)을 게임 규격 칸에 앉힌다.

납품은 크기는 맞지만 물건이 캔버스 **가운데에 떠 있거나 작게** 그려져 오는 일이 있다
(정유소 되튕기는 벽: 채움 15 % · 사방 여백 32~54 px). 그대로 쓰면 발자국 위에 떠서 선다.
그리는 것이 아니라 **앉히는 것**이다 — 잉크를 잘라 발밑(아래 가운데)에 붙이고,
폭이 칸의 80 % 에 못 미치면 칸에 맞게 키운다. 바닥에 눕는 것(불바닥)은 가운데 그대로 두고 폭만 맞춘다.

쓰는 법:  python gimmick_fit.py            # _exchange/in 의 obj_*{종류}.png 전부
원본은 _exchange/in/_raw/ 에 한 번만 떠 둔다(이미 있으면 덮지 않는다 — 다시 돌려도 원본에서 시작한다).
"""
import os
import re
import shutil

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
RAW = os.path.join(IN, '_raw')

SIZE = {'barrel': (72, 112), 'wallturret_s': (72, 132), 'wallturret_e': (72, 132), 'rock': (72, 112),
        'ricochet_wall_h': (216, 162), 'ricochet_wall_v': (72, 306), 'hazard': (144, 144),
        # 3차 (2026-09-28) — 화염 분사구 · 지뢰 · 무대 간판 소품
        'flamejet_s': (72, 112), 'flamejet_e': (72, 112), 'mine': (72, 72),
        'prop_tall': (72, 166), 'prop_wide': (144, 132)}
FLAT = {'hazard', 'mine'}  # 바닥에 눕는다 — 가운데에 둔다
NO_GROW = {'mine'}         # 일부러 칸보다 작게 그린 것 — 키우지 않는다
MIN_FILL_W = 0.8           # 잉크 폭이 칸의 이만큼에 못 미치면 키운다
FLIP = {'wallturret_e': 'wallturret_w', 'flamejet_e': 'flamejet_w'}   # 오른쪽 것을 뒤집어 왼쪽 것을 만든다
PAT = re.compile(r'^obj_(?:(street|rooftop|lab|refinery|junkyard|missile|holding)_)?'
                 r'(barrel|wallturret_s|wallturret_e|rock|ricochet_wall_h|ricochet_wall_v|hazard'
                 r'|flamejet_s|flamejet_e|mine|prop_tall|prop_wide)\.png$')


def fit(im, kind):
    w, h = SIZE[kind]
    if im.size != (w, h):
        im = im.resize((w, h), Image.LANCZOS)
    a = np.asarray(im)
    ys, xs = np.nonzero(a[..., 3] > 8)
    if len(xs) == 0:
        return im, '빈 그림'
    ink = im.crop((int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1))
    note = []
    if ink.width < w * MIN_FILL_W and kind not in NO_GROW:
        s = min(w / ink.width, h / ink.height)
        ink = ink.resize((max(1, round(ink.width * s)), max(1, round(ink.height * s))), Image.LANCZOS)
        q = np.asarray(ink).copy()
        q[..., 3] = np.where(q[..., 3] > 110, 255, 0)      # 키우며 생긴 반투명 가장자리는 도트답게 자른다
        ink = Image.fromarray(q)
        note.append(f'×{s:.2f}')
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    x = (w - ink.width) // 2
    y = (h - ink.height) // 2 if kind in FLAT else h - ink.height
    out.alpha_composite(ink, (x, y))
    note.append('가운데' if kind in FLAT else '발밑')
    return out, ' · '.join(note)


def main():
    os.makedirs(RAW, exist_ok=True)
    for name in sorted(os.listdir(IN)):
        m = PAT.match(name)
        if not m:
            continue
        kind = m.group(2)
        raw = os.path.join(RAW, name)
        if not os.path.exists(raw):
            shutil.copy(os.path.join(IN, name), raw)
        out, note = fit(Image.open(raw).convert('RGBA'), kind)
        out.save(os.path.join(IN, name))
        a = np.asarray(out)[..., 3] > 8
        ys, xs = np.nonzero(a)
        w, h = out.size
        print(f'{name}: {note} · 여백 좌{xs.min()} 우{w - 1 - xs.max()} 아래{h - 1 - ys.max()} 위{ys.min()}')
        if kind in FLIP:
            west = name.replace(kind, FLIP[kind])
            out.transpose(Image.FLIP_LEFT_RIGHT).save(os.path.join(IN, west))
            print(f'{west}: 반전')


if __name__ == '__main__':
    main()
