# -*- coding: utf-8 -*-
"""로비 v3 글자 보정 — 시안 글자와 게임 글자의 잉크를 재서 자리 · 크기 · 굵기를 고친다.

쓰는 법:
  python lobby_calib.py <글자 있는 스샷> <글자 뺀 스샷> [--apply]

  시안 잉크  = |시안 - 글자 지운 시안(debug_clean.png)|
  게임 잉크  = |글자 있는 스샷 - 글자 뺀 스샷|
두 잉크의 테두리 상자 · 잉크 양을 비교해 `lobby_v3_calib.json` 의 dx · dy · scale · dilate 를 누적한다.
--apply 가 없으면 재기만 한다.
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
REF = ROOT / "Projects/AVSR/_exchange/ref/lobby_v3"
SPEC = ROOT / "Assets/BaseResource/LobbyV3/lobby_v3_spec.json"
CALIB = ROOT / "Assets/BaseResource/LobbyV3/lobby_v3_calib.json"
THRESH = 60
SUBPIXEL = "--subpixel" in sys.argv

# 내용이 시안과 다른 칸(데이터로 채움) — 높이 · 세로 자리만 맞춘다
HEIGHT_ONLY = set()
# 시안 전용 이름 → 게임에서 같은 자리에 뜨는 칸이 없는 것
SKIP = {"_time2", "_label2", "_cost2"}


def load(p, size):
    im = Image.open(p).convert("RGB")
    if im.size != size:
        im = im.resize(size, Image.LANCZOS)
    return np.asarray(im).astype(np.int16)


def ink(a, b, box, others=()):
    x0, y0, x1, y1 = box
    d = np.abs(a[y0:y1, x0:x1] - b[y0:y1, x0:x1]).max(axis=2)
    # 이웃 글자 칸은 뺀다 — 위아래로 붙은 글자(가격 · 즉시 열기)가 섞이면 크기가 틀리게 잡힌다
    for ox0, oy0, ox1, oy1 in others:
        d[max(0, oy0 - y0):max(0, oy1 - y0), max(0, ox0 - x0):max(0, ox1 - x0)] = 0
    m = d > THRESH
    red = (a[y0:y1, x0:x1, 0] > 150) & (a[y0:y1, x0:x1, 1] < 90) & (a[y0:y1, x0:x1, 2] < 90)
    m &= ~red
    # 외톨이 점 제거 — 이웃 없는 점은 잡음
    n = np.zeros_like(m, dtype=np.int16)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            if dx or dy:
                n += np.roll(np.roll(m, dy, 0), dx, 1)
    m &= n >= 2
    # 작은 얼룩(판 메움 자국 · 아이콘 가장자리)은 글자가 아니다
    import cv2
    cnt, lab, stats, _ = cv2.connectedComponentsWithStats(m.astype(np.uint8), connectivity=8)
    for i in range(1, cnt):
        if stats[i, cv2.CC_STAT_AREA] < 6:
            m[lab == i] = False
    ys, xs = np.nonzero(m)
    if len(xs) < 8:
        return None
    return dict(x0=x0 + xs.min(), x1=x0 + xs.max() + 1, y0=y0 + ys.min(), y1=y0 + ys.max() + 1,
                count=int(m.sum()), weight=float(d[m].sum()) / 255.0, img=np.where(m, d, 0).astype(np.float32))


def main():
    shot, bare = sys.argv[1], sys.argv[2]
    apply = "--apply" in sys.argv
    spec = json.loads(SPEC.read_text(encoding="utf-8"))
    mock_img = Image.open(REF / "mockup_full.png").convert("RGB")
    size = mock_img.size
    mock = np.asarray(mock_img).astype(np.int16)
    clean = load(REF / "debug_clean.png", size)
    # 상자 칸 속 글자는 부품(글자 지운 판) + 아이콘을 시안 위에 얹은 것을 기준으로 — 지운 그림 전체는 아이콘 · 띠까지 지워 잉크가 섞인다
    chest_ref = mock_img.copy()
    lv3 = ROOT / "Assets/BaseResource/LobbyV3"
    for p in spec["parts"] + spec["icons"]:
        if p["name"].startswith("chest_"):
            continue
        im = Image.open(lv3 / (p["name"] + ".png")).convert("RGBA")
        x0, y0, x1, y1 = p["box"]
        if im.size != (x1 - x0, y1 - y0):
            im = im.resize((x1 - x0, y1 - y0), Image.LANCZOS)
        chest_ref.alpha_composite(im, (x0, y0)) if chest_ref.mode == "RGBA" else chest_ref.paste(im, (x0, y0), im)
    chest_ref = np.asarray(chest_ref.convert("RGB")).astype(np.int16)
    a = load(shot, size)
    b = load(bare, size)

    calib = {"items": []}
    if CALIB.exists():
        calib = json.loads(CALIB.read_text(encoding="utf-8"))
    by = {c["name"]: c for c in calib["items"]}

    worst = 0.0
    rows = []
    for t in spec["texts"]:
        name = t["name"]
        if name in SKIP:
            continue
        ax0, ay0, ax1, ay1 = t["area"]
        bx0, by0, bx1, by1 = t["box"]
        if t["align"] == "C":
            reg = (ax0 - 4, by0 - 3, ax1 + 4, by1 + 3)
        else:
            reg = (bx0 - 2, by0 - 3, max(ax1, bx1) + 4, by1 + 3)
        others = [u["box"] for u in spec["texts"] if u["name"] != name and u["name"] not in SKIP
                  and not (u["box"][2] <= reg[0] or u["box"][0] >= reg[2] or u["box"][3] <= reg[1] or u["box"][1] >= reg[3])]
        m = ink(mock, chest_ref if name.startswith("_") else clean, reg, others)
        o = ink(a, b, reg, others)
        if m is None or o is None:
            rows.append(f"{name:24s} 잉크 없음 m={m is not None} o={o is not None}")
            continue
        mw, mh = m["x1"] - m["x0"], m["y1"] - m["y0"]
        ow, oh = o["x1"] - o["x0"], o["y1"] - o["y0"]
        if name in HEIGHT_ONLY:
            s = mh / oh
            dx = 0.0
        else:
            s = (mw / ow * mh / oh) ** 0.5
            if t["align"] == "C":
                dx = (m["x0"] + m["x1"]) / 2 - (o["x0"] + o["x1"]) / 2
            else:
                dx = m["x0"] - o["x0"]
        dy = (m["y0"] + m["y1"]) / 2 - (o["y0"] + o["y1"]) / 2
        # 크기가 맞았으면 잉크 무게중심으로 잰다 — 테두리 상자는 정수라 ±1px 에서 왔다 갔다 한다
        if abs(mw / ow - 1) < 0.03 and abs(mh / oh - 1) < 0.05:
            def centroid(im):
                ys_, xs_ = np.mgrid[0:im.shape[0], 0:im.shape[1]]
                w_ = im.sum()
                return (xs_ * im).sum() / w_, (ys_ * im).sum() / w_
            mcx, mcy = centroid(m["img"]); ocx, ocy = centroid(o["img"])
            cdx, cdy = mcx - ocx, mcy - ocy
            # 글꼴이 다른 글자(▶ 등)는 무게중심이 치우친다 — 테두리 상자 값과 1.2px 넘게 다르면 상자 값을 쓴다
            if abs(cdx - dx) <= 1.2 and abs(cdy - dy) <= 1.2:
                dx, dy = 0.7 * cdx, 0.7 * cdy
        # 1px 안쪽은 테두리 상자로는 못 잰다 — 잉크 그림끼리 위상 상관으로 소수점까지 잰다
        if SUBPIXEL and abs(dx) <= 1.5 and abs(dy) <= 1.5 and abs(mw / ow - 1) < 0.03:
            import cv2
            (px, py), resp = cv2.phaseCorrelate(o["img"], m["img"])
            # 테두리 상자 값과 1px 넘게 다르면 믿지 않는다(작은 글자는 상관이 흔들린다). 믿어도 절반만 간다
            if resp > 0.2 and abs(px - dx) <= 1.0 and abs(py - dy) <= 1.0:
                dx, dy = 0.5 * px, 0.5 * py
        # 굵기 — 크기를 맞춘 뒤의 잉크 양 비교
        r = 1.0 if name in HEIGHT_ONLY else m["weight"] / (o["weight"] * s * s)
        err = max(abs(dx), abs(dy), abs(s - 1) * max(mw, mh), abs(r - 1) * 10)
        worst = max(worst, err)
        rows.append(f"{name:24s} dx {dx:+5.1f} dy {dy:+5.1f} 크기 {s:5.3f} 굵기 {r:5.3f}  "
                    f"시안 {mw}x{mh} 게임 {ow}x{oh}")
        if apply and by.get(name, {}).get("lock"):
            continue   # 손으로 찾은 값 — 자동 보정이 흔들지 않게(골드 숫자: 시안 글꼴과 모양이 달라 상자 값이 ±1px 에서 오간다)
        if apply:
            c = by.setdefault(name, {"name": name, "dx": 0.0, "dy": 0.0, "scale": 1.0, "dilate": 0.0})
            c.setdefault("aspect", 1.0)
            sh, sw = mh / oh, mw / ow
            # 높이로 글자 크기, 폭/높이로 가로 비율 — 시안 글꼴이 조금 좁아 한 폰트로는 둘 다 못 맞춘다. 1px 흔들림을 줄이려 0.8 만 간다
            c["scale"] = round(c["scale"] * sh ** 0.8, 4)
            c["aspect"] = round(c["aspect"] * (sw / sh) ** 0.8, 4)
            c["dx"] = round(c["dx"] + dx, 2)
            c["dy"] = round(c["dy"] + dy, 2)
            if name not in HEIGHT_ONLY:
                c["dilate"] = round(float(np.clip(c["dilate"] + 0.35 * (r - 1), -0.5, 0.5)), 3)
    print("\n".join(rows))
    print(f"가장 큰 어긋남 {worst:.1f}")
    if apply:
        calib["items"] = list(by.values())
        CALIB.write_text(json.dumps(calib, ensure_ascii=False, indent=1), encoding="utf-8")
        print("보정표 갱신:", CALIB)


if __name__ == "__main__":
    main()













