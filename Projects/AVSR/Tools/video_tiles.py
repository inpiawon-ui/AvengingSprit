"""영상에서 구간 장면을 뽑아 한 장으로 (2026-10-07) — ffmpeg 가 막혔을 때 OpenCV 로 읽는다.

사용: python video_tiles.py 영상.mp4 출력.png 시작초 끝초 간격초 [열 수] [컷 폭]
"""
import sys
import cv2
import numpy as np


def main():
    src, out = sys.argv[1], sys.argv[2]
    t0, t1, step = float(sys.argv[3]), float(sys.argv[4]), float(sys.argv[5])
    cols = int(sys.argv[6]) if len(sys.argv) > 6 else 8
    width = int(sys.argv[7]) if len(sys.argv) > 7 else 240
    cap = cv2.VideoCapture(src)
    fps = cap.get(cv2.CAP_PROP_FPS) or 30
    cuts = []
    t = t0
    while t <= t1 + 1e-6:
        cap.set(cv2.CAP_PROP_POS_FRAMES, int(round(t * fps)))
        ok, f = cap.read()
        if ok:
            h = int(f.shape[0] * width / f.shape[1])
            f = cv2.resize(f, (width, h), interpolation=cv2.INTER_AREA)
            cv2.putText(f, f'{t:.2f}s', (6, 20), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (255, 255, 255), 2)
            cuts.append(f)
        t += step
    if not cuts:
        raise SystemExit('장면 없음')
    rows = (len(cuts) + cols - 1) // cols
    h, w = cuts[0].shape[:2]
    sheet = np.zeros((rows * h, cols * w, 3), np.uint8)
    for i, c in enumerate(cuts):
        sheet[(i // cols) * h:(i // cols + 1) * h, (i % cols) * w:(i % cols + 1) * w] = c
    cv2.imwrite(out, sheet)
    print(out, sheet.shape)


if __name__ == '__main__':
    main()
