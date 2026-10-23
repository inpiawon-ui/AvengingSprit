# -*- coding: utf-8 -*-
"""소환 4단 사다리(PD 2026-10-23 「하늘 → 보라 → 금 → 무지개」) 부품 준비 — 받은 그림을 자르고 색상만 돌린다(새로 그리지 않는다).
- lab_reveal_light_rainbow(납품, 가로 6칸) → lab_reveal_light_rainbow_01~06 (금빛 판과 같은 자르기)
- lab_tank_glass(청록 185°) → lab_tank_glass_t0~t3 : 하늘 197° · 보라 264° · 금 45° · 흰금(금을 옅게 · 밝게)
- reveal_ring(파랑) → lab_ring_white_01~06 : 밝기만 남긴 흰 고리 — 게임이 등급색으로 물들인다(색 있는 그림에 색을 곱하지 않게)
"""
import os

import numpy as np
from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
D = os.path.join(ROOT, 'Assets', 'BaseResource', 'SummonReveal')


def hsv(a):
    mx, mn = a.max(-1), a.min(-1)
    d = mx - mn
    h = np.zeros_like(mx)
    m = d > 1e-6
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    rm = m & (mx == r)
    gm = m & (mx == g) & ~rm
    bm = m & ~rm & ~gm
    h[rm] = ((g - b)[rm] / d[rm]) % 6
    h[gm] = (b - r)[gm] / d[gm] + 2
    h[bm] = (r - g)[bm] / d[bm] + 4
    return h / 6, np.where(mx > 0, d / np.maximum(mx, 1e-6), 0), mx


def rgb(h, s, v):
    i = np.floor(h * 6).astype(int) % 6
    f = h * 6 - np.floor(h * 6)
    p, q, t = v * (1 - s), v * (1 - f * s), v * (1 - (1 - f) * s)
    out = np.zeros(h.shape + (3,))
    for k, (r, g, b) in enumerate([(v, t, p), (q, v, p), (p, v, t), (p, q, v), (t, p, v), (v, p, q)]):
        m = i == k
        out[m] = np.stack([r[m], g[m], b[m]], -1)
    return out


def main():
    # 무지개 빛 — lab_reveal_light 와 같은 자르기(cut_lab2.py)
    L = Image.open(os.path.join(IN, 'lab_reveal_light_rainbow.png')).convert('RGB')
    cw = L.width // 6
    top = max(0, (L.height - cw) // 2 - 20)
    for i in range(6):
        L.crop((i * cw, top, (i + 1) * cw, top + cw)).save(os.path.join(D, f'lab_reveal_light_rainbow_{i + 1:02d}.png'))

    # 배양액 유리 4단
    g = np.asarray(Image.open(os.path.join(D, 'lab_tank_glass.png')).convert('RGB')).astype(float) / 255
    h, s, v = hsv(g)
    h0 = 185 / 360
    for tag, hue, sk, vk in [('t0', 197, 1.0, 1.0), ('t1', 264, 1.0, 1.0), ('t2', 45, 1.0, 1.1), ('t3', 45, 0.55, 1.3)]:
        out = rgb((h + hue / 360 - h0) % 1, np.clip(s * sk, 0, 1), np.clip(v * vk, 0, 1))
        Image.fromarray((out * 255).round().astype(np.uint8)).save(os.path.join(D, f'lab_tank_glass_{tag}.png'))

    # 배양조 — 유리창 안 배양액(청록 165~195°)만 단계색으로. 받침 · 뚜껑의 청록 불빛은 그대로(유리창 밖)
    t = np.asarray(Image.open(os.path.join(D, 'lab_tank.png')).convert('RGBA')).astype(float) / 255
    th, ts, tv = hsv(t[..., :3])
    win = np.zeros(th.shape, bool)
    win[154:614, 93:350] = True   # 유리창(lab_crack 맞춤 때 잰 자리, 444×720 기준)
    liquid = win & (th * 360 >= 165) & (th * 360 <= 195) & (ts > 0.25)
    for tag, hue, sk, vk in [('t0', 197, 1.0, 1.0), ('t1', 268, 1.0, 1.0), ('t2', 44, 1.05, 1.08), ('t3', 44, 0.45, 1.25)]:
        nh = np.where(liquid, (th + hue / 360 - h0) % 1, th)
        ns = np.where(liquid, np.clip(ts * sk, 0, 1), ts)
        nv = np.where(liquid, np.clip(tv * vk, 0, 1), tv)
        out = np.concatenate([rgb(nh, ns, nv), t[..., 3:4]], -1)
        Image.fromarray((out * 255).round().astype(np.uint8), 'RGBA').save(os.path.join(D, f'lab_tank_{tag}.png'))

    # 흰 고리
    for i in range(1, 7):
        a = np.asarray(Image.open(os.path.join(D, f'reveal_ring_{i:02d}.png')).convert('RGB')).astype(float)
        w = a.max(-1)
        w[w < 60] = 0   # 둘레 뿌연 번짐 — 물들이면 색 안개가 된다
        Image.fromarray(np.repeat(w[..., None], 3, -1).round().astype(np.uint8)).save(os.path.join(D, f'lab_ring_white_{i:02d}.png'))
    print('ok')


if __name__ == '__main__':
    main()
