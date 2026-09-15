"""루프 후보마다 겹치는 전 구간을 창 단위로 대조해 검증된 최소 루프 길이와 시작점을 찾는다."""
import json, sys, wave
import numpy as np

DECIM = 4
REF_SEC = 8.0
MIN_LOOP_SEC = 5.0
WIN_SEC = 5.0
MATCH = 0.97
SILENT_RMS = 20.0


def load(path):
    with wave.open(path) as w:
        rate = w.getframerate()
        a = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).astype(np.float64)
    n = len(a) // DECIM * DECIM
    return a, a[:n].reshape(-1, DECIM).mean(axis=1), rate


def ncc_scan(x, ref):
    m = len(ref)
    size = 1 << int(np.ceil(np.log2(len(x) + m)))
    num = np.fft.irfft(np.fft.rfft(x, size) * np.conj(np.fft.rfft(ref, size)), size)[:len(x) - m + 1]
    c = np.concatenate(([0.0], np.cumsum(x * x)))
    energy = np.sqrt(np.maximum(c[m:] - c[:-m], 1e-9))
    return num / (energy * np.linalg.norm(ref))


def win_ncc(a, b):
    ra, rb = np.sqrt((a * a).mean()), np.sqrt((b * b).mean())
    if ra < SILENT_RMS and rb < SILENT_RMS:
        return 1.0
    d = np.linalg.norm(a) * np.linalg.norm(b)
    return float(np.dot(a, b) / d) if d > 0 else 0.0


def candidates(xd, fs):
    m = int(REF_SEC * fs)
    ref_pos = len(xd) - m - int(fs)
    corr = ncc_scan(xd[:ref_pos + m], xd[ref_pos:ref_pos + m])
    lags = ref_pos - np.arange(len(corr))
    min_lag = int(MIN_LOOP_SEC * fs)
    peaks = [i for i in range(1, len(corr) - 1)
             if corr[i] > 0.9 and corr[i] >= corr[i - 1] and corr[i] >= corr[i + 1] and lags[i] >= min_lag]
    chosen = []
    for i in sorted(peaks, key=lambda i: -corr[i]):
        if all(abs(lags[i] - c) > int(0.5 * fs) for c in chosen):
            chosen.append(int(lags[i]))
    return sorted(chosen)


def verify(xd, fs, lag):
    w = int(WIN_SEC * fs)
    positions = list(range(0, len(xd) - lag - w + 1, w))
    scores = [win_ncc(xd[p:p + w], xd[p + lag:p + lag + w]) for p in positions]
    k = len(scores)
    while k > 0 and scores[k - 1] >= MATCH:
        k -= 1
    if k == len(scores):
        return None
    start = positions[k]
    step, w1 = int(0.01 * fs), int(fs)
    while start - step >= 0 and win_ncc(xd[start - step:start - step + w1], xd[start - step + lag:start - step + lag + w1]) >= 0.99:
        start -= step
    return start, (len(xd) - lag) - start, min(scores[k:])


def refine_lag(full, lag_full, start_full, rate):
    seg = int(2 * rate)
    s = start_full + int(rate)
    base = full[s:s + seg]
    best = max(range(-8, 9), key=lambda d: np.dot(base, full[s + lag_full + d:s + lag_full + d + seg]))
    return lag_full + best


def analyse(path):
    full, xd, rate = load(path)
    fs = rate / DECIM
    fallback = None
    accepted = []
    for lag in candidates(xd, fs):
        v = verify(xd, fs, lag)
        if v is None:
            continue
        start, matched, worst = v
        if matched >= 0.95 * lag:
            accepted.append((round(start / fs), lag, start, worst))
        elif fallback is None or matched / lag > fallback[1]:
            fallback = (lag, matched / lag)
    if accepted:
        # 진짜 루프는 인트로 직후부터 끝까지 주기적이다 → 가장 이른 시작점, 그중 가장 짧은 길이
        _, lag, start, worst = min(accepted)
        loop = refine_lag(full, lag * DECIM, start * DECIM, rate)
        others = sorted({round(a[1] / fs, 2) for a in accepted})[:5]
        return dict(file=path.split("\\")[-1], verified=True, loopSamples=loop, startSamples=start * DECIM,
                    loopSec=round(loop / rate, 3), startSec=round(start / fs, 3),
                    loopsInRec=round((len(xd) - start) / lag, 2), worstNcc=round(worst, 4), rate=rate,
                    acceptedLags=others)
    return dict(file=path.split("\\")[-1], verified=False,
                bestLagSec=None if fallback is None else round(fallback[0] / fs, 3),
                coverage=None if fallback is None else round(fallback[1], 2))


for p in sys.argv[1:]:
    print(json.dumps(analyse(p)))
