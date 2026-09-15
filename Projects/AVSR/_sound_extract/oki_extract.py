"""OKI MSM6295 샘플 ROM에서 원본 ADPCM 샘플을 MAME와 동일한 디코딩으로 WAV 추출한다."""
import hashlib
import sys
import wave
from pathlib import Path

SAMPLE_RATE = 4_000_000 // 165  # PIN7_HIGH, 4MHz -> 24242Hz
NUM_ENTRIES = 128
INDEX_SHIFT = [-1, -1, -1, -1, 2, 4, 6, 8]
NIBBLE_BITS = [
    [1 if (n & 8) == 0 else -1, (n >> 2) & 1, (n >> 1) & 1, n & 1] for n in range(16)
]


def build_diff_lookup():
    table = []
    for step in range(49):
        step_val = int(16.0 * (11.0 / 10.0) ** step)
        for n in range(16):
            sign, b2, b1, b0 = NIBBLE_BITS[n]
            table.append(sign * (step_val * b2 + step_val // 2 * b1 + step_val // 4 * b0 + step_val // 8))
    return table


DIFF = build_diff_lookup()


def decode(rom, start, end):
    signal, step = 0, 0
    out = bytearray()
    for addr in range(start, end + 1):
        byte = rom[addr]
        for nibble in (byte >> 4, byte & 15):
            signal = max(-2048, min(2047, signal + DIFF[step * 16 + nibble]))
            step = max(0, min(48, step + INDEX_SHIFT[nibble & 7]))
            out += int(signal * 16).to_bytes(2, "little", signed=True)
    return bytes(out)


def read_entries(rom):
    mask = len(rom) - 1
    for idx in range(1, NUM_ENTRIES):
        o = idx * 8
        # 0xFF로 채워진 항목이 테이블 끝이다. 뒤는 샘플 데이터라 테이블로 읽으면 쓰레기가 나온다.
        if rom[o:o + 6] == b"\xff" * 6:
            return
        start = int.from_bytes(rom[o:o + 3], "big") & mask
        end = int.from_bytes(rom[o + 3:o + 6], "big") & mask
        # 헤더 크기는 ROM마다 다르다(이 게임은 0x200). 테이블 영역만 피하면 된다.
        if 0x10 <= start < end < len(rom):
            yield idx, start, end


def main(rom_path, out_dir, prefix):
    rom = Path(rom_path).read_bytes()
    out = Path(out_dir)
    out.mkdir(parents=True, exist_ok=True)
    seen = {}
    for idx, start, end in read_entries(rom):
        pcm = decode(rom, start, end)
        digest = hashlib.md5(rom[start:end + 1]).hexdigest()
        dup = seen.setdefault(digest, idx)
        name = out / f"{prefix}_{idx:03d}.wav"
        with wave.open(str(name), "wb") as w:
            w.setnchannels(1)
            w.setsampwidth(2)
            w.setframerate(SAMPLE_RATE)
            w.writeframes(pcm)
        secs = len(pcm) / 2 / SAMPLE_RATE
        note = "" if dup == idx else f" DUP_OF {dup:03d}"
        print(f"{prefix} {idx:03d} start={start:05x} end={end:05x} {secs:6.2f}s{note}")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3])
