# -*- coding: utf-8 -*-
"""10챕터 밸런스 모델 — 「몇 판 만에 깨는가」에서 거꾸로 적 능력치와 보상을 뽑는다.

쓰는 법:  python balance10.py            # 표를 찍는다(chapters.tsv 에 옮겨 적을 숫자)

생각의 순서
  1. 챕터마다 「깨는 데 걸리는 판 수」를 정한다(TRIES — 기획 2026-10-02 「완만하게」).
  2. 한 판이 주는 골드(클리어 / 사망)를 정한다 → 누적 골드가 나온다.
  3. 그 골드로 사람이 살 강화를 흉내 낸다(가장 싸게 세지는 것부터 산다) → 판마다의 **내 힘**이 나온다.
  4. 챕터 c 를 깨는 판에서의 내 힘 = 그 챕터가 요구하는 힘. 적 체력은 내 화력에, 적 공격력은 내 체력에 맞춘다.

⚠ 이건 **모델**이다. 실제 손맛(피하기 · 카드 운 · 상성 고르기)은 안 들어 있다.
  1챕터 수치는 지금 게임 값 그대로 닻으로 두고, 2챕터부터 이 모델의 배율을 곱한다.
  자동 조종으로 몇 판 돌려 맞는지 보고, 틀리면 TRIES 가 아니라 아래 손잡이(SKILL_*)를 돌린다.
"""
import math

CHAPTERS = 10
# 챕터를 깨는 데 걸리는 판 수(무과금 · 평범한 실력)
TRIES = [1.5, 1.5, 3.5, 3.5, 6, 6, 9, 9, 13.5, 13.5]
# 방당 적 수(가운데 값) — chapters.tsv 의 count
COUNT = [4, 5, 6, 7, 8, 9, 9, 10, 10, 11]

# ── 골드 ──
# 판 안에서 줍는 골드 총액(15방을 다 비웠을 때). 1챕터 464 는 지금 값(방 24 · 중간보스 80 · 보스 120).
RUN_GOLD_MUL = [1.0, 1.3, 1.7, 2.2, 2.8, 3.5, 4.3, 5.2, 6.2, 7.3]
RUN_GOLD_CH1 = 464
SHOP_SPEND = 0.20          # 판 골드 가운데 상점에 쓰는 몫
DEATH_PROGRESS = 0.45      # 죽는 판은 평균 이만큼 가서 죽는다(주운 골드 비율)
CLEAR_BONUS = [200, 300, 450, 650, 900, 1200, 1550, 1950, 2400, 3000]
CHEST = ['silver', 'silver', 'silver', 'gold', 'gold', 'gold', 'platinum', 'platinum', 'platinum', 'platinum']
CHEST_GOLD = {'silver': (100, 200), 'gold': (300, 500), 'platinum': (700, 1000)}

# ── 유저(유령) 레벨 ──
CLEAR_EXP = [300, 500, 800, 1200, 1700, 2300, 3000, 3800, 4700, 5700]
EXP_BASE, EXP_STEP = 100, 60                 # 다음 레벨까지 = 100 + 60 × (Lv − 1)
GHOST_HP_PER_LV, GHOST_ATK_PER_LV = 0.03, 0.016
PATH_GOLD = {10: 5000, 20: 10000, 30: 20000, 40: 30000, 50: 50000}

# ── 골드 강화 ──
# 한 단계 값 = STEP × (다음 레벨). 유령(모든 몸)은 비싸고, 호스트(그 몸만)는 싸다.
GHOST_STEP, HOST_STEP = 100, 60
GHOST_CAP = 50
STAR_CAP = 10                                # 호스트 강화 상한 = 10 × 성급
PCT = {'hp': 0.02, 'atk': 0.02, 'aspd': 0.01}

# ── 손잡이 ──
SKILL_HP = 0.06      # 챕터가 오를수록 카드 · 손이 느는 몫(적 체력에 더 얹는다) — 챕터당
# 적이 많아지면 동시에 맞는 양이 는다 — 그만큼 적 한 기의 공격력은 덜 올린다.
# 1.0 = 「적 수 × 공격력」이 내 체력에 비례한다(방 전체가 쏟아내는 피해가 내 체력을 따라간다).
# ⚠ 처음에는 0.35 였다. 자동 검증(2026-10-02)에서 「깨는 판의 힘」으로 넣은 3 · 6 · 10챕터가
#   전부 방 7~10 에서 죽었다 — 적 수가 4 → 11 로 느는 것을 너무 약하게 봤다.
COUNT_SOFTEN = 1.0


def level_of(exp):
    lv = 1
    while lv < 50 and exp >= EXP_BASE + EXP_STEP * (lv - 1):
        exp -= EXP_BASE + EXP_STEP * (lv - 1)
        lv += 1
    return lv


class Player:
    def __init__(self):
        self.gold = 0
        self.exp = 0
        self.ghost = {'hp': 0, 'atk': 0, 'aspd': 0}
        self.host = {'hp': 0, 'atk': 0, 'aspd': 0}
        self.star = 1
        self.paid = set()

    @property
    def lv(self):
        return level_of(self.exp)

    def hp_mul(self):
        return (1 + PCT['hp'] * (self.ghost['hp'] + self.host['hp'])) * (1 + GHOST_HP_PER_LV * (self.lv - 1))

    def dps_mul(self):
        return ((1 + PCT['atk'] * (self.ghost['atk'] + self.host['atk']))
                * (1 + PCT['aspd'] * (self.ghost['aspd'] + self.host['aspd']))
                * (1 + GHOST_ATK_PER_LV * (self.lv - 1)))

    def power(self):
        return math.sqrt(self.hp_mul() * self.dps_mul())

    def spend(self):
        """가진 골드로 「골드당 힘이 가장 많이 오르는 것」부터 산다."""
        while True:
            best = None
            for who, step, cap in (('ghost', GHOST_STEP, GHOST_CAP), ('host', HOST_STEP, STAR_CAP * self.star)):
                d = self.ghost if who == 'ghost' else self.host
                for stat in ('hp', 'atk', 'aspd'):
                    if d[stat] >= cap:
                        continue
                    cost = step * (d[stat] + 1)
                    if cost > self.gold:
                        continue
                    before = self.power()
                    d[stat] += 1
                    gain = self.power() - before
                    d[stat] -= 1
                    if best is None or gain / cost > best[0]:
                        best = (gain / cost, who, stat, cost)
            if best is None:
                return
            _, who, stat, cost = best
            (self.ghost if who == 'ghost' else self.host)[stat] += 1
            self.gold -= cost

    def path_rewards(self):
        for lv, g in PATH_GOLD.items():
            if self.lv >= lv and lv not in self.paid:
                self.paid.add(lv)
                self.gold += g


def run_gold(c):
    return RUN_GOLD_CH1 * RUN_GOLD_MUL[c]


def clear_income(c):
    lo, hi = CHEST_GOLD[CHEST[c]]
    return run_gold(c) * (1 - SHOP_SPEND) + CLEAR_BONUS[c] + (lo + hi) / 2


def death_income(c):
    return run_gold(c) * DEATH_PROGRESS * (1 - SHOP_SPEND)


def simulate():
    p = Player()
    rows, runs, total_gold = [], 0, 0
    for c in range(CHAPTERS):
        # 1.5 판 → 한 번은 1판, 한 번은 2판. 0.5 는 번갈아 올림 · 내림
        tries = int(TRIES[c]) + (1 if (TRIES[c] % 1 and c % 2 == 0) else 0)
        tries = max(1, tries)
        # 성급: 주력 몸 기준 — 조각이 모이는 속도(대략)
        p.star = 1 if runs < 12 else 2 if runs < 34 else 3
        for t in range(tries - 1):
            g = death_income(c)
            p.gold += g; total_gold += g; runs += 1
            p.path_rewards(); p.spend()
        # 깨는 판 — 이 판에 들어설 때의 힘이 이 챕터가 요구하는 힘이다
        req = (p.hp_mul(), p.dps_mul(), p.lv, dict(p.ghost), dict(p.host), p.star)
        g = clear_income(c)
        p.gold += g; total_gold += g; runs += 1
        p.exp += CLEAR_EXP[c]
        p.path_rewards(); p.spend()
        rows.append((c + 1, tries, runs, total_gold, req))
    return rows


def main():
    rows = simulate()
    print('── 판마다의 내 힘(그 챕터를 깨는 판에 들어설 때) ──')
    print('챕터  판수  누적판  누적골드   유령Lv  체력배  화력배  유령강화(체/공/속)  몸강화(체/공/속)  성급')
    for ch, tries, runs, gold, (hm, dm, lv, gh, ho, star) in rows:
        print(f'{ch:>3} {tries:>5} {runs:>6} {gold:>9.0f} {lv:>7} {hm:>7.2f} {dm:>7.2f}'
              f'   {gh["hp"]:>2}/{gh["atk"]:>2}/{gh["aspd"]:>2}            {ho["hp"]:>2}/{ho["atk"]:>2}/{ho["aspd"]:>2}         {star}')

    print()
    print('── chapters.tsv 에 옮겨 적을 숫자 ──')
    print('ch  hpMul  atkMul  bossHp  bossAtk  roomGold  midGold  bossGold  clearGold  chest     exp   표시보상(최소~최대)')
    base_hp, base_dps = rows[0][4][0], rows[0][4][1]
    for ch, tries, runs, gold, (hm, dm, lv, gh, ho, star) in rows:
        c = ch - 1
        skill = 1 + SKILL_HP * c
        # 적 체력 ← 내 화력. 1챕터는 0.5(지금 값 그대로 — 1챕터만 절반으로 깎여 있다)
        hp_mul = 0.5 * (dm / base_dps) * skill
        # 적 공격력 ← 내 체력. 적이 많아진 만큼은 덜 올린다
        atk_mul = (hm / base_hp) * (COUNT[0] / COUNT[c]) ** COUNT_SOFTEN
        atk_mul = atk_mul if c else 1.0     # 1 아래로도 내려간다 — 한 기는 약해져도 수가 는다
        boss_hp = 1650 * (dm / base_dps) * (1 + 0.12 * c)
        boss_atk = 18 if c == 0 else 30 * (hm / base_hp) ** 0.9
        rg = run_gold(c)
        room = rg / 464 * 24
        mid = rg / 464 * 80
        boss = rg / 464 * 120
        lo, hi = CHEST_GOLD[CHEST[c]]
        show_lo = CLEAR_BONUS[c] + lo + rg * 0.6
        show_hi = CLEAR_BONUS[c] + hi + rg
        print(f'{ch:>2} {hp_mul:>6.2f} {atk_mul:>7.2f} {boss_hp:>7.0f} {boss_atk:>8.0f} {room:>9.0f} {mid:>8.0f}'
              f' {boss:>9.0f} {CLEAR_BONUS[c]:>10} {CHEST[c]:<9} {CLEAR_EXP[c]:>5}   {show_lo:>6.0f} ~ {show_hi:.0f}')


if __name__ == '__main__':
    main()
