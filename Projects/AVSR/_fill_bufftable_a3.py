"""버프 테이블을 기획서 A 5-4·5-5·5-6 기준으로 확장한다.

기존 12종은 전부 범용이다. 거기에 **태그형**과 **호스트 전용**을 더한다.
이 둘이 있어야 3택1 이 "좋은 것 고르기"에서 "지금 이 몸에 맞는 것 고르기"로 바뀐다.

뽑기 비율(범용 60 / 태그 30 / 전용 10)은 코드가 갖고 있고, 여기서는 각 분류에
뽑을 것이 충분히 있도록 개수를 맞춘다. 태그가 5계열이라 계열당 최소 2종은 둔다 —
한 종뿐이면 그 계열 호스트를 쓸 때 매번 같은 카드가 나온다.
"""
import io
import os
import re

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
ASSET = os.path.join(ROOT, 'Assets', 'BundleResource', 'TableData', 'BuffTable.asset')

# BuffKind: 0 Attack 1 AttackSpeed 2 Range 3 MoveSpeed 4 GhostHp 5 Heal
#           6 MultiShot 7 Pierce 8 Lifesteal 9 Slow 10 UltimateCharge 11 ShotSpeed
# BuffScope: 0 Common 1 Tag 2 HostOnly
# BuffTag:   1 Shot 2 Melee 3 Magic 4 Deploy 5 Breath

TAG_BUFFS = [
    # (key, 이름, 설명, kind, value, tag, color)
    ('tag_shot_dmg',   '탄환 피해 증가', '탄환 계열 피해 +30%',   0, 30, 1, '#F0B428'),
    ('tag_shot_multi', '탄환 다발',     '탄환 계열 탄 +1발',     6,  1, 1, '#F0B428'),
    ('tag_melee_dmg',  '근접 피해 증가', '근접 계열 피해 +35%',   0, 35, 2, '#E8404A'),
    ('tag_melee_leech', '근접 흡혈',    '근접 계열 흡혈 +12%',   8, 12, 2, '#E8404A'),
    ('tag_magic_dmg',  '마법 피해 증가', '마법 계열 피해 +32%',   0, 32, 3, '#8A46D8'),
    ('tag_magic_range', '마법 사거리',  '마법 계열 사거리 +30%', 2, 30, 3, '#8A46D8'),
    ('tag_deploy_dmg', '설치물 강화',   '설치 계열 피해 +40%',   0, 40, 4, '#4AA8E8'),
    ('tag_deploy_rate', '설치 가동률',  '설치 계열 간격 -20%',   1, 20, 4, '#4AA8E8'),
    ('tag_breath_range', '브레스 범위', '브레스 계열 사거리 +35%', 2, 35, 5, '#F07028'),
    ('tag_breath_dmg', '브레스 강화',   '브레스 계열 피해 +38%', 0, 38, 5, '#F07028'),
]

HOST_BUFFS = [
    # (key, 이름, 설명, kind, value, hostKey, color)
    # hostKey 는 정본 슬러그다 — `AVSR_Roster.md` 참조. 폐기된 아마조네스·히트맨 전용 버프는
    # 갱스터·어설트갱스터로 옮겼다. 버프 자체를 지우면 방마다 나오는 선택지가 3장에서 줄어든다.
    ('host_ninja_star',    '표창 강화',   '닌자 — 탄 +2발',              6,  2, 'ninja',            '#3A5CD8'),
    ('host_dragon_breath', '브레스 폭발', '샐러맨더 — 피해 +55%',        0, 55, 'salamander',       '#F07028'),
    ('host_robot_missile', '미사일 강화', '로봇 — 간격 -30%',            1, 30, 'robot',            '#9AA4B4'),
    ('host_gangster_mark', '표식 릴레이', '갱스터 — 사거리 +45%',        2, 45, 'gangster',         '#D8A828'),
    ('host_vampire_leech', '피의 갈증',   '흡혈귀 — 흡혈 +20%',          8, 20, 'vampire',          '#A02838'),
    ('host_laser_focus',   '집속 레이저', '어설트 갱스터 — 피해 +70%',   0, 70, 'assault_gangster', '#C8C8D0'),
]


def entry(key, name, desc, kind, value, color, scope, tag=0, host=''):
    """유니티 YAML 한 항목. 한글은 이스케이프해서 넣는다(기존 항목과 같은 형식)."""
    def esc(s):
        return '"' + ''.join(f'\\u{ord(c):04X}' if ord(c) > 127 else c for c in s) + '"'
    return (f'  - _buffKey: {key}\n'
            f'    _nameKr: {esc(name)}\n'
            f'    _description: {esc(desc)}\n'
            f'    _kind: {kind}\n'
            f'    _value: {value}\n'
            f'    _stackable: 1\n'
            f'    _colorHex: {color}\n'
            f'    _scope: {scope}\n'
            f'    _tag: {tag}\n'
            f'    _hostKey: {host}\n')


def main():
    s = io.open(ASSET, encoding='utf-8').read()

    # 기존 항목에 새 필드를 채운다 — 전부 범용이다
    def add_scope(m):
        block = m.group(0)
        if '_scope:' in block:
            return block
        return block + '    _scope: 0\n    _tag: 0\n    _hostKey: \n'

    s = re.sub(r'    _colorHex: [^\n]*\n', lambda m: add_scope(m), s)

    added = 0
    tail = ''
    for k, n, d, kind, v, tag, c in TAG_BUFFS:
        if f'_buffKey: {k}\n' in s:
            continue
        tail += entry(k, n, d, kind, v, c, scope=1, tag=tag)
        added += 1
    for k, n, d, kind, v, host, c in HOST_BUFFS:
        if f'_buffKey: {k}\n' in s:
            continue
        tail += entry(k, n, d, kind, v, c, scope=2, host=host)
        added += 1

    s = s.rstrip('\n') + '\n' + tail
    io.open(ASSET, 'w', encoding='utf-8').write(s)
    print(f'버프 {added}종 추가 (태그형 {len(TAG_BUFFS)} · 호스트 전용 {len(HOST_BUFFS)})')


if __name__ == '__main__':
    main()
