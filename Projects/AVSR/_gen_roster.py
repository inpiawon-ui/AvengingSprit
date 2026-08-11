"""정본(Canon) 로스터 표를 런타임 JSON 에서 뽑아 `AVSR_Roster.md` 로 쓴다.

로스터는 **아트 폴더명 = 아틀라스 주소 = 테이블 키 = 정본 ID** 를 잇는 접착제다.
손으로 적으면 반드시 어긋나므로 정본 JSON 을 읽어 매번 다시 만든다.

`RENAME` 만 사람이 정한 값이다 — 목업 15종 중 정본과 같은 캐릭터인 10종의 대응이며,
그림·무기·역할을 대조해 판단했다. 나머지 5종은 정본에 없어 폐기했다(확정 #10-6).

사용: python _gen_roster.py
"""
import io
import json
import os
import sys

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
RUNTIME = os.path.join(HERE, 'Canon', 'Runtime', 'CH01_03_RUNTIME_DATA_v1.5.json')
UNIT = os.path.abspath(os.path.join(HERE, '..', '..', 'Assets', 'BaseResource', 'Unit'))
OUT = os.path.join(HERE, 'AVSR_Roster.md')

# 정본 EnemyID → 아트 키(폴더명). 소문자·밑줄, 정본 이름을 그대로 옮긴 것.
SLUG = {
    'E001': 'gangster',        'E002': 'fighter',        'E003': 'salamander',
    'E004': 'thug',            'E005': 'grenadier',      'E006': 'master_fighter',
    'E007': 'assault_gangster','E008': 'robot',          'E009': 'guru',
    'E010': 'white_wizard',    'E011': 'ninja',          'E012': 'medium',
    'E013': 'missile_merc',    'E014': 'vampire',        'E015': 'baseball',
    'E016': 'dragoon',         'E017': 'shield_trooper', 'E018': 'sensor_drone',
}

# 지금 그려 둔 목업 키 → 정본 아트 키. 여기 없는 목업 키는 폐기다.
RENAME = {
    'mafia': 'gangster',        'rambo': 'thug',        'rambo_laser': 'assault_gangster',
    'dragon': 'salamander',     'yogamaster': 'guru',   'wizard': 'white_wizard',
    'slugger': 'baseball',      'ninja': 'ninja',       'vampire': 'vampire',
    'robot': 'robot',
}
RETIRED = ['amazoness', 'hitman', 'snowwoman', 'ninja_red', 'wizard_green']

# `Canon/Runtime/DATA_CONTRACT_v1.5.json` 의 collectionSchemas.enemies 필드 순서.
# 런타임 JSON 은 이름 없는 위치 기반 배열이라 이 순서가 곧 스키마다.
FIELD_ORDER = [
    'EnemyID', 'EnemyName', 'Originality', 'CombatRole', 'MoveSpeed', 'MaxHP',
    'AttackDamage', 'EngageSpeed', 'AttackRange', 'AttackInterval', 'Telegraph',
    'AttackStyle', 'AIPattern', 'CombatPurpose', 'PossessionType',
    'PossessionCondition', 'HostID', 'BuffTag', 'CounterTag', 'EXP', 'Drop',
    'PossessPriority', 'Biome', 'MinChapter', 'Difficulty',
]

PTYPE = {'Immediate': '즉시', 'Condition': '조건부', 'NotPossessable': '불가'}

KR = {
    'gangster': '갱스터', 'fighter': '파이터', 'salamander': '샐러맨더', 'thug': '폭력배',
    'grenadier': '그레네이더', 'master_fighter': '마스터 파이터',
    'assault_gangster': '어설트 갱스터', 'robot': '로봇', 'guru': '구루',
    'white_wizard': '화이트 위저드', 'ninja': '닌자', 'medium': '영매',
    'missile_merc': '미사일 용병', 'vampire': '흡혈귀', 'baseball': '야구선수',
    'dragoon': '드라군', 'shield_trooper': '방패병', 'sensor_drone': '센서 드론',
}


def main():
    d = json.load(io.open(RUNTIME, encoding='utf-8'))
    have = {RENAME[k] for k in RENAME if os.path.isdir(os.path.join(UNIT, k))}

    # 적 → 호스트는 이름이 아니라 **HostID 필드**로 잇는다.
    # 이름으로 맞추면 폭력배→H02·어설트갱스터→H01 처럼 이름이 다른 v1.5 기술 락을 놓친다.
    fields = ['EnemyID', 'EnemyName', 'PossessionType', 'PossessionCondition', 'HostID']
    idx = {k: FIELD_ORDER.index(k) for k in fields}
    globals()['HOST_NAME'] = {h[0]: h[1] for h in d['hosts']}

    # 챕터별 스폰 수 — 아트 우선순위는 이 숫자가 정한다
    spawn = {}
    for s in d['layout']['enemySpawns']:
        spawn.setdefault(s[4], {}).setdefault(s[0][:3], 0)
        spawn[s[4]][s[0][:3]] += 1

    rows = []
    for e in d['enemies']:
        eid, ename = e[idx['EnemyID']], e[idx['EnemyName']]
        hid = e[idx['HostID']]
        slug = SLUG[eid]
        sp = spawn.get(eid, {})
        rows.append(dict(
            eid=eid, hid=None if hid in (None, 'None', '') else hid,
            slug=slug, kr=KR[slug], en=ename,
            ch1=sp.get('CH1', 0), ch2=sp.get('CH2', 0), ch3=sp.get('CH3', 0),
            total=sum(sp.values()),
            ptype=e[idx['PossessionType']], cond=e[idx['PossessionCondition']],
            have=slug in have,
        ))
    rows.sort(key=lambda r: (-r['ch1'], -r['total'], r['eid']))

    src = {v: k for k, v in RENAME.items()}
    # HostID → 그 호스트의 본체 슬러그. 적↔호스트가 어긋난 줄을 찾는 데 쓴다.
    globals()['SLUG_OF_HOST'] = {r['hid']: r['slug'] for r in rows
                                 if r['hid'] and r['en'] == HOST_NAME.get(r['hid'])}
    with io.open(OUT, 'w', encoding='utf-8', newline='\n') as f:
        w = f.write
        w('# AVSR 로스터 — 정본 대조표\n\n')
        w('> **자동 생성 — 손으로 고치지 말 것.** `python _gen_roster.py` 로 다시 만든다.\n'
          '> 출처: [`Canon/Runtime/CH01_03_RUNTIME_DATA_v1.5.json`](Canon/Runtime/'
          'CH01_03_RUNTIME_DATA_v1.5.json)\n\n')
        w('아트 폴더명 · 아틀라스 주소 · 테이블 키를 **하나의 슬러그**로 통일한다.\n')
        w('`Assets/BaseResource/Unit/{슬러그}/` → 아틀라스 `atlas/unit_{슬러그}` '
          '→ 스프라이트 `unit_{슬러그}_{방향}_{프레임}`.\n\n')
        w('**아트 순서는 CH1 스폰 수가 정한다.** P0 목표 방 `CH1_N01` 은 갱스터 ×2 + 파이터 ×1 이다.\n\n')

        w('| 슬러그 | 이름 | 정본 | 빙의 | 조건 | 호스트 | CH1 | CH2 | CH3 | 그림 |\n')
        w('|---|---|---|---|---|---|--:|--:|--:|---|\n')
        for r in rows:
            host = f"`{r['hid']}`" if r['hid'] else '—'
            art = ('보유' if r['have'] else '**필요**')
            if r['have'] and src.get(r['slug']) != r['slug']:
                art = f"보유 ← `{src[r['slug']]}`"
            cond = '' if r['cond'] in ('None', None) else r['cond']
            w(f"| `{r['slug']}` | {r['kr']} | `{r['eid']}` {r['en']} | "
              f"{PTYPE.get(r['ptype'], r['ptype'])} | {cond} | {host} | "
              f"{r['ch1'] or ''} | {r['ch2'] or ''} | {r['ch3'] or ''} | {art} |\n")

        need = [r for r in rows if not r['have']]
        w(f"\n적 **{len(rows)}종** 중 그림 보유 **{len(rows)-len(need)}종**, "
          f"신규 필요 **{len(need)}종** — "
          + ' · '.join(f"`{r['slug']}`" for r in need) + '\n')

        w('\n## 폐기 (목업에만 있던 5종)\n\n')
        w('정본에 대응이 없어 뺐다(확정 #10). 그림은 지우지 않고 '
          '`Projects/AVSR/_retired/Unit/` 에 보관한다.\n\n')
        w('| 목업 키 | 이유 |\n|---|---|\n')
        for k in RETIRED:
            w(f'| `{k}` | 정본 18적·12호스트에 대응 없음 |\n')

        w('\n## 리소스 요구량 — 적과 호스트가 다르다\n\n')
        w('출처: [`Canon/Docs/CHARACTER_RESOURCE_SPEC.md`](Canon/Docs/CHARACTER_RESOURCE_SPEC.md)\n\n')
        w('| | 적 18종 | 호스트 12종 (추가) |\n|---|---|---|\n')
        w('| 동작 | Idle · 이동 · 공격 · 피격 · 사망 | 궁극기 · 빙의 진입 · 빙의 이탈 |\n')
        w('| 방향 5장 기준 | 35장 | +15장 이상 |\n\n')
        multi = [r for r in rows if r['hid'] and SLUG_OF_HOST.get(r['hid']) != r['slug']]
        w('**적 18종과 호스트 12종은 1:1 이 아니다.** 아래 넷은 빙의하면 '
          '다른 몸의 호스트 프로필로 바뀐다(v1.5 기술 락 — 널 HostID 제거).\n\n')
        for r in multi:
            w(f"- `{r['eid']}` {r['kr']} → **{r['hid']}** "
              f"({KR[SLUG_OF_HOST[r['hid']]]}) 프로필\n")
        w('\n즉 이 넷은 **적 그림만** 있으면 되고 호스트 동작(궁극기·빙의 진출입)은 '
          '대상 호스트 것을 쓴다.\n')

    print(f'썼다: {OUT}')
    print(f'  적 {len(rows)}종 / 보유 {len(rows)-len(need)} / 신규 {len(need)}')


if __name__ == '__main__':
    main()
