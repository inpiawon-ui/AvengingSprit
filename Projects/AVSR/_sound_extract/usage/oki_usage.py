"""OKI 명령 추적 로그에서 샘플별 사용 곡과 곡별 사용 샘플을 집계한다.

OKI MSM6295 명령: 0x80|샘플번호 다음 워드가 채널(상위 니블 1/2/4/8)·감쇠(하위 니블).
비트 7이 꺼진 워드(상위 니블 채널 비트)는 정지 명령이다.
사용법: python oki_usage.py <oki_trace.txt>
"""
import collections
import sys

CHANNEL = {0x1: 1, 0x2: 2, 0x4: 3, 0x8: 4}


def parse(path):
    per_sample = collections.defaultdict(collections.Counter)   # (chip, sample) -> song -> count
    channels = collections.defaultdict(set)
    per_song = collections.defaultdict(collections.Counter)     # song -> (chip, sample) -> count
    pending = {}
    for line in open(path):
        p = line.split()
        if not p or p[0] != "W":
            continue
        song, chip, data = int(p[1]), int(p[2]), int(p[4], 16) & 0xff
        if chip in pending:
            sample = pending.pop(chip)
            ch = CHANNEL.get(data >> 4)
            per_sample[(chip, sample)][song] += 1
            per_song[song][(chip, sample)] += 1
            if ch:
                channels[(chip, sample)].add(ch)
            continue
        if data & 0x80:
            pending[chip] = data & 0x7f
    return per_sample, channels, per_song


def main(path):
    per_sample, channels, per_song = parse(path)
    print("=== sample -> songs (song: plays) ===")
    for chip in (1, 2):
        for s in range(1, 23 if chip == 2 else 16):
            uses = per_sample.get((chip, s))
            name = f"oki{chip}_{s:03d}"
            if not uses:
                print(f"  {name}: 사용 안 됨")
                continue
            bgm = {k: v for k, v in sorted(uses.items()) if k <= 12}
            sfx = {k: v for k, v in sorted(uses.items()) if k >= 16}
            print(f"  {name}: ch{sorted(channels[(chip, s)])} BGM{bgm} SFX{sfx}")
    print("=== song -> samples ===")
    for song in sorted(per_song):
        print(f"  {song:2d}: " + ", ".join(f"oki{c}_{s:03d}x{n}" for (c, s), n in sorted(per_song[song].items())))


if __name__ == "__main__":
    main(sys.argv[1])