"""게임 캡처로 팝업 글자를 잰다 — 넘침 · 쏠림 · 연출 가림 (2026-10-08, PD 「제대로 확인」).

게임에서 같은 순간을 세 장 찍는다(유니티 검사 장치 — 이 파일 아래 주석):
  A = 그대로 · C = 연출만 끔 · D = 글자와 연출을 다 끔 · rects.tsv = 노드 화면 사각형
글자 잉크 = |C − D| (연출이 없는 두 장이라 움직이는 빛이 안 섞인다).
  넘침 : 잉크가 제 칸 밖(24px 안, 다른 글자 칸 제외)에 있다
  쏠림 : 가운데 정렬 칸에서 잉크 가운데와 칸 가운데가 3px 이상 다르다
  가림 : 잉크 픽셀 중 연출이 바꾼(|A − C|) 비율이 3% 이상
python audit_popup_text.py {캡처 폴더}
"""
import glob
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import fx_preview as P  # noqa: E402

PANEL = {'levelup': 'BuffChoicePanel', 'altar': 'ShrinePanel', 'devil': 'EventPanel', 'devilnc': 'EventPanel',
         'shop': 'ShopPanel', 'clear': 'ChapterResultPopup', 'clearok': 'ChapterResultPopup'}
INK, FX = 60, 40          # 잉크 · 연출 차이 문턱(RGB 합)
SHIFT, COVER, SPILL = 3, 0.03, 20


def load(p):
    return np.asarray(Image.open(p).convert('RGB')).astype(np.int32)


def main(folder):
    rows = []
    for rp in sorted(glob.glob(os.path.join(folder, '*_rects.tsv'))):
        key = os.path.basename(rp)[:-len('_rects.tsv')]
        A, C, D = (load(os.path.join(folder, key + s)) for s in ('_A.png', '_C.png', '_D.png'))
        H, W = A.shape[:2]
        ink = np.abs(C - D).sum(axis=2) > INK
        fx = np.abs(A - C).sum(axis=2) > FX
        # 노드 사각형은 배치 표에서(창 = 720x1280 화면 그대로). rects.tsv 는 캔버스 좌표라 쓰지 않는다
        panel = PANEL[key.split('_', 1)[1]]
        nodes = []
        for n in P.LAYOUT.values():
            if n['panel'] != panel or not n['role'] or n['skip'] or n['hide']:
                continue
            # 비어 있거나 숨는 칸 — 옆 칸 잉크가 걸려 잘못 잡힌다(대가 없는 거래의 대가 칸 · 구매 결과 줄 · 구매 제한 줄)
            if n['name'] in ('ShopResultText', 'ShopLimitText') or (n['name'] == 'EventCostText' and key.endswith('devilnc')):
                continue
            if n['name'].startswith('ResultChest') and key.endswith('clear'):
                continue   # 상자 칸이 가득 찬 판 — 상자 줄이 숨는다
            x, y, w, h = P.rect(n['name'])
            if n['name'] == 'EventRewardText' and key.endswith('devilnc'):
                y += (P.rect('EventCostPill')[1] - P.rect('EventRewardPill')[1]) // 2   # 대가 없는 거래 — 코드가 아래로 내린다
            align = 'Left' if n['align'] == 'left' else ''
            nodes.append((n['name'], x, y, x + w, y + h, align))
        allt = np.zeros((H, W), bool)
        for _, x0, y0, x1, y1, _ in nodes:
            allt[max(0, y0):y1, max(0, x0):x1] = True
        for name, x0, y0, x1, y1, align in nodes:
            inside = ink[y0:y1, x0:x1]
            if inside.sum() < 8:
                continue
            m = 24
            ring = ink[max(0, y0 - m):y1 + m, max(0, x0 - m):x1 + m] & ~allt[max(0, y0 - m):y1 + m, max(0, x0 - m):x1 + m]
            spill = int(ring.sum())
            ys, xs = np.where(inside)
            iy0, iy1, ix0, ix1 = ys.min(), ys.max(), xs.min(), xs.max()
            dy = (iy0 + iy1) / 2 - (y1 - y0 - 1) / 2
            dx = (ix0 + ix1) / 2 - (x1 - x0 - 1) / 2
            vcenter = not any(a in align for a in ('Top', 'Bottom', 'Baseline'))
            hcenter = 'Left' not in align and 'Right' not in align
            cover = float((inside & fx[y0:y1, x0:x1]).sum()) / max(1, int(inside.sum()))
            issues = []
            if spill > SPILL:
                issues.append('넘침 %dpx' % spill)
            if vcenter and abs(dy) >= SHIFT:
                issues.append('위아래 %+.1f' % dy)
            if hcenter and abs(dx) >= SHIFT:
                issues.append('좌우 %+.1f' % dx)
            if cover >= COVER:
                issues.append('연출 가림 %d%%' % round(cover * 100))
            rows.append((key, name, issues, (x0, y0, x1 - x0, y1 - y0), (ix0, iy0, ix1 - ix0 + 1, iy1 - iy0 + 1)))
    bad = [r for r in rows if r[2]]
    print('검사 글자 칸 %d · 문제 %d' % (len(rows), len(bad)))
    for key, name, issues, box, inkb in bad:
        print('%-12s %-24s %s  칸%s 잉크%s' % (key, name, ' · '.join(issues), box, inkb))
    return bad


if __name__ == '__main__':
    main(sys.argv[1])
