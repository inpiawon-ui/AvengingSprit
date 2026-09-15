"""호스트 강제 실행 로그(fh_k*.txt)에서 공격 버튼 누름/뗌 구간별 효과음 횟수를 대비해 호스트 공격음을 판정한다.

공격 버튼은 6초 주기로 앞 3초만 누른다. 입력 반영 지연을 감안해 누름 구간을 0.1~3.3초로 본다.
누름 구간에서 뗌 구간보다 확연히 많은 소리가 그 호스트의 공격음이다.
사용법: python host_attack_contrast.py <로그 폴더>
"""
import collections
import glob
import os
import re
import sys

PERIOD = 6.0
ON_START, ON_END = 0.1, 3.3
RATIO = 4.0        # 누름/뗌 비율이 이 이상이고
MIN_COUNT = 20     # 누름 구간 횟수가 이 이상이면 공격음으로 판정

Q = re.compile(r"^Q ([\d.]+) cmd=02([0-9a-f]{2}) ret=([0-9a-f]{6}).* host=(\d+)")
H = re.compile(r"^H ([\d.]+) host=(\d+) stage=\d+ shot=(-?\d+)")
P = re.compile(r"^P ([\d.]+) shot=(\d+)")


def analyse(path):
    on, off, callers = collections.Counter(), collections.Counter(), collections.defaultdict(set)
    host_shots, progress = [], []
    for line in open(path):
        m = Q.match(line)
        if m:
            if int(m.group(4)) == 0:
                continue
            n = int(m.group(2), 16)
            n = n - 24 if n >= 40 else n
            if n == 0:
                continue
            phase = float(m.group(1)) % PERIOD
            (on if ON_START < phase < ON_END else off)[n] += 1
            callers[n].add(m.group(3))
            continue
        m = H.match(line)
        if m and int(m.group(2)) and int(m.group(3)) >= 0:
            host_shots.append(int(m.group(3)))
            continue
        m = P.match(line)
        if m:
            progress.append((float(m.group(1)), int(m.group(2))))
    attack = [n for n in on if on[n] >= MIN_COUNT and on[n] >= RATIO * max(off[n], 1)]
    return on, off, callers, attack, host_shots, progress


def main(folder):
    paths = sorted(glob.glob(os.path.join(folder, "fh_k*.txt")), key=lambda p: int(re.sub(r"\D", "", os.path.basename(p))))
    for path in paths:
        k = int(re.sub(r"\D", "", os.path.basename(path)))
        on, off, callers, attack, host_shots, progress = analyse(path)
        shots = host_shots[:1] + [s for t, s in progress if 20 <= t <= 50][:2]
        verdict = ", ".join(f"sfx_{n} ({on[n]}:{off[n]}, {'/'.join(sorted(callers[n]))})" for n in sorted(attack)) or "판정 불가"
        print(f"host {k:2d}: 공격음 {verdict}")
        print(f"          ON {dict(on.most_common(5))} | OFF {dict(off.most_common(5))} | shots {shots}")


if __name__ == "__main__":
    main(sys.argv[1])
