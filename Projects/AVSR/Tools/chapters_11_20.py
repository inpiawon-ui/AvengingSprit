# -*- coding: utf-8 -*-
"""4단계 — 11~20챕터 줄을 chapters.tsv 에 더한다 (PD 2026-10-23 「챕터 늘리기는 좋은데 일단 20개 정도까지만」).

정하는 것(구조): 무대 · 보스 · 대장 · 대장 2 · 잡몹 목록 · 쏠림 · 엘리트 · 상자 · 그림 칸 · 이름.
숫자(체력 · 공격 · 골드 · 전투력 · 연사)는 10챕터까지의 흐름을 같은 비율로 잇는다 — 한 판 통째 검증(4단계 ②) 뒤 다시 맞춘다.

  무대   1~10 과 같은 6무대 차례(junkyard → missile → street → rooftop → lab → refinery)를 잇는다.
  보스   원작 보스 6종을 그대로 돌린다(PD 10-23 「보스는 지금 있는 그대로 · 다 된 다음에 보면서 잡자」).
  잡몹   챕터마다 **처음 나오는 몬스터 하나 이상**(소개 방 · 메모리 room-objects-must-have-purpose) —
         2단계 새 모양 5 · 원소판 6 을 11~16챕터에 나눠 처음 내고, 17~20 은 섞어서 세게.
  대장   앞 챕터의 대장 2 가 다음 챕터 대장(1~10 과 같은 사슬). 사신(death)은 히든 — 쓰지 않는다.

쓰는 법: python chapters_11_20.py   (이미 11 줄이 있으면 다시 쓰지 않는다 — --force 로 덮어쓰기)
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
PATH = os.path.join(ROOT, 'Projects', 'AVSR', 'Rooms', 'chapters.tsv')

STAGES = ['junkyard', 'missile', 'street', 'rooftop', 'lab', 'refinery']
STAGE_ART = {s: i + 1 for i, s in enumerate(STAGES)}            # 로비 그림 칸 = 무대 차례(1~10 과 같다)
BOSS_ART = {'robot_snakes': 1, 'crusher': 2, 'python': 3, 'sludge': 4, 'guardian': 5, 'kingpin': 6}

# ch: (무대, 보스, 대장, 대장 2, 잡몹 — 앞 방 → 뒤 방, 쏠림, 엘리트, 이름, 한 줄 설명)
NEW = {
    11: ('lab', 'robot_snakes', 'guru', 'commando_mg',
         'roadwarden_2,scrapgunner_3,watcher_eye,coilwalker_2,skeleton_3,actor_enforcer_3,gorilla_fire,mushroom_2',
         'blade', 2, '감시 구역', '천장의 눈이 모든 걸 지켜본다\n붉은 렌즈가 켜지면 이미 늦었다.'),
    12: ('refinery', 'crusher', 'commando_mg', 'snowwoman',
         'shadow_cat,skeleton_3,scrapgunner_ice,coilwalker_2,mole_2,boar_2,actor_enforcer_4,obj_turret_3',
         'magic', 2, '그림자 공장', '불 꺼진 공장 바닥을 무언가 소리 없이 건넌다.'),
    13: ('junkyard', 'python', 'snowwoman', 'hopper_smg',
         'saw_monkey,skeleton_poison,bat_2,scrapgunner_sniper,actor_enforcer_4,mantis_2,armadillo_2,mushroom_2',
         'force', 2, '톱날 고물상', '고철 더미 너머로 돌아오는 톱날 소리.'),
    14: ('missile', 'sludge', 'hopper_smg', 'white_wizard',
         'lantern,coilwalker_ice,roadwarden_3,scrapgunner_sniper,bat_bounce,gorilla_charger,spider,mole_2',
         'blade', 2, '푸른 등불 기지', '꺼지지 않는 등불이 적의 몸을 감싼다.'),
    15: ('street', 'guardian', 'white_wizard', 'commando_grenade',
         'mimic,bat_poison,skeleton_mage,coil_mortar,roadwarden_3,gorilla_fire,mummy,spider_2',
         'magic', 2, '속임수 골목', '길가의 상자도 쓰레기통도 믿지 마라.'),
    16: ('rooftop', 'kingpin', 'commando_grenade', 'ninja',
         'roadwarden_fire,watcher_eye,shadow_cat,scrapgunner_ice,actor_enforcer_4,gorilla_charger,mantis_2,obj_turret_3',
         'even', 2, '불타는 옥상', '도시 꼭대기까지 불길이 번졌다.'),
    17: ('lab', 'robot_snakes', 'ninja', 'commando_laser',
         'skeleton_poison,coilwalker_ice,lantern,saw_monkey,watcher_eye,mummy_2,spider_2,bat_bounce',
         'force', 3, '오염 실험동', '독과 얼음이 섞인 실험실 — 무엇이 깨어났나.'),
    18: ('refinery', 'crusher', 'commando_laser', 'thug',
         'roadwarden_fire,gorilla_fire,mimic,shadow_cat,coil_mortar,scrapgunner_sniper,armadillo_2,mushroom_2',
         'blade', 3, '용광로 심장부', '쇳물이 끓는 곳, 버티는 놈만 살아남는다.'),
    19: ('junkyard', 'python', 'thug', 'amazon_elite',
         'bat_poison,scrapgunner_ice,saw_monkey,lantern,gorilla_charger,skeleton_mage,mole_2,mantis_2',
         'magic', 3, '폐허의 왕국', '버려진 것들이 왕을 세웠다.'),
    20: ('missile', 'sludge', 'amazon_elite', 'amazon',
         'watcher_eye,shadow_cat,mimic,lantern,coilwalker_ice,roadwarden_fire,gorilla_fire,skeleton_poison,spider_2,obj_turret_3',
         'even', 3, '최후의 발사대', '모든 것이 이 발사대에서 끝난다.'),
}


def load():
    lines = open(PATH, encoding='utf-8').read().split('\n')
    head = next(ln for ln in lines if ln and not ln.startswith('#')).split('\t')
    rows = [ln.split('\t') for ln in lines if ln and not ln.startswith('#')][1:]
    return lines, head, rows


def grow(prev, prev2, k):
    """10챕터까지의 마지막 두 칸 비율로 잇는다(10 → 9 의 비율, 지나친 비율은 1.25 로 묶는다)."""
    r = min(1.25, max(1.0, float(prev) / max(1e-6, float(prev2))))
    return float(prev) * r ** k


def main(force):
    lines, head, rows = load()
    have = {r[0] for r in rows}
    if '11' in have and not force:
        print('이미 11챕터 줄이 있다 — --force 로 덮어쓴다')
        return
    rows = [r for r in rows if int(r[0]) <= 10]
    col = {h: i for i, h in enumerate(head)}
    c9, c10 = rows[8], rows[9]
    out = []
    for ch in range(11, 21):
        stage, boss, leader, leader2, trash, lean, elite, name, desc = NEW[ch]
        k = ch - 10
        v = {h: '' for h in head}
        v.update(ch=str(ch), stage=stage, boss=boss, leader=leader, leader2=leader2, trash=trash, lean=lean,
                 count=f'{10 + k // 3}-{12 + k // 3}', elite=str(elite), crowd='0',
                 fire=c10[col['fire']], clearGold='0', chest='platinum',
                 music=str(STAGE_ART[stage]), art=str(STAGE_ART[stage]), bossArt=str(BOSS_ART[boss]), chestArt=c10[col['chestArt']],
                 name=name, desc=desc.replace('\n', '\\n'), powerStar=str(min(5, 3 + k // 4)))
        for key, fmt in (('hpMul', '{:.2f}'), ('atkMul', '{:.2f}'), ('bossHp', '{:.0f}'), ('bossAtk', '{:.0f}'),
                         ('roomGold', '{:.0f}'), ('midGold', '{:.0f}'), ('bossGold', '{:.0f}'), ('exp', '{:.0f}'),
                         ('priceMul', '{:.1f}'), ('power', '{:.2f}')):
            # 성장은 완만하게 — 10챕터 비율을 그대로 열 번 곱하면 20챕터가 수십 배가 된다. 비율의 절반만 잇는다
            #   (경험치 · 골드도 — 그대로 이으면 유령 Lv50 에 15챕터쯤 닿아 성장이 일찍 끝난다 · 4단계 조사)
            r = min(1.25, max(1.0, float(c10[col[key]]) / max(1e-6, float(c9[col[key]]))))
            r = 1 + (r - 1) * 0.5
            v[key] = fmt.format(float(c10[col[key]]) * r ** k)
        out.append([v[h] for h in head])
    text = []
    for ln in lines:
        if ln and not ln.startswith('#'):
            break
        text.append(ln)
    text.append('\t'.join(head))
    text += ['\t'.join(r) for r in rows + out]
    open(PATH, 'w', encoding='utf-8', newline='\n').write('\n'.join(text) + '\n')
    for r in out:
        print(' '.join(f'{h}={r[col[h]]}' for h in ('ch', 'stage', 'boss', 'leader', 'leader2', 'elite', 'hpMul', 'atkMul', 'bossHp', 'roomGold', 'power')))


if __name__ == '__main__':
    main('--force' in sys.argv)
