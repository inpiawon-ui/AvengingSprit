"""대략적인 루프 길이(초)를 받아 샘플 단위로 보정하고, 그 길이로 끝까지 반복되는 가장 이른 시작점을 찾는다.

사용법: python loop_refine.py <wav>=<대략초> [<wav>=<대략초> ...]
"""
import json
import sys

import numpy as np

# verify_loop2 는 임포트 시 sys.argv 를 처리하므로 잠시 비워 둔다
_saved_argv, sys.argv = sys.argv, sys.argv[:1]
import verify_loop2 as base  # noqa: E402
sys.argv = _saved_argv

SEARCH_SEC = 0.3
REF_SEC = 8.0


def refine_decimated(xd, fs, approx_sec):
    lag0 = int(round(approx_sec * fs))
    m = int(REF_SEC * fs)
    ref_pos = len(xd) - m - int(fs)
    lo = max(0, ref_pos - lag0 - int(SEARCH_SEC * fs))
    hi = ref_pos - lag0 + int(SEARCH_SEC * fs)
    if hi <= lo:
        raise ValueError("녹음이 루프 한 바퀴보다 짧다")
    corr = base.ncc_scan(xd[lo:hi + m], xd[ref_pos:ref_pos + m])
    q = lo + int(np.argmax(corr))
    return ref_pos - q, float(corr.max())


def analyse(path, approx_sec):
    full, xd, rate = base.load(path)
    fs = rate / base.DECIM
    lag, peak = refine_decimated(xd, fs, approx_sec)
    result = dict(file=path, approxSec=approx_sec, peakNcc=round(peak, 4))
    v = base.verify(xd, fs, lag)
    if v is None:
        return dict(result, verified=False, reason="끝부분부터 일치하는 창이 없음")
    start, matched, worst = v
    loop = base.refine_lag(full, lag * base.DECIM, start * base.DECIM, rate)
    start_samples = start * base.DECIM
    return dict(result, verified=matched >= 0.95 * lag, coverage=round(matched / lag, 2), worstNcc=round(worst, 4),
                loopSamples=int(loop), startSamples=int(start_samples), loopSec=round(loop / rate, 4),
                startSec=round(start_samples / rate, 4), endSamples=int(start_samples + loop), rate=rate,
                recordedSamples=len(full))


def main(args):
    for arg in args:
        path, approx = arg.rsplit("=", 1)
        print(json.dumps(analyse(path, float(approx)), ensure_ascii=False))


if __name__ == "__main__":
    main(sys.argv[1:])
