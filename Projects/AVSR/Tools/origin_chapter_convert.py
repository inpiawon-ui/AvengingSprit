# -*- coding: utf-8 -*-
"""원작(궁수의 전설) 30방 장 하나를 우리 챕터 30방으로 옮긴다 — 지형 · 물건 칸 · 몬스터 자리 · 마리 수 그대로(3단계, 2026-10-22).

PD 10-22 「원작 배치를 따라서 정확하게 유저가 느낄 수 있게」.
  · 우리 방 번호 n 의 전투방은 원작 같은 번호 방의 지형 묶음(Level_M_<b>_)에서 뽑는다 — 한 판 안에서 겹치지 않게.
  · 원작 장의 묶음 후보가 모자라면(보고서는 방마다 앞 3개만 싣는다) 다른 장의 같은 묶음에서 채운다(원작도 장끼리 지형을 돌려 쓴다).
  · 몬스터 칸은 그대로. 원작 맵의 몬스터 종류(근접 · 원거리 · 비행 · 포탑)를 원작 경향대로 칸에 나눠 앉힌다
    (원거리 = 양옆 끝 · 귀퉁이 · 포탑 = 물건 옆 · 비행 = 물 · 벽 사이 · 근접 = 나머지).
  · 그 역할 자리에 우리 챕터 몬스터(chapters.tsv trash · trash_roster.tsv 역할)를 채운다.

쓰는 법: python origin_chapter_convert.py <우리 챕터> <원작 장 id>   예) 1 112
결과  : Projects/AVSR/Rooms/origin_ch<N>.json · _exchange/plan_content_expand/step3_research/origin_ch<N>_preview.png
"""
import json
import os
import random
import re
import sys

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
ROOMS = os.path.join(ROOT, 'Projects', 'AVSR', 'Rooms')
ORIGIN = os.path.normpath(os.path.join(ROOT, '..', 'WA-Game-LegendOfArcher', 'Docs', 'design', 'origin-report'))
RESEARCH = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'plan_content_expand', 'step3_research')
FONT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Fonts', 'NotoSansKR-Bold.ttf')

# 우리 30방 골격(3단계 문서 「30방 골격안」) — 전투방이 아닌 방은 원작 지형을 안 쓴다
OURS = {1: '소개', 5: '천사', 10: '중간보스', 11: '소개', 15: '상점', 20: '중간보스', 25: '천사', 30: '보스'}
MOVE_BLOCK, SHOT_PASS = set('#~='), set('~=')


def load_monster_roles():
    """원작 몬스터 id → 역할. 보고서 몬스터 카드의 「공중」 꼬리표 · 이동 0 · 무기 칸으로 가른다."""
    t = open(os.path.join(ORIGIN, 'monsters.html'), encoding='utf-8').read()
    roles = {}
    for m in re.finditer(r"<article class=monster id=m(\d+)[^>]*>(.*?)</article>", t, re.S):
        mid, body = m.group(1), m.group(2)
        move = re.search(r'<dt>이동</dt><dd>([^<]+)', body)
        weapon = re.search(r'<dt>무기</dt><dd>([^<]+)', body)
        speed = float(re.sub(r'[^\d.]', '', move.group(1)) or 0) if move else 1.0
        if '공중' in body.split('</h3>')[0]:
            roles[mid] = 'flying'
        elif speed == 0:
            roles[mid] = 'turret'
        elif weapon and weapon.group(1).strip() not in ('—', '-', ''):
            roles[mid] = 'ranged'
        else:
            roles[mid] = 'melee'
    return roles


def load_ours(ch):
    rows = [ln.rstrip('\n').split('\t') for ln in open(os.path.join(ROOMS, 'chapters.tsv'), encoding='utf-8') if not ln.startswith('#')]
    head = rows[0]
    row = next(r for r in rows[1:] if r[0] == str(ch))
    c = dict(zip(head, row))
    roster, body = {}, {}
    for ln in open(os.path.join(ROOMS, 'trash_roster.tsv'), encoding='utf-8'):
        t = ln.rstrip('\n').split('\t')
        if t[0] != 'key':
            roster[t[0]], body[t[0]] = t[2], t[1]
    trash = c['trash'].split(',')
    # 소개 방(PD 10-22 — 원작 지형 · 마리 수에 새 몬스터만). 「새」는 앞 챕터들에 없던 **몸** — Lv 변종은 새 몬스터가 아니다
    seen = set()
    for r in rows[1:]:
        if r[0].isdigit() and int(r[0]) < ch:
            seen |= {body.get(k, k) for k in dict(zip(head, r))['trash'].split(',')}
    new = [k for k in trash if body.get(k, k) not in seen] or [k for k in trash if k not in seen] or trash

    def group(keys):
        out = {}
        for k in keys:
            r = roster.get(k, 'melee')
            r = 'ranged' if r in ('ranged', 'support') else r
            out.setdefault(r, []).append(k)
        return out
    return c, group(trash), new


def seat_roles(grid, w, counts):
    """몬스터 칸에 역할을 나눠 앉힌다 — 원작 경향(원거리 양옆 끝 · 귀퉁이, 포탑 물건 옆, 비행 물 · 벽 사이)."""
    h = len(grid) // w
    cells = [(i % w, i // w) for i, ch in enumerate(grid) if ch == 'm']

    def near(x, y, pred):
        return sum(1 for dx in (-1, 0, 1) for dy in (-1, 0, 1)
                   if 0 <= x + dx < w and 0 <= y + dy < h and pred(grid[(y + dy) * w + x + dx]))

    # 물에 둘러싸인 칸 = 물 속 섬(원작 「물로 갈라 건너편에서 쏘게」). 근접은 갇히므로 원거리 · 포탑이 먼저, 근접은 피한다
    isle = lambda x, y: near(x, y, lambda c: c == '~') >= 3
    score = {
        'ranged': lambda x, y: (2 if x <= 1 or x >= w - 2 else 0) + (1 if y <= 2 else 0) + (6 if isle(x, y) else 0),
        'turret': lambda x, y: near(x, y, lambda c: c in '#=') * 2 + (1 if x <= 1 or x >= w - 2 else 0) + (8 if isle(x, y) else 0),
        'flying': lambda x, y: near(x, y, lambda c: c in '~#') + (1 if y <= h // 2 else 0) + (3 if isle(x, y) else 0),
        'melee': lambda x, y: (1 if y <= h // 2 else 0) + (1 if 2 <= x <= w - 3 else 0) - (8 if isle(x, y) else 0),
    }
    seat, free = {}, list(cells)
    for role in ('turret', 'ranged', 'flying', 'melee'):
        n = counts.get(role, 0)
        for _ in range(n):
            if not free:
                break
            best = max(free, key=lambda p: (score[role](*p), -abs(p[0] - (w - 1) / 2)))
            seat[best] = role
            free.remove(best)
    for p in free:
        seat[p] = 'melee'
    return seat


def main(ch, src):
    data = json.load(open(os.path.join(ROOMS, 'origin_rooms.json'), encoding='utf-8'))
    maps, chapters = data['maps'], {c['id']: c for c in data['chapters']}
    roles = load_monster_roles()
    ours, by_role, new = load_ours(ch)
    rnd = random.Random(f'{ch}-{src}')
    used, rooms = set(), []
    source = chapters[str(src)]
    for r in source['rooms']:
        n = r['room']
        kind = OURS.get(n, '전투')
        out = {'room': n, 'ours': kind, 'origin_role': r['role'], 'bands': [b.split('/')[-1] for b in r['bands']]}
        if kind in ('전투', '소개'):
            band = r['bands'][0].split('/')[-1] if r['bands'] else ''
            pool = [k for k in r['pool'] if maps[k][1] not in used]
            if len(pool) < 3:   # 같은 묶음 다른 장에서 채운다 — 원작도 지형을 장끼리 돌려 쓴다
                pool += sorted(k for k, v in maps.items() if k.split('/')[-1].startswith(band) and v[1] not in used
                               and 'Hell' not in k)[:40]
            key = rnd.choice(pool)
            w, cells, mons, _ = maps[key]
            used.add(cells)
            counts = {}
            for mid, cnt in mons.items():
                counts[roles.get(mid, 'melee')] = counts.get(roles.get(mid, 'melee'), 0) + cnt
            seat = seat_roles(cells, w, counts)
            fill = {}
            for p, role in seat.items():
                pool_role = by_role
                if kind == '소개':   # 새 몬스터만 — 그 역할의 새 몬스터가 없으면 새 몬스터 아무나
                    pool_role = {rk: [k for k in v if k in new] for rk, v in by_role.items()}
                    pool_role = {rk: v for rk, v in pool_role.items() if v} or {'melee': new}
                pick = (pool_role.get(role) or pool_role.get('ranged' if role == 'turret' else 'melee')
                        or sum(pool_role.values(), []))
                fill[f'{p[0]},{p[1]}'] = {'role': role, 'monster': rnd.choice(pick)}
            out.update({'source': key, 'w': w, 'h': len(cells) // w, 'cells': cells, 'spawns': fill,
                        'origin_monsters': counts})
        rooms.append(out)
    path = os.path.join(ROOMS, f'origin_ch{ch}.json')
    json.dump({'chapter': ch, 'origin_chapter': src, 'origin_name': source['name'], 'rooms': rooms},
              open(path, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print(path)
    preview(ch, src, source['name'], rooms, ours)


COL = {'.': (66, 70, 80), '#': (128, 120, 112), '~': (60, 120, 210), '=': (205, 165, 60), '^': (225, 60, 60),
       '"': (80, 140, 70), 't': (175, 175, 190), 'n': (240, 240, 120), 'o': (130, 110, 150), 'b': (255, 255, 255)}
# 몬스터는 동그라미로 그린다(물건 네모와 헷갈리지 않게)
ROLE_COL = {'melee': (250, 250, 250), 'ranged': (255, 160, 40), 'flying': (110, 230, 250), 'turret': (210, 110, 250)}


def preview(ch, src, name, rooms, ours):
    font, small = ImageFont.truetype(FONT, 17), ImageFont.truetype(FONT, 13)
    cell, cols = 11, 10
    tw, th = 11 * cell + 14, 21 * cell + 62
    W, H = cols * tw + 20, 70 + 3 * th + 60
    img = Image.new('RGB', (W, H), (36, 38, 46))
    d = ImageDraw.Draw(img)
    d.text((12, 10), f'우리 {ch}챕터({ours.get("name", "")}) ← 원작 {src}장 — 방 30개, 원작 지형 · 물건 칸 · 몬스터 자리 그대로', font=font, fill=(235, 235, 235))
    for i, r in enumerate(rooms):
        x0, y0 = 10 + (i % cols) * tw, 46 + (i // cols) * th
        d.text((x0, y0), f'{r["room"]} {r["ours"]}', font=small, fill=(235, 235, 235))
        if 'cells' not in r:
            d.rectangle([x0, y0 + 20, x0 + 11 * cell, y0 + 20 + 15 * cell], outline=(90, 96, 110))
            d.text((x0 + 30, y0 + 90), r['ours'], font=font, fill=(160, 166, 180))
            continue
        w, h, cells = r['w'], r['h'], r['cells']
        for k, c in enumerate(cells):
            x, y = k % w, k // w
            bx, by = x0 + x * cell, y0 + 20 + y * cell
            d.rectangle([bx, by, bx + cell - 1, by + cell - 1], fill=COL.get(c, (66, 70, 80)) if c != 'm' else COL['.'])
            sp = r['spawns'].get(f'{x},{y}')
            if sp:
                d.ellipse([bx + 1, by + 1, bx + cell - 2, by + cell - 2], fill=ROLE_COL[sp['role']], outline=(20, 20, 24))
        n = sum(r['origin_monsters'].values())
        band = r['bands'][0].replace('Level_', '').replace('_', '').rstrip() if r['bands'] else ''
        d.text((x0, y0 + 22 + h * cell), f'{band} · {h}줄 · 적 {n}', font=small, fill=(200, 200, 210))
    y = H - 46
    x = 12
    for k, txt in [('#', '돌 · 나무(막음)'), ('~', '물(탄 통과)'), ('=', '울타리'), ('^', '가시'), ('t', '함정 레일'), ('"', '풀')]:
        d.rectangle([x, y, x + 14, y + 14], fill=COL[k]); d.text((x + 20, y - 3), txt, font=small, fill=(220, 220, 220))
        x += 34 + int(d.textlength(txt, font=small))
    for k, txt in [('melee', '근접'), ('ranged', '원거리'), ('flying', '비행'), ('turret', '포탑')]:
        d.ellipse([x, y, x + 14, y + 14], fill=ROLE_COL[k]); d.text((x + 20, y - 3), txt, font=small, fill=(220, 220, 220))
        x += 34 + int(d.textlength(txt, font=small))
    os.makedirs(RESEARCH, exist_ok=True)
    out = os.path.join(RESEARCH, f'origin_ch{ch}_preview.png')
    img.save(out)
    print(out, img.size)


if __name__ == '__main__':
    main(int(sys.argv[1]), int(sys.argv[2]))
