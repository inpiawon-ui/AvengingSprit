"""메인 ROM에서 스크립트 사운드 명령(F030 xx, 0044 xx)을 찾고 가장 가까운 스크립트 참조 코드를 붙인다."""
import bisect, collections, re, sys
# 사용법: python scan_scripts.py <작업 폴더: main_05even.bin(메인 ROM 짝홀 합본) · main.asm(unidasm 출력)>
SP = sys.argv[1]
rom = open(SP + r"\main_05even.bin", "rb").read()

def cpu(off):
    return off if off < 0x40000 else off - 0x40000 + 0x80000

def word(off):
    return int.from_bytes(rom[off:off + 2], "big")

# 코드의 즉치 주소 참조 (#$xxxxx) 수집
refs = collections.defaultdict(list)
imm = re.compile(r"#\$([0-9a-f]{4,6})\b")
for line in open(SP + r"\main.asm", encoding="utf-8", errors="ignore"):
    m = re.match(r"^([0-9a-f]{5}):", line)
    if not m:
        continue
    for v in imm.findall(line):
        val = int(v, 16)
        if 0x400 <= val < 0x40000 or 0x80000 <= val < 0xc0000:
            refs[val].append(line[:72].rstrip())
ref_addrs = sorted(refs)

# 데이터 속 롱 포인터 수집
ptrs = collections.defaultdict(list)
for off in range(0, len(rom) - 4, 2):
    val = int.from_bytes(rom[off:off + 4], "big")
    if 0x400 <= val < 0x40000 or 0x80000 <= val < 0xc0000:
        ptrs[val].append(cpu(off))
ptr_addrs = sorted(ptrs)

def nearest(sorted_list, addr, window=0x300):
    i = bisect.bisect_right(sorted_list, addr) - 1
    if i >= 0 and addr - sorted_list[i] <= window:
        return sorted_list[i]
    return None

def report(tag, opcode, valid):
    hits = collections.defaultdict(list)
    for off in range(0, len(rom) - 4, 2):
        if word(off) == opcode:
            arg = word(off + 2)
            if valid(arg):
                hits[arg].append(cpu(off))
    print(f"=== {tag} ===")
    for arg in sorted(hits):
        for addr in hits[arg][:6]:
            c = nearest(ref_addrs, addr)
            p = nearest(ptr_addrs, addr)
            code = refs[c][0] if c is not None else "-"
            data = f"ptr {p:06x} from {[hex(x) for x in ptrs[p][:3]]}" if p is not None else "-"
            print(f"  id {arg:2d} ({arg:#04x}) @ {addr:06x} | code {c and hex(c)}: {code} | {data}")
        if len(hits[arg]) > 6:
            print(f"  id {arg:2d}: +{len(hits[arg]) - 6} more")

report("anim script F030 + id", 0xF030, lambda a: 0x10 <= a <= 0x26 or 1 <= a <= 0x0c)
report("scene script 0044 + id", 0x0044, lambda a: 1 <= a <= 0x0c or 0x10 <= a <= 0x26)