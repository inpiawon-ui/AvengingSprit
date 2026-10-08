"""팝업 · 방 오브젝트 · 클리어 연출 미리보기 GIF (2026-10-08).

PD 「이펙트만 보고 맞냐고 하면 모른다 — 적용된 걸 봐야 안다」.
유니티는 다른 세션이 쓰는 중이라, **게임 캡처 + 게임 배치값(popup_c_layout.json) + 납품 부품(out/fxui)** 으로
게임에 넣을 박자(상의 consult_popup_fx_parts.md 1절) 그대로 합성한다. 입자는 ParticleFxPool 처럼 도트 네모.
python fx_preview.py [화면 ...] → _exchange/ref/fx_preview/{화면}.gif
"""
import json
import math
import os
import random
import sys

import numpy as np
from PIL import Image, ImageEnhance

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
CAP = os.path.join(EX, 'ref', 'popup_c_ingame')
FX = os.path.join(EX, 'out', 'fxui')
ART = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')
OUT = os.path.join(EX, 'ref', 'fx_preview')
W, H = 720, 1280
FPS = 25
GOLD = (255, 205, 80)
CYAN = (90, 240, 245)
RED = (255, 70, 60)
AMBER = (255, 170, 50)
WHITE = (255, 255, 255)

LAYOUT = {n['name']: n for n in json.load(open(os.path.join(HERE, 'popup_c_layout.json'), encoding='utf-8'))['nodes']}


def rect(name, orig=False):
    """화면 좌표 (x, y, w, h). orig=True 면 창을 세로로 늘리기 전 값(popup_c_layout.stretch 의 oy · oh)."""
    k = (lambda q, f: q.get('o' + f, q[f])) if orig else (lambda q, f: q[f])
    n = LAYOUT[name]
    x, y = n['x'], k(n, 'y')
    p = n['parent']
    while p:
        q = LAYOUT[p]
        x += q['x']
        y += k(q, 'y')
        p = q['parent']
    return x, y, n['w'], k(n, 'h')


_cache = {}


def frames(name):
    if name not in _cache:
        fs = []
        for i in range(1, 9):
            p = os.path.join(FX, 'fx_ui_%s_%d.png' % (name, i))
            if os.path.exists(p):
                fs.append(Image.open(p).convert('RGBA'))
        _cache[name] = fs
    return _cache[name]


def tint(im, color, alpha=1.0):
    a = np.asarray(im).astype(np.float32)
    for c in range(3):
        a[..., c] *= color[c] / 255.0
    a[..., 3] *= alpha
    return Image.fromarray(a.clip(0, 255).astype(np.uint8), 'RGBA')


def add(base, layer, at):
    """가산 합성 — 빛은 더한다(게임 AdditiveLight 와 같은 느낌)."""
    x, y = int(at[0]), int(at[1])
    lw, lh = layer.size
    x0, y0 = max(0, x), max(0, y)
    x1, y1 = min(W, x + lw), min(H, y + lh)
    if x1 <= x0 or y1 <= y0:
        return
    l = np.asarray(layer.crop((x0 - x, y0 - y, x1 - x, y1 - y))).astype(np.float32)
    b = base[y0:y1, x0:x1]
    b += l[..., :3] * (l[..., 3:4] / 255.0)


def over(base, layer, at, alpha=1.0):
    x, y = int(round(at[0])), int(round(at[1]))
    lw, lh = layer.size
    x0, y0 = max(0, x), max(0, y)
    x1, y1 = min(W, x + lw), min(H, y + lh)
    if x1 <= x0 or y1 <= y0:
        return
    l = np.asarray(layer.crop((x0 - x, y0 - y, x1 - x, y1 - y))).astype(np.float32)
    a = l[..., 3:4] / 255.0 * alpha
    b = base[y0:y1, x0:x1]
    b[:] = b * (1 - a) + l[..., :3] * a


class Fx:
    """한 번 재생하는 도트 프레임 이펙트."""

    def __init__(self, name, center, size, t0, step, color=WHITE, alpha=1.0, loop=False, squash=1.0, life=None):
        self.f = frames(name)
        self.c, self.s, self.t0, self.step = center, size, t0, step
        self.color, self.alpha, self.loop, self.squash, self.life = color, alpha, loop, squash, life

    def draw(self, base, t):
        if t < self.t0 or not self.f:
            return
        k = int((t - self.t0) / self.step)
        if self.life is not None and t - self.t0 > self.life:
            return
        if k >= len(self.f):
            if not self.loop:
                return
            k %= len(self.f)
        c = self.c(t) if callable(self.c) else self.c
        w, h = int(self.s), int(self.s * self.squash)
        im = tint(self.f[k].resize((w, h), Image.NEAREST), self.color, self.alpha)
        add(base, im, (c[0] - w / 2, c[1] - h / 2))


class Beam:
    def __init__(self, a, b, thick, t0, dur, color):
        self.a, self.b, self.th, self.t0, self.dur, self.color = a, b, thick, t0, dur, color

    def draw(self, base, t):
        if not (self.t0 <= t < self.t0 + self.dur):
            return
        a = self.a(t) if callable(self.a) else self.a
        b = self.b(t) if callable(self.b) else self.b
        length = int(math.hypot(b[0] - a[0], b[1] - a[1]))
        if length < 4:
            return
        k = 1 - abs((t - self.t0) / self.dur * 2 - 1)
        im = frames('beam_soft')[0].resize((length, self.th), Image.LANCZOS)
        im = tint(im, self.color, k)
        ang = math.degrees(math.atan2(-(b[1] - a[1]), b[0] - a[0]))
        im = im.rotate(ang, expand=True, resample=Image.BICUBIC)
        cx, cy = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
        add(base, im, (cx - im.width / 2, cy - im.height / 2))


class Dots:
    """ParticleFxPool 처럼 — 도트 네모가 날아가며 옅어진다."""

    def __init__(self, t0, n, origin, vel, life, color, size=(4, 7), spread=10, gravity=0, seed=1, target=None, rate=None,
                 until=None):
        rnd = random.Random(seed)
        self.p = []
        count = n
        for i in range(count):
            delay = (i / rate) if rate else rnd.uniform(0, 0.05)
            ox = origin[0] + rnd.uniform(-spread, spread)
            oy = origin[1] + rnd.uniform(-spread, spread)
            vx = vel[0] + rnd.uniform(-abs(vel[0]) * 0.6 - 20, abs(vel[0]) * 0.6 + 20)
            vy = vel[1] + rnd.uniform(-25, 25)
            self.p.append((t0 + delay, ox, oy, vx, vy, rnd.randint(*size), life * rnd.uniform(0.7, 1.1)))
        self.color, self.g, self.target, self.until = color, gravity, target, until

    def draw(self, base, t):
        for (t0, x, y, vx, vy, s, life) in self.p:
            dt = t - t0
            if dt < 0 or dt > life or (self.until and t0 > self.until):
                continue
            if self.target:
                k = min(1, dt / life)
                e = k * k * (3 - 2 * k)
                px = x + (self.target[0] - x) * e + math.sin(k * math.pi) * vx * 0.25
                py = y + (self.target[1] - y) * e + math.sin(k * math.pi) * vy * 0.25
            else:
                px = x + vx * dt
                py = y + vy * dt + 0.5 * self.g * dt * dt
            a = 1 - dt / life
            sq = Image.new('RGBA', (s, s), self.color + (int(255 * a),))
            add(base, sq, (px - s / 2, py - s / 2))


class Layer:
    """캡처에서 잘라 낸 패널 · 칸 — 크기 · 알파 · 위치 트윈."""

    def __init__(self, img, at, fn):
        self.img, self.at, self.fn = img, at, fn

    def draw(self, base, t):
        sc, al, dx, dy, br = self.fn(t)
        if al <= 0:
            return
        im = self.img
        if br != 1:
            im = ImageEnhance.Brightness(im).enhance(br)
        if sc != 1:
            w, h = im.size
            im = im.resize((max(1, int(w * sc)), max(1, int(h * sc))), Image.BILINEAR)
            ox = self.at[0] + (w - im.width) / 2
            oy = self.at[1] + (h - im.height) / 2
        else:
            ox, oy = self.at
        over(base, im, (ox + dx, oy + dy), al)


def ease_pop(t, t0, dur, s0=0.96, peak=1.0):
    if t < t0:
        return s0, 0.0
    k = min(1, (t - t0) / dur)
    s = s0 + (1 - s0) * (1 - (1 - k) ** 3)
    if peak != 1.0 and k < 1:
        s = s0 + (peak - s0) * math.sin(k * math.pi / 2) if k < 0.7 else peak + (1 - peak) * (k - 0.7) / 0.3
    return s, min(1, k * 1.6)


def bump(t, t0, dur, amp):
    if t < t0 or t > t0 + dur:
        return 1.0
    return 1 + amp * math.sin((t - t0) / dur * math.pi)


def masked(cap, frame_sprite, r):
    """캡처에서 창 틀 그림 모양대로 오린다 — 창 안 글자 · 칸이 게임 그대로 따라온다."""
    x, y, w, h = r
    m = Image.open(os.path.join(ART, frame_sprite + '.png')).convert('RGBA').resize((w, h), Image.LANCZOS).getchannel('A')
    crop = cap.crop((x, y, x + w, y + h)).convert('RGBA')
    crop.putalpha(m)
    return crop


def sub(cap, r):
    x, y, w, h = r
    return cap.crop((x, y, x + w, y + h)).convert('RGBA')


def render(name, base_img, items, dur, dim=0.55, dim_t=(0.0, 0.16), post=None):
    bg = np.asarray(base_img.convert('RGB')).astype(np.float32)
    out = []
    n = int(dur * FPS)
    for i in range(n):
        t = i / FPS
        b = bg.copy()
        if dim:
            k = min(1, max(0, (t - dim_t[0]) / max(1e-3, dim_t[1])))
            b *= 1 - dim * k
        for it in items:
            it.draw(b, t)
        if post:
            post(b, t)
        im = Image.fromarray(b.clip(0, 255).astype(np.uint8), 'RGB').resize((360, 640), Image.LANCZOS)
        out.append(im.quantize(colors=200, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE))
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + '.gif')
    out[0].save(path, save_all=True, append_images=out[1:], duration=int(1000 / FPS), loop=0, optimize=True)
    print(name, n, 'frames', os.path.getsize(path) // 1024, 'KB')


EMPTY = None


def empty_bg():
    return Image.open(os.path.join(CAP, 'audit_levelup_Japanese.png')).convert('RGB')


def cap(n):
    return Image.open(os.path.join(CAP, n + '.png')).convert('RGB')


# ── 화면별 ────────────────────────────────────────────────
def levelup():
    c = cap('levelup')
    fr = rect('BuffFrame')
    panel = masked(c, 'levelupframe', fr)
    cards = [rect('BuffCard%d' % i) for i in range(3)]
    card_img = [sub(c, r) for r in cards]
    emblem = (fr[0] + 345, fr[1] + 62)
    pick = 0
    T_PICK = 1.7

    def panel_fn(t):
        s, a = ease_pop(t, 0.0, 0.24)
        return s, a, 0, 0, 1

    items = [Fx('burst_rays', emblem, 380, 0.22, 0.04, GOLD),
             Layer(panel, fr[:2], panel_fn)]
    for i, (r, im) in enumerate(zip(cards, card_img)):
        def card_fn(t, i=i):
            t0 = 0.30 + 0.06 * i
            if t < t0:
                return 1, 0, 0, 10, 1
            k = min(1, (t - t0) / 0.18)
            dy = 10 * (1 - k)
            sc, br = 1.0, 1.0
            if t >= T_PICK:
                if i == pick:
                    sc = bump(t, T_PICK, 0.3, 0.06)
                    br = 1 + 0.35 * max(0, 1 - (t - T_PICK) / 0.4)
                else:
                    br = max(0.55, 1 - (t - T_PICK) / 0.25 * 0.45)
            return sc, k, 0, dy, br
        items.append(Layer(im, r[:2], card_fn))
    items += [
        Fx('ring_pulse', emblem, 200, 0.04, 0.03, GOLD),
        Fx('energy_arc', (fr[0] + 42, fr[1] + 150), 150, 0.10, 0.03, GOLD),
        Fx('energy_arc', (fr[0] + 648, fr[1] + 150), 150, 0.18, 0.03, GOLD),
        Dots(0.05, 12, (fr[0] + 345, fr[1] + 90), (0, 60), 0.5, GOLD, spread=60),
    ]
    pc = (cards[pick][0] + cards[pick][2] / 2, cards[pick][1] + 10)
    path = lambda t: (pc[0] + (emblem[0] - pc[0]) * min(1, (t - T_PICK - 0.05) / 0.33),
                      pc[1] + (emblem[1] - pc[1]) * min(1, (t - T_PICK - 0.05) / 0.33))
    items += [
        Fx('ring_pulse', (pc[0], cards[pick][1] + cards[pick][3] / 2), 340, T_PICK, 0.03, GOLD, squash=1.6),
        Fx('soul_stream', path, 120, T_PICK + 0.05, 0.04, GOLD, loop=True, life=0.33),
        Dots(T_PICK + 0.05, 8, (pc[0], pc[1] + 20), (0, -60), 0.4, GOLD, spread=30, target=emblem),
        Fx('ring_pulse', emblem, 180, T_PICK + 0.38, 0.03, GOLD),
    ]
    render('levelup', empty_bg(), items, 2.6)


def altar():
    c = cap('audit_altar_Korean')
    fr = rect('ShrineBox')
    panel = masked(c, 'shrineframe', fr)
    slots = [rect('ShrineChoice%d' % i) for i in range(3)]
    slot_img = [sub(c, r) for r in slots]
    symbol = (fr[0] + 295, fr[1] + 38)
    hp = (190, 100)
    T_PICK = 1.6
    pick = 1
    items = [Layer(panel, fr[:2], lambda t: (*ease_pop(t, 0, 0.22, 0.97), 0, 0, 1))]
    for i, (r, im) in enumerate(zip(slots, slot_img)):
        def fn(t, i=i):
            s, a = ease_pop(t, 0, 0.22, 0.97)
            br = 1 + 0.4 * max(0, 1 - abs(t - (0.40 + 0.06 * i)) / 0.12)
            sc = 1
            if t >= T_PICK:
                if i == pick:
                    sc = bump(t, T_PICK, 0.25, 0.025)
                    br = 1 + 0.45 * max(0, 1 - (t - T_PICK) / 0.45)
                else:
                    br = max(0.6, 1 - (t - T_PICK) / 0.25 * 0.4)
            return sc * s, a, 0, 0, br
        items.append(Layer(im, r[:2], fn))
    drop = lambda t: (symbol[0], symbol[1] - 80 + min(1, max(0, t - 0.02) / 0.18) * 80)
    sl = slots[pick]
    gem = (sl[0] + sl[2] - 40, sl[1] + sl[3] / 2)
    items += [
        Fx('soul_stream', drop, 70, 0.0, 0.03, CYAN, life=0.2),
        Fx('ring_pulse', symbol, 170, 0.2, 0.03, CYAN),
        Dots(0.2, 7, symbol, (0, -40), 0.5, CYAN, spread=30),
        Beam(lambda t: (slots[0][0] + 10, slots[0][1] + (slots[2][1] + slots[2][3] - slots[0][1]) * min(1, (t - 0.34) / 0.3)),
             lambda t: (slots[0][0] + slots[0][2] - 10, slots[0][1] + (slots[2][1] + slots[2][3] - slots[0][1]) * min(1, (t - 0.34) / 0.3)),
             26, 0.34, 0.32, CYAN),
        Dots(0.6, 12, (fr[0] + 40, fr[1] + 420), (0, -50), 1.4, CYAN, size=(3, 5), spread=10, rate=8),
        Dots(0.6, 12, (fr[0] + 550, fr[1] + 420), (0, -50), 1.4, CYAN, size=(3, 5), spread=10, rate=8, seed=3),
        Fx('ring_pulse', gem, 110, T_PICK, 0.03, CYAN),
        Fx('soul_stream', lambda t: (gem[0] + (hp[0] - gem[0]) * min(1, (t - T_PICK - 0.06) / 0.36),
                                     gem[1] + (hp[1] - gem[1]) * min(1, (t - T_PICK - 0.06) / 0.36) - 80 * math.sin(min(1, (t - T_PICK - 0.06) / 0.36) * math.pi)),
           90, T_PICK + 0.06, 0.04, CYAN, loop=True, life=0.36),
        Dots(T_PICK + 0.08, 6, gem, (40, -80), 0.42, CYAN, spread=10, target=hp),
        Fx('ring_pulse', hp, 120, T_PICK + 0.44, 0.03, CYAN),
    ]
    render('altar', empty_bg(), items, 2.5, dim=0.35)


def devil():
    c = cap('devil')
    fr = rect('EventBox')
    panel = masked(c, 'eventframe', fr)
    acc = rect('EventAcceptButton')
    acc_img = sub(c, acc)
    reward = rect('EventRewardPill')
    emblem = (fr[0] + 330, fr[1] + 45)
    ghost = (95, 75)   # HUD 유령 초상 — 대가를 치르는 쪽
    T_ACC = 1.7

    def panel_fn(t):
        s, a = ease_pop(t, 0, 0.28, 0.97)
        s *= bump(t, 0.1, 0.18, 0.012)
        dx = 0
        if 0.2 < t < 0.5:
            dx = 3 * math.sin((t - 0.2) / 0.3 * math.pi * 4)
        if T_ACC < t < T_ACC + 0.2:
            dx = 6 * math.sin((t - T_ACC) / 0.2 * math.pi * 2)
        s *= bump(t, T_ACC + 0.1, 0.25, 0.025)
        return s, a, dx, 0, 1

    def acc_fn(t):
        s, a = ease_pop(t, 0, 0.28, 0.97)
        sc = 1 + (0.018 * math.sin((t - 0.6) * 6.0) if 0.6 < t < T_ACC else 0)
        br = 1 + (0.5 * max(0, 1 - (t - T_ACC) / 0.3) if t >= T_ACC else 0)
        return s * sc, a, 0, 0, br

    items = [Fx('smoke_wisp', (fr[0] + 200, fr[1] + 560), 220, 0.0, 0.05, RED, 0.7),
             Fx('smoke_wisp', (fr[0] + 470, fr[1] + 570), 220, 0.08, 0.05, RED, 0.7),
             Layer(panel, fr[:2], panel_fn), Layer(acc_img, acc[:2], acc_fn),
             Fx('energy_arc', (fr[0] + 110, fr[1] + 55), 150, 0.12, 0.03, RED),
             Fx('energy_arc', (fr[0] + 550, fr[1] + 55), 150, 0.2, 0.03, RED),
             Dots(0.05, 10, (fr[0] + 330, fr[1] + 560), (0, -90), 0.8, (255, 120, 60), spread=200),
             Fx('ring_pulse', (acc[0] + acc[2] / 2, acc[1] + acc[3] / 2), 300, 0.32, 0.03, RED, 0.8, squash=0.35),
             Dots(0.6, 14, (fr[0] + 330, fr[1] + 590), (0, -70), 1.0, (255, 110, 60), size=(3, 5), spread=220, rate=10),
             ]
    path = lambda t: (ghost[0] + (emblem[0] - ghost[0]) * min(1, (t - T_ACC) / 0.35),
                      ghost[1] + (emblem[1] - ghost[1]) * min(1, (t - T_ACC) / 0.35) + 60 * math.sin(min(1, (t - T_ACC) / 0.35) * math.pi))
    items += [Fx('soul_stream', path, 110, T_ACC, 0.04, RED, loop=True, life=0.35),
              Fx('burst_rays', emblem, 260, T_ACC + 0.35, 0.03, RED, 0.9),
              Fx('ring_pulse', (reward[0] + reward[2] / 2, reward[1] + reward[3] / 2), 220, T_ACC + 0.4, 0.03, GOLD, squash=0.4)]
    render('devil', empty_bg(), items, 2.7)


def shop():
    c = cap('audit_shop_Korean')
    fr = rect('ShopBox')
    panel = masked(c, 'shopframe', fr)
    cells = [rect('ShopItem%d' % i) for i in range(6)]
    cell_img = [sub(c, r) for r in cells]
    symbol = (fr[0] + 345, fr[1] + 30)
    hud_gold = (75, 152)
    T_BUY = 1.7
    buy = 0
    items = [Layer(panel, fr[:2], lambda t: (*ease_pop(t, 0, 0.22, 0.98), 0, 0, 1))]
    for i, (r, im) in enumerate(zip(cells, cell_img)):
        def fn(t, i=i):
            s, a = ease_pop(t, 0, 0.22, 0.98)
            br = 1 + 0.35 * max(0, 1 - abs(t - (0.36 + 0.04 * i)) / 0.1)
            sc = 1
            if t >= T_BUY and i == buy:
                sc = bump(t, T_BUY, 0.22, 0.05)
                br = max(0.45, 1.3 - (t - T_BUY) / 0.3 * 0.85)
            return sc * s, a, 0, 0, br
        items.append(Layer(im, r[:2], fn))
    cc = (cells[buy][0] + 40, cells[buy][1] + 40)
    items += [Fx('ring_pulse', symbol, 140, 0.04, 0.03, AMBER),
              Fx('energy_arc', (fr[0] + 30, fr[1] + 300), 140, 0.1, 0.03, AMBER),
              Fx('energy_arc', (fr[0] + 660, fr[1] + 300), 140, 0.16, 0.03, AMBER),
              Dots(0.1, 8, (fr[0] + 30, fr[1] + 420), (0, -70), 0.7, GOLD, spread=12),
              Dots(0.1, 8, (fr[0] + 660, fr[1] + 420), (0, -70), 0.7, GOLD, spread=12, seed=4),
              Dots(0.7, 10, (fr[0] + 345, fr[1] + 520), (0, -50), 1.2, GOLD, size=(3, 5), spread=300, rate=6),
              Fx('burst_rays', cc, 96, T_BUY, 0.03, GOLD, 0.9),
              Dots(T_BUY + 0.05, 6, cc, (-40, -120), 0.42, GOLD, size=(6, 9), spread=10, target=hud_gold),
              Fx('ring_pulse', hud_gold, 110, T_BUY + 0.45, 0.03, GOLD)]
    render('shop', empty_bg(), items, 2.6)


def clear():
    mock = Image.open(os.path.join(EX, 'in', 'mock_fxstory_clear_v2.png')).convert('RGB').resize((2160, 1280))
    cut = mock.crop((1440, 0, 2160, 1280))
    pr = (40, 282, 580, 760)        # 패널(시안 틀) — 위 · 오른쪽 주석 글자(「원혼 입자 상승」 · 「천천히 점멸」)를 피해 오린다
    # 오른쪽 위 주석 화살표를 지운다 — 틀은 좌우 대칭이라 왼쪽 위를 뒤집어 덮는다
    from PIL import ImageOps
    corner = ImageOps.mirror(cut.crop((pr[0] + 10, pr[1], pr[0] + 130, pr[1] + 70)))
    cut.paste(corner, (pr[0] + pr[2] - 130, pr[1]))
    panel = cut.crop((pr[0], pr[1], pr[0] + pr[2], pr[1] + pr[3])).convert('RGBA')
    title = (pr[0] + 290, pr[1] + 178)
    gold = (pr[0] + 160, pr[1] + 348)
    chest = (pr[0] + 160, pr[1] + 508)
    chest_r = (pr[0] + 60, pr[1] + 408, 220, 150)
    ok_r = (pr[0] + 140, pr[1] + 578, 300, 90)
    chest_img = sub(cut, chest_r)
    ok_img = sub(cut, ok_r)

    def chest_fn(t):
        if t < 0.55:
            return 1, 0, 0, -8, 1
        k = min(1, (t - 0.55) / 0.15)
        return 1, k, 0, -8 * (1 - k), 1 + 0.3 * max(0, 1 - (t - 0.7) / 0.3)

    def ok_fn(t):
        s, a = ease_pop(t, 0, 0.3, 0.97)
        sc = 1 + (0.02 * math.sin((t - 1.1) * 5) if t > 1.1 else 0)
        br = 1 + (0.1 * (0.5 + 0.5 * math.sin((t - 1.1) * 5)) if t > 1.1 else 0)
        return s * sc, a, 0, 0, br

    items = [Fx('burst_rays', (title[0], title[1] - 40), 560, 0.05, 0.04, GOLD),
             Layer(panel, pr[:2], lambda t: (*ease_pop(t, 0, 0.3, 0.97), 0, 0, 1)),
             Fx('energy_arc', (pr[0] + 10, pr[1] + 300), 170, 0.12, 0.03, (150, 220, 255)),
             Fx('energy_arc', (pr[0] + 590, pr[1] + 300), 170, 0.18, 0.03, (150, 220, 255)),
             Beam(lambda t: (pr[0] + 60, title[1]), lambda t: (pr[0] + 60 + 480 * min(1, (t - 0.1) / 0.25), title[1]), 40, 0.1, 0.3, GOLD),
             Fx('coin_orbit', gold, 220, 0.4, 0.05, squash=0.6),
             Dots(0.4, 14, gold, (0, -140), 0.6, GOLD, size=(5, 8), spread=40, gravity=380),
             Layer(chest_img, chest_r[:2], chest_fn),
             Fx('reward_pop', (chest[0], chest[1] + 40), 200, 0.7, 0.04, (150, 220, 255)),
             Fx('ring_pulse', (chest[0], chest[1] + 45), 220, 0.7, 0.03, (150, 220, 255), squash=0.35),
             Layer(ok_img, ok_r[:2], ok_fn),
             Dots(1.1, 10, (pr[0] + 20, pr[1] + 500), (0, -60), 1.3, (150, 220, 255), size=(3, 5), spread=12, rate=5),
             Dots(1.1, 10, (pr[0] + 580, pr[1] + 500), (0, -60), 1.3, (150, 220, 255), size=(3, 5), spread=12, rate=5, seed=7)]
    render('clear', empty_bg(), items, 2.8)


# ── 방 오브젝트 ────────────────────────────────────────────
PROP_W, PROP_H = 236, 300


def prop_place(kind, art):
    """방 캡처에서 물건 자리를 찾는다(게임이 놓은 자리 그대로)."""
    c = np.asarray(cap('room_' + kind).convert('RGB')).astype(np.float32)
    spr = Image.open(os.path.join(ART, art + '.png')).convert('RGBA')
    sc = min(PROP_W / spr.width, PROP_H / spr.height)
    spr = spr.resize((int(spr.width * sc), int(spr.height * sc)), Image.LANCZOS)
    s = np.asarray(spr).astype(np.float32)
    m = s[..., 3] > 200
    best = None
    for y in range(200, 560, 4):
        for x in range(100, 420, 4):
            d = np.abs(c[y:y + spr.height, x:x + spr.width][m] - s[..., :3][m]).mean()
            if best is None or d < best[0]:
                best = (d, x, y)
    _, bx, by = best
    for y in range(by - 4, by + 5):
        for x in range(bx - 4, bx + 5):
            d = np.abs(c[y:y + spr.height, x:x + spr.width][m] - s[..., :3][m]).mean()
            if d < best[0]:
                best = (d, x, y)
    return spr, (best[1], best[2])


def room(kind, art, color, mode):
    spr, (px, py) = prop_place(kind, art)
    feet = (px + spr.width / 2, py + PROP_H / 2 + (spr.height / 2))
    cx = px + spr.width / 2
    top = py
    ring = Image.open(os.path.join(ART, 'obj_ring_%s.png' % mode)).convert('RGBA').resize((230, 86), Image.LANCZOS)
    mark = Image.open(os.path.join(ART, 'obj_mark_%s.png' % mode)).convert('RGBA').resize((52, 52), Image.LANCZOS)
    ring_at = (cx - 115, py + spr.height - 43 - 14)
    player0 = (cx, py + spr.height + 210)
    T_NEAR, T_ACT = 1.4, 1.9

    def player_pos(t):
        if t < T_NEAR - 0.3:
            return player0
        k = min(1, (t - (T_NEAR - 0.3)) / 0.45)
        return (cx, player0[1] - 130 * k)

    used = lambda t: t > T_ACT + 0.35
    items = []
    items.append(Layer(ring, ring_at, lambda t: (bump(t, T_NEAR, 0.4, 0.15) if t < T_ACT else 1,
                                                 0.3 if used(t) else 0.75 + 0.25 * math.sin(t * 4.5), 0, 0, 1)))
    items.append(Fx('ring_pulse', (cx, ring_at[1] + 43), 260, 0.0, 0.07, color, 0.6, squash=0.36))
    items.append(Fx('ring_pulse', (cx, ring_at[1] + 43), 260, 0.0 + 1.4, 0.07, color, 0.4, squash=0.36, life=0.5))
    if mode == 'heal':
        items.append(Fx('smoke_wisp', (cx, top + 10), 150, 0.1, 0.12, color, 0.5))
    elif mode == 'devil':
        items.append(Fx('smoke_wisp', (cx - 50, top + 30), 170, 0.0, 0.11, color, 0.6))
        items.append(Fx('smoke_wisp', (cx + 40, top + 50), 150, 0.5, 0.11, color, 0.5))
    else:
        items.append(Fx('energy_arc', (cx + 60, top + 140), 120, 0.5, 0.05, color, 0.7))
    items.append(Dots(0.0, 12, (cx, top + 120), (0, -40), 1.4, color, size=(3, 5), spread=60, rate=8))

    def prop_fn(t):
        dy = 0
        br = 1.0
        sc = 1.0
        if T_NEAR <= t < T_ACT:
            br = 1 + 0.25 * min(1, (t - T_NEAR) / 0.2)
            sc = bump(t, T_NEAR, 0.3, 0.025)
        if t >= T_ACT:
            br = max(0.45, 1.3 - (t - T_ACT) / 0.4 * 0.85)
        return sc, 1, 0, dy, br
    items.append(Layer(spr, (px, py), prop_fn))

    def mark_fn(t):
        if used(t):
            return 1, 0, 0, 0, 1
        return 1, 1, 0, 6 * math.sin(t * 2 * math.pi / 1.4), 1
    items.append(Layer(mark, (cx - 26, top - 12 - 52), mark_fn))

    ghost = Image.open(os.path.join(CAP, 'room_' + kind + '.png')).convert('RGBA')
    # 플레이어(유령) — 캡처의 유령을 잘라 다가가게 한다
    g_r = (cx - 45, py + spr.height + 70, 90, 100)
    gimg = sub(cap('room_' + kind), g_r)
    items.append(Layer(gimg, (0, 0), lambda t: (1, 1, player_pos(t)[0] - 45, player_pos(t)[1] - 50, 1)))

    # 다가감
    if mode == 'heal':
        items.append(Beam((cx, top + spr.height - 40), (cx, top + 20), 70, T_NEAR, 0.4, color))
        items.append(Fx('energy_arc', (cx - 80, top + 150), 120, T_NEAR + 0.05, 0.03, color))
        items.append(Fx('energy_arc', (cx + 80, top + 150), 120, T_NEAR + 0.12, 0.03, color))
    elif mode == 'devil':
        items.append(Fx('energy_arc', (cx - 85, top + 20), 110, T_NEAR, 0.03, color))
        items.append(Fx('energy_arc', (cx + 85, top + 20), 110, T_NEAR + 0.06, 0.03, color))
        items.append(Fx('burst_rays', (cx, top + 130), 90, T_NEAR + 0.1, 0.03, color, 0.8))
    else:
        items.append(Fx('burst_rays', (cx, top + 95), 120, T_NEAR, 0.03, color, 0.9))
    # 작동
    pl = lambda t: player_pos(t)
    if mode == 'heal':
        items.append(Fx('soul_stream', lambda t: (cx + (pl(t)[0] - cx) * min(1, (t - T_ACT) / 0.3),
                                                  top + 160 + (pl(t)[1] - top - 160) * min(1, (t - T_ACT) / 0.3)),
                        100, T_ACT, 0.04, color, loop=True, life=0.3))
        items.append(Fx('ring_pulse', lambda t: pl(t), 140, T_ACT + 0.3, 0.03, color))
        items.append(Dots(T_ACT + 0.3, 9, (cx, player0[1] - 130), (40, -80), 0.5, color, spread=20, target=(190, 100)))
    elif mode == 'devil':
        items.append(Fx('chain_snap', (cx - 70, top + 170), 130, T_ACT, 0.04))
        items.append(Fx('chain_snap', (cx + 70, top + 190), 130, T_ACT + 0.04, 0.04))
        items.append(Fx('smoke_wisp', (cx, top + 250), 200, T_ACT, 0.05, color, 0.8))
        items.append(Fx('burst_rays', (cx, top + 130), 200, T_ACT, 0.03, color))
    else:
        items.append(Fx('coin_orbit', (cx, top + 200), 300, T_ACT, 0.05, squash=0.6))
        items.append(Fx('ring_pulse', (cx, ring_at[1] + 43), 300, T_ACT, 0.03, color, squash=0.36))
    base = empty_room_bg(kind, (px, py, spr.width, spr.height), g_r)
    render('room_' + kind, base, items, 2.8, dim=0)


def empty_room_bg(kind, prop_r, g_r):
    """물건 · 바닥 링 · 표식 · 유령이 없는 바닥 — 빈 방 캡처에서 가져온다."""
    room_c = cap('room_' + kind)
    empty = empty_bg()
    out = room_c.copy()
    x, y, w, h = prop_r
    box = (int(x - 20), int(y - 90), int(x + w + 20), int(y + h + 40))
    out.paste(empty.crop(box), box[:2])
    gx, gy, gw, gh = [int(v) for v in g_r]
    out.paste(empty.crop((gx - 10, gy - 10, gx + gw + 10, gy + gh + 10)), (gx - 10, gy - 10))
    return out


SCREENS = {
    'levelup': levelup, 'altar': altar, 'devil': devil, 'shop': shop, 'clear': clear,
    'room_heal': lambda: room('heal', 'obj_heal_shrine', CYAN, 'heal'),
    'room_devil': lambda: room('devil', 'obj_devil_altar', RED, 'devil'),
    'room_shop': lambda: room('shop', 'obj_shop_stall', AMBER, 'shop'),
}

if __name__ == '__main__':
    for k in (sys.argv[1:] or list(SCREENS)):
        SCREENS[k]()
