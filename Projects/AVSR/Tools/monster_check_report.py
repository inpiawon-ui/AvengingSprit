# -*- coding: utf-8 -*-
"""몬스터 검증 녹화(`MonsterCheckRecorder`) 결과 줄을 판정표로 만든다 — 2단계 몬스터 조합.

입력: Editor.log 의 `[MON] done …` 줄(같은 몬스터는 마지막 줄) → 출력: step2_check/monster_recording.md
판정: 통과 / 우려 / 미검 — 피해가 들어갔는가 · 원소가 걸렸는가 · 성질이 쓰는 예고 · 자국이 떴는가.

쓰는 법: python monster_check_report.py
"""
import os
import re

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
LOG = os.path.join(os.environ['LOCALAPPDATA'], 'Unity', 'Editor', 'Editor.log')
OUT = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'plan_content_expand', 'step2_check', 'monster_recording.md')
CLIPS = 'Projects/AVSR/_exchange/skillfx_now'
NEW_BODIES = {'watcher_eye', 'shadow_cat', 'saw_monkey', 'mimic', 'lantern', 'soul_hound'}
NEEDS_TELL = ('Slam', 'Snipe', 'Mortar', 'Dive', 'ArmadilloRoll', 'Laser', 'Teleport', 'MantisDouble', 'SpiderPool')
NEEDS_ZONE = ('MushroomPool', 'SpiderPool')

# 사람이 본 것 — 녹화를 직접 보고 적는다(값만으로 판정할 수 없는 것)
NOTES = {
    'skeleton': ('우려', '연사 몸(갱스터)에 맞을 때마다 예고(0.45초)가 끊겨 한 대도 못 침 — 예전 해골도 같다. 3단계 밸런스에서 본다'),
    'skeleton_poison': ('우려', '독 자국은 깔린다(4). 몸으로 치는 독은 위와 같은 이유로 못 봄'),
    'soul_hound': ('미검', '몸을 버리자 유령이 곧바로 다시 빙의해 쫓을 시간이 없었다 — 몸이 없는 방에서 다시 잰다'),
    'saw_monkey': ('우려', '10초에 228 — 톱날이 갈 때 · 올 때 다 맞아 세다. 3단계 밸런스에서 줄인다'),
}


def main():
    text = open(LOG, encoding='utf-8', errors='replace').read()
    starts = [m.start() for m in re.finditer(r'\[MON\] 시작 — 47종', text)]
    text = text[starts[-1]:] if starts else text
    rows = {}
    order = []
    for m in re.finditer(r'\[MON\] done (\S+) (.*)', text):
        key, rest = m.group(1), m.group(2)
        kv = dict(re.findall(r'(\S+?)=(\S*)', rest))
        if key not in rows:
            order.append(key)
        rows[key] = kv
    lines = []
    count = {'통과': 0, '우려': 0, '미검': 0, '실패': 0}
    for key in order:
        kv = rows[key]
        traits = kv.get('성질', '').replace(',', '')
        el = kv.get('원소', 'None')
        dmg = int(kv.get('내피해', '0') or 0)
        tell = int(kv.get('예고최대', '0') or 0)
        zone = int(kv.get('자국최대', '0') or 0)
        el_ok = {'Fire': kv.get('화상') == 'True', 'Ice': kv.get('냉기') == 'True',
                 'Poison': kv.get('독') == 'True' or zone > 0}.get(el, True)
        why = []
        if dmg <= 0 and 'ShieldAlly' not in traits:
            why.append('피해 0')
        if not el_ok:
            why.append(f'{el} 안 걸림')
        if any(t in traits for t in NEEDS_TELL) and tell == 0:
            why.append('예고 없음')
        if any(t in traits for t in NEEDS_ZONE) and zone == 0:
            why.append('자국 없음')
        if 'ShieldAlly' in traits and kv.get('등불방패') != 'True':
            why.append('방패 안 씀')
        verdict = '통과' if not why else '실패'
        note = ', '.join(why)
        if key in NOTES:
            verdict, note = NOTES[key]
        if key in NEW_BODIES and verdict == '통과':
            verdict, note = '미검', '행동은 됨 — 그림이 아직 없어 흰 네모로 섰다(그림 받은 뒤 다시 찍는다)'
        count[verdict] += 1
        el_col = '' if el == 'None' else f"{el} · 걸림={'예' if el_ok else '아니오'}"
        lines.append(f"| {key} | {traits or '—'} | {el_col} | {dmg if dmg > -1000 else '—'} | {tell} | {zone} | {verdict} | {note} | `{CLIPS}/clip_mon_{key}.mp4` |")
    head = ('# 2단계 몬스터 검증 녹화\n\n'
            f"통과 {count['통과']} · 우려 {count['우려']} · 미검 {count['미검']} · 실패 {count['실패']} — "
            '`Game.Editor.MonsterCheckRecorder`(1챕터 판 하나에서 몬스터를 차례로 세워 10초씩) · `python Projects/AVSR/Tools/monster_check_report.py`\n\n'
            '내 몸 = 갱스터(무적 끔 · 체력 5000). 예고 · 자국 = 녹화 동안 동시에 떠 있던 최대 수. 원소 걸림 = 내 몸에 화상 · 냉기 · 독이 한 번이라도 걸렸나(독은 자국도 침).\n\n'
            '| 몬스터 | 성질 | 원소 | 내 피해 | 예고 | 자국 | 판정 | 메모 | 영상 |\n|---|---|---|---|---|---|---|---|---|\n')
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, 'w', encoding='utf-8') as f:
        f.write(head + '\n'.join(lines) + '\n')
    print(OUT, count)


if __name__ == '__main__':
    main()
