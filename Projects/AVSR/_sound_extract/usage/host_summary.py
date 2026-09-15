"""usage_logger.lua 로그(k_s*.txt)에서 호스트 번호별 스냅샷과 효과음별 (호출 위치·객체 종류·호스트) 분포를 요약한다."""
import collections, glob, os, re, sys
# 사용법: python host_summary.py <로그 폴더>
SP = sys.argv[1]
q = re.compile(r"^Q ([\d.]+) cmd=([0-9a-f]{4}) ret=([0-9a-f]{6}) stage=(\d+) area=(\d+) shot=(-?\d+) a0=\w+ a5=\w+ a6=\w+ tA6=([0-9a-f]{4}) tA2=([0-9a-f]{4}) host=(\d+)")
h = re.compile(r"^H ([\d.]+) host=(\d+) stage=(\d+) shot=(-?\d+)")
hosts = collections.defaultdict(list)
sfx = collections.defaultdict(lambda: collections.Counter())
shots = collections.defaultdict(list)
for path in sorted(glob.glob(os.path.join(SP, "k_s*.txt"))):
    run = os.path.basename(path)[:-4]
    for line in open(path):
        m = h.match(line)
        if m:
            if int(m.group(4)) >= 0 and len(hosts[int(m.group(2))]) < 4:
                hosts[int(m.group(2))].append(f"{run}#{m.group(4)}")
            continue
        m = q.match(line)
        if not m:
            continue
        cmd = int(m.group(2), 16)
        if cmd >> 8 != 2 or (cmd & 0xff) == 0:
            continue
        lo = cmd & 0xff
        lo = lo - 24 if lo >= 40 else lo
        key = (lo, m.group(3), m.group(7), int(m.group(9)))
        sfx[lo][key[1:]] += 1
        if int(m.group(6)) >= 0 and len(shots[key]) < 2:
            shots[key].append(f"{run}#{m.group(6)}")
print("=== host kind -> snapshots ===")
for k in sorted(hosts):
    print(f"  host {k:2d}: {hosts[k]}")
print("=== sfx -> (caller, typeA6, host) ===")
for lo in sorted(sfx):
    print(f"sfx {lo}:")
    for (ret, t, host), n in sfx[lo].most_common(14):
        print(f"    @{ret} tA6={t} host={host:2d} x{n:5d} shots={shots[(lo, ret, t, host)]}")