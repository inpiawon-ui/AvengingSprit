"""UI 하네스 비교판 — 하네스 실행 폴더(report.json)를 읽어 상태마다 «시안 | 해상도별 게임» 한 줄을 만들고,
위반 부품에 상자를 그린다(빨강 = 실패, 주황 = 경고). 게임을 모른다 — 시안 폴더는 report.json 의 mockFolder.

사용:  uv run --no-project --with pillow python Tools/uiharness/contact_sheet.py <실행 폴더> [--height 360]
결과:  <실행 폴더>/sheets/<상태>.png  ·  <실행 폴더>/sheets/_all.png (전체 한 장)
"""
import argparse
import json
import os

from PIL import Image, ImageDraw, ImageFont

FAIL = (255, 40, 40)
WARN = (255, 160, 0)
SKIP_RULES = {"R3", "R4"}   # 리소스 크기 경고는 그림별 표로 본다 — 상자로 그리면 화면이 덮인다


def font(size):
    # 한글 라벨 — 기본 글꼴엔 한글이 없다. 없으면 기본 글꼴.
    for path in ("C:/Windows/Fonts/malgunbd.ttf", "C:/Windows/Fonts/malgun.ttf", "/System/Library/Fonts/AppleSDGothicNeo.ttc"):
        try:
            return ImageFont.truetype(path, size)
        except OSError:
            continue
    return ImageFont.load_default()


LABEL = font(14)


def load(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def draw_violations(img, shot, scale):
    d = ImageDraw.Draw(img)
    h = shot["height"]
    for v in shot["violations"]:
        if v["rule"] in SKIP_RULES:
            continue
        color = FAIL if v["severity"] == "Fail" else WARN
        x0, y0, x1, y1 = v["box"]
        # 하네스 좌표는 아래가 0 — 그림 좌표로 뒤집는다
        box = [x0 * scale, (h - y1) * scale, x1 * scale, (h - y0) * scale]
        d.rectangle(box, outline=color, width=3)
        d.text((box[0] + 2, max(0, box[1] - 16)), v["rule"], fill=color, font=LABEL)


def row_for(run, state, mock_dir, height):
    tiles = []
    if state.get("mock") and mock_dir:
        p = os.path.join(mock_dir, state["mock"] + ".png")
        if os.path.exists(p):
            m = Image.open(p).convert("RGB")
            tiles.append(("시안", m.resize((int(m.width * height / m.height), height))))
    for shot in state["shots"]:
        p = os.path.join(run, shot["file"])
        if not os.path.exists(p):
            continue
        g = Image.open(p).convert("RGB")
        scale = height / g.height
        g = g.resize((int(g.width * scale), height))
        draw_violations(g, shot, scale)
        fails = sum(1 for v in shot["violations"] if v["severity"] == "Fail" and v["rule"] not in SKIP_RULES)
        tiles.append((f"{shot['resolution']}  실패 {fails}", g))

    if not tiles:
        return None
    width = sum(t.width for _, t in tiles) + 10 * (len(tiles) - 1)
    row = Image.new("RGB", (width, height + 22), (20, 20, 24))
    d = ImageDraw.Draw(row)
    x = 0
    for label, t in tiles:
        row.paste(t, (x, 22))
        d.text((x + 4, 2), label, fill=(255, 255, 0), font=LABEL)
        x += t.width + 10
    d.text((width - 420, 2), state["name"] + ("  [도달 실패] " + state["error"] if state.get("error") else ""), fill=(255, 255, 255), font=LABEL)
    return row


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("run")
    ap.add_argument("--height", type=int, default=360)
    args = ap.parse_args()

    report = load(os.path.join(args.run, "report.json"))
    mock_dir = report.get("mockFolder")
    out = os.path.join(args.run, "sheets")
    os.makedirs(out, exist_ok=True)

    rows = []
    for state in report["states"]:
        row = row_for(args.run, state, mock_dir, args.height)
        if row is None:
            continue
        row.save(os.path.join(out, state["name"] + ".png"))
        rows.append(row)

    if rows:
        w = max(r.width for r in rows)
        sheet = Image.new("RGB", (w, sum(r.height for r in rows) + 8 * len(rows)), (0, 0, 0))
        y = 0
        for r in rows:
            sheet.paste(r, (0, y))
            y += r.height + 8
        sheet.save(os.path.join(out, "_all.png"))
    print(f"{len(rows)} rows -> {out}")


if __name__ == "__main__":
    main()
