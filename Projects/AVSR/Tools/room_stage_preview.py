# -*- coding: utf-8 -*-
"""챕터의 실제 방(원작에서 옮긴 30방)을 그 무대 바닥 · 부품 · 몬스터로 게임 규칙 그대로 그려 본다 — 3단계(2026-10-23).

PD 10-23 「저걸 예시로 잡은 거지 배치는 저렇게 똑같이는 안 할 거지?」 — 시안은 무대끼리 비교하려고 같은 방(28번)으로 그렸다.
실제 게임은 챕터마다 다른 원작 장(1챕터 122 · 2챕터 126 …)의 방이 선다. 이 도구는 그 **실제 방**을 보여 준다.
그리는 규칙은 게임(BattleDirector.OriginBlockPick · WaterEdge)과 같다 — 새로 그리는 것은 없고 부품을 칸에 놓기만 한다.

쓰는 법: python room_stage_preview.py <챕터> [방 번호 …]   (빼면 전투방 앞 6개)
결과  : _exchange/ref/room_tpl_mock/stage_ch<챕터>_rooms.png
"""
import json
import os
import sys

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
ROOMS = os.path.join(ROOT, 'Projects', 'AVSR', 'Rooms')
UNIT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Unit')
GAME_ART = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')
FONT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Fonts', 'NotoSansKR-Bold.ttf')
CELL = 720 / 11
PX = 72.0
GATE = (16.0 - 15 * 10 / 11) * PX      # 위 문 띠(origin_rooms_bake.py GATE) — 픽셀
BOTTOM = 2.9 * PX                      # 아래 입구 난간 띠(origin_rooms_bake.py BOTTOM)
UNIT_PX = 84


def tall_floor(stage, height_px):
    """게임 `BattleDirector.TallFloor` 와 같이 — 위 문 띠 · 가운데 바닥(되풀이) · 아래 난간으로 잘라 쌓는다."""
    art = Image.open(os.path.join(ROOT, 'Assets', 'BundleResource', 'RoomFloor', f'roomfloor_env_{stage}.png')).convert('RGB')
    if height_px <= art.height + 36:
        return art.resize((720, int(height_px)))
    top_h, bot_h = int(art.height * 2.4 / 16), int(art.height * 2.9 / 16)
    top, mid, bot = art.crop((0, 0, 720, top_h)), art.crop((0, top_h, 720, art.height - bot_h)), art.crop((0, art.height - bot_h, 720, art.height))
    img = Image.new('RGB', (720, int(height_px)))
    img.paste(top, (0, 0))
    y, end = top_h, int(height_px) - bot_h
    while y < end:
        piece = min(mid.height, end - y)
        img.paste(mid.crop((0, 0, 720, piece)), (0, y))
        y += piece
    img.paste(bot, (0, end))
    return img


def chapter_row(ch):
    rows = [ln.rstrip('\n').split('\t') for ln in open(os.path.join(ROOMS, 'chapters.tsv'), encoding='utf-8') if not ln.startswith('#')]
    head = rows[0]
    return dict(zip(head, next(r for r in rows[1:] if r[0] == str(ch))))


def roster():
    out = {}
    for ln in open(os.path.join(ROOMS, 'trash_roster.tsv'), encoding='utf-8'):
        t = ln.rstrip('\n').split('\t')
        if t[0] != 'key':
            out[t[0]] = t[1]
    return out


def part_loader(stage):
    dirs = [os.path.join(EX, 'ref', 'room_obj_parts', 'cut', stage), GAME_ART]

    def load(name):
        for d in dirs:
            p = os.path.join(d, f'obj_{stage}_{name}.png')
            if os.path.exists(p):
                return Image.open(p).convert('RGBA')
        return None
    return load


def unit_sprite(key, body):
    for k in (key, body):
        p = os.path.join(UNIT, k, f'unit_{k}_s.png')
        if os.path.exists(p):
            return Image.open(p).convert('RGBA').resize((UNIT_PX, UNIT_PX), Image.NEAREST)
    return None


def draw_room(r, stage, load, bodies):
    w, cells = r['w'], r['cells']
    h = len(cells) // w
    rows, first = h, 0                      # 방 전체(21줄 방은 길다 — 게임에서는 카메라가 따라간다)
    img = tall_floor(stage, GATE + h * CELL + BOTTOM)
    k = CELL / 72.0
    sz = (int(CELL) + 2, int(CELL) + 2)
    top = lambda gy: GATE + gy * CELL
    blocks = [b for b in (load(f'block_{i}') for i in range(1, 7)) if b]
    wet = lambda x, y: 0 <= x < w and 0 <= y < h and cells[y * w + x] == '~'
    picks = {}

    def block_pick(x, y, n):
        gx, gy_ = x, h - 1 - y
        pick = ((((gx * 31 + gy_ * 17) & 0xFFFFFFFF) * 2654435761) & 0xFFFFFFFF) >> 16
        pick %= n
        for _ in range(n):
            if all(picks.get((gx + dx, gy_ + dy)) != pick for dx, dy in ((-1, 0), (0, 1), (-1, 1), (1, 1))):
                break
            pick = (pick + 1) % n
        picks[(gx, gy_)] = pick
        return pick

    for y in range(h):
        for x in range(w):
            if cells[y * w + x] in '#o' and blocks:
                block_pick(x, y, len(blocks))
    alt = lambda x, y: (x + (h - 1 - y)) % 2
    # 바닥에 눕는 것 — 물 · 가시 · 레일
    for gy in range(rows):
        y = first + gy
        for x in range(w):
            c = cells[y * w + x]
            x0, y0 = int(x * CELL), int(top(gy))
            if c == '~':
                s = load(f'water_{alt(x, y) + 1}') or load('water_1')
                if s:
                    img.paste(s.resize(sz, Image.NEAREST), (x0, y0), s.resize(sz, Image.NEAREST))
                n, so, wl, e = not wet(x, y - 1), not wet(x, y + 1), not wet(x - 1, y), not wet(x + 1, y)
                for on, name in ((n, 'water_edge_n'), (so, 'water_edge_s'), (wl, 'water_edge_w'), (e, 'water_edge_e'),
                                 (n and wl, 'water_corner_nw'), (n and e, 'water_corner_ne'),
                                 (so and wl, 'water_corner_sw'), (so and e, 'water_corner_se')):
                    p = load(name) if on else None
                    if p:
                        p = p.resize(sz, Image.NEAREST)
                        img.paste(p, (x0, y0), p)
            elif c == '^':
                s = load(f'spike_{alt(x, y) + 1}') or load('spike_1')
                if s:
                    s = s.resize(sz, Image.NEAREST)
                    img.paste(s, (x0, y0), s)
            elif c == 't':
                ImageDraw.Draw(img).rectangle([x0 + 8, y0 + 26, x0 + int(CELL) - 8, y0 + 38], fill=(150, 150, 160))
    # 솟는 것 — 위 줄부터(아래 줄이 앞)
    for gy in range(rows):
        y = first + gy
        for x in range(w):
            c = cells[y * w + x]
            if c in '#o' and blocks:
                o = blocks[picks[(x, h - 1 - y)]]
            elif c == '=':
                o = load(f'fence_{alt(x, y) + 1}') or load('fence_1')
            else:
                o = None
            if o:
                o = o.resize((int(o.width * k) + 2, int(o.height * k) + 2), Image.NEAREST)
                img.paste(o, (int(x * CELL), int(top(gy) - 30 * k)), o)
            sp = r['spawns'].get(f'{x},{y}')
            if sp:
                u = unit_sprite(sp['monster'], bodies.get(sp['monster'], sp['monster']))
                if u:
                    cx, cy = x * CELL + CELL / 2, top(gy) + CELL / 2
                    img.paste(u, (int(cx - UNIT_PX / 2), int(cy - UNIT_PX * 0.72)), u)
    return img


def main(ch, nums):
    d = json.load(open(os.path.join(ROOMS, f'origin_ch{ch}.json'), encoding='utf-8'))
    stage = chapter_row(ch)['stage']
    load, bodies = part_loader(stage), roster()
    rooms = [r for r in d['rooms'] if 'cells' in r and (not nums or r['room'] in nums)][:6]
    font = ImageFont.truetype(FONT, 26)
    W = 360                                  # 반으로 줄여 나란히 — 방마다 높이가 다르다(18.9 m · 24.4 m)
    ims = [draw_room(r, stage, load, bodies) for r in rooms]
    H = max(int(im.height * W / 720) for im in ims)
    sheet = Image.new('RGB', (len(rooms) * (W + 10) + 10, H + 60), (30, 30, 40))
    dr = ImageDraw.Draw(sheet)
    for i, (r, im) in enumerate(zip(rooms, ims)):
        im = im.resize((W, int(im.height * W / 720)))
        x = 10 + i * (W + 10)
        dr.text((x + 4, 8), f'{ch}챕터 {r["room"]}번 방 · {r["h"]}줄', font=font, fill=(235, 235, 235))
        sheet.paste(im, (x, 50 + H - im.height))   # 아래(입구)를 맞춘다
    out = os.path.join(EX, 'ref', 'room_tpl_mock', f'stage_ch{ch}_rooms.png')
    sheet.save(out)
    print(out, stage, [r['room'] for r in rooms])


if __name__ == '__main__':
    main(int(sys.argv[1]), [int(a) for a in sys.argv[2:]])
