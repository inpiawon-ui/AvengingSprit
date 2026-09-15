"""A/B 스냅샷 쌍(적을 64픽셀 옮기기 전·후)의 차이로 적 스프라이트를 오려 종류 번호별 몽타주를 만든다.

사용법: python body_pairs.py <작업 폴더>
"""
import collections
import glob
import os
import re
import sys

import numpy as np
from PIL import Image, ImageDraw

DIFF_THRESHOLD = 60      # RGB 합 차이
MIN_PIXELS = 40          # 이보다 작으면 화면 밖·가려짐으로 본다
MOVE_PX = 64
PER_KIND = 4
SCALE = 3
CELL = (96, 96)          # 오려낼 영역(원본 픽셀)


def pairs_from_log(path):
    pending = None
    for line in open(path, encoding="utf-8", errors="ignore"):
        m = re.match(r"^I ([\d.]+) kind=(\d+) obj=\w+ shotA=(\d+)", line)
        if m:
            pending = (int(m.group(2)), int(m.group(3)))
            continue
        m = re.match(r"^J shotB=(\d+)", line)
        if m and pending:
            yield pending[0], pending[1], int(m.group(1))
            pending = None


def crop_original(a, b):
    diff = np.abs(a - b).sum(axis=2)
    diff[190:, :48] = 0
    # 옮긴 적만 남긴다: A 에서 바뀐 픽셀이 B 에서 정확히 64픽셀 오른쪽에 같은 색으로 나타나야 한다.
    # 다른 캐릭터의 애니메이션·탄 같은 우연한 변화는 이 조건을 만족하지 않는다.
    shifted = np.zeros_like(a)
    shifted[:, MOVE_PX:] = b[:, :-MOVE_PX]
    same_after_move = np.abs(a - shifted).sum(axis=2) < 24
    mask = (diff > DIFF_THRESHOLD) & same_after_move
    mask[:, :0] = False
    ys, xs = np.nonzero(mask)
    if ys.size < MIN_PIXELS:
        return None, 0
    cy, cx = int(np.median(ys)), int(np.median(xs))
    h, w = CELL
    y0, x0 = max(cy - h // 2, 0), max(cx - w // 2, 0)
    y1, x1 = min(y0 + h, a.shape[0]), min(x0 + w, a.shape[1])
    return a[y0:y1, x0:x1].astype(np.uint8), int(ys.size)


def main(folder, prefix="body_s"):
    by_kind = collections.defaultdict(list)
    for log in sorted(p for p in glob.glob(os.path.join(folder, f"{prefix}*.txt")) if ".std" not in p):
        run = os.path.basename(log)[:-4]
        for kind, sa, sb in pairs_from_log(log):
            pa = os.path.join(folder, f"snap_{run}", "avspirit", f"{sa:04d}.png")
            pb = os.path.join(folder, f"snap_{run}", "avspirit", f"{sb:04d}.png")
            if not (os.path.exists(pa) and os.path.exists(pb)):
                continue
            a = np.asarray(Image.open(pa).convert("RGB")).astype(np.int16)
            b = np.asarray(Image.open(pb).convert("RGB")).astype(np.int16)
            crop, score = crop_original(a, b)
            if crop is not None:
                by_kind[kind].append((score, run, sa, crop))
    kinds = sorted(by_kind)
    print("kinds captured:", {k: len(by_kind[k]) for k in kinds})
    cw, ch = CELL[1] * SCALE, CELL[0] * SCALE
    sheet = Image.new("RGB", (60 + PER_KIND * (cw + 6), max(len(kinds), 1) * (ch + 6)), (30, 30, 30))
    d = ImageDraw.Draw(sheet)
    for row, kind in enumerate(kinds):
        best = sorted(by_kind[kind], key=lambda r: -r[0])[:PER_KIND]
        y = row * (ch + 6)
        d.text((6, y + ch // 2), f"#{kind}", fill=(255, 255, 0))
        for col, (score, run, sa, crop) in enumerate(best):
            img = Image.fromarray(crop).resize((crop.shape[1] * SCALE, crop.shape[0] * SCALE), Image.NEAREST)
            sheet.paste(img, (60 + col * (cw + 6), y))
            d.text((60 + col * (cw + 6) + 4, y + 4), f"{run}#{sa}", fill=(0, 255, 0))
    out = os.path.join(folder, f"{prefix}kinds.png")
    sheet.save(out)
    print(out, sheet.size)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else "body_s")
