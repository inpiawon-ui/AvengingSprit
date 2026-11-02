# -*- coding: utf-8 -*-
"""AVSR 경제 모델 — 지금 값(실측)과 설계안을 같은 자로 잰다.

  python Projects/AVSR/Tools/economy_model.py            # 지금 값 표
  python Projects/AVSR/Tools/economy_model.py --plan     # 설계안 표

값은 GameConfig.asset · ChestTable · GachaTable · StoreTable · SeasonTable · chapters.tsv 에서 2026-11-02 에 읽은 것이다.
판 안에서만 정해지는 값(파티에 섰던 호스트 수 · 판 안 조각)은 플레이 기록이 없어 가정이다 — 아래 ASSUME 에 모았다.
"""
import sys

# ── 가정(플레이 기록이 생기면 바꾼다) ──────────────────────────────
ASSUME = dict(
    runs_per_day=6,          # 무과금 「꾸준히 하는 유저」 — 하루 1시간(한 판 약 10분)
    party_hosts_per_run=4,   # 한 판에 파티로 섰던 호스트 수(빙의로 바뀌며 늘어난다)
    run_drop_shards=4,       # 판 안 조각(잡은 몸 1 · 잃은 몸 3)
    met_hosts_per_run=6,     # 클리어 조각을 나눠 받는 「만난 호스트」 수
)

# ── 지금 값(실측) ───────────────────────────────────────────────
RUN_GOLD = [244, 298, 386, 508, 664, 806, 1006, 1192, 1436, 1690,
            1851, 1993, 2189, 2369, 2580, 2799, 3052, 3337, 3633, 3941]
PRICE_MUL = [1, 1.3, 1.7, 2.2, 2.8, 3.5, 4.3, 5.2, 6.2, 7.3,
             7.9, 8.7, 9.4, 10.3, 11.2, 12.2, 13.2, 14.4, 15.7, 17.1]
CLEAR_EXP = [300, 500, 800, 1200, 1700, 2300, 3000, 3800, 4700, 5700,
             6306, 6977, 7720, 8541, 9449, 10455, 11567, 12797, 14159, 15665]
CHEST = ['silver'] * 3 + ['gold'] * 3 + ['platinum'] * 14
CHEST_GOLD = dict(silver=150, gold=400, platinum=850)       # 최소~최대 평균
CHEST_SHARDS = dict(silver=8, gold=18, platinum=34)

SHARD_CURVE = [8, 12, 16, 20, 35, 40, 46, 52, 58, 90]        # 숙련도 0→10 (0→1 = 해금)
STAR_GOLD = [0, 400, 700, 1200, 1800, 2500, 3300, 4200, 5200, 8000]
GRADE_MUL = [1, 1.5, 2]                                       # R · SR · SSR
HOSTS = [11, 6, 5]                                            # 얻을 수 있는 R · SR · SSR(사신 제외)
SKILL_GOLD = [400, 800, 800]                                  # 액티브 · 패시브 · 필살기 한 단계 × (lv+1)
SKILL_MAX = [10, 5, 5]
GHOST_STEP, GHOST_LINES, GHOST_MAX = 30, 8, 50
USER_EXP_BASE, USER_EXP_STEP, GHOST_LV_MAX = 100, 60, 50

FREE_GEM_DAY = 20 + 100 / 7 + 200 / 28                        # 매일 미션 · 매주 미션 · 패스 무료 줄
GACHA_SHARDS_PER_PULL = 0.70 * 2 + 0.25 * 5 + 0.05 * 20
GACHA_GEM = 60


def r(x):
    return int(round(x))


def skill_gold(grade):
    tot = 0
    for slot in range(3):
        for lv in range(1, SKILL_MAX[slot]):
            tot += round(SKILL_GOLD[slot] * (lv + 1) * GRADE_MUL[grade] / 10) * 10
    return tot


def star_cost(grade):
    shards = sum(r(s * GRADE_MUL[grade]) for s in SHARD_CURVE)
    gold = sum(round(g * GRADE_MUL[grade] / 10) * 10 for g in STAR_GOLD)
    return shards, gold


def exp_to(level):
    return sum(USER_EXP_BASE + USER_EXP_STEP * (l - 1) for l in range(1, level))


def now():
    a = ASSUME
    print('## 지금 값 — 한 판(클리어) · 하루(%d판) 획득\n' % a['runs_per_day'])
    print('| 챕터 | 판 골드 | 상자 골드 | 클리어 골드 합 | 조각/판 | 유령 경험치/판 | 하루 골드 | 하루 조각 | 하루 경험치 |')
    print('|---|---|---|---|---|---|---|---|---|')
    for c in (1, 3, 5, 10, 15, 20):
        i = c - 1
        g = RUN_GOLD[i] + CHEST_GOLD[CHEST[i]]
        s = CHEST_SHARDS[CHEST[i]] + 2 * a['party_hosts_per_run'] + a['run_drop_shards']
        day_g = g * a['runs_per_day'] + 100 * PRICE_MUL[i]
        day_s = s * a['runs_per_day'] + GACHA_SHARDS_PER_PULL * (1 + FREE_GEM_DAY / GACHA_GEM)
        print(f'| {c} | {RUN_GOLD[i]:,} | {CHEST_GOLD[CHEST[i]]:,} | {g:,} | {s} | {CLEAR_EXP[i]:,} | '
              f'{r(day_g):,} | {r(day_s):,} | {CLEAR_EXP[i] * a["runs_per_day"]:,} |')

    print('\n## 지금 값 — 다 키우는 데 드는 양\n')
    ghost = GHOST_LINES * sum(GHOST_STEP * (lv + 1) for lv in range(GHOST_MAX))
    rows = []
    tot_sg = tot_sh = tot_st = 0
    for gi, name in enumerate(('R', 'SR', 'SSR')):
        sh, st = star_cost(gi)
        sg = skill_gold(gi)
        rows.append((name, HOSTS[gi], sh, st, sg))
        tot_sh += sh * HOSTS[gi]; tot_st += st * HOSTS[gi]; tot_sg += sg * HOSTS[gi]
    print('| 등급 | 수 | 조각(해금~5성) | 성급 골드 | 스킬 골드(3칸 끝까지) |')
    print('|---|---|---|---|---|')
    for n, k, sh, st, sg in rows:
        print(f'| {n} | {k} | {sh:,} | {st:,} | {sg:,} |')
    print(f'| 전부 | 22 | {tot_sh:,} | {tot_st:,} | {tot_sg:,} |')
    total_gold = ghost + tot_st + tot_sg
    print(f'\n- 유령 강화 8줄 × 50단계: {ghost:,} 골드')
    print(f'- 골드 합계: {total_gold:,} · 조각 합계: {tot_sh:,}')
    print(f'- 유령 Lv50 까지 경험치: {exp_to(GHOST_LV_MAX):,} · 첫 클리어 1~20챕터 합: {sum(CLEAR_EXP):,}')

    print('\n## 지금 값 — 날짜로 바꾸면(무과금, 그 챕터에서 매일 %d판 클리어)\n' % a['runs_per_day'])
    print('| 챕터 | 골드 다 쓰기까지 | 조각 다 모으기까지 | 유령 Lv50 까지 | 젬 10연차 주기 |')
    print('|---|---|---|---|---|')
    for c in (5, 10, 20):
        i = c - 1
        day_g = (RUN_GOLD[i] + CHEST_GOLD[CHEST[i]]) * a['runs_per_day']
        day_s = (CHEST_SHARDS[CHEST[i]] + 2 * a['party_hosts_per_run'] + a['run_drop_shards']) * a['runs_per_day']
        print(f'| {c} | {total_gold / day_g:.0f}일 | {tot_sh / day_s:.0f}일 | '
              f'{exp_to(GHOST_LV_MAX) / (CLEAR_EXP[i] * a["runs_per_day"]):.1f}일 | {540 / FREE_GEM_DAY:.0f}일 |')
    print(f'\n- 무상 젬 하루 {FREE_GEM_DAY:.1f} · 뽑기 1회 평균 조각 {GACHA_SHARDS_PER_PULL:.2f}개 = '
          f'젬 {GACHA_GEM / GACHA_SHARDS_PER_PULL:.1f}개에 조각 1개')
    s10 = CHEST_SHARDS['platinum'] + 2 * a['party_hosts_per_run'] + a['run_drop_shards']
    print(f'- 10챕터 한 판 조각 {s10}개 = 뽑기 {s10 / GACHA_SHARDS_PER_PULL:.1f}회분(젬 {s10 / GACHA_SHARDS_PER_PULL * GACHA_GEM:,.0f})')


# ── 설계안(PD 2026-11-02 결정 반영) ─────────────────────────────
PLAN = dict(
    mul_step=0.8,            # 챕터 값 배율 M(c) = 1 + 0.8(c−1)
    run_gold=244,            # 1챕터 판 골드(방에 굽힌 합) — × M
    clear_gold=120,          # 클리어 골드(옛 상자 골드 몫) — × M
    clear_exp=300,           # 클리어 유령 경험치 — × M
    clear_shards=6,          # 클리어 조각(만난 호스트에 나눔) — 챕터와 무관
    exp_base=100, exp_quad=2,       # 다음 레벨 경험치 = 100 + 2 × (Lv−1)² — 10챕터 입장 ≈ Lv50
    ghost_step=30,           # 유령 강화 한 단계 = 30 × (lv+1) — 지금 값 그대로
    ghost_knee=100, ghost_accel=120,  # 100레벨 뒤 120레벨마다 배율 +1
    host_per_chapters=5,     # 라이브 — 5챕터마다 새 호스트 1명(등급 R · SR · SSR 돌아가며)
)


def runs_to_clear(c):
    """그 챕터를 깨기까지 무과금 판 수(전투 쪽 목표값). 1~10은 10-02 밸런스 표, 그 뒤는 15 + 0.5(c−10)."""
    early = [1, 2, 3, 4, 5, 6, 8, 9, 11, 13]
    return early[c - 1] if c <= len(early) else 15 + 0.5 * (c - 10)


def plan_ghost_cost(lv):
    p = PLAN
    return round(p['ghost_step'] * (lv + 1) * (1 + max(0, lv + 1 - p['ghost_knee']) / p['ghost_accel']) / 10) * 10


def plan_mul(c):
    return 1 + PLAN['mul_step'] * (c - 1)


def plan_exp_to_next(level):
    return PLAN['exp_base'] + PLAN['exp_quad'] * (level - 1) ** 2


def plan_level(exp):
    lv = 1
    while exp >= plan_exp_to_next(lv):
        exp -= plan_exp_to_next(lv)
        lv += 1
    return lv


def plan():
    p = PLAN
    print('## 설계안 — 챕터별 곡선(공식)\n')
    print('| 챕터 | 값 배율 M | 판 골드 | 클리어 골드 | 한 판 골드 합 | 클리어 경험치 | 깬 뒤 유령 Lv | 그 레벨 강화 1단계 | 깨는 데 판 수 |')
    print('|---|---|---|---|---|---|---|---|---|')
    exp = 0
    rows = {}
    for c in range(1, 101):
        exp += runs_to_clear(c) * p['clear_exp'] * plan_mul(c)
        rows[c] = plan_level(exp)
    for c in (1, 2, 3, 5, 9, 10, 15, 20, 30, 50, 100):
        m = plan_mul(c)
        run, cg = r(p['run_gold'] * m), r(p['clear_gold'] * m)
        lv = rows[c]
        print(f'| {c} | {m:.1f} | {run:,} | {cg:,} | {run + cg:,} | {r(p["clear_exp"] * m):,} | {lv} | '
              f'{plan_ghost_cost(lv - 1):,} | {runs_to_clear(c):g} |')

    # 진행 시뮬레이션 — 무과금이 챕터를 차례로 깨며 번 골드 vs 그때까지 열린 싱크
    print('\n## 설계안 — 1~100챕터 진행(무과금 하루 6판)\n')
    print('| 챕터 도달 | 날짜 | 유령 Lv | 누적 골드 수입 | 열린 싱크(유령 + 호스트) | 수입 ÷ 싱크 |')
    print('|---|---|---|---|---|---|')
    gold = runs = 0
    host_sink = sum(star_cost(g)[1] + skill_gold(g) for g in range(3) for _ in range(HOSTS[g]))
    for c in range(1, 101):
        n = runs_to_clear(c)
        runs += n
        gold = int(gold + n * r((p['run_gold'] + p['clear_gold']) * plan_mul(c)))
        if c % p['host_per_chapters'] == 0:
            g = (c // p['host_per_chapters']) % 3
            host_sink += star_cost(g)[1] + skill_gold(g)
        lv = rows[c]
        ghost = GHOST_LINES * sum(plan_ghost_cost(x) for x in range(lv))
        if c in (5, 10, 20, 30, 40, 50, 75, 100):
            sink = ghost + host_sink
            print(f'| {c} | {runs / 6:.0f}일 | {lv} | {gold:,} | {sink:,} | {gold / sink:.2f} |')


def plan_now_compare():
    """지금 값으로 같은 진행을 돌린다(20챕터 뒤는 표 끝 값이 이어진다고 본다)."""
    print('\n## 지금 값 — 같은 진행(비교)\n')
    print('| 챕터 도달 | 누적 골드 수입 | 싱크(유령 50 + 호스트 22) | 수입 ÷ 싱크 |')
    print('|---|---|---|---|')
    sink = GHOST_LINES * sum(GHOST_STEP * (lv + 1) for lv in range(GHOST_MAX)) + \
        sum(star_cost(g)[1] + skill_gold(g) for g in range(3) for _ in range(HOSTS[g]))
    gold = 0
    for c in range(1, 101):
        i = min(c, 20) - 1
        gold += runs_to_clear(c) * (RUN_GOLD[i] + CHEST_GOLD[CHEST[i]])
        if c in (5, 10, 20, 30, 50, 75, 100):
            print(f'| {c} | {gold:,} | {sink:,} | {gold / sink:.2f} |')


if __name__ == '__main__':
    if '--plan' in sys.argv:
        plan()
        plan_now_compare()
    else:
        now()
