"""호스트 12종에 기획서 A 3-3·4-3 필드를 채운다.

넣는 값은 셋이다.

  moveAttack       이동 중에도 쏘는가. **기본은 전부 꺼 둔다** — "멈춰야 쏜다"가
                   이 게임의 최상위 규칙이라, 예외를 남발하면 규칙이 죽는다.
                   근접형 둘만 켠다. 붙어서 때리는 쪽은 멈춰야 할 이유가
                   원래 약하고, 이동 사격이 곧 "달려들며 벤다"가 된다.

  targetType       누구를 먼저 때리는가. 같은 화력도 이걸 바꾸면 교전이 달라진다.
                   저격·단발처럼 한 방이 무거운 쪽은 마무리(LowestHp),
                   관통·광역처럼 여럿을 훑는 쪽은 탱커부터(HighestHp).

  possessPriority  유령이 빙의 대상을 고를 때의 선호. 높을수록 먼저 잡힌다.
                   **판을 뒤집는 몸일수록 높게** 준다 — 급할 때 무엇이 잡히는지가
                   곧 그 순간의 선택지다.
"""
import io
import os
import re

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
ASSET = os.path.join(ROOT, 'Assets', 'BundleResource', 'TableData', 'HostTable.asset')

# 호스트 → (moveAttack, targetType, possessPriority)
#   targetType: 0 Nearest / 1 LowestHp / 2 HighestHp
SPEC = {
    'amazoness':  (1, 0, 2),   # 고속 근거리 — 달려들며 벤다
    'rambo':      (0, 0, 3),   # 연사, 무난한 주력
    'wizard':     (0, 2, 4),   # 관통 — 앞뒤로 겹친 탱커부터 뚫는다
    'ninja':      (1, 1, 4),   # 확산 + 기동 — 마무리에 강하다
    'mafia':      (0, 0, 3),   # 광각 확산
    'hitman':     (0, 1, 5),   # 저격 한 방 — 낮은 체력부터 끊는다
    'yogamaster': (0, 2, 3),   # 자기 주위 광역 — 붙은 큰 놈부터
    'dragon':     (0, 2, 5),   # 브레스 — 판을 여는 몸이라 우선순위를 높게
    'robot':      (0, 2, 4),   # 관통
    'snowwoman':  (0, 1, 4),   # 단발 둔화 — 끊어 먹기
    'slugger':    (1, 0, 3),   # 근접 반사 — 붙어서 튕겨낸다
    'vampire':    (0, 1, 5),   # 흡혈 — 생존이 걸린 몸이라 높게
}

FIELDS = ('_moveAttack', '_targetType', '_possessPriority')


def main():
    s = io.open(ASSET, encoding='utf-8').read()
    added = 0

    for key, (move, target, pri) in SPEC.items():
        # 이 호스트 항목의 범위를 찾는다 — 다음 `- _hostKey:` 직전까지
        m = re.search(rf'^  - _hostKey: {key}$', s, re.M)
        if not m:
            print(f'  없음 {key}')
            continue
        nxt = re.search(r'^  - _hostKey: ', s[m.end():], re.M)
        end = m.end() + (nxt.start() if nxt else len(s) - m.end())
        block = s[m.start():end]

        if all(f in block for f in FIELDS):
            continue

        # `_reflectsShots` 바로 뒤에 끼워 넣는다 — 공격 방식 묶음의 끝이다
        new = re.sub(r'(    _reflectsShots: \d+\n)',
                     rf'\1    _moveAttack: {move}\n'
                     rf'    _targetType: {target}\n'
                     rf'    _possessPriority: {pri}\n',
                     block, count=1)
        if new == block:
            print(f'  삽입 지점 못 찾음 {key}')
            continue
        s = s[:m.start()] + new + s[end:]
        added += 1

    io.open(ASSET, 'w', encoding='utf-8').write(s)
    print(f'호스트 {added}종에 A-2 필드 삽입')


if __name__ == '__main__':
    main()
