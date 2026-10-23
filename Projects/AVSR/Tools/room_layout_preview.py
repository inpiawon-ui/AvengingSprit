# -*- coding: utf-8 -*-
"""원작에서 옮긴 방을 **게임 칸 그대로** 놓아 본 미리보기 — 시안에서 자른 부품을 칸에 붙인다 (3단계, 2026-10-23).

코덱스 시안은 화풍은 좋지만 칸 자리를 못 지킨다(가시를 물에 붙여 그려 지나가는 줄이 사라졌다 — 두 번).
그래서 자리는 게임이 쓰는 칸(origin_ch*.json) 그대로 두고, 시안에서 **잘라 낸 부품**(가시 판 · 물 칸)과 게임 몬스터 그림을 붙인다.
새로 그리는 것은 없다 — 자르기 · 배치만. 「실제 게임에서 이렇게 보인다」를 보이는 그림이다.

쓰는 법: python room_layout_preview.py <챕터> <방 번호> <첫 줄> <줄 수>
결과  : _exchange/ref/room_tpl_mock/layout_ch<챕터>_room<방>.png (720 x 1280)
"""
import json
import os
import sys

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
UNIT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Unit')
CELL = 720 / 11
TOP = 205
UNIT_PX = 84                     # 게임 잡몹 상자(96 칸 그림이 화면에서 84 px)

# 시안 mock_room_obj_b2.png 에서 자른 부품 칸(720 x 1280 좌표) — 그 칸 하나만 깨끗하게 보이는 자리
PARTS = {
    '^': (256, 468, 328, 532),   # 가운데 가시 판
    '~': (262, 540, 330, 605),   # 웅덩이 왼쪽 위 칸
}
ROLE_SPRITE = {'melee': 'skeleton', 'ranged': 'scrapgunner', 'flying': 'bat', 'turret': 'scrapgunner'}


def part(src, box):
    return src.crop(box).resize((int(round(CELL)), int(round(CELL))), Image.NEAREST)


def main(ch, room_no, first, rows):
    d = json.load(open(os.path.join(ROOT, 'Projects', 'AVSR', 'Rooms', f'origin_ch{ch}.json'), encoding='utf-8'))
    r = next(x for x in d['rooms'] if x['room'] == room_no)
    w, cells = r['w'], r['cells']
    mock = Image.open(os.path.join(EX, 'in', 'mock_room_obj_b2.png')).convert('RGB')
    base = Image.open(os.path.join(EX, 'ref', 'room_tpl_mock', 'base_room_now.png')).convert('RGB')
    # 바탕: 지금 화면(HUD · 조작) 위에 방 바닥만 깨끗한 것으로 — 시안의 바닥 칸(빈 칸)을 깐다
    floor_tile = mock.crop((560, 600, 625, 665)).resize((int(round(CELL)), int(round(CELL))), Image.NEAREST)
    img = base.copy()
    for gy in range(rows):
        for x in range(w):
            img.paste(floor_tile, (int(round(x * CELL)), int(round(TOP + gy * CELL))))
    tiles = {k: part(mock, b) for k, b in PARTS.items()}
    # 받은 부품(cut_room_obj_parts.py)이 있으면 그것을 — 막는 물건은 자리로 골라 섞는다(게임 `obj_block_{n}` 과 같은 규칙)
    cut = os.path.join(EX, 'ref', 'room_obj_parts', 'cut', 'junkyard')
    def load(name):
        p = os.path.join(cut, f'obj_junkyard_{name}.png')
        return Image.open(p).convert('RGBA') if os.path.exists(p) else None
    blocks = [b for b in (load(f'block_{i}') for i in range(1, 7)) if b]
    spikes = [s for s in (load('spike_1'), load('spike_2')) if s]
    fences = [f for f in (load('fence_1'), load('fence_2')) if f]
    k = CELL / 72.0   # 게임은 72 px/칸 그림을 65 px 칸에 줄여 그린다
    rise = 30 * k
    h_total = len(cells) // w

    # ── 게임 `BattleDirector.OriginBlockPick` 과 같은 규칙 — 게임 칸 좌표(아래가 0) · 같은 흩뿌리기 · 왼쪽 · 위 칸과 다른 그림
    picks = {}

    def block_pick(x, y, n):
        gx, gy_ = x, h_total - 1 - y
        pick = ((((gx * 31 + gy_ * 17) & 0xFFFFFFFF) * 2654435761) & 0xFFFFFFFF) >> 16
        pick %= n
        for _ in range(n):
            if all(picks.get((gx + dx, gy_ + dy)) != pick for dx, dy in ((-1, 0), (0, 1), (-1, 1), (1, 1))):
                break
            pick = (pick + 1) % n
        picks[(gx, gy_)] = pick
        return pick

    def alt(x, y):   # 가시 · 울타리 두 장 엇갈림
        return (x + (h_total - 1 - y)) % 2

    for y in range(h_total):   # 막는 칸은 방 전체를 위 줄부터 골라 둔다(게임도 방 전체 차례로 고른다)
        for x in range(w):
            if cells[y * w + x] in '#o' and blocks:
                block_pick(x, y, len(blocks))
    for gy in range(rows):
        y = first + gy
        for x in range(w):
            c = cells[y * w + x]
            x0, y0 = int(round(x * CELL)), int(round(TOP + gy * CELL))
            if c == '~' and load('water_1'):
                # 게임 `BattleDirector.WaterEdge` 와 같다 — 수면 두 장 엇갈림 + 물이 아닌 쪽에만 둑 · 두 쪽 다 땅이면 귀퉁이
                sz = (int(CELL) + 2, int(CELL) + 2)   # 게임처럼 올림 + 1 로 살짝 겹친다(칸 틈 없게)
                img.paste(load(f'water_{alt(x, y) + 1}').resize(sz, Image.NEAREST), (x0, y0))
                wet = lambda xx, yy: 0 <= xx < w and 0 <= yy < h_total and cells[yy * w + xx] == '~'
                n, s, wl, e = not wet(x, y - 1), not wet(x, y + 1), not wet(x - 1, y), not wet(x + 1, y)
                for on, name in ((n, 'water_edge_n'), (s, 'water_edge_s'), (wl, 'water_edge_w'), (e, 'water_edge_e'),
                                 (n and wl, 'water_corner_nw'), (n and e, 'water_corner_ne'),
                                 (s and wl, 'water_corner_sw'), (s and e, 'water_corner_se')):
                    if on:
                        part_img = load(name).resize(sz, Image.NEAREST)
                        img.paste(part_img, (x0, y0), part_img)
            elif c == '~' or (c == '^' and not spikes):
                img.paste(tiles[c], (x0, y0))
            elif c == '^':
                s = spikes[alt(x, y) % len(spikes)].resize((int(round(CELL)), int(round(CELL))), Image.NEAREST)
                img.paste(s, (x0, y0), s)
    # 솟는 물건은 위 줄부터 그려 아래 줄이 앞에 오게(게임의 깊이 순서)
    for gy in range(rows):
        y = first + gy
        for x in range(w):
            c = cells[y * w + x]
            pool = blocks if c in '#o' else fences if c == '=' else None
            if not pool:
                continue
            o = pool[picks[(x, h_total - 1 - y)]] if pool is blocks else pool[alt(x, y) % len(pool)]
            o = o.resize((int(round(o.width * k)), int(round(o.height * k))), Image.NEAREST)
            img.paste(o, (int(round(x * CELL)), int(round(TOP + gy * CELL - rise))), o)
    for gy in range(rows):
        y = first + gy
        for x in range(w):
            sp = r['spawns'].get(f'{x},{y}')
            if not sp:
                continue
            p = os.path.join(UNIT, ROLE_SPRITE[sp['role']], f"unit_{ROLE_SPRITE[sp['role']]}_s.png")
            u = Image.open(p).convert('RGBA').resize((UNIT_PX, UNIT_PX), Image.NEAREST)
            cx, cy = x * CELL + CELL / 2, TOP + gy * CELL + CELL / 2
            img.paste(u, (int(cx - UNIT_PX / 2), int(cy - UNIT_PX * 0.72)), u)
    out = os.path.join(EX, 'ref', 'room_tpl_mock', f'layout_ch{ch}_room{room_no}.png')
    img.save(out)
    # 칸 격자를 얹은 판 — 지나가는 줄 확인용
    g = img.copy()
    dr = ImageDraw.Draw(g)
    for i in range(w + 1):
        dr.line([(i * CELL, TOP), (i * CELL, TOP + rows * CELL)], fill=(0, 255, 0), width=1)
    for j in range(rows + 1):
        dr.line([(0, TOP + j * CELL), (720, TOP + j * CELL)], fill=(0, 255, 0), width=1)
    g.save(out.replace('.png', '_grid.png'))
    print(out)


if __name__ == '__main__':
    main(*(int(a) for a in sys.argv[1:5]))
