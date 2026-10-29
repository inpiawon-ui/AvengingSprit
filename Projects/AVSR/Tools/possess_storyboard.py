"""빙의 파티 빙의 연출 4컷 판 — 지금 게임에 들어간 빙의 그림 · 혼 연출 부품을 실제 크기 그대로 잘라 붙인다(PD 2026-10-29).

PD 「지금 우리 게임에 들어가 있는 게 원작 컨셉에 맞춘 거야 — 보조는 원래 없는 시스템이니 기존에 있던 걸 참고해서 맞춰 줘」.
새 그림은 없다. 크기는 BattleDirector.SoulFx.cs 의 화면 크기(물결 132×176 · 가닥 117×156 · 혼줄 굵기 18)를 따른다.
사용: python Projects/AVSR/Tools/possess_storyboard.py  → Projects/AVSR/_exchange/ref/possess_fx_1029/board_*.png
"""
import math
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
B = os.path.join(ROOT, 'Assets', 'BaseResource')
OUT = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'possess_fx_1029')
BG = os.path.join(ROOT, 'Assets', 'BundleResource', 'RoomFloor', 'roomfloor_ch1_lane_wide.png')
FONT = r'C:\Windows\Fonts\malgunbd.ttf'
PW, PH, ZOOM = 360, 280, 2          # 한 칸 = 게임 화면 360×280 을 2배로
SUB_TINT = (0.80, 0.68, 1.0)        # Unit.PartySubColor
SUB_ALPHA = 0.70


def img(rel):
    return Image.open(os.path.join(B, rel)).convert('RGBA')


def unit(key, name):
    return img(f'Unit/{key}/unit_{key}_{name}.png')


def fx(name, i):
    return img(f'InGameMainUI/fx_{name}_{i}.png')


def fit(im, size):
    return im.resize((int(size[0]), int(size[1])), Image.NEAREST)


def alpha(im, a):
    im = im.copy()
    im.putalpha(im.getchannel('A').point(lambda v: int(v * a)))
    return im


def tint_sub(im):
    r, g, b, a = im.split()
    r = r.point(lambda v: int(v * SUB_TINT[0]))
    g = g.point(lambda v: int(v * SUB_TINT[1]))
    return alpha(Image.merge('RGBA', (r, g, b, a)), SUB_ALPHA)


def paste_feet(canvas, im, feet, pivot_y=None):
    """im 의 발밑(그림 바닥 가운데)을 feet 에 둔다."""
    w, h = im.size
    bb = im.getbbox() or (0, 0, w, h)
    py = bb[3] if pivot_y is None else pivot_y
    canvas.alpha_composite(im, (int(feet[0] - w / 2), int(feet[1] - py)))


def paste_center(canvas, im, c):
    canvas.alpha_composite(im, (int(c[0] - im.size[0] / 2), int(c[1] - im.size[1] / 2)))


def cord(canvas, a, b, frame=2, a_mul=1.0):
    src = img(f'InGameMainUI/fx_soulcord_{frame}.png')
    length = math.dist(a, b)
    if length < 4:
        return
    band = alpha(fit(src, (length, 18)), a_mul)
    ang = math.degrees(math.atan2(-(b[1] - a[1]), b[0] - a[0]))
    rot = band.rotate(ang, expand=True, resample=Image.NEAREST)
    paste_center(canvas, rot, ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2))


def ripple(canvas, feet, i=3, a_mul=1.0):
    paste_feet(canvas, alpha(fit(fx('soulripple', i), (132, 176)), a_mul), feet, pivot_y=int(176 * 0.62))


def wisp(canvas, feet, i=3, a_mul=1.0):
    paste_feet(canvas, alpha(fit(fx('soulwisp', i), (117, 156)), a_mul), feet, pivot_y=int(156 * 0.92))


def death(canvas, center, i):
    paste_center(canvas, fit(fx('souldeath', i), (112, 112)), center)


def ring(canvas, feet):
    paste_center(canvas, fit(img('InGameMainUI/party_sub_ring.png'), (110, 55)), (feet[0], feet[1] - 2))


def background():
    bg = Image.open(BG).convert('RGBA')
    return bg.crop((180, 420, 180 + PW, 420 + PH))


def panel(draw_fn, label):
    c = background()
    draw_fn(c)
    c = c.resize((PW * ZOOM, PH * ZOOM), Image.NEAREST)
    d = ImageDraw.Draw(c)
    f = ImageFont.truetype(FONT, 26)
    d.rectangle((0, 0, d.textlength(label, font=f) + 20, 40), fill=(10, 14, 30, 230))
    d.text((10, 4), label, font=f, fill=(255, 255, 255))
    return c


def board(name, title, panels):
    cells = [panel(fn, lb) for lb, fn in panels]
    W, H = PW * ZOOM, PH * ZOOM
    sheet = Image.new('RGBA', (W * 2 + 6, H * 2 + 6 + 60), (10, 14, 30, 255))
    d = ImageDraw.Draw(sheet)
    d.text((14, 12), title, font=ImageFont.truetype(FONT, 30), fill=(255, 214, 106))
    for i, c in enumerate(cells):
        sheet.alpha_composite(c, ((i % 2) * (W + 6), 60 + (i // 2) * (H + 6)))
    os.makedirs(OUT, exist_ok=True)
    p = os.path.join(OUT, f'board_{name}.png')
    sheet.convert('RGB').save(p)
    print(p)


MAIN = (120, 220)       # 메인 발밑
TGT = (265, 150)        # 대상 발밑
SUBP = (60, 175)        # 기존 보조 발밑
CHEST = -40             # 발밑 → 가슴


def mid(a, b, t, lift=40):
    x = a[0] + (b[0] - a[0]) * t
    y = a[1] + (b[1] - a[1]) * t - math.sin(math.pi * t) * lift
    return (x, y)


def scene_subA():
    frag_from = (MAIN[0] + 8, MAIN[1] + CHEST - 30)
    frag_to = (TGT[0], TGT[1] + CHEST)

    def p1(c):
        wisp(c, MAIN, 2, 0.6)
        paste_feet(c, unit('baseball', 's'), MAIN)
        paste_feet(c, unit('medium', 's'), TGT)
        paste_center(c, unit('ghost', 'shrink2'), frag_from)

    def p2(c):
        paste_feet(c, unit('baseball', 's'), MAIN)
        paste_feet(c, unit('medium', 's'), TGT)
        m = mid(frag_from, frag_to, 0.55)
        cord(c, frag_from, m)
        paste_center(c, unit('ghost', 'shrink2'), m)

    def p3(c):
        ripple(c, TGT, 3)
        paste_feet(c, unit('baseball', 's'), MAIN)
        paste_feet(c, unit('medium', 'possess1'), TGT)
        paste_center(c, unit('ghost', 'shrink3'), frag_to)

    def p4(c):
        ring(c, TGT)
        paste_feet(c, unit('baseball', 's'), MAIN)
        paste_feet(c, tint_sub(unit('medium', 's')), TGT)
        cord(c, (MAIN[0], MAIN[1] + CHEST), (TGT[0], TGT[1] + CHEST), 3, 0.45)

    board('subA', '보조 빙의 A안 「분혼」 — 메인에서 작은 혼(축소 그림)이 나가 꽂힌다 · 메인은 계속 싸움',
          [('1 · 0.0초 혼 조각이 나옴', p1), ('2 · 0.3초 혼줄 달고 날아감', p2),
           ('3 · 0.5초 원작 빙의 자세 · 물결', p3), ('4 · 1.0초 보랏빛 + 혼고리', p4)])


def scene_subB():
    gh_up = (MAIN[0], MAIN[1] + CHEST - 55)

    def p1(c):
        ripple(c, MAIN, 3)
        wisp(c, MAIN, 3, 0.8)
        paste_feet(c, unit('baseball', 's'), MAIN)
        paste_feet(c, unit('medium', 's'), TGT)
        cord(c, (MAIN[0], MAIN[1] + CHEST), gh_up)
        paste_center(c, unit('ghost', 's'), gh_up)

    def p2(c):
        paste_feet(c, unit('baseball', 's'), MAIN)
        paste_feet(c, unit('medium', 'possess1'), TGT)
        paste_center(c, unit('ghost', 'shrink1'), (TGT[0] - 18, TGT[1] + CHEST - 10))

    def p3(c):
        ring(c, TGT)
        ripple(c, TGT, 4, 0.7)
        paste_feet(c, unit('baseball', 's'), MAIN)
        paste_feet(c, tint_sub(unit('medium', 's')), TGT)
        paste_center(c, unit('ghost', 's'), mid((TGT[0], TGT[1] + CHEST - 40), gh_up, 0.25))

    def p4(c):
        ring(c, TGT)
        paste_feet(c, unit('baseball', 's'), MAIN)
        paste_feet(c, tint_sub(unit('medium', 's')), TGT)
        paste_center(c, unit('ghost', 'shrink2'), (MAIN[0], MAIN[1] + CHEST))

    board('subB', '보조 빙의 B안 「갔다 오기」 — 유령 본체가 나가 빙의하고 돌아온다(지금 빠져나오기 · 들어가기 그대로)',
          [('1 · 0.0초 유령이 빠져나옴', p1), ('2 · 0.5초 원작 빙의 자세로 쏙', p2),
           ('3 · 0.9초 보랏빛 · 유령 다시 나옴', p3), ('4 · 1.4초 메인으로 돌아옴', p4)])


def scene_mainSwap():
    gh_up = (MAIN[0], MAIN[1] + CHEST - 55)

    def p1(c):
        ripple(c, MAIN, 3)
        wisp(c, MAIN, 3)
        paste_feet(c, unit('baseball', 'possess1'), MAIN)
        paste_feet(c, unit('medium', 's'), TGT)
        cord(c, (MAIN[0], MAIN[1] + CHEST), gh_up)
        paste_center(c, unit('ghost', 's'), gh_up)

    def p2(c):
        paste_feet(c, alpha(unit('baseball', 'possess2'), 0.45), MAIN)
        death(c, (MAIN[0], MAIN[1] - 30), 3)
        paste_feet(c, unit('medium', 's'), TGT)
        paste_center(c, unit('ghost', 's'), mid(gh_up, (TGT[0], TGT[1] + CHEST), 0.5))

    def p3(c):
        death(c, (MAIN[0], MAIN[1] - 30), 6)
        paste_feet(c, unit('medium', 'possess1'), TGT)
        paste_center(c, unit('ghost', 'shrink2'), (TGT[0], TGT[1] + CHEST))

    def p4(c):
        ripple(c, TGT, 5, 0.6)
        paste_feet(c, unit('medium', 's'), TGT)

    board('mainSwap', '메인 교체 — 빠져나오기 → 옛 메인은 놓아줌(쓰러지지 않고 옅어지며 흩어짐) → 들어가기',
          [('1 · 0.0초 유령이 빠져나옴', p1), ('2 · 0.4초 옛 메인 옅어지며 흩어짐', p2),
           ('3 · 0.8초 원작 빙의 자세로 쏙', p3), ('4 · 1.1초 새 메인', p4)])


def scene_subSwap():
    frag_from = (SUBP[0], SUBP[1] + CHEST - 20)
    frag_to = (TGT[0], TGT[1] + CHEST)

    def p1(c):
        paste_feet(c, alpha(tint_sub(unit('thug', 'possess1')), 0.6), SUBP)
        death(c, (SUBP[0], SUBP[1] - 30), 2)
        paste_feet(c, unit('baseball', 's'), MAIN)
        paste_feet(c, unit('medium', 's'), TGT)
        paste_center(c, unit('ghost', 'shrink2'), frag_from)

    def p2(c):
        death(c, (SUBP[0], SUBP[1] - 30), 5)
        paste_feet(c, unit('baseball', 's'), MAIN)
        paste_feet(c, unit('medium', 's'), TGT)
        m = mid(frag_from, frag_to, 0.5, 60)
        cord(c, frag_from, m)
        paste_center(c, unit('ghost', 'shrink2'), m)

    def p3(c):
        ripple(c, TGT, 3)
        paste_feet(c, unit('baseball', 's'), MAIN)
        paste_feet(c, unit('medium', 'possess1'), TGT)
        paste_center(c, unit('ghost', 'shrink3'), frag_to)

    def p4(c):
        ring(c, TGT)
        paste_feet(c, unit('baseball', 's'), MAIN)
        paste_feet(c, tint_sub(unit('medium', 's')), TGT)

    board('subSwap', '보조 교체 — 옛 보조는 옅어지며 흩어지고, 그 혼 조각이 새 호스트로 옮겨 간다',
          [('1 · 0.0초 옛 보조 놓아줌 · 혼 조각', p1), ('2 · 0.3초 혼 조각이 날아감', p2),
           ('3 · 0.5초 원작 빙의 자세로 쏙', p3), ('4 · 1.0초 새 보조', p4)])


if __name__ == '__main__':
    scene_subA()
    scene_subB()
    scene_mainSwap()
    scene_subSwap()
