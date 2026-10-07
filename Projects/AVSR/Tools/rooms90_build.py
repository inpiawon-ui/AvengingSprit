# -*- coding: utf-8 -*-
"""손으로 그린 90방(rooms90.txt)을 검사하고 게임이 읽는 표(rooms90.tsv)와 배치도 한 장으로 굽는다.

쓰는 법:  python rooms90_build.py            # 검사 + tsv + 배치도
          python rooms90_build.py --strict   # 경고도 실패로 (커밋 전)

입력  Projects/AVSR/Rooms/rooms90.txt   ← 사람이 고치는 유일한 자리
출력  Projects/AVSR/Rooms/rooms90.tsv   ← `Tools > Game > 90방 임포트` 가 읽는다
      Projects/AVSR/Rooms/rooms90_sheet.png

── 방 한 칸 = 1 m. 방은 10 × 16 m. 글자 한 줄이 가로 10 m, 줄 16개가 세로 16 m다. ──
   맨 윗줄이 y 15~16(문 구역), 맨 아랫줄이 y 0~1(입구 쪽).

물건(대문자·기호)은 **발자국 크기만큼 같은 글자로 채운다.** 한 칸만 찍으면 오류다 —
그래야 그림을 보고 어디까지 막히는지 바로 읽힌다.

  P 기둥 1×1        C 상자 2×1(가로)    B 덩어리 2×2      L 낮은 벽 3×1(가로)
  R 바리케이드 3×1  I 난간 1×2(세로)    S 주기 가시 2×2   O 회전 톱니 2×2(반경 2.2 m 돈다)
  H 왕복 해머 2×2(위아래로 ±1.7 m 오간다)      = 도랑 가로 2×1    # 도랑 세로 1×2
  W 되튕기는 벽 3×1(가로)   V 되튕기는 벽 1×3(세로)   F 불바닥 2×2
  X 폭발 통 1×1    T 벽 포탑 1×1(아래로 쏜다)   < > 벽 포탑(왼쪽/오른쪽으로 쏜다)   K 미는 바위 1×1

적(소문자)은 한 글자가 한 기다. 방마다 legend 로 글자→몸을 정하고, 빼앗을 수 있는 몸은 `*` 를 붙인다.
"""
import os
import sys
import re

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
SRC_DIR = os.path.join(ROOT, 'Projects', 'AVSR', 'Rooms')   # rooms90_ch1.txt … rooms90_chN.txt
TSV = os.path.join(ROOT, 'Projects', 'AVSR', 'Rooms', 'rooms90.tsv')
SHEET = os.path.join(ROOT, 'Projects', 'AVSR', 'Rooms', 'rooms90_sheet.png')

W, H = 10, 16
GATE_TOP = 13.5          # 이 위로는 물건이 못 올라간다(문 구역 2.5 m)
ENTRANCE = (5.0, 1.7)    # 플레이어가 서는 자리
ENTRANCE_CLEAR = 4.5     # 적은 입구에서 이만큼 떨어진다
FIRST_ROOM_POSSESS_M = 5.5   # 첫 방의 몸 — 입구에서 이 안(빙의 사거리 429 px = 6 m 에서 여유를 뺐다)
FIRST_ROOM_ENEMY_GAP = 3.0   # 첫 방의 적 — 몸보다 이만큼 뒤(위)

# 글자 → (종류, 폭, 높이)
GLYPH = {
    'P': ('PILLAR', 1, 1),
    'C': ('CRATE', 2, 1),
    'B': ('BULK', 2, 2),
    'L': ('LOW_COVER', 3, 1),
    'R': ('BARRICADE', 3, 1),
    'I': ('RAIL', 1, 2),
    'S': ('TIMED_SPIKE', 2, 2),
    'O': ('ROTATING_BLADE', 2, 2),
    'H': ('SWING_HAMMER', 2, 2),
    '=': ('CHANNEL_H', 2, 1),
    '#': ('CHANNEL_V', 1, 2),
    'W': ('RICOCHET_WALL', 3, 1),
    'V': ('RICOCHET_WALL', 1, 3),
    'F': ('HAZARD', 2, 2),
    'X': ('EXPLOSIVE_BARREL', 1, 1),
    'T': ('WALL_TURRET_S', 1, 1),
    '<': ('WALL_TURRET_W', 1, 1),
    '>': ('WALL_TURRET_E', 1, 1),
    'K': ('PUSH_ROCK', 1, 1),
    # ── 2차 (2026-09-28) — 피해야 하는 것 7종 + 물건 다양화 ──
    '-': ('LASER_H', 4, 1),          # 레이저 문(가로) — 꺼짐 → 깜빡 → 켜짐
    '!': ('LASER_V', 1, 4),          # 레이저 문(세로)
    'A': ('SLIDE_BLADE_H', 2, 2),    # 레일 톱날 — 좌우로 ±2 m 오간다
    'U': ('SLIDE_BLADE_V', 2, 2),    # 레일 톱날 — 위아래로 ±2 m 오간다
    'Z': ('SWING_HAMMER_H', 2, 2),   # 가로 해머 — 좌우로 ±1.7 m 오간다
    'J': ('FLAME_JET_S', 1, 1),      # 화염 분사구(아래로 3 m)
    '}': ('FLAME_JET_E', 1, 1),      # 화염 분사구(오른쪽으로)
    '{': ('FLAME_JET_W', 1, 1),      # 화염 분사구(왼쪽으로)
    'D': ('DROP_ZONE', 2, 2),        # 낙하물 — 예고 원이 뜨고 떨어진다(바닥에 그림 없음)
    'G': ('SLOW_POOL', 2, 2),        # 끈끈이 웅덩이 — 밟으면 느려진다
    'M': ('MINE', 1, 1),             # 근접 지뢰
    'N': ('BLOCK', 1, 1),            # 낮은 돌 블록 — 모아서 담을 쌓는다
    'Q': ('PROP_TALL', 1, 1),        # 무대 간판 소품(키 큰 것 — 가로등·안테나·배양관 …)
    'E': ('PROP_WIDE', 2, 1),        # 무대 간판 소품(넓은 것 — 폐차·실외기·제어반 …)
    # ── 3차 (2026-10-07) — 키 큰 것을 대신할 **낮은 엄폐** (PD 「위로 긴 것들은 2D 라 애매하다 · 다른 오브젝트로」)
    #    솟음이 낮아 그림 = 막힌 칸이다. 그래도 **적 탄도 막는다**(모래주머니 뒤에 숨는다) — 방 한가운데 엄폐는 이것으로.
    '$': ('SANDBAG', 2, 1),          # 모래주머니 낮은 담
    '%': ('BARREL_PILE', 2, 1),      # 눕혀 쌓은 드럼통
    '~': ('FALLEN_PILLAR', 2, 1),    # 쓰러져 누운 콘크리트 기둥
    '^': ('JERSEY_ROW', 3, 1),       # 낮은 콘크리트 블록 셋 한 줄
    '&': ('SCRAP_PILE', 2, 1),       # 납작한 고철 판 무더기
    '?': ('WRECK_CAR', 2, 1),        # 위에서 본 납작한 폐차
    '@': ('PIT', 2, 2),              # 바닥 구덩이 — 몸은 못 건너고 탄은 넘어간다(도랑과 같다)
}

# 키 큰 것 — 적 탄도 막는다. 낮은 것(상자·낮은 벽·바리케이드)은 적 탄이 넘어온다.
TALL = {'PILLAR', 'BULK', 'RAIL', 'RICOCHET_WALL', 'WALL_TURRET_S', 'WALL_TURRET_W', 'WALL_TURRET_E',
        'PUSH_ROCK', 'PROP_TALL'}
# 낮은 엄폐(3차) — 솟음이 낮지만 탄을 막는다. 원거리 적의 엄폐로도 친다
LOW_SHIELD = {'SANDBAG', 'BARREL_PILE', 'FALLEN_PILLAR', 'JERSEY_ROW', 'SCRAP_PILE', 'WRECK_CAR'}
# 원거리 적이 숨을 수 있는 것 — 적 탄도 막는 것
COVER = TALL | LOW_SHIELD
# 몸을 막는 것
SOLID = TALL | LOW_SHIELD | {'CRATE', 'LOW_COVER', 'BARRICADE', 'EXPLOSIVE_BARREL', 'SWING_HAMMER', 'SWING_HAMMER_H',
                'BLOCK', 'PROP_WIDE', 'FLAME_JET_S', 'FLAME_JET_E', 'FLAME_JET_W'}
# 몸은 못 건너지만 탄은 지나가는 것
CHANNEL = {'CHANNEL_H', 'CHANNEL_V', 'PIT'}
# 밟으면 아픈 것 (피해, 간격)
HAZARD = {'TIMED_SPIKE': ('SPIKE', 6, 0.8), 'HAZARD': ('FIRE', 6, 0.8),
          'ROTATING_BLADE': ('BLADE', 10, 0.5), 'SWING_HAMMER': ('HAMMER', 12, 0.7),
          'SWING_HAMMER_H': ('HAMMER', 12, 0.7),
          'SLIDE_BLADE_H': ('BLADE', 10, 0.5), 'SLIDE_BLADE_V': ('BLADE', 10, 0.5),
          'LASER_H': ('LASER', 12, 0.6), 'LASER_V': ('LASER', 12, 0.6)}
# 바닥에 있고 몸도 탄도 안 막는 것 — 적이 그 위에 서도 된다(경고만)
FLOOR = {'DROP_ZONE', 'SLOW_POOL', 'MINE'}

BLADE_RADIUS = 2.2
HAMMER_HALF_TRAVEL = 1.7
SLIDE_HALF_TRAVEL = 2.0

# ── 챕터 정의 — chapters.tsv 가 단일 출처다(유니티 임포터도 같은 표를 읽는다) ──
# 챕터 수 · 잡몹 목록 · 적 수 범위를 여기 코드에 적지 않는다. 새 챕터는 그 표에 한 줄을 더한다.
CHAPTERS_TSV = os.path.join(ROOT, 'Projects', 'AVSR', 'Rooms', 'chapters.tsv')


def load_chapters():
    rows, head = {}, None
    with open(CHAPTERS_TSV, encoding='utf-8') as f:
        for raw in f:
            line = raw.rstrip('\n')
            if not line.strip() or line.startswith('#'):
                continue
            cells = line.split('\t')
            if head is None:
                head = cells
                continue
            d = dict(zip(head, cells))
            lo, hi = d['count'].split('-')
            rows[int(d['ch'])] = {
                'stage': d['stage'], 'boss': d['boss'], 'leader': d['leader'],
                'trash': set(d['trash'].split(',')), 'lean': d['lean'],
                'count': (int(lo), int(hi)), 'name': d['name'],
            }
    return rows


CHAPTERS = load_chapters()
TRASH = {ch: c['trash'] for ch, c in CHAPTERS.items()}

# 상성(가위바위보) — `AffinityRule.KindOf` 와 같아야 한다. 힘 → 날 → 술 → 힘.
KIND = {
    'bat': 'blade', 'roadwarden': 'blade', 'scrapgunner': 'blade', 'mole': 'blade',
    'actor_enforcer': 'force', 'turret_cross': 'force', 'boar': 'force',
    'skeleton': 'magic', 'coilwalker': 'magic',
    'gangster': 'blade', 'thug': 'blade', 'hopper': 'blade', 'hopper_smg': 'blade', 'commando_mg': 'blade',
    'ninja': 'blade', 'amazon': 'blade', 'amazon_elite': 'blade',
    'baseball': 'force', 'guru': 'force', 'ninja_chain': 'force', 'commando_grenade': 'force',
    'commando_missile': 'force', 'commando_laser': 'force', 'robot': 'force',
    'dragoon': 'magic', 'snowwoman': 'magic', 'dragon_blue': 'magic', 'salamander': 'magic',
    'vampire': 'magic', 'white_wizard': 'magic', 'medium': 'magic', 'death': 'magic',
}
BEATS = {'force': 'blade', 'blade': 'magic', 'magic': 'force'}
RANGED_TRASH = {'scrapgunner', 'roadwarden', 'coilwalker', 'turret_cross', 'mole'}
STATIC_TRASH = {'turret_cross'}   # 안 움직인다 — 자리가 곧 전부다
# 엄폐가 필요 없는 원거리 — 포탑은 서 있는 자리가 전부고, 두더지는 땅속으로 다닌다
NO_COVER_TRASH = STATIC_TRASH | {'mole'}

# 호스트 데뷔 챕터 — 지금 배정표(RoomDef60)에서 처음 나오는 챕터 그대로
DEBUT = {
    'gangster': 1, 'amazon': 1, 'commando_grenade': 1, 'salamander': 1, 'hopper': 1,
    'hopper_smg': 2, 'thug': 2, 'commando_mg': 2, 'robot': 2, 'guru': 2, 'white_wizard': 2,
    'snowwoman': 3, 'ninja': 3, 'vampire': 3,
    'baseball': 4, 'medium': 4, 'dragon_blue': 4, 'commando_laser': 4, 'ninja_chain': 4,
    'amazon_elite': 5, 'commando_missile': 5,
    'death': 6, 'dragoon': 6,
}
# 붙어서 싸우는 몸 — HostTable 의 AttackKind 가 Melee(0)·Pulse(6) 인 것. 나머지는 쏜다.
MELEE_HOST = {'amazon', 'amazon_elite', 'death', 'guru', 'baseball', 'ninja_chain'}
MAX_HOSTS = 2
# 히든 캐릭터 — 적으로도 몸으로도 방에 세우지 않는다(사신은 따로 얻는 몸이다, PD 2026-10-07)
HIDDEN_HOST = {'death'}

# 방마다 적 수 — 챕터 안에서도 뒤로 갈수록 는다
COUNT = {ch: c['count'] for ch, c in CHAPTERS.items()}
COMBAT_NO = ['001', '002', '003', '005', '006', '007', '009', '010', '011', '013', '014']


class Room:
    def __init__(self, cid):
        self.id = cid            # ROOM_CH1_001
        self.name = ''
        self.note = ''
        self.legend = {}
        self.rows = []
        self.objects = []        # (kind, cx, cy, w, h)
        self.spawns = []         # (actor, cx, cy, host)
        self.chapter = int(re.match(r'ROOM_CH(\d+)_', cid).group(1))
        self.no = cid[-3:]
        self.line = 0


def parse(path):
    rooms, cur, in_map = [], None, False
    with open(path, encoding='utf-8') as f:
        for ln, raw in enumerate(f, 1):
            line = raw.rstrip('\n')
            if in_map:
                if line.strip() == '' or line.startswith('['):
                    in_map = False
                else:
                    cur.rows.append(line.rstrip())
                    continue
            s = line.strip()
            if not s or s.startswith('//'):
                continue
            m = re.match(r'\[(CH\d+_\d{3})\]\s*(.*)', s)
            if m:
                cur = Room('ROOM_' + m.group(1))
                cur.line = ln
                rest = m.group(2)
                if '|' in rest:
                    cur.name, cur.note = [t.strip() for t in rest.split('|', 1)]
                else:
                    cur.name = rest.strip()
                rooms.append(cur)
                continue
            if s.startswith('legend:'):
                for tok in s[7:].split():
                    g, unit = tok.split('=')
                    cur.legend[g] = (unit.rstrip('*'), unit.endswith('*'))
                continue
            if s.startswith('map:'):
                in_map = True
                continue
            raise SystemExit(f'{path}:{ln}: 모르는 줄 — {s}')
    return rooms


def build(room, errors, warns):
    rows = room.rows
    tag = f'{room.id}({room.name})'
    if len(rows) != H:
        errors.append(f'{tag}: 줄이 {len(rows)}개 — 16개여야 한다')
        return
    for r, row in enumerate(rows):
        if len(row) != W:
            errors.append(f'{tag}: {r + 1}째 줄 폭 {len(row)} — 10이어야 한다: "{row}"')
            return
    grid = [list(r) for r in rows]
    used = [[False] * W for _ in range(H)]

    # ── 물건: 발자국을 통째로 같은 글자로 채웠는지 본다 ──
    for r in range(H):
        for c in range(W):
            g = grid[r][c]
            if used[r][c] or g not in GLYPH:
                continue
            kind, w, h = GLYPH[g]
            ok = r + h <= H and c + w <= W
            if ok:
                for dr in range(h):
                    for dc in range(w):
                        if grid[r + dr][c + dc] != g or used[r + dr][c + dc]:
                            ok = False
            if not ok:
                errors.append(f'{tag}: {r + 1}줄 {c + 1}칸 "{g}" — {kind} 는 {w}×{h} 칸을 같은 글자로 채워야 한다')
                used[r][c] = True
                continue
            for dr in range(h):
                for dc in range(w):
                    used[r + dr][c + dc] = True
            cx = c + w / 2.0
            cy = (H - r) - h / 2.0
            room.objects.append((kind, cx, cy, float(w), float(h)))

    # ── 적 ──
    for r in range(H):
        for c in range(W):
            g = grid[r][c]
            if g == '.' or g in GLYPH:
                continue
            if g not in room.legend:
                errors.append(f'{tag}: {r + 1}줄 {c + 1}칸 "{g}" — legend 에 없다')
                continue
            unit, host = room.legend[g]
            room.spawns.append((unit, c + 0.5, (H - r) - 0.5, host))

    check(room, errors, warns)


def rect(o):
    kind, cx, cy, w, h = o
    return cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2


def sweep_rect(o):
    """움직이는 것이 실제로 훑는 자리. 톱니는 축 반경, 해머는 위아래 왕복."""
    kind, cx, cy, w, h = o
    if kind == 'ROTATING_BLADE':
        r = BLADE_RADIUS + 1.0
        return cx - r, cy - r, cx + r, cy + r
    if kind == 'SWING_HAMMER':
        return cx - w / 2, cy - h / 2 - HAMMER_HALF_TRAVEL, cx + w / 2, cy + h / 2 + HAMMER_HALF_TRAVEL
    if kind == 'SWING_HAMMER_H':
        return cx - w / 2 - HAMMER_HALF_TRAVEL, cy - h / 2, cx + w / 2 + HAMMER_HALF_TRAVEL, cy + h / 2
    if kind == 'SLIDE_BLADE_H':
        return cx - w / 2 - SLIDE_HALF_TRAVEL, cy - h / 2, cx + w / 2 + SLIDE_HALF_TRAVEL, cy + h / 2
    if kind == 'SLIDE_BLADE_V':
        return cx - w / 2, cy - h / 2 - SLIDE_HALF_TRAVEL, cx + w / 2, cy + h / 2 + SLIDE_HALF_TRAVEL
    return rect(o)


MOVERS = ('ROTATING_BLADE', 'SWING_HAMMER', 'SWING_HAMMER_H', 'SLIDE_BLADE_H', 'SLIDE_BLADE_V')


def inside(x, y, r, margin=0.0):
    return r[0] - margin < x < r[2] + margin and r[1] - margin < y < r[3] + margin


# 넉넉한 길 — 발자국을 이만큼(m) 부풀려도 문까지 가야 한다.
# 물건은 그림이 70% 라 한 칸 띄운 틈이 1.3 m, 발 폭이 0.8 m 다 — 지나가기는 하지만 모서리에 걸려
# 비비적거린다(2026-10-02 자동 검증에서 CH6_003 기둥 틈에 끼어 맴돌았다). 0.3 이면 두 칸 틈부터 통과다.
WIDE_PAD = 0.3


def reachable(room, pad=0.0):
    """입구에서 문 아래(y 13)까지 걸어갈 수 있나. `RoomMapWindow.Reachable` 과 같은 자로 잰다.

    pad 를 주면 발자국을 그만큼 부풀려 잰다 — 「좁은 틈을 비집지 않고도 가는 길」이 있는가.
    """
    step = 0.25
    us = 1.2                                       # GameConfig.UnitScale
    bw, bh = 96 * us, 92 * us
    hx = max(bw * 0.25, 21) / 72 + pad
    hy = max(bh * 0.16, 14) / 72 + pad
    drop = (bh * 0.5) / 72 - hy
    half_x, half_y = bw * 0.5 / 72, bh * 0.5 / 72
    scale = 0.7                                     # ObstacleViewScale
    boxes = []
    for o in room.objects:
        kind, cx, cy, w, h = o
        if kind not in SOLID and kind not in CHANNEL:
            continue
        if kind == 'SWING_HAMMER':                  # 오가는 길 전체를 막힌 것으로 본다
            boxes.append((cx, cy, w * scale / 2, h * scale / 2 + HAMMER_HALF_TRAVEL))
        elif kind == 'SWING_HAMMER_H':
            boxes.append((cx, cy, w * scale / 2 + HAMMER_HALF_TRAVEL, h * scale / 2))
        else:
            boxes.append((cx, cy, w * scale / 2, h * scale / 2))
    gw, gh = int(W / step), int(H / step)

    def free(gx, gy):
        px, py = gx * step, gy * step
        if px < half_x or px > W - half_x or py < half_y or py > H - half_y:
            return False
        fy = py - drop + pad
        for bx, by, bz, bwd in boxes:
            if abs(px - bx) < bz + hx and abs(fy - by) < bwd + hy:
                return False
        return True

    sx, sy = round(ENTRANCE[0] / step), round(ENTRANCE[1] / step)
    while sy <= gh and not free(sx, sy):
        sy += 1
    if sy > gh:
        return False, 0
    seen = {(sx, sy)}
    q = [(sx, sy)]
    top = 0
    while q:
        x, y = q.pop()
        top = max(top, y * step)
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx <= gw and 0 <= ny <= gh and (nx, ny) not in seen and free(nx, ny):
                seen.add((nx, ny))
                q.append((nx, ny))
    return top >= H - 3.0, len(seen)


def check(room, errors, warns):
    tag = f'{room.id}({room.name})'
    ch = room.chapter
    # 물건 자리
    for o in room.objects:
        kind, cx, cy, w, h = o
        if cy + h / 2 > GATE_TOP + 0.001:
            errors.append(f'{tag}: {kind}({cx},{cy}) 가 문 구역(y>{GATE_TOP})을 침범')
        if kind in SOLID and abs(cx - ENTRANCE[0]) < 1.5 + w / 2 and cy - h / 2 < 3.0:
            errors.append(f'{tag}: {kind}({cx},{cy}) 가 입구 코앞을 막는다')
    # 움직이는 것끼리·움직이는 것과 벽
    for o in room.objects:
        kind = o[0]
        if kind not in MOVERS:
            continue
        sw = sweep_rect(o)
        if kind in ('SWING_HAMMER', 'SLIDE_BLADE_V') and (sw[1] < 0.3 or sw[3] > GATE_TOP):
            errors.append(f'{tag}: {kind}({o[1]},{o[2]}) 왕복 길이 방 밖으로 나간다')
        if kind in ('SWING_HAMMER_H', 'SLIDE_BLADE_H') and (sw[0] < 0 or sw[2] > W):
            errors.append(f'{tag}: {kind}({o[1]},{o[2]}) 왕복 길이 방 밖으로 나간다')
        if kind == 'ROTATING_BLADE' and (o[1] - BLADE_RADIUS < 0 or o[1] + BLADE_RADIUS > W):
            warns.append(f'{tag}: 톱니({o[1]},{o[2]}) 날이 벽 그림에 반쯤 들어간다')
        for p in room.objects:
            if p is o or p[0] in HAZARD or p[0] in CHANNEL or p[0] in FLOOR:
                continue
            pr = rect(p)
            if kind == 'ROTATING_BLADE':
                # 날 끝이 닿는 거리(축 반경 + 날 반폭)보다 가까운 모서리가 있으면 날이 물건을 긁는다
                nx = min(max(o[1], pr[0]), pr[2])
                ny = min(max(o[2], pr[1]), pr[3])
                hit = ((nx - o[1]) ** 2 + (ny - o[2]) ** 2) ** 0.5 < BLADE_RADIUS + 1.0
            else:
                hit = pr[0] < sw[2] and pr[2] > sw[0] and pr[1] < sw[3] and pr[3] > sw[1]
            if hit:
                warns.append(f'{tag}: {kind}({o[1]},{o[2]}) 가 훑는 자리에 {p[0]}({p[1]},{p[2]}) 가 있다')
    # 적
    n = len(room.spawns)
    lo, hi = COUNT[ch]
    if room.no in COMBAT_NO and not (lo <= n <= hi):
        warns.append(f'{tag}: 적 {n}기 — CH{ch} 은 {lo}~{hi}')
    hosts = [s for s in room.spawns if s[3]]
    if room.no in COMBAT_NO and not hosts:
        errors.append(f'{tag}: 빼앗을 몸이 없다')
    if len(hosts) > MAX_HOSTS:
        errors.append(f'{tag}: 빼앗을 몸 {len(hosts)} — 최대 {MAX_HOSTS} (넘치면 잡몹으로 강등된다)')
    seen_host = set()
    has_melee = has_ranged = False
    for actor, x, y, host in room.spawns:
        if host:
            if actor not in DEBUT:
                errors.append(f'{tag}: 모르는 호스트 {actor}')
            elif actor in HIDDEN_HOST:
                errors.append(f'{tag}: {actor} 는 히든 캐릭터 — 방에 세우지 않는다(PD 2026-10-07)')
            elif DEBUT[actor] > ch:
                errors.append(f'{tag}: {actor} 는 CH{DEBUT[actor]} 데뷔 — CH{ch} 에 못 나온다')
            if actor in seen_host:
                errors.append(f'{tag}: 같은 몸 {actor} 이 둘')
            seen_host.add(actor)
            if actor in MELEE_HOST: has_melee = True
            else: has_ranged = True
        else:
            if actor not in TRASH[ch]:
                errors.append(f'{tag}: 잡몹 {actor} 는 CH{ch} 목록에 없다 (그림이 안 올라간다)')
            if actor in RANGED_TRASH: has_ranged = True
            else: has_melee = True
        d = ((x - ENTRANCE[0]) ** 2 + (y - ENTRANCE[1]) ** 2) ** 0.5
        if d < ENTRANCE_CLEAR:
            errors.append(f'{tag}: {actor}({x},{y}) 가 입구에서 {d:.1f} m — {ENTRANCE_CLEAR} m 이상')
        for o in room.objects:
            kind = o[0]
            if kind in FLOOR:
                continue                                  # 바닥 것 위에는 서도 된다
            if kind in MOVERS and kind != 'ROTATING_BLADE':
                if inside(x, y, sweep_rect(o), 0.3):      # 오가는 길 위에 서면 첫 박자에 치인다
                    errors.append(f'{tag}: {actor}({x},{y}) 가 {kind}({o[1]},{o[2]}) 가 오가는 길에 선다')
                continue
            if kind in HAZARD:
                if inside(x, y, rect(o)):
                    warns.append(f'{tag}: {actor}({x},{y}) 가 {kind} 위에 선다')
                continue
            if inside(x, y, rect(o), 0.3):
                errors.append(f'{tag}: {actor}({x},{y}) 가 {kind}({o[1]},{o[2]}) 속에 선다')
        # 원거리는 엄폐 뒤에 세운다 — 키 큰 것이 1.6 m 안에
        ranged = (actor in RANGED_TRASH) if not host else (actor not in MELEE_HOST)
        if ranged and actor not in NO_COVER_TRASH:
            near = any(o[0] in COVER and abs(o[1] - x) < o[3] / 2 + 1.6 and abs(o[2] - y) < o[4] / 2 + 1.6
                       for o in room.objects)
            if not near:
                warns.append(f'{tag}: 원거리 {actor}({x},{y}) 근처에 키 큰 엄폐가 없다')
    if room.no in COMBAT_NO and not (has_melee and has_ranged):
        warns.append(f'{tag}: 근접·원거리가 다 있어야 한다 (근접 {has_melee} · 원거리 {has_ranged})')
    # 챕터 첫 방 — 들어서자마자 빙의하고 시작한다(기획 2026-10-06).
    # 유령은 몸이 없으면 에너지가 닳으므로, 첫 방에서 적을 지나 몸을 찾으러 가게 하면
    # 빙의를 배우기도 전에 죽는다. 몸은 입구에서 빙의 사거리 안, 적은 그 몸보다 3 m 이상 뒤.
    if room.no == '001':
        hosts = [(x, y) for actor, x, y, host in room.spawns if host]
        if hosts:
            hx, hy = min(hosts, key=lambda h: (h[0] - ENTRANCE[0]) ** 2 + (h[1] - ENTRANCE[1]) ** 2)
            d = ((hx - ENTRANCE[0]) ** 2 + (hy - ENTRANCE[1]) ** 2) ** 0.5
            if d > FIRST_ROOM_POSSESS_M:
                errors.append(f'{tag}: 첫 방의 몸이 입구에서 {d:.1f} m — 빙의 사거리 {FIRST_ROOM_POSSESS_M} m 안에 둔다')
            for actor, x, y, host in room.spawns:
                if not host and y < hy + FIRST_ROOM_ENEMY_GAP:
                    errors.append(f'{tag}: 첫 방의 {actor}({x},{y}) 가 몸({hx},{hy})보다 '
                                  f'{FIRST_ROOM_ENEMY_GAP} m 이상 뒤에 있어야 한다')
    ok, cells = reachable(room)
    if not ok:
        errors.append(f'{tag}: 입구에서 문까지 못 간다')
    elif not reachable(room, WIDE_PAD)[0]:
        warns.append(f'{tag}: 문까지 가는 길이 전부 한 칸 틈을 지난다 — 두 칸 폭 길이 하나는 있어야 한다')


def write_tsv(rooms):
    with open(TSV, 'w', encoding='utf-8', newline='\n') as f:
        f.write('# rooms90-hand-1.0 — rooms90_build.py 가 굽는다. 손으로 고치지 않는다.\n')
        for r in rooms:
            f.write(f'ROOM\t{r.id}\t{r.name}\t{r.note}\n')
            for kind, cx, cy, w, h in r.objects:
                hz = HAZARD.get(kind, ('NONE', 0, 0))
                move = 1 if (kind in SOLID or kind in CHANNEL) else 0
                shot = 1 if kind in SOLID else 0
                eshot = 1 if kind in COVER else 0
                f.write(f'OBJ\t{kind}\t{cx:g}\t{cy:g}\t{w:g}\t{h:g}\t{move}\t{shot}\t{eshot}\t{hz[0]}\t{hz[1]}\t{hz[2]:g}\n')
            for actor, x, y, host in r.spawns:
                f.write(f'SPAWN\t{actor}\t{x:g}\t{y:g}\t{1 if host else 0}\n')


COL = {'PILLAR': (205, 190, 120), 'CRATE': (170, 120, 70), 'BULK': (150, 140, 120), 'RAIL': (120, 130, 160),
       'LOW_COVER': (100, 140, 170), 'BARRICADE': (140, 110, 90), 'TIMED_SPIKE': (220, 200, 60),
       'ROTATING_BLADE': (240, 240, 250), 'CHANNEL_H': (60, 90, 160), 'CHANNEL_V': (60, 90, 160),
       'SWING_HAMMER': (200, 200, 230), 'RICOCHET_WALL': (170, 170, 220), 'HAZARD': (235, 100, 40),
       'EXPLOSIVE_BARREL': (250, 140, 30), 'WALL_TURRET_S': (90, 200, 220), 'WALL_TURRET_W': (90, 200, 220),
       'WALL_TURRET_E': (90, 200, 220), 'PUSH_ROCK': (130, 120, 110),
       'LASER_H': (255, 80, 120), 'LASER_V': (255, 80, 120), 'SLIDE_BLADE_H': (240, 240, 250),
       'SLIDE_BLADE_V': (240, 240, 250), 'SWING_HAMMER_H': (200, 200, 230),
       'FLAME_JET_S': (200, 80, 50), 'FLAME_JET_E': (200, 80, 50), 'FLAME_JET_W': (200, 80, 50),
       'DROP_ZONE': (120, 70, 70), 'SLOW_POOL': (110, 170, 90), 'MINE': (230, 50, 50),
       'BLOCK': (190, 200, 205), 'PROP_TALL': (150, 160, 200), 'PROP_WIDE': (170, 150, 120),
       'SANDBAG': (190, 170, 120), 'BARREL_PILE': (90, 110, 150), 'FALLEN_PILLAR': (175, 175, 170),
       'JERSEY_ROW': (185, 190, 195), 'SCRAP_PILE': (110, 100, 95), 'WRECK_CAR': (95, 100, 120),
       'PIT': (30, 30, 35)}


def sheet(rooms, out=SHEET):
    S = 15
    pw, ph = W * S + 10, H * S + 30
    cols = 15
    img = Image.new('RGB', (cols * pw + 10, max(r.chapter for r in rooms) * ph + 10), (26, 28, 34))
    dr = ImageDraw.Draw(img)
    try:
        font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 11)
        small = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 9)
    except Exception:
        font = small = ImageFont.load_default()
    for i, r in enumerate(rooms):
        col = (int(r.no) - 1) % 15
        cx0 = col * pw + 6
        cy0 = (r.chapter - 1) * ph + 6
        dr.text((cx0, cy0), f'{r.id[5:]} {r.name}'[:22], fill=(235, 235, 235), font=font)
        top = cy0 + 16
        dr.rectangle([cx0, top, cx0 + W * S, top + H * S], fill=(50, 54, 64), outline=(95, 100, 115))
        dr.rectangle([cx0, top, cx0 + W * S, top + 2.5 * S], fill=(42, 46, 56))
        for k in range(1, W):
            dr.line([cx0 + k * S, top, cx0 + k * S, top + H * S], fill=(58, 62, 72))
        for k in range(1, H):
            dr.line([cx0, top + k * S, cx0 + W * S, top + k * S], fill=(58, 62, 72))

        def px(x, y):
            return cx0 + x * S, top + (H - y) * S

        for kind, x, y, w, h in r.objects:
            if kind in ('ROTATING_BLADE',):
                X, Y = px(x, y)
                rr = BLADE_RADIUS * S
                dr.ellipse([X - rr, Y - rr, X + rr, Y + rr], outline=(200, 200, 230))
            if kind in MOVERS and kind != 'ROTATING_BLADE':
                sw = sweep_rect((kind, x, y, w, h))
                x0, y0 = px(sw[0], sw[3])
                x1, y1 = px(sw[2], sw[1])
                dr.rectangle([x0, y0, x1, y1], outline=(200, 200, 230))
            if kind.startswith('FLAME_JET'):
                dx, dy = (-1, 0) if kind.endswith('_W') else (1, 0) if kind.endswith('_E') else (0, -1)
                fx, fy = x + dx * 2.0, y + dy * 2.0
                hw, hh = (1.5, 0.55) if dx else (0.55, 1.5)
                x0, y0 = px(fx - hw, fy + hh)
                x1, y1 = px(fx + hw, fy - hh)
                dr.rectangle([x0, y0, x1, y1], outline=(255, 140, 60))
            x0, y0 = px(x - w / 2, y + h / 2)
            x1, y1 = px(x + w / 2, y - h / 2)
            dr.rectangle([x0 + 1, y0 + 1, x1 - 1, y1 - 1], fill=COL.get(kind, (200, 60, 200)), outline=(15, 15, 15))
            g = [k for k, v in GLYPH.items() if v[0] == kind and v[1] == w and v[2] == h]
            dr.text((x0 + 3, y0 + 1), g[0] if g else '?', fill=(20, 20, 20), font=small)
        for actor, x, y, host in r.spawns:
            X, Y = px(x, y)
            rad = 6
            colr = (90, 220, 120) if host else ((255, 150, 60) if actor in RANGED_TRASH else (230, 70, 70))
            dr.ellipse([X - rad, Y - rad, X + rad, Y + rad], fill=colr, outline=(0, 0, 0))
            dr.text((X - 3, Y - 6), actor[0], fill=(0, 0, 0), font=small)
        X, Y = px(*ENTRANCE)
        dr.rectangle([X - 5, Y - 5, X + 5, Y + 5], fill=(80, 160, 255))
    img.save(out)
    return out


def mix_report(rooms):
    """챕터마다 잡몹의 날 · 힘 · 술 수와, 그 방에 많은 쪽을 이기는 몸(답이 되는 몸)이 선 방 수."""
    by = {}
    for r in rooms:
        d = by.setdefault(r.chapter, {'blade': 0, 'force': 0, 'magic': 0, 'answer': 0, 'rooms': 0, 'hosts': set()})
        d['rooms'] += 1
        cnt = {'blade': 0, 'force': 0, 'magic': 0}
        for actor, x, y, host in r.spawns:
            if host:
                d['hosts'].add(actor)
            else:
                k = KIND.get(actor)
                if k:
                    cnt[k] += 1
                    d[k] += 1
        major = max(cnt, key=cnt.get)
        if any(host and BEATS.get(KIND.get(actor)) == major for actor, x, y, host in r.spawns):
            d['answer'] += 1
    for ch in sorted(by):
        d = by[ch]
        lean = CHAPTERS[ch]['lean'] if ch in CHAPTERS else '?'
        print(f"구성 CH{ch}: 날 {d['blade']} · 힘 {d['force']} · 술 {d['magic']} (목표 {lean})"
              f" · 답이 되는 몸이 선 방 {d['answer']}/{d['rooms']} · 몸 {len(d['hosts'])}종")


def main():
    strict = '--strict' in sys.argv
    only = None                        # --only 7  → 그 챕터 파일만 검사한다(tsv 는 안 쓴다)
    if '--only' in sys.argv:
        only = int(sys.argv[sys.argv.index('--only') + 1])
    rooms = []
    names = [n for n in os.listdir(SRC_DIR) if re.match(r'rooms90_ch\d+\.txt$', n)]
    names.sort(key=lambda n: int(re.search(r'ch(\d+)', n).group(1)))
    for name in names:
        ch = int(re.search(r'ch(\d+)', name).group(1))
        if only is not None and ch != only:
            continue
        if ch not in CHAPTERS:
            raise SystemExit(f'{name}: chapters.tsv 에 CH{ch} 줄이 없다')
        rooms += parse(os.path.join(SRC_DIR, name))
    errors, warns = [], []
    ids = set()
    for r in rooms:
        if r.id in ids:
            errors.append(f'{r.id} 가 두 번 있다')
        ids.add(r.id)
        build(r, errors, warns)
    for w in warns:
        print('경고', w)
    for e in errors:
        print('오류', e)
    print(f'방 {len(rooms)} · 물건 {sum(len(r.objects) for r in rooms)} · 적 {sum(len(r.spawns) for r in rooms)}'
          f' · 경고 {len(warns)} · 오류 {len(errors)}')
    mix_report(rooms)
    if errors or (strict and warns):
        sys.exit(1)
    if only is not None:
        out = os.path.join(SRC_DIR, f'rooms90_sheet_ch{only}.png')
        print('배치도(이 챕터만):', sheet(rooms, out))
        return
    # 챕터마다 전투방 11칸이 다 있어야 한다
    for ch in CHAPTERS:
        have = {r.no for r in rooms if r.chapter == ch}
        miss = [n for n in COMBAT_NO if n not in have]
        if miss:
            raise SystemExit(f'CH{ch}: 방이 빠졌다 — {miss}')
    write_tsv(rooms)
    print('저장:', TSV)
    print('배치도:', sheet(rooms))


if __name__ == '__main__':
    main()
