"""팝업 · 방 오브젝트 · 클리어 — 떠 있는 동안 은은하게 도는 꾸밈 이펙트 미리보기 (2026-10-08).

PD 「한 방 팍팍이 아니라 은은하게 팝업을 꾸미는 이펙트 — 인터랙션이 없는데 일회성은 의미가 없다」.
그래서 등장 · 선택 박자는 빼고, 창이 떠 있는 동안 계속 도는 것만 넣는다:
  관로를 따라 흐르는 빛 · 유리관 속 기포와 유령 · 문장 숨쉬기 · 제목 판에 가끔 지나가는 반사광 · 불씨 · 연기.
부품은 공통 이펙트(out/fxui)를 그대로 쓰고, 입자는 ParticleFxPool 처럼 도트 네모.
GIF 는 끊김 없이 도는 3.2초 한 바퀴(앞에서 3초 미리 돌려 둔 상태부터 찍는다).
python fx_ambient.py [화면 ...] → _exchange/ref/fx_ambient/{화면}.gif
"""
import math
import os
import random
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import fx_preview as P  # noqa: E402

OUT = os.path.join(P.EX, 'ref', 'fx_ambient')
LOOP = 3.2
WARM = 3.0
GOLD, CYAN, RED, AMBER, WHITE = P.GOLD, P.CYAN, P.RED, P.AMBER, P.WHITE
ICE = (150, 220, 255)


def soft_tint(im, color, alpha):
    return P.tint(im, color, alpha)


class Flow:
    """관로를 따라 흐르는 빛 — 혼 꼬리(soul_stream)를 작게, 진행 방향으로 돌려 길을 따라 보낸다."""

    def __init__(self, path, color, n=3, size=44, speed=120, alpha=1.0, phase=0.0):
        self.pts = path
        self.seg = [math.hypot(path[i + 1][0] - path[i][0], path[i + 1][1] - path[i][1]) for i in range(len(path) - 1)]
        self.total = sum(self.seg)
        self.color, self.n, self.size, self.speed, self.alpha, self.phase = color, n, size, speed, alpha, phase

    def at(self, d):
        d %= self.total
        for i, L in enumerate(self.seg):
            if d <= L:
                a, b = self.pts[i], self.pts[i + 1]
                k = d / max(1e-6, L)
                return (a[0] + (b[0] - a[0]) * k, a[1] + (b[1] - a[1]) * k), math.degrees(math.atan2(-(b[1] - a[1]), b[0] - a[0]))
            d -= L
        return self.pts[-1], 0

    def draw(self, base, t):
        f = P.frames('soul_stream')
        # 한 바퀴(LOOP)에 길이 정수배로 맞춰 GIF 가 끊기지 않게
        laps = max(1, round(self.speed * LOOP / self.total))
        v = self.total * laps / LOOP
        for i in range(self.n):
            d = v * t + self.total * (i / self.n + self.phase)
            (x, y), ang = self.at(d)
            im = f[int(t / 0.06) % len(f)].resize((self.size, self.size), P.Image.NEAREST).rotate(ang, resample=Image.BICUBIC)
            P.add(base, soft_tint(im, self.color, self.alpha), (x - self.size / 2, y - self.size / 2))
            core = im.resize((self.size // 2, self.size // 2), Image.LANCZOS)
            P.add(base, soft_tint(core, (255, 255, 235), 0.8 * self.alpha), (x - self.size / 4, y - self.size / 4))


class Rising:
    """기포 · 불씨 — 칸 안에서 아래에서 위로 천천히 오르는 도트(끊김 없이 이어진다)."""

    def __init__(self, box, color, per_sec=6, speed=(40, 70), size=(3, 6), wobble=6, seed=1, alpha=1.0):
        x, y, w, h = box
        rnd = random.Random(seed)
        self.p = []
        n = int(per_sec * LOOP)
        for i in range(n):
            t0 = i / per_sec
            sp = rnd.uniform(*speed)
            life = h / sp
            self.p.append((t0, x + rnd.uniform(0, w), y + h, sp, life, rnd.randint(*size), rnd.uniform(0, 6.28)))
        self.color, self.wob, self.alpha = color, wobble, alpha

    def draw(self, base, t):
        for (t0, x, y, sp, life, s, ph) in self.p:
            for wrap in (-2, -1, 0):            # 한 바퀴 앞 것도 그려 이어 붙인다
                dt = t - (t0 + wrap * LOOP)
                if not (0 <= dt <= life):
                    continue
                k = dt / life
                a = math.sin(k * math.pi) * self.alpha
                px = x + math.sin(dt * 3 + ph) * self.wob
                py = y - sp * dt
                sq = Image.new('RGBA', (s, s), self.color + (int(255 * a),))
                P.add(base, sq, (px - s / 2, py - s / 2))


class TubeGhost:
    """유리관 속 유령 — 혼 꼬리를 위로 세워 관 안을 천천히 올라갔다 다시 아래서 나온다."""

    def __init__(self, box, color, n=2, size=46, alpha=0.75, phase=0.0):
        self.box, self.color, self.n, self.size, self.alpha, self.phase = box, color, n, size, alpha, phase

    def draw(self, base, t):
        x, y, w, h = self.box
        f = P.frames('soul_stream')
        for i in range(self.n):
            k = (t / LOOP + i / self.n + self.phase) % 1.0
            py = y + h - k * h
            px = x + w / 2 + math.sin(k * 2 * math.pi * 2 + i) * w * 0.18
            a = self.alpha * math.sin(k * math.pi)
            im = f[int(t / 0.08) % len(f)].resize((self.size, self.size), Image.NEAREST).rotate(90, resample=Image.BICUBIC)
            P.add(base, soft_tint(im, self.color, a), (px - self.size / 2, py - self.size / 2))


class Breath:
    """문장 숨쉬기 — 고리 한 장을 크기 · 밝기만 천천히 오르내린다(한 바퀴 = LOOP/2)."""

    def __init__(self, center, color, size=150, alpha=0.6, frame=3):
        self.c, self.color, self.size, self.alpha, self.frame = center, color, size, alpha, frame

    def draw(self, base, t):
        k = 0.5 + 0.5 * math.sin(t / LOOP * 2 * math.pi * 2)
        s = int(self.size * (0.92 + 0.12 * k))
        im = P.frames('ring_pulse')[self.frame].resize((s, s), Image.LANCZOS)
        P.add(base, soft_tint(im, self.color, self.alpha * (0.35 + 0.65 * k)), (self.c[0] - s / 2, self.c[1] - s / 2))


class Sheen:
    """제목 판에 가끔 지나가는 반사광 — 비스듬한 빛 띠가 판을 한 번 훑는다(한 바퀴에 한 번)."""

    def __init__(self, box, color=WHITE, at=0.2, dur=0.6, alpha=0.55, width=46):
        self.box, self.color, self.at, self.dur, self.alpha, self.width = box, color, at, dur, alpha, width

    def draw(self, base, t):
        k = (t / LOOP - self.at) % 1.0 * LOOP / self.dur
        if not 0 <= k <= 1:
            return
        x, y, w, h = self.box
        cx = x - 40 + (w + 80) * k
        band = P.frames('beam_soft')[0].resize((int(h * 1.6), self.width), Image.LANCZOS).rotate(70, expand=True, resample=Image.BICUBIC)
        band = soft_tint(band, self.color, self.alpha * math.sin(k * math.pi))
        # 판 밖으로 새지 않게 판 크기로 자른다
        layer = Image.new('RGBA', (w, h), (0, 0, 0, 0))
        layer.alpha_composite(band, (int(cx - x - band.width / 2), int(h / 2 - band.height / 2)))
        P.add(base, layer, (x, y))


class LoopFx:
    """도트 프레임을 끝없이 돌린다(연기 · 전기) — 알파 맥박 선택."""

    def __init__(self, name, center, size, step, color, alpha=0.6, squash=1.0, period=None, offset=0.0):
        self.name, self.c, self.size, self.step, self.color, self.alpha, self.squash = name, center, size, step, color, alpha, squash
        self.period, self.offset = period, offset

    def draw(self, base, t):
        f = P.frames(self.name)
        if self.period:                            # 한 번 재생 후 쉬었다 다시(주기)
            tt = (t - self.offset) % self.period
            k = int(tt / self.step)
            if k >= len(f):
                return
        else:
            k = int(t / self.step) % len(f)
        w, h = int(self.size), int(self.size * self.squash)
        im = f[k].resize((w, h), Image.NEAREST)
        P.add(base, soft_tint(im, self.color, self.alpha), (self.c[0] - w / 2, self.c[1] - h / 2))


def render(name, base_img, back, panel_layers, front, dim=0.45):
    bg = np.asarray(base_img.convert('RGB')).astype(np.float32) * (1 - dim)
    out = []
    n = int(LOOP * P.FPS)
    for i in range(n):
        t = WARM + i / P.FPS
        tl = t % LOOP
        b = bg.copy()
        for it in back:
            it.draw(b, tl)
        for (img, at) in panel_layers:
            P.over(b, img, at)
        for it in front:
            it.draw(b, tl)
        im = Image.fromarray(b.clip(0, 255).astype(np.uint8), 'RGB').resize((360, 640), Image.LANCZOS)
        out.append(im.quantize(colors=220, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE))
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + '.gif')
    out[0].save(path, save_all=True, append_images=out[1:], duration=int(1000 / P.FPS), loop=0, optimize=True)
    print(name, n, 'frames', os.path.getsize(path) // 1024, 'KB')


def L(fr, x, y):
    return (fr[0] + x, fr[1] + y)


# ── 팝업 ──────────────────────────────────────────────────
# 코덱스 검수(_exchange/review_fx_ambient1.md) 반영: 카드 · 선택지 · 가격 · 버튼 위 장식은 뺀다(선택을 재촉하는 신호로 읽힌다),
# 단발 번개(energy_arc)도 뺀다. 한 화면 4계통 이하. 밝기는 코덱스 값보다 한 단계 남겨 「살아 있음」이 보이게(Claude 판단).
TITLE_SHEEN = dict(dur=0.9, alpha=0.32, width=28)


def levelup():
    c = P.cap('levelup')
    fr = P.rect('BuffFrame')
    panel = P.masked(c, 'levelupframe', fr)
    emblem = L(fr, 345, 75)
    title = P.rect('BuffTitleText')
    # 관로 자리 — 틀 그림(levelupframe 690x790)에서 잰 금빛 관 중심선
    pipes = [
        [L(fr, 285, 84), L(fr, 160, 84), L(fr, 112, 140), L(fr, 106, 334), L(fr, 125, 372)],
        [L(fr, 405, 84), L(fr, 530, 84), L(fr, 578, 140), L(fr, 584, 334), L(fr, 565, 372)],
        [L(fr, 345, 272), L(fr, 345, 330)],
        [L(fr, 130, 725), L(fr, 255, 725), L(fr, 300, 712)],
        [L(fr, 560, 725), L(fr, 435, 725), L(fr, 390, 712)],
    ]
    tubes = [(fr[0] + 27, fr[1] + 118, 38, 132), (fr[0] + 625, fr[1] + 118, 38, 132)]
    back = [Rising((fr[0] + 60, fr[1] + 620, 570, 150), GOLD, per_sec=2, speed=(22, 34), size=(2, 4), seed=11, alpha=0.5)]
    front = [Breath(emblem, GOLD, 136, 0.55)]
    front += [Flow(p, GOLD, n=1, size=32, speed=72, alpha=0.7, phase=0.13 * i) for i, p in enumerate(pipes)]
    for i, tb in enumerate(tubes):
        front += [Rising(tb, (240, 205, 120), per_sec=3, speed=(20, 32), size=(2, 4), wobble=3, seed=20 + i, alpha=0.7),
                  TubeGhost(tb, GOLD, n=1, size=36, alpha=0.7, phase=0.5 * i)]
    front += [Sheen(title, (255, 235, 185), at=0.15, **TITLE_SHEEN)]
    render('levelup', P.empty_bg(), back, [(panel, fr[:2])], front)


def altar():
    c = P.cap('audit_altar_Korean')
    fr = P.rect('ShrineBox')
    panel = P.masked(c, 'shrineframe', fr)
    symbol = L(fr, 295, 38)
    title = P.rect('ShrineTitleText')
    tubes = [(fr[0] + 26, fr[1] + 110, 34, 330), (fr[0] + 530, fr[1] + 110, 34, 330)]
    front = [Breath(symbol, CYAN, 110, 0.5)]
    for i, tb in enumerate(tubes):
        front += [Rising(tb, (160, 230, 240), per_sec=4, speed=(28, 46), size=(2, 4), wobble=3, seed=40 + i, alpha=0.75),
                  TubeGhost(tb, CYAN, n=1, size=36, alpha=0.6, phase=0.25 + 0.5 * i)]
    front += [Flow([(125, 870), (225, 902), (495, 902), (595, 870)], CYAN, n=1, size=30, speed=64, alpha=0.6),
              Sheen(title, (200, 250, 255), at=0.1, **TITLE_SHEEN)]
    render('altar', P.empty_bg(), [], [(panel, fr[:2])], front, dim=0.35)


def devil():
    c = P.cap('devil')
    fr = P.rect('EventBox')
    panel = P.masked(c, 'eventframe', fr)
    emblem = L(fr, 330, 45)
    title = P.rect('EventTitleText')
    smoke = (175, 55, 50)
    back = [LoopFx('smoke_wisp', (200, 912), 170, 0.2, smoke, 0.32),
            LoopFx('smoke_wisp', (520, 912), 170, 0.2, smoke, 0.32),
            Rising((125, 820, 470, 130), (230, 100, 60), per_sec=3, speed=(28, 48), size=(2, 4), seed=51, alpha=0.65)]
    front = [Breath(emblem, RED, 122, 0.5),
             Flow([(75, 522), (75, 892)], (230, 80, 65), n=1, size=30, speed=62, alpha=0.6),
             Flow([(645, 522), (645, 892)], (230, 80, 65), n=1, size=30, speed=62, alpha=0.6, phase=0.5),
             Sheen(title, (240, 165, 150), at=0.2, **TITLE_SHEEN)]
    render('devil', P.empty_bg(), back, [(panel, fr[:2])], front)


def shop():
    c = P.cap('audit_shop_Korean')
    fr = P.rect('ShopBox')
    panel = P.masked(c, 'shopframe', fr)
    symbol = L(fr, 345, 40)
    title = P.rect('ShopTitleText')
    tubes = [(fr[0] + 14, fr[1] + 150, 34, 300), (fr[0] + 642, fr[1] + 150, 34, 300)]
    front = [Breath(symbol, AMBER, 108, 0.5)]
    for i, tb in enumerate(tubes):
        front += [TubeGhost(tb, AMBER, n=1, size=36, alpha=0.62, phase=0.3 + 0.5 * i),
                  Rising(tb, (240, 195, 110), per_sec=3, speed=(22, 38), size=(2, 4), wobble=3, seed=70 + i, alpha=0.7)]
    front += [Flow([L(fr, 120, 75), L(fr, 250, 60), L(fr, 440, 60), L(fr, 570, 75)], AMBER, n=1, size=30, speed=64, alpha=0.62),
              Flow([(105, 886), (235, 900), (485, 900), (615, 886)], AMBER, n=1, size=30, speed=64, alpha=0.55, phase=0.5),
              Sheen(title, (255, 225, 160), at=0.1, **TITLE_SHEEN)]
    render('shop', P.empty_bg(), [], [(panel, fr[:2])], front)


def clear():
    mock = Image.open(os.path.join(P.EX, 'in', 'mock_fxstory_clear_v2.png')).convert('RGB').resize((2160, 1280))
    cut = mock.crop((1440, 0, 2160, 1280))
    from PIL import ImageOps
    pr = (40, 282, 580, 760)
    corner = ImageOps.mirror(cut.crop((pr[0] + 10, pr[1], pr[0] + 130, pr[1] + 70)))
    cut.paste(corner, (pr[0] + pr[2] - 130, pr[1]))
    panel = cut.crop((pr[0], pr[1], pr[0] + pr[2], pr[1] + pr[3])).convert('RGBA')
    emblem = (pr[0] + 290, pr[1] + 80)
    title = (pr[0] + 90, pr[1] + 150, 400, 60)
    back = [Rising((30, 382, 60, 560), (125, 195, 220), per_sec=2, speed=(22, 36), size=(2, 4), wobble=3, seed=81, alpha=0.6),
            Rising((570, 382, 60, 560), (125, 195, 220), per_sec=2, speed=(22, 36), size=(2, 4), wobble=3, seed=82, alpha=0.6)]
    front = [Breath(emblem, ICE, 130, 0.5),
             Sheen(title, (255, 235, 185), at=0.1, **TITLE_SHEEN),
             Rising((145, 608, 110, 42), GOLD, per_sec=2, speed=(15, 24), size=(2, 3), seed=83, alpha=0.55),
             Flow([(70, 482), (70, 842)], (235, 190, 75), n=1, size=30, speed=64, alpha=0.6),
             Flow([(590, 482), (590, 842)], (235, 190, 75), n=1, size=30, speed=64, alpha=0.6, phase=0.5)]
    render('clear', P.empty_bg(), back, [(panel, pr[:2])], front)


# ── 방 오브젝트(대기 중 장식) ─────────────────────────────
def room(kind, art, color, mode):
    spr, (px, py) = P.prop_place(kind, art)
    cx = px + spr.width / 2
    ring = Image.open(os.path.join(P.ART, 'obj_ring_%s.png' % mode)).convert('RGBA').resize((230, 86), Image.LANCZOS)
    mark = Image.open(os.path.join(P.ART, 'obj_mark_%s.png' % mode)).convert('RGBA').resize((52, 52), Image.LANCZOS)
    ring_at = (cx - 115, py + spr.height - 43 - 14)
    g_r = (cx - 45, py + spr.height + 70, 90, 100)
    base = P.empty_room_bg(kind, (px, py, spr.width, spr.height), g_r)
    back = [LoopFx('ring_pulse', (cx, ring_at[1] + 43), 230, 0.24, color, 0.42, squash=0.34)]
    if mode == 'heal':
        back += [LoopFx('smoke_wisp', (cx, py + 5), 110, 0.24, color, 0.3)]
        front = [Breath((cx, py + 120), color, 108, 0.36),
                 Rising((cx - 38, py + 55, 76, 140), (160, 230, 240), per_sec=4, speed=(22, 38), size=(2, 4), wobble=3,
                        seed=91, alpha=0.7)]
    elif mode == 'devil':
        back += [LoopFx('smoke_wisp', (cx - 48, py + 35), 125, 0.24, (175, 55, 50), 0.32),
                 LoopFx('smoke_wisp', (cx + 48, py + 42), 115, 0.24, (175, 55, 50), 0.3)]
        front = [Breath((cx, py + 120), color, 108, 0.36),
                 Rising((cx - 65, py + 5, 130, 180), (230, 95, 55), per_sec=3, speed=(25, 42), size=(2, 4), wobble=3,
                        seed=92, alpha=0.7)]
    else:
        front = [Breath((cx, py + 100), color, 92, 0.32),
                 Rising((cx - 82, py + 55, 164, 145), (240, 185, 80), per_sec=2, speed=(18, 30), size=(2, 4), wobble=3,
                        seed=93, alpha=0.55),
                 Breath((cx - 95, py + 95), AMBER, 38, 0.34),
                 Breath((cx + 95, py + 175), AMBER, 38, 0.34)]
    layers = [(ring, ring_at), (spr, (px, py)), (P.sub(P.cap('room_' + kind), g_r), g_r[:2])]
    # 머리 위 표식은 둥실 — 매 컷 위치가 바뀌므로 front 에 넣는다
    front.append(Bob(mark, (cx - 26, py - 12 - 52), amp=3))
    render('room_' + kind, base, back, layers, front, dim=0)


class Bob:
    def __init__(self, img, at, amp=6):
        self.img, self.at, self.amp = img, at, amp

    def draw(self, base, t):
        dy = self.amp * math.sin(t / LOOP * 2 * math.pi)   # 한 바퀴에 한 번 오르내림(검수)
        P.over(base, self.img, (self.at[0], self.at[1] + dy))


SCREENS = {
    'levelup': levelup, 'altar': altar, 'devil': devil, 'shop': shop, 'clear': clear,
    'room_heal': lambda: room('heal', 'obj_heal_shrine', CYAN, 'heal'),
    'room_devil': lambda: room('devil', 'obj_devil_altar', RED, 'devil'),
    'room_shop': lambda: room('shop', 'obj_shop_stall', AMBER, 'shop'),
}

if __name__ == '__main__':
    for k in (sys.argv[1:] or list(SCREENS)):
        SCREENS[k]()
