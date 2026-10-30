# 배경 남은 것(10-30) 납품 검수 · 정리 — 납품(_exchange/in/bgfix_*.png)을 게임 이름으로 bg_apply/ 아래에 두고 비교 그림을 만든다
# 결과: bg_apply/holding_1030/ · bg_apply/apron_fix_1030/ (게임 파일 이름) · 각 폴더 compare_*.png
# 쓰는 법: python Projects/AVSR/Tools/bg_fix_review_1030.py
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[3]
EX = ROOT / "Projects/AVSR/_exchange"
IN = EX / "in"
AP = EX / "bg_apply"
RF = ROOT / "Assets/BundleResource/RoomFloor"

STAGES = ["junkyard", "missile", "street", "rooftop", "lab", "refinery"]
BOSSES = ["guardian", "kingpin", "python", "robot_snakes", "sludge"]


def floor_of(tag):
    if tag.startswith("boss_"):
        return RF / f"roomfloor_{tag[5:]}.png"
    if tag == "holding":
        new = AP / "holding_1030/roomfloor_env_holding.png"
        return new if new.exists() else RF / "roomfloor_env_holding.png"
    return AP / f"{tag}/roomfloor_env_{tag}.png"


def now_apron(tag):
    if tag.startswith("boss_"):
        return RF / f"roomapron_boss_{tag[5:]}.png"
    if tag == "holding":
        return RF / "roomapron_holding.png"
    return AP / f"{tag}/roomapron_{tag}.png"


def game_name(tag):
    return f"roomapron_boss_{tag[5:]}.png" if tag.startswith("boss_") else f"roomapron_{tag}.png"


def stack(floor: Path, apron: Image.Image) -> Image.Image:
    """바닥 아래 300 px + 방 밖 위 300 px — 게임에서 이어 붙는 모습."""
    f = Image.open(floor).convert("RGB")
    o = Image.new("RGB", (720, 600))
    o.paste(f.crop((0, f.height - 300, 720, f.height)), (0, 0))
    o.paste(apron.convert("RGB").crop((0, 0, 720, 300)), (0, 300))
    return o


def label(im, text):
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, 8 * len(text) + 8, 16), fill=(0, 0, 0))
    d.text((4, 2), text, fill=(255, 255, 0))
    return im


def fix_size(im, w, h, name, notes):
    if im.size != (w, h):
        notes.append(f"{name}: 납품 {im.size[0]}x{im.size[1]} → {w}x{h} 로 맞춤(LANCZOS 아님 · 최근접)")
        im = im.resize((w, h), Image.NEAREST)
    return im


def main():
    notes = []
    (AP / "holding_1030").mkdir(exist_ok=True)
    (AP / "apron_fix_1030").mkdir(exist_ok=True)

    # 대기실 바닥
    src = IN / "bgfix_holding_floor.png"
    if src.exists():
        im = fix_size(Image.open(src).convert("RGB"), 720, 1152, "holding_floor", notes)
        im.save(AP / "holding_1030/roomfloor_env_holding.png")
        now = Image.open(RF / "roomfloor_env_holding.png").convert("RGB")
        ref = Image.open(AP / "junkyard/roomfloor_env_junkyard.png").convert("RGB")
        c = Image.new("RGB", (1080, 576))
        for i, (t, a) in enumerate((("now", now), ("new", im), ("style ref junkyard", ref))):
            c.paste(label(a.resize((360, 576), Image.NEAREST), t), (i * 360, 0))
        c.save(AP / "holding_1030/compare_holding_floor.png")

    tags = ["holding"] + STAGES + [s + "v2" for s in STAGES] + ["boss_" + b for b in BOSSES]
    rows = []
    for tag in tags:
        src = IN / f"bgfix_{tag}_apron.png"
        if not src.exists():
            continue
        new = fix_size(Image.open(src).convert("RGB"), 720, 540, f"{tag}_apron", notes)
        folder = AP / ("holding_1030" if tag == "holding" else "apron_fix_1030")
        new.save(folder / game_name(tag))
        old = Image.open(now_apron(tag)).convert("RGB")
        row = Image.new("RGB", (1440, 600 + 270), (20, 20, 20))
        row.paste(label(stack(floor_of(tag), old), f"{tag} now"), (0, 0))
        row.paste(label(stack(floor_of(tag), new), f"{tag} new"), (720, 0))
        row.paste(old.resize((360, 270), Image.NEAREST), (0, 600))
        row.paste(new.resize((360, 270), Image.NEAREST), (720, 600))
        row.save(folder / f"compare_{tag}_apron.png")
        rows.append(row.resize((720, 435)))
    if rows:
        sheet = Image.new("RGB", (1440, 435 * ((len(rows) + 1) // 2)))
        for i, r in enumerate(rows):
            sheet.paste(r, ((i % 2) * 720, (i // 2) * 435))
        sheet.save(AP / "apron_fix_1030/compare_all.png")
    print("정리", len(rows), "장 · 메모", notes)


if __name__ == "__main__":
    main()
