# -*- coding: utf-8 -*-
"""원작에서 옮긴 30방(`Rooms/origin_ch<N>.json`)을 게임이 읽는 표 `Rooms/rooms30.tsv` 로 굽는다 (3단계, 2026-10-22).

PD 10-22 「원작 배치를 따라서 정확하게 유저가 느낄 수 있게」. 칸 하나하나를 그대로 옮긴다.

── 크기 ──────────────────────────────────────────────────────────
  방 폭은 지금처럼 10 m(화면 폭), 그것을 원작처럼 **11 칸**으로 나눈다 — 한 칸 10/11 m.
  세로는 원작 줄 수 그대로(15 또는 21). 아래에 입구 난간 띠(BOTTOM 2.9 m), 위에 문 띠(GATE 2.36 m)를 둔다
  → 15줄 방 = 18.9 m · 21줄 방 = 24.4 m(카메라가 세로로 따라간다 · 바닥은 잘라 쌓는다).
  y 는 아래(입구)가 0. 원작 맨 아랫줄이 y 2.9 ~ 한 칸.

── 칸 → 물건(하는 일은 원작 그대로, 그림은 무대 물건) ────────────────
  #  BLOCK   몸 막음 · 탄 막음 · 적탄 막음        (막는 물건 — 무대마다 모양 여러 가지, 그림 발주 중)
  o  BLOCK   〃 (원작 「기타」 — 작은 막는 소품)
  ~  WATER   몸 막음 · 탄 통과                    (물 — 이어지는 타일, 그림 발주 중)
  =  FENCE   몸 막음 · 탄 통과                    (울타리)
  ^  SPIKE   몸 · 탄 통과 · 밟으면 피해(늘 서 있음)
  t  레일     TRACK 줄로 따로 — 레일을 따라 톱이 오간다 / 돈다
  "  풀      하는 일이 없어 넣지 않는다(원작도 꾸밈)
  m  몬스터   SPAWN (역할에 채운 챕터 몬스터)

표 형식(탭):  ROOM id 이름 설명 · SIZE 폭m 높이m 칸m · OBJ(rooms90.tsv 와 같다) · SPAWN · TRACK 고리 톱수 x,y;x,y;…
쓰는 법: python origin_rooms_bake.py [챕터 …]   (빼면 origin_ch*.json 전부)
"""
import math
import glob
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
ROOMS = os.path.join(ROOT, 'Projects', 'AVSR', 'Rooms')
OUT = os.path.join(ROOMS, 'rooms30.tsv')

WIDTH_M, COLS = 10.0, 11
CELL = WIDTH_M / COLS
GATE = 16.0 - 15 * CELL      # 위 문 띠 ≈ 2.36 m — 무대 바닥 그림의 문 띠(2.4 m)와 맞다
# 아래 입구 난간 띠 — 무대 바닥 그림 아래 2.9 m 는 입구 난간이다(BattleDirector.TallFloor 의 FloorBottomBandFrac).
# 원작 칸을 그 위부터 깐다. 예전(2026-10-22)엔 0 에서 깔아 원작 맨 아랫줄 물건 · 물이 난간 그림 위에 얹혔다
# (PD 10-23 실제 방 미리보기 — 1챕터 7 · 24 · 28번 방). 플레이어 시작 자리(1.7 m)는 그대로 난간 문 안이다.
BOTTOM = 2.9
SPIKE_DAMAGE, SPIKE_TICK = 6, 0.8   # 지금 불바닥 · 가시와 같은 값(rooms90_build.py HAZARD)

# 칸 글자 → (종류, 몸 막음, 탄 막음, 적탄 막음, 해저드)
CELL_KIND = {
    '#': ('BLOCK', 1, 1, 1, 'NONE'),
    'o': ('BLOCK', 1, 1, 1, 'NONE'),
    '~': ('WATER', 1, 0, 0, 'NONE'),
    '=': ('FENCE', 1, 0, 0, 'NONE'),
    '^': ('SPIKE', 0, 0, 0, 'SPIKE'),
}


def fmt(v):
    return f'{v:.3f}'.rstrip('0').rstrip('.')


def center(x, row, rows):
    return (x + 0.5) * CELL, BOTTOM + (rows - 1 - row + 0.5) * CELL


def tracks(cells, w, h):
    """'t' 칸을 레일 줄로 — 고리(모든 칸 이웃 둘)는 한 바퀴, 외줄은 끝에서 끝, 갈래가 있으면 곧은 토막마다 따로."""
    rail = {(i % w, i // w) for i, c in enumerate(cells) if c == 't'}
    nb = lambda p: [q for q in ((p[0] + 1, p[1]), (p[0] - 1, p[1]), (p[0], p[1] + 1), (p[0], p[1] - 1)) if q in rail]
    seen, out = set(), []
    for start in sorted(rail, key=lambda p: (p[1], p[0])):
        if start in seen:
            continue
        comp, stack = set(), [start]
        while stack:
            p = stack.pop()
            if p in comp:
                continue
            comp.add(p)
            stack += nb(p)
        seen |= comp
        degree = {p: len(nb(p)) for p in comp}
        if len(comp) >= 4 and all(d == 2 for d in degree.values()):          # 고리
            path, prev, cur = [start], None, start
            while True:
                nxt = [q for q in nb(cur) if q != prev and q in comp]
                nxt = [q for q in nxt if q not in path] or []
                if not nxt:
                    break
                prev, cur = cur, nxt[0]
                path.append(cur)
            out.append((1, 2 if len(path) >= 12 else 1, path))
        elif all(d <= 2 for d in degree.values()):                            # 외줄
            end = next((p for p, d in degree.items() if d <= 1), start)
            path, prev, cur = [end], None, end
            while True:
                nxt = [q for q in nb(cur) if q != prev and q not in path]
                if not nxt:
                    break
                prev, cur = cur, nxt[0]
                path.append(cur)
            if len(path) >= 2:
                out.append((0, 1, path))
        else:                                                                 # 갈래 — 곧은 토막마다 톱 하나
            for horiz in (True, False):
                done = set()
                for p in sorted(comp, key=lambda q: (q[1], q[0]) if horiz else (q[0], q[1])):
                    if p in done:
                        continue
                    run, q = [p], p
                    while True:
                        q = (q[0] + 1, q[1]) if horiz else (q[0], q[1] + 1)
                        if q not in comp:
                            break
                        run.append(q)
                    done |= set(run)
                    if len(run) >= 3:
                        out.append((0, 1, run))
    return out


# 몸을 막는 칸 · 그중 틀 수 있는 칸(물 · 울타리 — 바위는 원작 모양을 지키려 두지 않는다)
BODY_BLOCK = {'#', 'o', '~', '='}
OPENABLE = {'~', '='}


def open_islands(cells, w, h, spawns):
    """물 · 울타리에 둘러싸여 아래 입구에서 못 가는 적 자리가 있으면 한 칸씩 터서 잇는다(PD 2026-10-27).

    원작은 「물 건너편에서 쏘게」 적을 섬에 가뒀지만, 우리 게임은 그 몸에 **빙의**한다 — 갇힌 몸을 빼앗으면 못 나오고,
    근접 몸은 칠 수가 없다(1챕터 7번 방). 원거리 적이 물 뒤에서 쏘는 맛은 남기고 길을 한 칸만 낸다.
    트는 칸은 이미 갈 수 있는 곳에 바로 닿는 칸 → 방 한가운데에 가까운 칸 순서. 튼 칸 수를 돌려준다.
    """
    g = list(cells)
    opened = 0
    cx, cy = (w - 1) / 2, (h - 1) / 2

    def nb(x, y):
        return [(a, b) for a, b in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)) if 0 <= a < w and 0 <= b < h]

    def free(q):
        return g[q[1] * w + q[0]] not in BODY_BLOCK

    def flood(starts):
        seen, stack = set(), list(starts)
        while stack:
            q = stack.pop()
            if q in seen or not free(q):
                continue
            seen.add(q)
            stack += [n for n in nb(*q) if n not in seen]
        return seen

    for _ in range(w * h):
        reach = flood([(x, h - 1) for x in range(w)])
        cut = [c for c in (tuple(map(int, k.split(','))) for k in spawns) if c not in reach]
        if not cut:
            break
        isle = flood([cut[0]])
        wall = {q for p in isle for q in nb(*p) if g[q[1] * w + q[0]] in OPENABLE}
        if not wall:
            break   # 바위로만 막혔다 — 원작 모양 그대로 둔다
        best = min(wall, key=lambda q: (not any(n in reach for n in nb(*q)), (q[0] - cx) ** 2 + (q[1] - cy) ** 2))
        g[best[1] * w + best[0]] = '.'
        opened += 1
    return ''.join(g), opened


OPENED = []
STARTED = []

# ── 첫 방 호스트 (PD 2026-10-28 「1스테이지에 기본 호스트를 둬서 유령으로 시작하면 그 호스트로 시작」) ──
# 원작 방은 적 자리가 전부 빙의 불가라, 유령으로 출전하면 첫 방에 빼앗을 몸이 없었다.
# 1번 방에 chapters.tsv 의 starter 호스트를 입구 앞(시작 자리에서 빙의 사거리 5 m 안)에 세운다.
# 그 몸 둘레 3 m 안의 원작 적은 뒤(위)로 민다 — 빼앗기 전에 붙어서 때리지 않게(예전 손배치 규칙 「적은 몸보다 3 m 뒤」).
START_AT = (WIDTH_M * 0.5, 1.7)     # 플레이어 시작 자리(RoomImporter60.EntranceAt)
STARTER_DIST = 4.7                  # 시작 자리에서 몸까지 — 빙의 사거리 5 m 안
STARTER_CLEAR = 3.0                 # 몸 둘레 이 안의 적은 민다


EXTRA_GAP = 2.0        # 더 세우는 몬스터 — 원작 적과 이만큼 띄운다
EXTRA_SELF_GAP = 3.0   # 더 세운 몬스터끼리는 이만큼(PD 「너무 겹쳐 놓지 말고 좀 띄워」)
EXTRA_HOST_GAP = 3.0   # 1번 방 첫 호스트와는 이만큼


def extras():
    """chapters.tsv extra 칸 — {챕터: [(몬스터, 마리), …]}"""
    out, head = {}, None
    for line in open(os.path.join(ROOMS, 'chapters.tsv'), encoding='utf-8').read().split('\n'):
        if not line.strip() or line.startswith('#'):
            continue
        t = line.split('\t')
        if head is None:
            head = t
            continue
        d = dict(zip(head, t))
        v = d.get('extra', '').strip()
        if v:
            out[int(d['ch'])] = [(k.strip(), int(n)) for k, n in (p.split(':') for p in v.split(','))]
    return out


def place_extras(cells, w, rows, spawn_lines, extra):
    """방 가운데에서 가까운 빈 칸부터, 다른 적 · 첫 호스트와 떨어진 자리에 세운다."""
    taken = []
    host = None
    for line in spawn_lines:
        t = line.split('\t')
        xy = (float(t[2]), float(t[3]))
        taken.append(xy)
        if t[4] == '1':
            host = xy
    mid = (WIDTH_M * 0.5, BOTTOM + rows * CELL * 0.5)
    free = []
    for i, c in enumerate(cells):
        if c in CELL_KIND:
            continue
        x, y = center(i % w, i // w, rows)
        free.append(((x - mid[0]) ** 2 + (y - mid[1]) ** 2, x, y))
    free.sort()
    added = 0
    mine = []
    for key, n in extra:
        for _ in range(n):
            for _, x, y in free:
                if any(((x - a) ** 2 + (y - b2) ** 2) ** 0.5 < EXTRA_GAP for a, b2 in taken):
                    continue
                if any(((x - a) ** 2 + (y - b2) ** 2) ** 0.5 < EXTRA_SELF_GAP for a, b2 in mine):
                    continue
                if host and ((x - host[0]) ** 2 + (y - host[1]) ** 2) ** 0.5 < EXTRA_HOST_GAP:
                    continue
                spawn_lines.append('\t'.join(['SPAWN', key, fmt(x), fmt(y), '0']))
                taken.append((x, y))
                mine.append((x, y))
                added += 1
                break
    return added


def starters():
    out, head = {}, None
    for line in open(os.path.join(ROOMS, 'chapters.tsv'), encoding='utf-8').read().split('\n'):
        if not line.strip() or line.startswith('#'):
            continue
        t = line.split('\t')
        if head is None:
            head = t
            continue
        d = dict(zip(head, t))
        if d.get('starter', '').strip():
            out[int(d['ch'])] = d['starter'].strip()
    return out


def place_starter(cells, w, rows, spawn_lines, host):
    """막는 물건이 없는 칸 중 시작 자리에서 4~5 m(빙의 사거리 안)이면서 가장 가운데 칸에 몸을 세운다."""
    best = None
    for i, c in enumerate(cells):
        if c in CELL_KIND:
            continue
        x, y = center(i % w, i // w, rows)
        d = ((x - START_AT[0]) ** 2 + (y - START_AT[1]) ** 2) ** 0.5
        if d > 5.0 or d < 4.0:
            continue
        score = (abs(x - START_AT[0]), abs(d - STARTER_DIST))
        if best is None or score < best[0]:
            best = (score, x, y)
    if best is None:
        return None
    _, hx, hy = best
    moved = 0
    for k, line in enumerate(spawn_lines):
        t = line.split('\t')
        x, y = float(t[2]), float(t[3])
        if ((x - hx) ** 2 + (y - hy) ** 2) ** 0.5 < STARTER_CLEAR:
            # 같은 열에서 몸보다 3 m 뒤 · 막히지 않은 칸까지 한 칸씩 올린다(방 위 문 띠는 넘지 않는다)
            col = min(w - 1, max(0, int(x / CELL)))
            # 칸 가운데가 몸에서 3 m 이상이 되는 첫 칸(올림) — 내림이면 2.7 m 칸에 섰다
            idx = math.ceil((hy + STARTER_CLEAR - BOTTOM) / CELL - 0.5)
            while idx < rows and cells[(rows - 1 - idx) * w + col] in CELL_KIND:
                idx += 1
            if idx >= rows:
                continue
            t[3] = fmt(BOTTOM + (idx + 0.5) * CELL)
            spawn_lines[k] = '\t'.join(t)
            moved += 1
    spawn_lines.insert(0, '\t'.join(['SPAWN', host, fmt(hx), fmt(hy), '1']))
    return hx, hy, moved


def bake_room(ch, r, lines):
    rid = f'ROOM_CH{ch}_{r["room"]:03d}'
    w, rows, cells = r['w'], r['h'], r['cells']
    cells, n_open = open_islands(cells, w, rows, r['spawns'])
    if n_open:
        OPENED.append(f'{rid} {n_open}칸')
    height = BOTTOM + rows * CELL + GATE
    lines.append('\t'.join(['ROOM', rid, f'원작 {r["source"].split("/")[-1]}', f'{r["ours"]} · 원작 {r["source"]}']))
    lines.append('\t'.join(['SIZE', fmt(WIDTH_M), fmt(height), fmt(CELL)]))
    for i, c in enumerate(cells):
        if c not in CELL_KIND:
            continue
        kind, bm, bs, be, hz = CELL_KIND[c]
        x, y = center(i % w, i // w, rows)
        dmg, tick = (SPIKE_DAMAGE, SPIKE_TICK) if hz != 'NONE' else (0, 0)
        lines.append('\t'.join(['OBJ', kind, fmt(x), fmt(y), fmt(CELL), fmt(CELL), str(bm), str(bs), str(be), hz, str(dmg), fmt(tick)]))
    for loop, saws, path in tracks(cells, w, rows):
        pts = ';'.join(f'{fmt(px)},{fmt(py)}' for px, py in (center(x, y, rows) for x, y in path))
        lines.append('\t'.join(['TRACK', str(loop), str(saws), pts]))
    spawn_lines = []
    for k, v in sorted(r['spawns'].items()):
        x, row = map(int, k.split(','))
        px, py = center(x, row, rows)
        spawn_lines.append('\t'.join(['SPAWN', v['monster'], fmt(px), fmt(py), '0']))
    host = STARTERS.get(ch) if r['room'] == 1 else None
    if host:
        got = place_starter(cells, w, rows, spawn_lines, host)
        STARTED.append(f'{rid} {host}' + (f' ({fmt(got[0])}, {fmt(got[1])}) · 민 적 {got[2]}' if got else ' — 자리 없음'))
    if ch in EXTRAS:
        EXTRA_ADDED.append(place_extras(cells, w, rows, spawn_lines, EXTRAS[ch]))
    lines.extend(spawn_lines)
    return rid


STARTERS = {}
EXTRAS = {}
EXTRA_ADDED = []


def main(chapters):
    STARTERS.update(starters())
    EXTRAS.update(extras())
    lines = ['# rooms30-origin-1.0 — origin_rooms_bake.py 가 굽는다. 손으로 고치지 않는다.']
    stats = []
    for ch in chapters:
        d = json.load(open(os.path.join(ROOMS, f'origin_ch{ch}.json'), encoding='utf-8'))
        n = 0
        for r in d['rooms']:
            if 'cells' in r:
                bake_room(ch, r, lines)
                n += 1
        stats.append(f'{ch}챕터 ← 원작 {d["origin_chapter"]}장 · 방 {n}')
    with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
        f.write('\n'.join(lines) + '\n')
    print('\n'.join(stats))
    print(OUT, len(lines), '줄')
    print("섬 트기(갇힌 적 자리에 길 냄):", len(OPENED), "방 —", ", ".join(OPENED))
    print("더 세운 몬스터:", sum(EXTRA_ADDED), "마리 ·", len(EXTRA_ADDED), "방")
    print("첫 방 호스트:", len(STARTED), "방")
    for s in STARTED:
        print("  ", s)


if __name__ == '__main__':
    args = [int(a) for a in sys.argv[1:]]
    if not args:
        args = sorted(int(re.findall(r'\d+', os.path.basename(p))[0]) for p in glob.glob(os.path.join(ROOMS, 'origin_ch*.json')))
    main(args)
