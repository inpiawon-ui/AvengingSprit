"""호스트 12종 → 15종. 목업의 세 칸을 별개 캐릭터로 되살린다.

목업 호스트 목록은 3열 5행 = 15칸인데 기획에서 12칸으로 줄이면서
`람보(레이저)`·`마법사(녹색)`·`닌자(적색)` 을 "색상 변형 = 코스튬"으로 분리했다
(확정 #10). 그런데 목업을 확대해 보면 색만 다른 게 아니다 —
람보(기관총)은 갈색 머리·붉은 두건·올리브 군복이고, 람보(레이저)는
금발·파란 바이저·청색 군복에 레이저 라이플을 들었다. 마법사는 분홍이 여성,
녹색이 콧수염 난 남성이다. **다른 캐릭터다.** 15종으로 되돌린다.

표 순서 = 그리드 순서 = 해금 순서이므로, 세 종을 각자의 짝 바로 뒤에 끼운다.
기존 12종의 값은 하나도 건드리지 않는다.

⚠ 새 세 종의 스탯·공격 방식·얼티밋은 목업 그림과 짝 캐릭터에서 유도한 값이다.
   밸런스 확정 전까지는 잠정치다.
"""
import io
import os
import re
import shutil

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
HOST = os.path.join(ROOT, 'Assets', 'BundleResource', 'TableData', 'HostTable.asset')
ULT = os.path.join(ROOT, 'Assets', 'BundleResource', 'TableData', 'UltimateTable.asset')
UNIT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Unit')


def esc(s):
    """유니코드 이스케이프 — .asset 은 한글을 \\uXXXX 로 담는다."""
    return '"' + ''.join(f'\\u{ord(c):04X}' if ord(c) > 127 else c for c in s) + '"'


# (뒤에 끼울 기준 키, 새 항목)
NEW = [
    ('rambo', dict(
        key='rambo_laser', en='RAMBO LASER', kr='람보(레이저)', role='관통 레이저 사수',
        hp=70, atk=78, spd=65, dash=55,
        # 마법사도 관통이지만 느리고 강하다(간격 1.35 · 피해 1.75).
        # 레이저는 그 반대 — 빠르고 약하게 잡아 같은 관통이라도 체감이 갈린다.
        kind=4, shots=1, spread=0, rng=3.3, interval=0.7, dmg=0.9,
        slow=0, moveatk=0, target=0, tag=1, prio=3,
        ult='laser_storm', ch=1, stage=8)),
    ('wizard', dict(
        key='wizard_green', en='WIZARD GREEN', kr='마법사(녹색)', role='광역 자연 마법',
        hp=62, atk=80, spd=58, dash=50,
        # 닌자도 확산이지만 3발 16도다. 이쪽은 5발 34도 + 둔화 — 장판에 가깝다.
        kind=3, shots=5, spread=34, rng=3.0, interval=1.1, dmg=0.55,
        slow=20, moveatk=0, target=2, tag=3, prio=4,
        ult='verdant_gale', ch=1, stage=16)),
    ('ninja', dict(
        key='ninja_red', en='NINJA RED', kr='닌자(적색)', role='고속 표창 투척',
        hp=62, atk=70, spd=92, dash=95,
        # 청색 닌자는 3발 확산. 적색은 단발 고속 연사로 갈랐다.
        kind=2, shots=1, spread=0, rng=2.4, interval=0.45, dmg=0.42,
        slow=0, moveatk=1, target=1, tag=1, prio=4,
        ult='crimson_fang', ch=1, stage=25)),
]

ULTS = [
    ('laser_storm', 'LASER STORM', '레이저 스톰', '관통 광선을 전방으로 난사, 4초 지속'),
    ('verdant_gale', 'VERDANT GALE', '푸른 질풍', '가시 폭풍으로 주변을 묶고 지속 피해'),
    ('crimson_fang', 'CRIMSON FANG', '핏빛 송곳니', '잔상을 남기며 돌진, 경로의 적을 베어낸다'),
]


def host_block(d):
    return (f"  - _hostKey: {d['key']}\n"
            f"    _nameEn: {d['en']}\n"
            f"    _nameKr: {esc(d['kr'])}\n"
            f"    _role: {esc(d['role'])}\n"
            f"    _hp: {d['hp']}\n    _atk: {d['atk']}\n    _spd: {d['spd']}\n    _dash: {d['dash']}\n"
            f"    _attackKind: {d['kind']}\n    _shotCount: {d['shots']}\n"
            f"    _spreadDegrees: {d['spread']}\n    _rangeMul: {d['rng']}\n"
            f"    _intervalMul: {d['interval']}\n    _damageMul: {d['dmg']}\n"
            f"    _lifestealPercent: 0\n    _slowPercent: {d['slow']}\n"
            f"    _reflectsShots: 0\n    _moveAttack: {d['moveatk']}\n"
            f"    _targetType: {d['target']}\n    _tag: {d['tag']}\n"
            f"    _possessPriority: {d['prio']}\n"
            f"    _ultimateKey: {d['ult']}\n"
            f"    _unlockType: 1\n    _unlockChapter: {d['ch']}\n    _unlockStage: {d['stage']}\n")


def main():
    s = io.open(HOST, encoding='utf-8').read()
    for after, d in NEW:
        if f"_hostKey: {d['key']}" in s:
            print(f"  이미 있음: {d['key']}")
            continue
        m = re.search(r'  - _hostKey: %s\n.*?(?=  - _hostKey: |\Z)' % after, s, re.S)
        s = s[:m.end()] + host_block(d) + s[m.end():]
        print(f"  추가: {d['key']} ({d['kr']}) — {after} 뒤")
    io.open(HOST, 'w', encoding='utf-8', newline='').write(s)

    u = io.open(ULT, encoding='utf-8').read()
    for key, en, kr, desc in ULTS:
        if f'_ultimateKey: {key}' in u:
            continue
        u = u.rstrip('\n') + (f"\n  - _ultimateKey: {key}\n"
                              f"    _nameEn: {en}\n"
                              f"    _nameKr: {esc(kr)}\n"
                              f"    _description: {esc(desc)}\n")
        print(f"  얼티밋 추가: {key} ({en})")
    io.open(ULT, 'w', encoding='utf-8', newline='').write(u)

    # 그림이 올 때까지 짝 캐릭터 것을 임시로 쓴다. 안 그러면 적으로 나올 때
    # 스프라이트가 null 이라 **보이지 않는 적**이 된다.
    for after, d in NEW:
        dst = os.path.join(UNIT, d['key'])
        os.makedirs(dst, exist_ok=True)
        p = os.path.join(dst, f"unit_{d['key']}.png")
        if not os.path.exists(p):
            shutil.copy2(os.path.join(UNIT, after, f'unit_{after}.png'), p)
            print(f"  임시 스프라이트: unit_{d['key']}.png ← unit_{after}.png")


if __name__ == '__main__':
    main()
