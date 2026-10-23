# -*- coding: utf-8 -*-
"""11~20챕터 수치를 밸런스 모델(balance10.py)에서 뽑아 chapters.tsv 에 적는다 — 10챕터 손 값을 닻으로(4단계 ②, 2026-10-23).

1~10챕터 hpMul · atkMul · bossHp · bossAtk 는 PD 체감으로 손본 값이라 덮지 않는다(balance10.py 머리 주석).
11~20 은 「10챕터 값 × (그 챕터 모델 값 ÷ 10챕터 모델 값)」 — 모델이 말하는 오름 비율만 빌린다.
  적 체력 ← 내 화력 × 손 느는 몫(1 + 0.06c) · 적 공격 ← 내 체력 × (적 수 비)^0.5 · 보스 체력 ← 내 화력 × (1 + 0.12c)
power(권장 전투력)는 모델 값을 그대로.

쓰는 법: python chapters_11_20_numbers.py
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import balance10 as B  # noqa: E402

PATH = B._TSV


def main():
    rows = B.simulate()
    model = {}
    for ch, tries, runs, gold, (hm, dm, lv, gh, ho, star) in rows:
        c = ch - 1
        model[ch] = dict(hp=dm * (1 + B.SKILL_HP * c), atk=hm * (B.COUNT[0] / B.COUNT[c]) ** B.COUNT_SOFTEN,
                         boss_hp=dm * (1 + 0.12 * c), boss_atk=hm ** 0.9, power=math.sqrt(hm * dm), star=star)
    lines = open(PATH, encoding='utf-8').read().split('\n')
    head, col, ten = None, None, None
    for ln in lines:
        if ln and not ln.startswith('#'):
            x = ln.split('\t')
            if head is None:
                head = x; col = {h: i for i, h in enumerate(head)}; continue
            if x[0] == '10':
                ten = x
    out = []
    for ln in lines:
        if not ln or ln.startswith('#') or ln.split('\t')[0] in ('ch',):
            out.append(ln); continue
        x = ln.split('\t')
        ch = int(x[0])
        if ch > 10:
            m, m10 = model[ch], model[10]
            x[col['hpMul']] = f"{float(ten[col['hpMul']]) * m['hp'] / m10['hp']:.2f}"
            x[col['atkMul']] = f"{float(ten[col['atkMul']]) * m['atk'] / m10['atk']:.2f}"
            x[col['bossHp']] = f"{float(ten[col['bossHp']]) * m['boss_hp'] / m10['boss_hp']:.0f}"
            x[col['bossAtk']] = f"{float(ten[col['bossAtk']]) * m['boss_atk'] / m10['boss_atk']:.0f}"
            x[col['power']] = f"{m['power']:.2f}"
            x[col['powerStar']] = str(m['star'])
            print(ch, *(f'{h}={x[col[h]]}' for h in ('hpMul', 'atkMul', 'bossHp', 'bossAtk', 'power', 'powerStar')))
        out.append('\t'.join(x))
    open(PATH, 'w', encoding='utf-8', newline='\n').write('\n'.join(out))


if __name__ == '__main__':
    main()
