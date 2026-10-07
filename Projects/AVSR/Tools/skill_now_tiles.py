"""스킬 연출 지금 상태 — 녹화(skillfx_now/NN_key.mp4)에서 장면을 뽑아 몸마다 한 장 (2026-10-07).

시안 발주 때 「지금 게임 화면 · 캐릭터 모습」 앵커로 붙인다.
사용: python skill_now_tiles.py key1 key2 ...  → _exchange/ref/skill_now/{key}.png
"""
import glob
import os
import sys

import cv2
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
SRC = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'skillfx_now')
OUT = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'skill_now')


def tiles(key):
    hits = [p for p in glob.glob(os.path.join(SRC, '*_%s.mp4' % key)) if 'before' not in p]
    if not hits:
        print('없음', key)
        return
    cap = cv2.VideoCapture(hits[0])
    fps = cap.get(cv2.CAP_PROP_FPS) or 30
    n = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))
    dur = n / fps
    cuts = []
    for k in range(8):
        t = 0.3 + (dur - 0.6) * k / 7
        cap.set(cv2.CAP_PROP_POS_FRAMES, int(t * fps))
        ok, f = cap.read()
        if not ok:
            continue
        h, w = f.shape[:2]
        f = f[int(h * 0.08):int(h * 0.78)]          # HUD 아래 · 조작부 위 — 전투 구역만
        f = cv2.resize(f, (270, int(f.shape[0] * 270 / w)), interpolation=cv2.INTER_AREA)
        cv2.putText(f, '%.1fs' % t, (6, 22), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 2)
        cuts.append(f)
    rows = [np.hstack(cuts[i:i + 4]) for i in range(0, len(cuts), 4) if len(cuts[i:i + 4]) == 4]
    os.makedirs(OUT, exist_ok=True)
    cv2.imwrite(os.path.join(OUT, key + '.png'), np.vstack(rows))
    print(key)


if __name__ == '__main__':
    for k in sys.argv[1:]:
        tiles(k)
