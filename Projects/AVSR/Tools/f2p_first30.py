# -*- coding: utf-8 -*-
"""무과금 유저 첫 30분 따라가기 — 실제 게임 값(chapters.tsv · GameConfig · ChestTable)으로 분 단위 기록 (2026-10-13).

PD 「무과금 유저라고 생각하고 30분 동안 처음부터 진행해 봐 · 챕터 클리어 골드가 너무 많다 · 레벨업이 너무 쉽다」.
손으로 판을 치는 대신, 판 결과(깨나 / 죽나)는 balance10 의 「깨는 데 걸리는 판 수(TRIES)」를 따르고
돈 · 경험치 · 강화 · 상자 시계는 게임 규칙 그대로 센다.

python f2p_first30.py [--minutes 30] [--test-chests]
"""
import argparse
import csv
import math
import os

HERE = os.path.dirname(os.path.abspath(__file__))
ROOMS = os.path.join(HERE, '..', 'Rooms', 'chapters.tsv')

# ── 판 시간(분) — 자동 조종 전체 판 녹화 2.2~3.3분(_exchange/ref/fullrun) × 사람 손 1.6 + 로비 0.5 ──
CLEAR_MIN = [3.6, 4.0, 4.0, 4.3, 5.3, 5.5, 5.8, 6.0, 6.3, 6.5]
LOBBY_MIN = 0.5
DEATH_PROGRESS = 0.45          # 죽는 판은 평균 이만큼 가서 죽는다(balance10 과 같다)
SHOP_SPEND = 0.20              # 판 골드 중 상점에서 쓰는 몫
TRIES = [1.5, 1.5, 3.5, 3.5, 6, 6, 9, 9, 13.5, 13.5]

# ── GameConfig 값 ──
EXP_BASE, EXP_STEP = 100, 60
CLEAR_EXP = [300, 500, 800, 1200, 1700, 2300, 3000, 3800, 4700, 5700]
PATH = {10: (5000, 50), 20: (10000, 100), 30: (20000, 200), 40: (30000, 300), 50: (50000, 500)}
GHOST_STEP, HOST_STEP = 30, 60
SHARE = {'ghost': 0.2, 'host': 1.75}
PCT = {'hp': 2.0, 'atk': 2.0, 'aspd': 1.0}
HOST_CAP_PER_MASTERY = 5
ENTRY_B = 60                   # B 몸 입장료 × priceMul
CHEST = {'silver': (10800, 150, 8), 'gold': (28800, 400, 18), 'platinum': (43200, 850, 34)}   # 초 · 골드 평균 · 조각 평균
START_GEM = 0


def load_chapters():
    rows = []
    with open(ROOMS, encoding='utf-8-sig') as f:
        lines = [l for l in f if not l.startswith('#') and l.strip()]
    for r in csv.DictReader(lines, delimiter='\t'):
        rows.append(r)
    return rows


def level_of(exp):
    lv = 1
    while lv < 50 and exp >= EXP_BASE + EXP_STEP * (lv - 1):
        exp -= EXP_BASE + EXP_STEP * (lv - 1)
        lv += 1
    return lv


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--minutes', type=float, default=30)
    ap.add_argument('--test-chests', action='store_true', help='테스트 모드 상자 시계(1/60)')
    ap.add_argument('--clear-mul', type=float, default=1.0, help='클리어 골드 배율(안 비교용)')
    ap.add_argument('--path-mul', type=float, default=1.0, help='성장 경로 골드 배율(안 비교용)')
    ap.add_argument('--quiet', action='store_true')
    a = ap.parse_args()
    ch = load_chapters()

    t = 0.0; gold = 0; exp = 0; gem = START_GEM; mastery = 1
    ghost = {k: 0 for k in PCT}; host = {k: 0 for k in PCT}
    paid = set(); chests = []          # (열리는 시각 분, 키)
    log, total_gold, spent_up, src = [], 0, 0, {'clear': 0, 'run': 0, 'chest': 0, 'path': 0}
    c, tries_left = 0, None
    runs = 0

    def power():
        hp = 1 + PCT['hp'] / 100 * (SHARE['ghost'] * ghost['hp'] + SHARE['host'] * host['hp'])
        atk = 1 + PCT['atk'] / 100 * (SHARE['ghost'] * ghost['atk'] + SHARE['host'] * host['atk'])
        sp = 1 + PCT['aspd'] / 100 * (SHARE['ghost'] * ghost['aspd'] + SHARE['host'] * host['aspd'])
        m = 1.07 ** max(0, mastery - 1)
        return math.sqrt(hp * atk * sp * m * m)

    def spend():
        nonlocal gold, spent_up
        lv = level_of(exp)
        while True:
            best = None
            for who, step, cap, d in (('ghost', GHOST_STEP, min(50, lv), ghost),
                                      ('host', HOST_STEP, HOST_CAP_PER_MASTERY * mastery, host)):
                for s in PCT:
                    if d[s] >= cap: continue
                    cost = step * (d[s] + 1)
                    if cost > gold: continue
                    before = power(); d[s] += 1; gain = power() - before; d[s] -= 1
                    if best is None or gain / cost > best[0]: best = (gain / cost, d, s, cost)
            if best is None: return
            _, d, s, cost = best
            d[s] += 1; gold -= cost; spent_up += cost

    while t < a.minutes and c < len(ch):
        row = ch[c]
        if tries_left is None:
            n = TRIES[c]; tries_left = int(n) + (1 if (n % 1 and c % 2 == 0) else 0)
        price = float(row['priceMul'])
        entry = int(round(ENTRY_B * price))
        rg = int(row['roomGold']) * 11 + int(row['midGold']) + int(row['bossGold'])
        gold = max(0, gold - entry)
        tries_left -= 1; runs += 1
        clear = tries_left <= 0
        if clear:
            dt = CLEAR_MIN[c] + LOBBY_MIN
            cg = int(int(row['clearGold']) * a.clear_mul)
            g = cg + int(rg * (1 - SHOP_SPEND))
            src['clear'] += cg; src['run'] += int(rg * (1 - SHOP_SPEND))
            exp += CLEAR_EXP[c]
            if len([x for x in chests if x[0] > t]) < 3:
                secs = CHEST[row['chest']][0] / (60 if a.test_chests else 1)
                chests.append((t + dt + secs / 60.0, row['chest']))
        else:
            dt = CLEAR_MIN[c] * DEATH_PROGRESS + LOBBY_MIN
            g = int(rg * DEATH_PROGRESS * (1 - SHOP_SPEND)); src['run'] += g
        t += dt; gold += g; total_gold += g
        # 열린 상자
        for x in list(chests):
            if x[0] <= t:
                _, gg, sh = CHEST[x[1]]; gold += gg; total_gold += gg; src['chest'] += gg; chests.remove(x)
        lv = level_of(exp)
        for L, (pg, pm) in PATH.items():
            if lv >= L and L not in paid:
                pg = int(pg * a.path_mul)
                paid.add(L); gold += pg; gem += pm; total_gold += pg; src['path'] += pg
        spend()
        log.append((t, runs, c + 1, 'CLEAR' if clear else 'dead ', g, gold, level_of(exp),
                    dict(ghost), dict(host), round(power(), 2)))
        if clear: c += 1; tries_left = None

    print(f'── 무과금 첫 {a.minutes:.0f}분 (상자 시계 {"테스트 1/60" if a.test_chests else "라이브"}) ──')
    print('  분   판  챕터  결과   얻은골드  남은골드  유령Lv  유령강화(체/공/속)  몸강화(체/공/속)  힘배율')
    for (tt, r, cc, res, g, gl, lv, gh, ho, pw) in ([] if a.quiet else log):
        print(f'{tt:5.1f} {r:>3}  {cc:>3}  {res}  {g:>8} {gl:>9}  {lv:>5}   {gh["hp"]:>2}/{gh["atk"]:>2}/{gh["aspd"]:>2}           '
              f'{ho["hp"]:>2}/{ho["atk"]:>2}/{ho["aspd"]:>2}        {pw}')
    print(f'번 골드 {total_gold:,} (클리어 {src["clear"]:,} · 판 {src["run"]:,} · 상자 {src["chest"]:,} · 성장경로 {src["path"]:,}) · 강화에 쓴 골드 {spent_up:,}')
    print(f'깬 챕터 {c} · 판 {runs} · 유령 Lv {level_of(exp)} · 젬 {gem}')


if __name__ == '__main__':
    main()
