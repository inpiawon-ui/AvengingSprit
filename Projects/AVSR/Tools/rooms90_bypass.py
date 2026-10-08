# -*- coding: utf-8 -*-
"""기믹 우회로 찾기 — 방의 위험 기믹을 하나도 안 거치고 문까지 가는 길이 있는 방을 찾는다.

PD 2026-10-08: 「옆에 샛길이 있으면 가운데 세팅한 게 의미가 없다 — 저런 걸 피해서 잘 지나가게 만드는 게 포인트」.
위험 자리(밟으면 아픈 것 · 오가는 것이 훑는 자리 · 불길 · 낙하물 · 끈끈이 · 지뢰)를 `CLEAR` m 부풀려
막힌 것으로 치고도 입구에서 문까지 걸어가면 「우회로 있음」이다.

python Projects/AVSR/Tools/rooms90_bypass.py [--only 챕터]
"""
import os, re, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import rooms90_build as rb

CLEAR = 0.5     # 위험에서 이만큼(m) 떨어져서도 지나가면 「안 거쳤다」로 본다
FLAME_REACH = 3.0
DANGER = set(rb.HAZARD) | {'FLAME_JET_S', 'FLAME_JET_E', 'FLAME_JET_W', 'DROP_ZONE', 'SLOW_POOL', 'MINE',
                           'PISTON_E', 'PISTON_W', 'LASER_PILLAR', 'COLLAPSE_FLOOR'}


def danger_rect(o):
    kind, cx, cy, w, h = o
    if kind == 'FLAME_JET_S':
        return cx - w / 2, cy - h / 2 - FLAME_REACH, cx + w / 2, cy + h / 2
    if kind == 'FLAME_JET_E':
        return cx - w / 2, cy - h / 2, cx + w / 2 + FLAME_REACH, cy + h / 2
    if kind == 'FLAME_JET_W':
        return cx - w / 2 - FLAME_REACH, cy - h / 2, cx + w / 2, cy + h / 2
    if kind == 'ROTATING_BLADE':                    # 팔 끝이 그리는 원 — 빌드의 sweep_rect 는 1 m 넉넉히 잡아 모서리 샛길을 못 본다
        r = rb.BLADE_RADIUS
        return cx - r, cy - r, cx + r, cy + r
    if kind == 'LASER_PILLAR':
        r = rb.LASER_PILLAR_REACH
        return cx - r, cy - r, cx + r, cy + r
    return rb.sweep_rect(o)


def bypass(room):
    """위험을 막힌 것으로 쳐도 문까지 가면 True."""
    extra = []
    for o in room.objects:
        if o[0] not in DANGER:
            continue
        x0, y0, x1, y1 = danger_rect(o)
        x0 -= CLEAR; y0 -= CLEAR; x1 += CLEAR; y1 += CLEAR
        # 막는 상자로 바꿔 넣는다 — reachable 이 SOLID 발자국 × 0.7 로 재므로 그만큼 키운다
        w, h = (x1 - x0) / 0.7, (y1 - y0) / 0.7
        extra.append(('BLOCK', (x0 + x1) / 2, (y0 + y1) / 2, w, h))
    if not extra:
        return None
    saved = room.objects
    room.objects = saved + extra
    ok, _ = rb.reachable(room)
    room.objects = saved
    return ok


def main():
    only = int(sys.argv[sys.argv.index('--only') + 1]) if '--only' in sys.argv else None
    names = sorted([n for n in os.listdir(rb.SRC_DIR) if re.match(r'rooms90_ch\d+\.txt$', n)],
                   key=lambda n: int(re.search(r'ch(\d+)', n).group(1)))
    hits = []
    for name in names:
        ch = int(re.search(r'ch(\d+)', name).group(1))
        if only is not None and ch != only:
            continue
        for r in rb.parse(os.path.join(rb.SRC_DIR, name)):
            errs, warns = [], []
            rb.build(r, errs, warns)
            res = bypass(r)
            if res:
                kinds = sorted({o[0] for o in r.objects if o[0] in DANGER})
                hits.append(r.id)
                print(f'우회로 {r.id} {r.name} — {", ".join(kinds)}')
    print(f'우회로 있는 방 {len(hits)}')


if __name__ == '__main__':
    main()
