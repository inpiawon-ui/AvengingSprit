# -*- coding: utf-8 -*-
"""여러 납품 판에서 **칸별로 제일 좋은 것**을 골라 한 벌을 만든다.

한 판을 다시 받을 때마다 지난번 통과한 칸까지 같이 바뀐다. 전부 통과할 때까지 돌리면
끝이 없으므로, 판을 모아 두고 칸마다 가장 좋은 것을 고른다(자르기 · 고르기는 그림 그리기가 아니다).

점수 기준 — 지금 게임 그림이 내는 «움직인 양» 에 얼마나 가까운가.
  걷기1 · 걷기2 : 두 장의 차이 (미끄러지듯 걷지 않게)
  피격 · 공격1  : 대기와의 차이
  공격2        : 공격1과의 차이 (반동이 보이게)
  쓰러짐2      : 뼈 비율 (이 게임의 죽음은 «뼈만 남는다»)
  대기 · 쓰러짐1: 점수를 못 매긴다 — 가장 점수가 높은 판의 것을 따라간다

쓰는 법:
  python unit_up_pick.py <키> <방향> <판1> <판2> ... [--apply]
  --apply 를 주면 Assets/BaseResource/Unit/{키}/ 에 덮어쓴다(원본은 _exchange/ref/char_up/old/ 에 남긴다).
"""
import shutil
import subprocess
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
TOOLS = Path(__file__).resolve().parent
CUT = ROOT / "Projects/AVSR/_exchange/ref/char_up/cut"
PICK = ROOT / "Projects/AVSR/_exchange/ref/char_up/pick"
OLD = ROOT / "Projects/AVSR/_exchange/ref/char_up/old"
ACTS = ["", "_walk1", "_walk2", "_atk1", "_atk2", "_hit", "_die1", "_die2"]


def ink(path):
    return np.asarray(Image.open(path).convert("RGBA"))[..., 3] > 8


def bone_ratio(path):
    px = np.asarray(Image.open(path).convert("RGBA"))
    a = px[..., 3] > 8
    bone = a & (px[..., 0] > 170) & (px[..., 1] > 150) & (px[..., 2] > 120)
    return bone.sum() / max(1, a.sum())


def main():
    key, facing = sys.argv[1], sys.argv[2]
    sheets = [Path(p) for p in sys.argv[3:] if not p.startswith("--")]
    apply = "--apply" in sys.argv

    takes = {}
    for sheet in sheets:
        subprocess.run([sys.executable, str(TOOLS / "unit_up_cut.py"), key, facing, str(sheet)],
                       check=True, capture_output=True)
        dst = CUT.parent / ("cut_" + sheet.stem)
        if dst.exists():
            shutil.rmtree(dst)
        shutil.copytree(CUT, dst)
        takes[sheet.stem] = dst

    scores = {}   # 판 → {동작: 점수}
    for name, folder in takes.items():
        f = lambda act: folder / f"unit_{key}_{facing}{act}.png"
        s = {}
        s["_walk1"] = s["_walk2"] = int((ink(f("_walk1")) ^ ink(f("_walk2"))).sum())
        s["_hit"] = int((ink(f("")) ^ ink(f("_hit"))).sum())
        s["_atk1"] = int((ink(f("")) ^ ink(f("_atk1"))).sum())
        s["_atk2"] = int((ink(f("_atk1")) ^ ink(f("_atk2"))).sum())
        s["_die2"] = int(bone_ratio(f("_die2")) * 10000)
        scores[name] = s

    best_overall = max(scores, key=lambda n: sum(scores[n].values()))
    PICK.mkdir(parents=True, exist_ok=True)
    chosen = {}
    for act in ACTS:
        if act in ("", "_die1"):
            pick = best_overall
        else:
            pick = max(scores, key=lambda n: scores[n][act])
        chosen[act] = pick
        shutil.copy2(takes[pick] / f"unit_{key}_{facing}{act}.png",
                     PICK / f"unit_{key}_{facing}{act}.png")
        note = f"{scores[pick][act]}" if act in scores[pick] else "기준 없음 — 총점 1위 판"
        print(f"{act or 'idle':8s} ← {pick}  ({note})")

    print("고른 판:", PICK)
    if apply:
        game = ROOT / "Assets/BaseResource/Unit" / key
        OLD.mkdir(parents=True, exist_ok=True)
        for act in ACTS:
            name = f"unit_{key}_{facing}{act}.png"
            if (game / name).exists() and not (OLD / name).exists():
                shutil.copy2(game / name, OLD / name)   # 되돌릴 수 있게
            shutil.copy2(PICK / name, game / name)
        print("게임에 반영:", game, "(원본은", OLD, ")")


if __name__ == "__main__":
    main()
