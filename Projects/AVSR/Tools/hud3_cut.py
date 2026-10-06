# -*- coding: utf-8 -*-
"""인게임 HUD 3차(D안) — 코덱스가 그린 «빈 판»에서 부품을 오려 낸다.

그림은 그리지 않는다. 판을 자르고, 마젠타 바탕을 뚫고, 가장자리에 번진 마젠타 기운을 걷어 낼 뿐이다.

  원본 (Projects/AVSR/_exchange/ref/ingame_ui/)
    hud3_clean_host.png    몸이 있을 때의 빈 판 (1080 x 1920, 시안과 같은 자리)
    hud3_clean_ghost.png   유령 상태의 빈 판
    hud_draft_D2_ghost.png 유령 상태 시안 — 몸 칸의 «빈 얼굴» · «빈 막대» 덧판을 여기서 오린다
    hud3_bar_fills.png     막대 채움 세 줄
    hud3_pad_base.png · hud3_pad_knob.png · hud3_possess_frame.png

  결과 : Assets/BaseResource/InGameMainUI/hud3/hud3_*.png  (시안 1080 해상도 그대로)
  자리 : 같은 폴더의 hud3_rects.json — 부품이 시안(1080 x 1920)의 어디에 놓이는지(잉크 기준)

쓰는 법 : python hud3_cut.py
"""
import json
import os

import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
SRC = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'ingame_ui')
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI', 'hud3')
MAGENTA = np.array([255, 0, 255])


def key_out(rgb):
    """마젠타를 뚫는다. 배경은 **가장자리에서 이어진** 마젠타만 — 보라 테두리를 지우지 않게.

    번진 빛(글로우)은 마젠타와 섞여 있다. 배경 경계에서 몇 픽셀 안쪽까지만 섞임을 풀어
    (색 = (픽셀 − (1−a)·마젠타) / a) 반투명 빛으로 되돌린다.
    """
    a = rgb.astype(np.float32)
    dist = np.abs(a - MAGENTA).sum(axis=2)
    near = dist < 90
    lab, _ = ndimage.label(near)
    edge = np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))
    bg = np.isin(lab, edge[edge > 0])
    ring = ndimage.binary_dilation(bg, iterations=5) & ~bg
    alpha = np.ones(dist.shape, np.float32)
    alpha[bg] = 0.0
    ramp = np.clip((dist - 60.0) / 220.0, 0.0, 1.0)
    alpha[ring] = np.maximum(ramp[ring], 0.0)
    out = a.copy()
    m = ring & (alpha > 0.02)
    out[m] = (a[m] - (1.0 - alpha[m])[:, None] * MAGENTA) / alpha[m][:, None]
    out = np.clip(out, 0, 255)
    return np.dstack([out, alpha * 255.0]).astype(np.uint8)


def load(name):
    return np.asarray(Image.open(os.path.join(SRC, name)).convert('RGB'))


def save(name, rgba, origin, rects):
    """잉크만 남게 다듬어 저장하고, 시안 속 자리를 적는다."""
    al = rgba[..., 3]
    ys, xs = np.nonzero(al > 8)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    Image.fromarray(rgba[y0:y1, x0:x1]).save(os.path.join(OUT, name + '.png'))
    rects[name] = [int(origin[0] + x0), int(origin[1] + y0), int(x1 - x0), int(y1 - y0)]


def cut(src, box, name, rects, keep_left=None):
    x0, y0, x1, y1 = box
    rgba = key_out(src[y0:y1, x0:x1])
    if keep_left is not None:   # 이웃 부품의 빛이 넘어온 쪽을 잘라 낸다
        rgba[:, keep_left:, 3] = 0
    # 오린 칸 가장자리에 걸린 이웃 부품의 빛 부스러기를 버린다 — 큰 덩어리만 남긴다
    lab, n = ndimage.label(rgba[..., 3] > 8)
    if n > 1:
        sizes = ndimage.sum(np.ones(lab.shape), lab, range(1, n + 1))
        keep = np.isin(lab, [i + 1 for i, v in enumerate(sizes) if v >= 0.05 * sizes.max()])
        rgba[~keep, 3] = 0
    save(name, rgba, (x0, y0), rects)


def patch(src, box, name, rects, circle=None):
    """불투명 덧판(유령 상태 몸 칸의 빈 얼굴 · 빈 막대). circle=(cx, cy, r) 이면 원으로 오린다."""
    x0, y0, x1, y1 = box
    rgb = src[y0:y1, x0:x1]
    alpha = np.full(rgb.shape[:2], 255.0, np.float32)
    if circle is not None:
        cx, cy, r = circle
        yy, xx = np.mgrid[y0:y1, x0:x1]
        d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2)
        alpha = np.clip((r - d) * 255.0 / 2.0, 0, 255)
    else:   # 가장자리 3px 를 부드럽게 — 판의 남색과 이음매가 안 보이게
        h, w = alpha.shape
        e = np.minimum.reduce([np.arange(w)[None, :].repeat(h, 0), np.arange(w)[::-1][None, :].repeat(h, 0),
                               np.arange(h)[:, None].repeat(w, 1), np.arange(h)[::-1][:, None].repeat(w, 1)])
        alpha = np.clip(e / 3.0, 0, 1) * 255.0
    save(name, np.dstack([rgb, alpha]).astype(np.uint8), (x0, y0), rects)


def main():
    os.makedirs(OUT, exist_ok=True)
    rects = {}
    host = load('hud3_clean_host.png')
    ghost = load('hud3_clean_ghost.png')
    shot_g = load('hud_draft_D2_ghost.png')

    # ── 위 ──
    cut(host, (6, 28, 460, 203), 'hud3_ghost_pill', rects)
    cut(host, (621, 28, 1075, 204), 'hud3_host_pill', rects)
    cut(host, (450, 145, 632, 197), 'hud3_stage_track', rects)
    cut(host, (226, 197, 339, 310), 'hud3_pause', rects)
    cut(host, (21, 203, 232, 304), 'hud3_gold_chip', rects)
    cut(host, (897, 203, 1061, 366), 'hud3_aff_ring', rects)
    # ── 유령 상태 덧판 (시안에서 — 판 안쪽 남색 위에 그려진 것이라 불투명으로 덮는다) ──
    patch(shot_g, (933, 66, 1043, 178), 'hud3_host_empty_face', rects, circle=(988, 122, 52))
    patch(shot_g, (662, 138, 928, 184), 'hud3_host_empty_bar', rects)
    # ── 아래 ──
    cut(host, (673, 1622, 877, 1874), 'hud3_skill_frame', rects)
    cut(ghost, (673, 1622, 877, 1874), 'hud3_skill_empty', rects)

    # 따로 받은 부품 — 시안 속 자리는 판에서 잰 값에 맞춘다(잉크 크기는 그대로, 가운데 맞춤)
    def single(file, name, center):
        rgba = key_out(load(file))
        al = rgba[..., 3]
        ys, xs = np.nonzero(al > 8)
        w, h = xs.max() + 1 - xs.min(), ys.max() + 1 - ys.min()
        save(name, rgba, (center[0] - w / 2 - xs.min(), center[1] - h / 2 - ys.min()), rects)

    single('hud3_pad_base.png', 'hud3_pad_base', (139.5, 1739.0))
    single('hud3_pad_knob.png', 'hud3_pad_knob', (140.0, 1740.0))
    if os.path.exists(os.path.join(SRC, 'hud3_possess_frame.png')):
        single('hud3_possess_frame.png', 'hud3_possess_frame', (972.0, 1748.0))

    # ── 막대 채움 — 띠 가운데만 오려 낸다(끝의 번짐 없이) ──
    fills = load('hud3_bar_fills.png')
    # 보라 띠는 마젠타와 가깝다(거리 ~105) — 문턱을 낮추고 작은 부스러기는 버린다
    dist = np.abs(fills.astype(int) - MAGENTA).sum(axis=2) > 60
    lab, n = ndimage.label(dist)
    names = ['hud3_fill_red', 'hud3_fill_purple', 'hud3_fill_cyan']
    objs = [sl for sl in ndimage.find_objects(lab)
            if (sl[0].stop - sl[0].start) * (sl[1].stop - sl[1].start) > 2000]
    objs = sorted(objs, key=lambda s: s[0].start)
    for name, sl in zip(names, objs):
        y0, y1 = sl[0].start + 2, sl[0].stop - 2
        x0, x1 = sl[1].start + 4, sl[1].stop - 4
        Image.fromarray(fills[y0:y1, x0:x1]).convert('RGBA').save(os.path.join(OUT, name + '.png'))
        rects[name] = [0, 0, int(x1 - x0), int(y1 - y0)]

    with open(os.path.join(OUT, 'hud3_rects.json'), 'w', encoding='utf-8') as f:
        json.dump(rects, f, ensure_ascii=False, indent=1)
    for k, v in rects.items():
        print(k, v)


if __name__ == '__main__':
    main()
