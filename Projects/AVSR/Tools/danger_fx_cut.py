# -*- coding: utf-8 -*-
"""위험 예고 그림 납품(_exchange/ref/danger_fx/fx_*.png) → Assets/BundleResource/ParticleFx/Danger/.

채움 · 차오름 4장은 그대로 옮긴다. 테두리 띠 2장은 빛이 아래 20줄에만 있어 위쪽 빈 44줄을 잘라 낸다 —
게임은 띠 그림 전체를 둘레 두께(14 px)에 펴므로, 빈 줄이 남으면 빛이 3 px 로 얇아진다.
그다음 Unity 에서 `Tools/Game/위험 예고 그림 들이기` 를 돌린다(주소 Fx/*).
"""
import os

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
SRC = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'danger_fx')
DST = os.path.join(ROOT, 'Assets', 'BundleResource', 'ParticleFx', 'Danger')

if __name__ == '__main__':
    os.makedirs(DST, exist_ok=True)
    for k in ['danger_fill', 'danger_core', 'safe_fill', 'safe_core']:
        Image.open(os.path.join(SRC, 'fx_%s.png' % k)).convert('RGBA').save(os.path.join(DST, 'fx_%s.png' % k))
    for k in ['danger_edge', 'safe_edge']:
        im = Image.open(os.path.join(SRC, 'fx_%s.png' % k)).convert('RGBA')
        im.crop((0, 40, im.size[0], im.size[1])).save(os.path.join(DST, 'fx_%s.png' % k))
    print('ok')
