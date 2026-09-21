# -*- coding: utf-8 -*-
"""캐릭터 그림 **전수 검수**.

눈으로 38명 × 5방향 × 8동작 = 1520장을 볼 수는 없다. 숫자로 먼저 거른다.

재는 것 — 규격(발이 흔들리나 · 키가 들쭉날쭉한가)과 동작(움직인 양이 충분한가)
  발줄     : 잉크 맨 아랫줄. 프레임마다 다르면 걸을 때 위아래로 **떤다**
  발 중심  : 잉크 가로 중심. 다르면 좌우로 **미끄러진다**
  키       : 잉크 세로 길이. 들쭉날쭉하면 커졌다 작아졌다 한다
  걷기     : 걷기1 ↔ 걷기2 의 차이. 작으면 **다리가 안 움직이고 미끄러진다**
  피격     : 대기 ↔ 피격. 작으면 맞아도 티가 안 난다
  공격     : 공격1 ↔ 공격2. 작으면 «두 번 그린 같은 그림»이다
  죽음     : 쓰러짐2 의 뼈 비율. 이 게임의 죽음은 «뼈만 남는다»

쓰는 법:
  python unit_qa_all.py            # 전체
  python unit_qa_all.py amazon     # 한 명만
"""
import glob
import os
import sys

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
UNITS = os.path.join(ROOT, "Assets", "BaseResource", "Unit")
FACINGS = ["s", "se", "e", "ne", "n"]
ACTS = ["", "_walk1", "_walk2", "_atk1", "_atk2", "_hit", "_die1", "_die2"]

# ── 문턱 ────────────────────────────────────────────────────
#
# **다듬어 통과한 갱스터의 실측값**에서 잡았다(2026-09-21). 상상해서 정하지 않는다.
#   갱스터: 발줄 0~1 · 발중심 1.6~4.9 · 키 0~1
#           걷기 480~1011 · 피격 1479~2157 · 공격 667~2296 · 뼈 0.185~0.268
# 문턱은 그 **최솟값보다 살짝 아래**로 둔다 — 갱스터가 겨우 통과하는 선이 아니라,
# 「갱스터만큼은 된다」를 재는 선이어야 한다.

WALK_MIN = 450       # 걷기 두 장의 차이(픽셀). 아래면 다리가 안 움직이고 미끄러진다
HIT_MIN = 600
ATK_MIN = 600
BONE_MIN = 0.15      # 쓰러짐2 의 뼈 비율. 이 게임의 죽음은 «뼈만 남는다»
FOOT_TOL = 2         # 발줄(맨 아랫줄)이 이만큼 넘게 흔들리면 걸을 때 떤다
CENTER_TOL = 6       # 발 중심이 이만큼 넘게 흔들리면 좌우로 미끄러진다
HEIGHT_TOL = 8       # 키가 이만큼 넘게 들쭉날쭉하면 커졌다 작아졌다 한다


def ink(path):
    a = np.asarray(Image.open(path).convert("RGBA"))[..., 3]
    return a > 8


def box(mask):
    ys, xs = np.nonzero(mask)
    if len(xs) == 0:
        return None
    return xs.min(), xs.max(), ys.min(), ys.max()


def foot_center(mask):
    """**발**의 가로 중심. 실루엣 전체 중심을 쓰면 안 된다 —
    무기를 휘두르거나 팔을 뻗으면 중심이 통째로 밀려, 발은 제자리인데 «미끄러진다»로 잡힌다.
    맨 아랫줄에서 위로 10줄(발) 안의 잉크만 본다."""
    ys, xs = np.nonzero(mask)
    if len(xs) == 0:
        return None
    bottom = ys.max()
    sel = ys >= bottom - 9
    return float(xs[sel].mean())


def bone_ratio(path):
    px = np.asarray(Image.open(path).convert("RGBA"))
    a = px[..., 3] > 8
    if a.sum() == 0:
        return 0.0
    bone = a & (px[..., 0] > 170) & (px[..., 1] > 150) & (px[..., 2] > 120)
    return bone.sum() / a.sum()


def check(key, facing):
    """한 방향을 재서 문제 목록을 돌려준다."""
    folder = os.path.join(UNITS, key)
    paths = {act: os.path.join(folder, f"unit_{key}_{facing}{act}.png") for act in ACTS}
    if not all(os.path.exists(p) for p in paths.values()):
        return ["그림 없음"], {}

    masks = {act: ink(p) for act, p in paths.items()}
    stats, bad = {}, []

    # ── 규격 — 쓰러지는 두 장은 원래 눕는다. 서 있는 여섯 장만 본다
    stand = ["", "_walk1", "_walk2", "_atk1", "_atk2", "_hit"]
    feet, centers, heights = [], [], []
    for act in stand:
        b = box(masks[act])
        if b is None:
            bad.append(f"{act or 'idle'} 비었다")
            continue
        x0, x1, y0, y1 = b
        feet.append(y1)
        centers.append(foot_center(masks[act]))
        heights.append(y1 - y0 + 1)

    if feet:
        stats["발줄흔들림"] = max(feet) - min(feet)
        stats["발중심흔들림"] = round(max(centers) - min(centers), 1)
        stats["키차이"] = max(heights) - min(heights)
        if stats["발줄흔들림"] > FOOT_TOL:
            bad.append(f"발줄 {stats['발줄흔들림']}px 흔들림")
        if stats["발중심흔들림"] > CENTER_TOL:
            bad.append(f"좌우 {stats['발중심흔들림']}px 미끄러짐")
        if stats["키차이"] > HEIGHT_TOL:
            bad.append(f"키 {stats['키차이']}px 들쭉날쭉")

    # ── 동작
    stats["걷기"] = int((masks["_walk1"] ^ masks["_walk2"]).sum())
    stats["피격"] = int((masks[""] ^ masks["_hit"]).sum())
    stats["공격"] = int((masks["_atk1"] ^ masks["_atk2"]).sum())
    stats["뼈"] = round(bone_ratio(paths["_die2"]), 3)

    if stats["걷기"] < WALK_MIN:
        bad.append(f"걷기 {stats['걷기']} (다리가 안 움직인다)")
    if stats["피격"] < HIT_MIN:
        bad.append(f"피격 {stats['피격']}")
    if stats["공격"] < ATK_MIN:
        bad.append(f"공격 {stats['공격']}")
    if stats["뼈"] < BONE_MIN:
        bad.append(f"뼈 {stats['뼈']} (뼈가 안 남는다)")
    return bad, stats


def main():
    only = sys.argv[1] if len(sys.argv) > 1 else None
    keys = sorted(d for d in os.listdir(UNITS) if os.path.isdir(os.path.join(UNITS, d)))
    if only:
        keys = [k for k in keys if k == only]

    rows = []
    for key in keys:
        problems = {}
        for facing in FACINGS:
            bad, _ = check(key, facing)
            if bad and bad != ["그림 없음"]:
                problems[facing] = bad
            elif bad == ["그림 없음"]:
                problems[facing] = bad
        rows.append((key, problems))

    rows.sort(key=lambda r: -sum(len(v) for v in r[1].values()))
    total = 0
    for key, problems in rows:
        n = sum(len(v) for v in problems.values())
        total += n
        if n == 0:
            print(f"{key:18s} 이상 없음")
            continue
        print(f"{key:18s} 문제 {n}건")
        for facing, bad in problems.items():
            print(f"    {facing:3s} {' · '.join(bad)}")
    print(f"\n합계 {total}건 / 유닛 {len(rows)}명")


if __name__ == "__main__":
    main()
