"""스킬 연출 영상 자동 점검 (2026-10-07).

영상마다 0.5초 간격으로 장면을 뽑아 두 가지를 센다.
  - 흰 네모: 순수 흰색(그림이 안 올라온 몸)의 큰 덩어리가 있는 장면 수
  - 컷인: 스킬 컷인 띠(화면 가운데 어두운 가로 띠 위 글자)가 보이는 장면 수 — 0 이면 스킬이 안 나갔다
눈으로 훑기 전에 기계로 먼저 거른다. 사용: python skillfx_check.py
"""
import os, glob, subprocess, tempfile
import numpy as np
import cv2

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
DIR = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'skillfx_now')


def frames(path, fps=2):
    with tempfile.TemporaryDirectory() as tmp:
        subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', path, '-vf', f'fps={fps}', os.path.join(tmp, '%03d.png')], check=False)
        return [cv2.imread(os.path.join(tmp, f)) for f in sorted(os.listdir(tmp))]


def white_boxes(img):
    """전장 영역(위 HUD · 아래 조작부 제외)에서 거의 순수 흰색 덩어리 수(넓이 250px 이상)."""
    h = img.shape[0]
    field = img[int(h * 0.11):int(h * 0.62)]
    m = ((field > 245).all(axis=2)).astype(np.uint8)
    n, _, stats, _ = cv2.connectedComponentsWithStats(m)
    # 그림 없는 몸은 테두리가 칼같은 꽉 찬 사각형이다 — 빛 구슬 · 섬광은 가장자리가 번져 채움 비율이 낮다
    cnt = 0
    for i in range(1, n):
        x, y, bw, bh, area = stats[i]
        if bw >= 24 and bh >= 24 and 0.6 < bw / bh < 1.7 and area / (bw * bh) > 0.92:
            cnt += 1
    return cnt


def cutin(img):
    """컷인 띠 — 화면 높이 30~42% 구간이 어둡게 깔리고(평균 밝기 낮음) 가운데에 밝은 글자가 있다(실측 2026-10-07)."""
    h, w = img.shape[:2]
    band = img[int(h * 0.30):int(h * 0.42), int(w * 0.35):int(w * 0.75)]
    g = cv2.cvtColor(band, cv2.COLOR_BGR2GRAY)
    return g.mean() < 60 and (g > 200).mean() > 0.008


def main():
    bad = 0
    for v in sorted(glob.glob(os.path.join(DIR, '*.mp4'))):
        fs = [f for f in frames(v) if f is not None]
        wb = sum(1 for f in fs if white_boxes(f) > 0)
        ci = sum(1 for f in fs if cutin(f))
        ok = wb == 0 and ci > 0
        bad += not ok
        print(f"{'OK ' if ok else '확인'} {os.path.basename(v):26s} 장면 {len(fs):2d} · 흰네모 {wb:2d} · 컷인 {ci:2d}")
    print('문제', bad)


if __name__ == '__main__':
    main()
