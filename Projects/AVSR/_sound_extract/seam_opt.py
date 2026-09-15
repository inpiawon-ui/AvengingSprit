"""루프 이음매가 가장 매끄러운 지점으로 루프 시작을 옮긴다.

주기가 맞는 구간 안에서는 어디서 잘라도 루프 길이가 같으므로, 시작점을 뒤로 밀며
'루프 끝 다음에 원래 이어지던 소리'와 '되돌아가 재생할 소리'가 가장 닮은 곳을 고른다.
사용법: python seam_opt.py <입력 plan JSON> <출력 plan JSON>
"""
import json
import sys
import wave

import numpy as np

SEAM_WINDOW = 960       # 20ms @48kHz
SEARCH_SEC = 20.0
STEP = 48               # 1ms
LOOP_DRIFT = 16         # 루프 길이 미세 보정 허용(샘플)


def pcm(path):
    with wave.open(path) as w:
        return np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).astype(np.float64), w.getframerate()


def ncc(a, b):
    d = np.linalg.norm(a) * np.linalg.norm(b)
    return float(np.dot(a, b) / d) if d else 0.0


def seam_score(src, start, loop):
    end = start + loop
    return ncc(src[end:end + SEAM_WINDOW], src[start:start + SEAM_WINDOW])


def optimise(src, rate, start, loop):
    best = (seam_score(src, start, loop), start, loop)
    limit = min(start + int(SEARCH_SEC * rate), len(src) - loop - LOOP_DRIFT - SEAM_WINDOW)
    for s in range(start, limit, STEP):
        head = src[s:s + SEAM_WINDOW]
        head_norm = np.linalg.norm(head)
        if head_norm == 0:
            continue
        for d in range(-LOOP_DRIFT, LOOP_DRIFT + 1):
            tail = src[s + loop + d:s + loop + d + SEAM_WINDOW]
            denom = head_norm * np.linalg.norm(tail)
            if denom and np.dot(head, tail) / denom > best[0]:
                best = (float(np.dot(head, tail) / denom), s, loop + d)
    return best


def main(plan_in, plan_out):
    plan = json.load(open(plan_in))
    for item in plan["items"]:
        src, rate = pcm(item["src"])
        start, loop = item["loop"]["startSamples"], item["loop"]["loopSamples"]
        before = seam_score(src, start, loop)
        score, new_start, new_loop = optimise(src, rate, start, loop)
        item["loop"] = {"startSamples": new_start, "loopSamples": new_loop}
        item["seamNcc"] = round(score, 4)
        print(f"{item['dst']:16s} seam {before:.4f} -> {score:.4f}  start {start / rate:7.3f}s -> {new_start / rate:7.3f}s"
              f"  loop {loop} -> {new_loop} ({new_loop - loop:+d})")
    json.dump(plan, open(plan_out, "w"), indent=2)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])