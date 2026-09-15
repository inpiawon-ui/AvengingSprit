"""MAME 전곡 녹음 WAV를 스케줄 로그대로 잘라 곡별 모노 WAV로 저장하고 길이·루프 여부를 보고한다."""
import sys
import wave
from pathlib import Path

import numpy as np

SILENCE = 48          # 디지털 무음 판정 진폭(16bit)
TAIL_WINDOW = 5.0     # 정지 직전 이 구간에 소리가 있으면 '계속 재생 중(루프 추정)'


def load_mono(path):
    with wave.open(str(path)) as w:
        ch, rate = w.getnchannels(), w.getframerate()
        data = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).reshape(-1, ch)
    return data[:, 0].copy(), rate


def read_schedule(path):
    starts, spans = {}, []
    for line in Path(path).read_text().splitlines():
        parts = line.split()
        if parts[0] == "START":
            starts[int(parts[1])] = float(parts[2])
        elif parts[0] == "STOP":
            n = int(parts[1])
            spans.append((n, starts[n], float(parts[2])))
    return spans


def main(wav_path, sched_path, out_dir):
    mono, rate = load_mono(wav_path)
    out = Path(out_dir)
    out.mkdir(parents=True, exist_ok=True)
    for n, t0, t1 in read_schedule(sched_path):
        seg = mono[int(t0 * rate):int(t1 * rate)]
        loud = np.flatnonzero(np.abs(seg) > SILENCE)
        if loud.size == 0:
            print(f"N={n:3d} SILENT")
            continue
        first, last = loud[0], loud[-1]
        still_playing = last > len(seg) - int(TAIL_WINDOW * rate)
        clip = seg[first:last + 1]
        with wave.open(str(out / f"song_{n:03d}.wav"), "wb") as w:
            w.setnchannels(1)
            w.setsampwidth(2)
            w.setframerate(rate)
            w.writeframes(clip.tobytes())
        tag = "PLAYING_AT_STOP" if still_playing else "ENDED"
        print(f"N={n:3d} lead={first / rate:5.2f}s len={len(clip) / rate:7.2f}s peak={int(np.abs(clip).max())} {tag}")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3])
