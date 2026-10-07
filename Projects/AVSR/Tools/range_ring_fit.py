"""사거리 원 그림 앉히기 (2026-10-07).

in/range_ring.png(마젠타 바탕의 흰 원) → 바탕을 투명하게 → 512 x 512 로 줄여
Assets/BaseResource/InGameMainUI/range_ring.png 에 둔다. 흰 선 가운데의 반지름 비(그림 반 폭 = 1)를 재서 출력한다
— BattleDirector.RangeRing 의 RangeRingLineRatio 에 넣는다.

흰 선 · 안쪽 흰빛은 마젠타 위에 그려져 섞여 있다. 「얼마나 흰가」를 알파로 삼고 색은 순백으로 둔다(마젠타 기운이 남지 않게).
"""
import os
import numpy as np
from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
SRC = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in', 'range_ring.png')
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI', 'range_ring.png')
SIZE = 512


def main():
    a = np.asarray(Image.open(SRC).convert('RGB')).astype(np.float32)
    h, w = a.shape[:2]
    bg = np.mean([a[4, 4], a[4, w - 5], a[h - 5, 4], a[h - 5, w - 5]], axis=0)   # 마젠타(255, 0, 255) 쯤
    # 흰색(255,255,255)과 바탕 사이 어디쯤인가 — 초록 채널이 가장 크게 갈린다(바탕 0 · 흰색 255)
    t = np.clip((a[..., 1] - bg[1]) / (255.0 - bg[1]), 0, 1)
    rgba = np.dstack([np.full((h, w), 255.0), np.full((h, w), 255.0), np.full((h, w), 255.0), t * 255.0]).astype(np.uint8)
    im = Image.fromarray(rgba, 'RGBA')

    # 선 가운데 반지름 — 가운데에서 바깥으로 가며 알파가 가장 진한 곳(네 방향 평균)
    cy, cx = h / 2.0, w / 2.0
    radii = []
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        prof = [t[int(cy + dy * r), int(cx + dx * r)] for r in range(int(min(cx, cy)) - 1)]
        radii.append(int(np.argmax(prof)))
    ratio = float(np.mean(radii)) / (w / 2.0)

    im = im.resize((SIZE, SIZE), Image.LANCZOS)
    im.save(OUT)
    print(os.path.relpath(OUT, ROOT), im.size, '선 반지름', radii, '비율 %.4f' % ratio)


if __name__ == '__main__':
    main()
