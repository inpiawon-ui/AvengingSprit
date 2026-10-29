# -*- coding: utf-8 -*-
"""무과금 · 호스트 잠금 상태로 처음부터 — 몸 해금 · 성급 · 강화 · 챕터를 분 단위로 따라간다 (2026-10-13).

PD 「모든 호스트를 잠가 두고 무과금으로 어떻게 진행되는지 알고 싶어」 ·
「결과 창은 판에서 주운 골드만, 상자에 캐릭터 조각과 골드 — 클리어 골드는 없다」.

규칙(게임 값 그대로):
  시작 몸 갱스터(숙련도 1) · 다른 몸은 조각으로 해금(숙련도 0 → 1)
  조각 — 판에서 적 몸을 잡으면 1, 빼앗아 쓰다 잃으면 3(그 몸 것) · 상자(은 6~10 / 금 14~22 / 백금 28~40, 1~4명에게 나눠)
  상자 해제 3 · 8 · 12 시간(라이브), 칸 3개 — 칸이 차 있으면 못 받는다
  성급 = 조각(8 · 12 · 16 … × 등급 0.8/1.2/1.8) + 골드(0 · 400 · 700 … × 등급)
  강화 — 유령 탭(상한 = 유령 레벨) 30 × 다음 레벨 · 호스트 탭(상한 = 숙련도 × 5) 60 × 다음 레벨
  유령 레벨 = 챕터 클리어 경험치 · 성장 경로 Lv10 5,000 골드(지금 값)
판 결과(깨나 / 죽나)는 balance10 의 「깨는 데 걸리는 판 수」를 따른다 — 이 표는 힘이 아니라 시간을 본다.

python f2p_locked.py [--minutes 120] [--chest-hours 1.0]
"""
import argparse
import csv
import math
import os
import random

HERE = os.path.dirname(os.path.abspath(__file__))
ROOMS = os.path.join(HERE, '..', 'Rooms')

GRADE = {'gangster': 0, 'thug': 0, 'amazon': 0, 'hopper': 0, 'hopper_smg': 1, 'commando_mg': 0,
         'commando_laser': 1, 'commando_grenade': 0, 'salamander': 0, 'dragoon': 1, 'dragon_blue': 0,
         'guru': 0, 'white_wizard': 0, 'medium': 1, 'ninja': 1, 'ninja_chain': 1, 'robot': 1, 'baseball': 1,
         'snowwoman': 0, 'vampire': 1, 'commando_missile': 1}
GRADE_MUL = [0.8, 1.2, 1.8]
KIND = {k: 'blade' for k in ('gangster', 'thug', 'hopper', 'hopper_smg', 'commando_mg', 'ninja', 'amazon')}
KIND.update({k: 'force' for k in ('baseball', 'guru', 'ninja_chain', 'commando_grenade', 'commando_missile', 'commando_laser', 'robot')})
KIND.update({k: 'magic' for k in ('dragoon', 'snowwoman', 'dragon_blue', 'salamander', 'vampire', 'white_wizard', 'medium')})
SHARD_CURVE = [8, 12, 16, 20, 35, 40, 46, 52, 58, 90]
STAR_GOLD = [0, 400, 700, 1200, 1800, 2500, 3300, 4200, 5200, 8000]
CHEST = {'silver': (3, 150, 8, (1, 2)), 'gold': (8, 400, 18, (2, 3)), 'platinum': (12, 850, 34, (3, 4))}

CLEAR_MIN = [3.6, 4.0, 4.0, 4.3, 5.3, 5.5, 5.8, 6.0, 6.3, 6.5]
LOBBY_MIN = 0.5
DEATH_PROGRESS = 0.45
SHOP_SPEND = 0.20
TRIES = [1.5, 1.5, 3.5, 3.5, 6, 6, 9, 9, 13.5, 13.5]
POSSESS_LOST_SHARE = 0.3       # 만난 적 몸 중 빼앗아 쓰다 잃는 몫(3조각), 나머지는 잡아서 1조각
EXP_BASE, EXP_STEP = 100, 60
CLEAR_EXP = [300, 500, 800, 1200, 1700, 2300, 3000, 3800, 4700, 5700]
PATH_GOLD = {10: 5000, 20: 10000, 30: 20000, 40: 30000, 50: 50000}
ENTRY = [60, 120, 200]


def chapters():
    with open(os.path.join(ROOMS, 'chapters.tsv'), encoding='utf-8-sig') as f:
        lines = [l for l in f if not l.startswith('#') and l.strip()]
    return list(csv.DictReader(lines, delimiter='\t'))


def roster(c):
    """그 챕터 15방에 서는 적 몸(호스트) 목록 — 방 파일에서 센다."""
    import re
    txt = open(os.path.join(ROOMS, f'rooms90_ch{c}.txt'), encoding='utf-8').read()
    pat = r'\b(' + '|'.join(sorted(GRADE, key=len, reverse=True)) + r')\b'
    return re.findall(pat, txt)


def level_of(exp):
    lv = 1
    while lv < 50 and exp >= EXP_BASE + EXP_STEP * (lv - 1):
        exp -= EXP_BASE + EXP_STEP * (lv - 1); lv += 1
    return lv


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--minutes', type=float, default=120)
    ap.add_argument('--chest-hours', type=float, default=1.0, help='상자 시계 배율(1 = 라이브)')
    ap.add_argument('--seed', type=int, default=1)
    # ── PD 안(2026-10-13): 상자를 버리고 「그 챕터에서 만난 호스트의 조각 + 골드 + 경험치」를 클리어 보상으로 ──
    ap.add_argument('--met-reward', action='store_true', help='상자 없음 · 판 안 조각 없음 · 클리어 때 만난 몸마다 조각')
    ap.add_argument('--met-shards', type=int, default=1, help='클리어 때 만난 몸 한 기당 조각')
    ap.add_argument('--unlock-mul', type=float, default=1.0, help='해금 · 성급 조각 배율')
    ap.add_argument('--stamina', action='store_true', help='스태미너 최대 30 · 판당 5 · 6분에 1')
    a = ap.parse_args()
    rnd = random.Random(a.seed)
    ch = chapters()

    owned = {'gangster': 1}; shards = {k: 0 for k in GRADE}
    gold = 0; exp = 0; t = 0.0; paid = set()
    ghost = {'hp': 0, 'atk': 0, 'aspd': 0}; host = {k: {'hp': 0, 'atk': 0, 'aspd': 0} for k in GRADE}
    chests = []
    main_host = 'gangster'
    events = []
    c = 0; tries_left = None; runs = 0; earned = {'run': 0, 'chest': 0, 'path': 0}
    stamina = 30; waited = 0.0

    def note(s):
        events.append((t, s))

    def power(k):
        up = lambda s, per: 1 + per / 100 * (0.2 * ghost[s] + 1.75 * host[k][s])
        m = 1.07 ** max(0, owned.get(k, 0) - 1)
        return math.sqrt(up('hp', 2) * up('atk', 2) * up('aspd', 1) * m * m)

    def try_unlock_and_star():
        nonlocal gold
        for k in GRADE:
            m = owned.get(k, 0)
            if m >= 10: continue
            need = round(SHARD_CURVE[m] * GRADE_MUL[GRADE[k]] * a.unlock_mul)
            cost = round(STAR_GOLD[m] * GRADE_MUL[GRADE[k]] / 10) * 10
            if shards[k] >= need and gold >= cost:
                shards[k] -= need; gold -= cost; owned[k] = m + 1
                note(f'{"해금" if m == 0 else "성급"} {k} → 숙련도 {m + 1}' + (f' (골드 {cost})' if cost else ''))

    def spend():
        nonlocal gold
        lv = level_of(exp)
        k = main_host
        while True:
            best = None
            for who, step, cap, d in (('ghost', 30, min(50, lv), ghost), ('host', 60, 5 * owned.get(k, 0), host[k])):
                for s in d:
                    if d[s] >= cap: continue
                    cost = step * (d[s] + 1)
                    if cost > gold: continue
                    before = power(k); d[s] += 1; gain = power(k) - before; d[s] -= 1
                    if best is None or gain / cost > best[0]: best = (gain / cost, d, s, cost)
            if best is None: return
            _, d, s, cost = best; d[s] += 1; gold -= cost

    while t < a.minutes and c < len(ch):
        row = ch[c]
        if tries_left is None:
            n = TRIES[c]; tries_left = int(n) + (1 if (n % 1 and c % 2 == 0) else 0)
        price = float(row['priceMul'])
        if a.stamina:
            if stamina < 5:   # 모자라면 찰 때까지 기다린다(이 동안은 게임을 안 한다)
                wait = (5 - stamina) * 6.0; t += wait; waited += wait; stamina = 5
                note(f'스태미너가 모자라 {wait:.0f}분 기다림')
            stamina -= 5
        else:
            gold = max(0, gold - round(ENTRY[GRADE[main_host]] * price))
        rg = int(row['roomGold']) * 11 + int(row['midGold']) + int(row['bossGold'])
        tries_left -= 1; runs += 1
        clear = tries_left <= 0
        prog = 1.0 if clear else DEATH_PROGRESS
        dt = CLEAR_MIN[c] * prog + LOBBY_MIN
        # 판 골드 · 판 조각
        g = int(rg * prog * (1 - SHOP_SPEND)); gold += g; earned['run'] += g
        pool = roster(c + 1)
        meet = pool[:max(1, int(round(len(pool) * prog)))]
        if not a.met_reward:
            for k in meet:
                shards[k] += 3 if rnd.random() < POSSESS_LOST_SHARE else 1
        elif clear:
            for k in meet: shards[k] += a.met_shards
            hrs, cg, sh, _ = CHEST[row['chest']]; gold += cg; earned['chest'] += cg   # 상자 골드를 바로
        if a.stamina:
            stamina = min(30, stamina + dt / 6.0)
        t += dt
        if clear:
            exp += CLEAR_EXP[c]
        if clear and not a.met_reward:
            busy = [x for x in chests if x[0] > t]
            if len(busy) < 3:
                hrs, cg, sh, (lo, hi) = CHEST[row['chest']]
                start = max([t] + [x[0] for x in busy])        # 한 번에 하나씩 연다고 보면 줄을 선다
                chests.append((start + hrs * 60 * a.chest_hours, row['chest']))
            else:
                note('상자 칸이 차서 상자를 못 받음')
        for x in list(chests):
            if x[0] <= t:
                hrs, cg, sh, (lo, hi) = CHEST[x[1]]
                gold += cg; earned['chest'] += cg
                who = rnd.sample(sorted(GRADE), rnd.randint(lo, hi))
                for k in who: shards[k] += sh // len(who)
                chests.remove(x); note(f'{x[1]} 상자 열림 — 골드 {cg} · 조각 {sh}개를 {len(who)}명에게')
        if a.stamina and level_of(exp) > level_of(exp - (CLEAR_EXP[c] if clear else 0)): stamina = max(stamina, 30)   # 레벨업 = 가득
        lv = level_of(exp)
        for L, pg in PATH_GOLD.items():
            if lv >= L and L not in paid:
                paid.add(L); gold += pg; earned['path'] += pg; note(f'유령 Lv{L} 성장 경로 골드 {pg:,}')
        try_unlock_and_star(); spend()
        if clear:
            note(f'{c + 1}챕터 클리어 ({runs}판째) · 유령 Lv{lv} · 주력 힘 ×{power(main_host):.2f} · 가진 몸 {len(owned)}명')
            c += 1; tries_left = None

    print(f'── 무과금 · 호스트 잠금 · {a.minutes:.0f}분 (상자 시계 ×{a.chest_hours}) ──')
    for tt, s in events:
        print(f'{tt:6.1f}분  {s}')
    kinds = {}
    for k in owned: kinds.setdefault(KIND[k], []).append(f'{k}({owned[k]})')
    if a.stamina: print(f'스태미너 기다린 시간 {waited:.0f}분(시계 시간에 포함)')
    print(f'끝: {c}챕터 · 판 {runs} · 유령 Lv{level_of(exp)} · 남은 골드 {gold:,} · 번 골드 판 {earned["run"]:,} · 상자 {earned["chest"]:,} · 성장경로 {earned["path"]:,}')
    print('가진 몸(숙련도): ' + ' / '.join(f'{kk}: ' + ', '.join(v) for kk, v in sorted(kinds.items())))
    near = sorted(((round(SHARD_CURVE[0] * GRADE_MUL[GRADE[k]]) - shards[k], k) for k in GRADE if k not in owned))[:5]
    print('해금 가까운 몸(남은 조각): ' + ', '.join(f'{k} {n}' for n, k in near))


if __name__ == '__main__':
    main()
