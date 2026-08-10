"""사거리 재조정 — 원거리 호스트가 근거리처럼 느껴진다는 피드백 반영.

원거리 ×3, 근거리 ×2 로 늘린다. 호스트마다 `_rangeMul` 을 곱하는 방식이라
호스트 간 상대적 차이(히트맨이 가장 길고 드래곤이 짧다)는 그대로 유지된다.

근거리로 묶는 것은 `Melee`(0) 와 `Pulse`(6) 다. Pulse 는 자기 주위 광역이라
사거리를 원거리처럼 늘리면 화면 전체를 훑는 장판이 되어 버린다.

⚠️ 적도 같은 프로필을 쓴다. `_rangeMul` 만 올리면 적 사거리도 함께 3배가 되어
   화면 밖에서 맞는 그림이 된다. 그래서 적 쪽 기준 사거리를 낮춰 상쇄한다 —
   적은 지금 느낌을 대체로 유지하고, 늘어난 사거리는 플레이어가 가져간다.
"""
import io
import os
import re

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
HOST = os.path.join(ROOT, 'Assets', 'BundleResource', 'TableData', 'HostTable.asset')

MELEE_KINDS = {0, 6}      # Melee · Pulse
RANGED_MUL = 3.0
MELEE_MUL = 2.0


def main():
    s = io.open(HOST, encoding='utf-8').read()
    out, last = [], 0

    for m in re.finditer(r'  - _hostKey: (\w+)(.*?)(?=  - _hostKey: |\Z)', s, re.S):
        key, block = m.group(1), m.group(2)
        kind = int(re.search(r'_attackKind: (\d+)', block).group(1))
        old = float(re.search(r'_rangeMul: ([\d.]+)', block).group(1))

        mul = MELEE_MUL if kind in MELEE_KINDS else RANGED_MUL
        new = round(old * mul, 3)
        block2 = re.sub(r'(_rangeMul: )[\d.]+', rf'\g<1>{new}', block, count=1)

        out.append(s[last:m.start()] + '  - _hostKey: ' + key + block2)
        last = m.end()
        print(f'  {key:<12}{"근거리" if kind in MELEE_KINDS else "원거리"}  '
              f'{old} → {new}   (호스트 {265 * old:.0f} → {265 * new:.0f})')

    io.open(HOST, 'w', encoding='utf-8').write(''.join(out) + s[last:])


if __name__ == '__main__':
    main()
