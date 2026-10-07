"""영상의 한 구역만 잘라 구간 장면을 한 장으로 (2026-10-07) — 스킬 이펙트를 크게 보려고.

사용: python video_crop_tiles.py 영상.mp4 출력.png 시작초 끝초 간격초 x0 y0 x1 y1 [열 수] [컷 폭]
  자리(x0 y0 x1 y1)는 720 x 1280 기준 px — 영상 크기가 달라도 비율로 맞춘다.
"""
import sys
import cv2
import numpy as np


def main():
    src, out = sys.argv[1], sys.argv[2]
    t0, t1, step = float(sys.argv[3]), float(sys.argv[4]), float(sys.argv[5])
    x0, y0, x1, y1 = (int(v) for v in sys.argv[6:10])
    cols = int(sys.argv[10]) if len(sys.argv) > 10 else 6
    width = int(sys.argv[11]) if len(sys.argv) > 11 else 300
    cap = cv2.VideoCapture(src)
    fps = cap.get(cv2.CAP_PROP_FPS) or 30
    cuts = []
    t = t0
    while t <= t1 + 1e-6:
        cap.set(cv2.CAP_PROP_POS_FRAMES, int(round(t * fps)))
        ok, f = cap.read()
        if ok:
            sx, sy = f.shape[1] / 720.0, f.shape[0] / 1280.0
            f = f[int(y0 * sy):int(y1 * sy), int(x0 * sx):int(x1 * sx)]
            h = int(f.shape[0] * width / f.shape[1])
            f = cv2.resize(f, (width, h), interpolation=cv2.INTER_AREA)
            cv2.putText(f, f'{t:.2f}s', (6, 22), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 2)
            cuts.append(f)
        t += step
    rows = []
    for i in range(0, len(cuts), cols):
        row = cuts[i:i + cols]
        while len(row) < cols:
            row.append(np.zeros_like(cuts[0]))
        rows.append(np.hstack(row))
    sheet = np.vstack(rows)
    cv2.imwrite(out, sheet)
    print(out, sheet.shape)


if __name__ == '__main__':
    main()
