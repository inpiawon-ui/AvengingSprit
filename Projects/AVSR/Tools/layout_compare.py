# -*- coding: utf-8 -*-
"""원작 방 배치 패턴 vs 우리 손배치 방 — 같은 잣대로 재고 같은 그림 형식으로 나란히 (3단계 조사, 2026-10-22).

잣대: 물건 면적 % · 물건 좌우 대칭 · 방당 적 수 · 적 배치 좌우 대칭 · 입구에서 가장 가까운 적(칸) · 양옆 끝 2열에 선 적 비율.
원작 값 · 예시는 `plan_content_expand/step3_research/origin_layouts.md`(조사 원문)에서 옮겼다.
쓰는 법: python layout_compare.py → _exchange/plan_content_expand/step3_research/layout_compare.png + 숫자 출력
"""
import glob
import math
import os
import re
import statistics

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
ROOMS = os.path.join(ROOT, 'Projects', 'AVSR', 'Rooms')
OUT = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'plan_content_expand', 'step3_research', 'layout_compare.png')
FONT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Fonts', 'NotoSansKR-Bold.ttf')

# 원작 대표 예 10 (조사 원문 그대로 — 폭 11, 문 줄 뺌)
ORIGIN = {
    '① 빈 방 · 원거리 5': """...........|.r.......r.|...........|...........|...........|...........|...........|.....r.....|...........|...........|...........|...........|...........|.r...S...r.|...........""",
    '② 네 귀퉁이': """##.......##|#r...f...r#|...........|...........|...........|..r.....r..|...........|.....r.....|...........|...........|...........|...........|...........|#....S....#|##.......##""",
    '③ 가운데 기둥 + 귀퉁이': """###.....###|...........|..f..f..f..|...........|...........|..f.....f..|...........|....###....|...........|.....f.....|...........|...........|#.........#|#....S....#|#.........#""",
    '④ 좌우 세로 벽 둘': """..#.....#..|..#.....#..|..f..f..f..|...........|..#.....#..|..#.....#..|..#.....#..|...........|..f.....f..|..#.....#..|..#.....#..|..#.....#..|..#.....#..|..#..S..#..|..#.....#..""",
    '⑤ 가로 띠 + 가시': """##.......##|####.m.####|.....^.....|.r...^...r.|...........|...........|^^^^...^^^^|.....r.....|...........|.....^.....|m....^....m|.....^.....|...........|#....S....#|##.......##""",
    '⑦ 물 십자 + 포탑': """#.........#|...........|.~~.....~~.|.~~r.~.r~~.|.....~.....|.....~.....|..~~~t~~~..|.....~.....|...r.~.r...|.~~..~..~~.|.~~.....~~.|...........|...........|.....S.....|#.........#""",
    '⑧ 양옆 물 + 돌 블록': """...........|...m...m...|~~~#...#~~~|~~~#...#~~~|...........|...........|...........|...........|...m...m...|...........|...........|~~#.....#~~|~~#.....#~~|.....S.....|m.........m""",
    '⑨ 가운데 못 + ㄱ자 벽': """..m.......m|...........|...........|.#.....###.|.#..m....m.|.#.........|....~~~....|....~~~....|....~~~....|.........#.|......m..#.|.###.....#.|...........|.....S.....|m..........""",
    '⑩ 지그재그': """...........|...........|...........|#####m.....|...........|...m...m...|...........|.....m.....|...........|...........|...........|.....m#####|...........|.....S.....|...........""",
}

COL = {'.': (70, 72, 80), '#': (150, 120, 90), '~': (70, 130, 200), '=': (150, 150, 160), '^': (220, 80, 80),
       'x': (120, 100, 140), 'm': (240, 90, 80), 'r': (250, 170, 60), 'f': (120, 220, 240), 't': (200, 120, 240),
       'S': (120, 240, 130), 'h': (120, 240, 130), 'E': (255, 255, 255)}
ENEMY = set('mrft')


def parse_ours():
    rooms = []
    for path in sorted(glob.glob(os.path.join(ROOMS, 'rooms90_ch*.txt')), key=lambda p: int(re.findall(r'\d+', os.path.basename(p))[-1])):
        lines = open(path, encoding='utf-8').read().split('\n')
        i = 0
        while i < len(lines):
            m = re.match(r'\[(CH\d+_\d+)\]', lines[i])
            if not m:
                i += 1
                continue
            rid, legend, grid = m.group(1), {}, []
            i += 1
            while i < len(lines) and not lines[i].startswith('['):
                ln = lines[i].rstrip()
                if ln.startswith('legend:'):
                    for tok in ln[7:].split():
                        k, v = tok.split('=')
                        legend[k] = v
                elif ln == 'map:':
                    grid = [lines[i + 1 + r].rstrip() for r in range(16)]
                    i += 16
                i += 1
            rooms.append((rid, legend, grid))
    return rooms


def stats(grid, enemy_of):
    h, w = len(grid), len(grid[0])
    obst = [(x, y) for y in range(h) for x in range(w) if grid[y][x] not in '.' and not enemy_of(grid[y][x]) and grid[y][x] not in 'S']
    foes = [(x, y) for y in range(h) for x in range(w) if enemy_of(grid[y][x])]
    oset, fset = set(obst), set(foes)
    sym_o = sum(1 for (x, y) in obst if (w - 1 - x, y) in oset) / max(1, len(obst))
    sym_f = sum(1 for (x, y) in foes if (w - 1 - x, y) in fset) / max(1, len(foes))
    ex, ey = (w - 1) / 2, h - 1.5   # 입구(아래 가운데)
    near = min((math.hypot(x - ex, y - ey) for x, y in foes), default=0)
    edge = sum(1 for x, _ in foes if x <= 1 or x >= w - 2) / max(1, len(foes))
    return len(obst) / (w * h), sym_o, len(foes), sym_f, near, edge


def draw_grid(d, grid, x0, y0, cell, enemy_of, hostcells=()):
    for y, row in enumerate(grid):
        for x, ch in enumerate(row):
            c = COL.get(ch)
            if (x, y) in hostcells:
                c = COL['h']
            elif enemy_of(ch):
                c = COL.get(ch, COL['m']) if ch in COL and ch in ENEMY else COL['m']
            elif c is None:
                c = (150, 120, 90)
            d.rectangle([x0 + x * cell, y0 + y * cell, x0 + (x + 1) * cell - 2, y0 + (y + 1) * cell - 2], fill=c)


def main():
    ours = parse_ours()
    rows = []
    for rid, legend, grid in ours:
        if len(grid) < 16:
            continue
        is_foe = (lambda lg: (lambda ch: ch.islower() and ch in lg))(legend)
        rows.append((rid, legend, grid, stats(grid, is_foe)))
    o_rows = [(k, v.split('|')) for k, v in ORIGIN.items()]
    o_stats = [stats(g, lambda ch: ch in ENEMY) for _, g in o_rows]

    def summary(st):
        return (statistics.median(s[0] for s in st) * 100, statistics.mean(1 if s[1] >= 0.95 else 0 for s in st) * 100,
                statistics.median(s[2] for s in st), statistics.mean(1 if s[3] >= 0.95 else 0 for s in st) * 100,
                statistics.median(s[4] for s in st), statistics.mean(s[5] for s in st) * 100)
    us = summary([r[3] for r in rows])
    print('우리 %d방 — 물건 면적 중앙 %.1f%% · 물건 좌우 대칭 %.0f%% · 적 수 중앙 %d · 적 좌우 대칭 %.0f%% · 가장 가까운 적 중앙 %.1f칸 · 양옆 끝 %.0f%%' % ((len(rows),) + us))

    font = ImageFont.truetype(FONT, 22)
    small = ImageFont.truetype(FONT, 15)
    cell = 14
    gw, gh = 11 * cell + 24, 16 * cell + 60
    sample = [r for r in rows if r[0].endswith(('_003', '_006', '_010'))][:10]
    W = max(len(o_rows), len(sample)) * gw + 20
    H = 70 + gh + 70 + gh + 150
    img = Image.new('RGB', (W, H), (34, 36, 44))
    d = ImageDraw.Draw(img)
    d.text((12, 10), '원작 방 대표 예 (폭 11 · 문 위 · S 시작)', font=font, fill=(235, 235, 235))
    for i, (name, g) in enumerate(o_rows):
        x0 = 12 + i * gw
        draw_grid(d, g, x0, 50, cell, lambda ch: ch in ENEMY)
        d.text((x0, 50 + 15 * cell + 4), name, font=small, fill=(220, 220, 220))
    y1 = 70 + gh
    d.text((12, y1), '우리 손배치 방 (폭 10 · 3 · 6 · 10번 방 몇 개)', font=font, fill=(235, 235, 235))
    for i, (rid, legend, grid, st) in enumerate(sample):
        x0 = 12 + i * gw
        is_foe = (lambda lg: (lambda ch: ch.islower() and ch in lg))(legend)
        hosts = {(x, y) for y in range(16) for x in range(10) if grid[y][x] in legend and legend[grid[y][x]].endswith('*')}
        draw_grid(d, grid, x0, y1 + 40, cell, is_foe, hosts)
        d.text((x0, y1 + 40 + 16 * cell + 4), f'{rid} · 적 {st[2]}', font=small, fill=(220, 220, 220))
    yl = y1 + 40 + gh
    leg = [('#', '막는 물건'), ('~', '물(탄은 통과)'), ('^', '가시 · 위험물'), ('m', '근접'), ('r', '원거리'), ('f', '비행'),
           ('t', '포탑'), ('h', '빼앗을 몸(우리)'), ('S', '시작')]
    x = 12
    for k, txt in leg:
        d.rectangle([x, yl, x + 14, yl + 14], fill=COL[k])
        d.text((x + 20, yl - 3), txt, font=small, fill=(220, 220, 220))
        x += 30 + d.textlength(txt, font=small)
    d.text((12, yl + 30), '우리 %d방: 물건 면적 중앙 %.0f%% · 물건 좌우 대칭 %.0f%% · 적 수 중앙 %d · 적 좌우 대칭 %.0f%% · 입구에서 가장 가까운 적 중앙 %.1f칸 · 양옆 끝 %.0f%%'
           % ((len(rows),) + us), font=small, fill=(250, 200, 120))
    d.text((12, yl + 56), '원작(조사 8천여 맵): 물건 면적 중앙 8% · 물건 좌우 대칭 56~75% · 5~6마리 방 적 좌우 대칭 73% · 가장 가까운 적 중앙 5칸 · 양옆 끝 43%',
           font=small, fill=(150, 220, 160))
    d.text((12, yl + 82), '(우리 적 수는 방 지도 칸만 — 실행 때 「추가 몫」으로 더 선다)', font=small, fill=(170, 170, 180))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    img.save(OUT)
    print(OUT, img.size)


if __name__ == '__main__':
    main()
