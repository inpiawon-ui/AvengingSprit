# 인게임 배경 심플화 — 납품(_exchange/in/bg_*)을 게임 규격으로 맞춰 bg_apply/<무대>/ 에 모으고 비교 시트를 만든다 (PD 2026-10-29)
# 그림을 새로 그리지 않는다: 분홍 바탕 뚫기 · 덩어리 자르기 · 크기 맞춤만 한다.
# 쓰는 법: python Projects/AVSR/Tools/bg_simple_apply.py
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[3]
EX = ROOT / "Projects/AVSR/_exchange"
IN = EX / "in"
OUT = EX / "bg_apply"
FLOOR_DIR = ROOT / "Assets/BundleResource/RoomFloor"
BLOCK_DIR = ROOT / "Assets/BaseResource/InGameMainUI"

STAGES = ["junkyard", "missile", "street", "rooftop", "lab", "refinery"]
MOCK = {"junkyard": "mock_simple3_ch1.png"}
MOCK.update({s: f"mock_simple2_ch{i}.png" for i, s in enumerate(STAGES[1:], 2)})
MOCK_V2 = {s: f"mock_simple_obj_ch{i}.png" for i, s in enumerate(STAGES, 7)}
BLOCK_W, BLOCK_H = 72, 102


def fit_exact(src: Path, size, dst: Path):
    im = Image.open(src).convert("RGB")
    if im.size != size:
        # 비율이 다르면 가운데를 잘라 비율을 맞춘 뒤 줄인다(늘이거나 찌그러뜨리지 않는다)
        sw, sh = im.size
        tr = size[0] / size[1]
        if sw / sh > tr:
            nw = round(sh * tr)
            im = im.crop(((sw - nw) // 2, 0, (sw - nw) // 2 + nw, sh))
        elif sw / sh < tr:
            nh = round(sw / tr)
            im = im.crop((0, (sh - nh) // 2, sw, (sh - nh) // 2 + nh))
        im = im.resize(size, Image.LANCZOS)
    dst.parent.mkdir(parents=True, exist_ok=True)
    im.save(dst)
    return im


def pink_mask(a: np.ndarray) -> np.ndarray:
    r, g, b = a[..., 0].astype(int), a[..., 1].astype(int), a[..., 2].astype(int)
    # 분홍 바탕과 가장자리 번짐(분홍이 섞인 색)
    return (r > 150) & (b > 150) & (g < 110) & (abs(r - b) < 90)


def cut_blocks(src: Path, n: int):
    a = np.array(Image.open(src).convert("RGB"))
    fg = ~pink_mask(a)
    # 가장자리 줄의 분홍 섞인 점(보라 테두리로 보인다)을 겉에서부터 네 겹까지 벗긴다 — 속의 보라 · 네온은 건드리지 않는다
    r, g, b = (a[..., i].astype(int) for i in range(3))
    # 어두운 자홍(116,5,116)도 바탕이 섞인 것이다
    tinted = (r > g + 50) & (b > g + 50) & (r > 60) & (b > 60) & (abs(r - b) < 60)
    for _ in range(4):
        ring = fg & ~ndimage.binary_erosion(fg)
        fg &= ~(ring & tinted)
    # 한 물건 안의 작은 틈(드럼통 두 개 사이 등)은 이어 붙여 한 덩어리로 센다
    joined = ndimage.binary_closing(fg, structure=np.ones((15, 15)))
    lab, cnt = ndimage.label(joined)
    sizes = ndimage.sum(joined, lab, range(1, cnt + 1))
    keep = sorted(np.argsort(sizes)[::-1][:n] + 1)
    boxes = []
    for k in keep:
        ys, xs = np.where(lab == k)
        boxes.append((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1, k))
    boxes.sort(key=lambda b: (b[0]))
    rgba = np.dstack([a, np.where(fg, 255, 0).astype(np.uint8)])
    out = []
    for x0, y0, x1, y1, k in boxes:
        part = rgba[y0:y1, x0:x1].copy()
        part[..., 3] = np.where((lab[y0:y1, x0:x1] == k) & fg[y0:y1, x0:x1], 255, 0)
        out.append(Image.fromarray(part, "RGBA"))
    return out, len(sizes)


def to_cell(part: Image.Image) -> Image.Image:
    w, h = part.size
    s = min(BLOCK_W / w, BLOCK_H / h)
    nw, nh = max(1, round(w * s)), max(1, round(h * s))
    # 알파를 미리 곱해 줄여야 분홍이 가장자리로 번지지 않는다
    pm = np.array(part).astype(float)
    pm[..., :3] *= pm[..., 3:4] / 255
    small = Image.fromarray(pm.astype(np.uint8), "RGBA").resize((nw, nh), Image.LANCZOS)
    sa = np.array(small).astype(float)
    al = sa[..., 3:4]
    sa[..., :3] = np.where(al > 0, sa[..., :3] * 255 / np.maximum(al, 1), 0)
    sa[..., 3] = np.where(sa[..., 3] >= 128, 255, 0)   # 도트 — 반투명 가장자리 없이
    # 테두리 두 줄에 남은 자홍빛은 바탕이 섞인 것 — 지우면 외곽선이 사라지니 같은 밝기의 무채색으로 돌린다
    op = sa[..., 3] > 0
    ring = op & ~ndimage.binary_erosion(op, iterations=2)
    r, g, b = sa[..., 0], sa[..., 1], sa[..., 2]
    tint = ring & (r > g + 30) & (b > g + 30) & (abs(r - b) < 60)
    grey = (r + g + b) / 3 * 0.7
    for i in range(3):
        sa[..., i] = np.where(tint, grey, sa[..., i])
    small = Image.fromarray(np.clip(sa, 0, 255).astype(np.uint8), "RGBA")
    cell = Image.new("RGBA", (BLOCK_W, BLOCK_H), (0, 0, 0, 0))
    cell.paste(small, ((BLOCK_W - nw) // 2, BLOCK_H - nh), small)   # 바닥에 세운다
    return cell


def checker(size, c=12):
    im = Image.new("RGB", size, (70, 70, 78))
    d = ImageDraw.Draw(im)
    for y in range(0, size[1], c):
        for x in range((y // c % 2) * c, size[0], c * 2):
            d.rectangle((x, y, x + c - 1, y + c - 1), fill=(95, 95, 104))
    return im


def font(sz):
    for f in ("C:/Windows/Fonts/malgun.ttf", "C:/Windows/Fonts/gulim.ttc"):
        try:
            return ImageFont.truetype(f, sz)
        except OSError:
            pass
    return ImageFont.load_default()


def blocks_row(paths, scale=2):
    w = len(paths) * (BLOCK_W * scale + 8)
    row = checker((max(w, 8), BLOCK_H * scale))
    for i, p in enumerate(paths):
        if p.exists():
            b = Image.open(p).convert("RGBA").resize((BLOCK_W * scale, BLOCK_H * scale), Image.NEAREST)
            row.paste(b, (i * (BLOCK_W * scale + 8), 0), b)
    return row


def stage_sheet(s, log):
    f = font(22)
    cols = []
    # 지금 게임 파일 | 새 파일 | 승인 시안
    cur_floor = Image.open(FLOOR_DIR / f"roomfloor_env_{s}.png").convert("RGB")
    new_floor_p = OUT / s / f"roomfloor_env_{s}.png"
    new_floor = Image.open(new_floor_p).convert("RGB") if new_floor_p.exists() else checker((720, 1152))
    mock = Image.open(IN / MOCK[s]).convert("RGB")
    cur_side = Image.open(FLOOR_DIR / f"roomside_{s}.png").convert("RGB")
    new_side_p = OUT / s / f"roomside_{s}.png"
    new_side = Image.open(new_side_p).convert("RGB") if new_side_p.exists() else checker((160, 320))
    cur_blk = blocks_row([BLOCK_DIR / f"obj_{s}_block_{i}.png" for i in range(1, 7)])
    new_blk = blocks_row([OUT / s / f"obj_{s}_block_{i}.png" for i in range(1, 4)])
    v2_blk = blocks_row([OUT / f"{s}v2" / f"obj_{s}v2_block_{i}.png" for i in range(1, 6)])
    mock_v2 = Image.open(IN / MOCK_V2[s]).convert("RGB")

    W = 720 * 3 + 40 * 4
    H = 40 + 1280 + 40 + 330 + 40 + 220 + 40 + 220 + 40
    sh = Image.new("RGB", (W, H), (30, 30, 34))
    d = ImageDraw.Draw(sh)
    x = 40
    for title, im in (("지금 게임 바닥", cur_floor), ("새 바닥", new_floor), ("승인 시안", mock)):
        d.text((x, 8), title, font=f, fill=(255, 255, 255))
        sh.paste(im, (x, 40))
        x += 760
    y = 40 + 1280 + 40
    d.text((40, y - 34), "옆벽(세로 두 번 이어 붙임) — 지금 | 새", font=f, fill=(255, 255, 255))
    for i, im in enumerate((cur_side, new_side)):
        for k in range(2):
            sh.paste(im.resize((80, 160)), (40 + i * 200, y + k * 160))
    # 새 옆벽을 새 바닥 왼쪽에 붙여 본 모습
    joint = Image.new("RGB", (160 + 300, 320))
    joint.paste(new_side, (0, 0))
    joint.paste(new_floor.crop((0, 400, 300, 720)), (160, 0))
    sh.paste(joint.resize((345, 240)), (440, y))
    d.text((440, y + 245), "새 옆벽 + 새 바닥 맞닿음", font=f, fill=(200, 200, 200))
    y += 330 + 40
    d.text((40, y - 34), "블록 — 지금 1~6", font=f, fill=(255, 255, 255))
    sh.paste(cur_blk, (40, y))
    d.text((1100, y - 34), "블록 — 새 1~3", font=f, fill=(255, 255, 255))
    sh.paste(new_blk, (1100, y))
    y += 220 + 40
    d.text((40, y - 34), "7~12챕터 변형 블록 — 새 1~5", font=f, fill=(255, 255, 255))
    sh.paste(v2_blk, (40, y))
    d.text((1100, y - 34), "변형 승인 시안(플레이 영역)", font=f, fill=(255, 255, 255))
    sh.paste(mock_v2.crop((0, 190, 720, 1030)).resize((257, 300)), (1100, y))
    sh = sh.crop((0, 0, W, y + 320))
    sh.save(OUT / f"sheet_{s}.png")
    log.append(f"시트 sheet_{s}.png")


def main():
    log = []
    for s in STAGES:
        p = IN / f"bg_{s}_floor.png"
        if p.exists():
            fit_exact(p, (720, 1152), OUT / s / f"roomfloor_env_{s}.png")
            log.append(f"{s} 바닥 {Image.open(p).size}")
        p = IN / f"bg_{s}_side.png"
        if p.exists():
            fit_exact(p, (160, 320), OUT / s / f"roomside_{s}.png")
            log.append(f"{s} 옆벽 {Image.open(p).size}")
        for tag, n in ((s, 3), (f"{s}v2", 5)):
            p = IN / f"bg_{tag}_blocks.png"
            if not p.exists():
                continue
            parts, found = cut_blocks(p, n)
            (OUT / tag).mkdir(parents=True, exist_ok=True)
            for i, part in enumerate(parts, 1):
                to_cell(part).save(OUT / tag / f"obj_{tag}_block_{i}.png")
            log.append(f"{tag} 블록 {len(parts)}/{n} (덩어리 {found}개 찾음 · 크기 {[q.size for q in parts]})")
        stage_sheet(s, log)
    # 전체 요약 — 무대마다 새 바닥 + 새 블록
    f = font(20)
    tiles = []
    for s in STAGES:
        fl = OUT / s / f"roomfloor_env_{s}.png"
        t = Image.new("RGB", (410, 576 + 230), (30, 30, 34))
        if fl.exists():
            t.paste(Image.open(fl).convert("RGB").resize((360, 576)), (0, 0))
        t.paste(blocks_row([OUT / s / f"obj_{s}_block_{i}.png" for i in range(1, 4)], 1), (0, 584))
        t.paste(blocks_row([OUT / f"{s}v2" / f"obj_{s}v2_block_{i}.png" for i in range(1, 6)], 1), (0, 694))
        ImageDraw.Draw(t).text((6, 6), s, font=f, fill=(255, 255, 255))
        tiles.append(t)
    sm = Image.new("RGB", (6 * 420, 576 + 230), (20, 20, 24))
    for i, t in enumerate(tiles):
        sm.paste(t, (i * 420, 0))
    sm.save(OUT / "sheet_all.png")
    print("\n".join(log))


# ─────────────────────────────────────────
# 2차 — 물 · 가시판 · 울타리 · 방 밖 바닥 · 보스 방 (PD 2026-10-29 「전체적으로 어울리게」)
BOSSES = ["robot_snakes", "crusher", "python", "sludge", "guardian", "kingpin"]
WATER_COLS, WATER_ROWS = 5, 4
BANK = 29   # 둑 두께(72 칸 기준) — 지금 게임 둑(obj_*_water_edge_n 위 28 px)과 맞춘다


def cut_water(src: Path, s: str, out: Path):
    a = np.array(Image.open(src).convert("RGB"))
    fg = ~pink_mask(a)
    lab, cnt = ndimage.label(ndimage.binary_closing(fg, structure=np.ones((9, 9))))
    k = np.argmax(ndimage.sum(fg, lab, range(1, cnt + 1))) + 1
    ys, xs = np.where(lab == k)
    # 바깥 몇 점은 분홍이 섞인 번짐이다 — 안으로 4 점 들어와 자른다
    pool = Image.fromarray(a).crop((xs.min() + 4, ys.min() + 4, xs.max() - 3, ys.max() - 3))
    # 웅덩이를 5 x 4 칸으로 나눠 72 칸 크기로 한꺼번에 줄인다(칸마다 따로 줄이면 이음매가 생긴다)
    grid = pool.resize((72 * WATER_COLS, 72 * WATER_ROWS), Image.LANCZOS)
    cell = lambda c, r: grid.crop((c * 72, r * 72, c * 72 + 72, r * 72 + 72)).convert("RGBA")

    def band(img, sides):
        m = np.zeros((72, 72), bool)
        if "n" in sides: m[:BANK, :] = True
        if "s" in sides: m[72 - BANK:, :] = True
        if "w" in sides: m[:, :BANK] = True
        if "e" in sides: m[:, 72 - BANK:] = True
        arr = np.array(img)
        arr[..., 3] = np.where(m, 255, 0)
        return Image.fromarray(arr, "RGBA")

    mc, mr = WATER_COLS // 2, 1
    parts = {
        "water_1": cell(mc, 1), "water_2": cell(mc, 2),
        "water_edge_n": band(cell(mc, 0), "n"), "water_edge_s": band(cell(mc, WATER_ROWS - 1), "s"),
        "water_edge_w": band(cell(0, 1), "w"), "water_edge_e": band(cell(WATER_COLS - 1, 1), "e"),
        "water_corner_nw": band(cell(0, 0), "nw"), "water_corner_ne": band(cell(WATER_COLS - 1, 0), "ne"),
        "water_corner_sw": band(cell(0, WATER_ROWS - 1), "sw"),
        "water_corner_se": band(cell(WATER_COLS - 1, WATER_ROWS - 1), "se"),
    }
    for n, im in parts.items():
        im.save(out / f"obj_{s}_{n}.png")
    return pool.size


def to_square(part: Image.Image) -> Image.Image:
    # 가시판은 바닥에 눕는 칸 — 72 x 72 칸 가운데에 꽉 채운다
    cell = to_cell(part)   # 72 x 102 에 바닥 맞춤으로 줄인 것에서 물건만 다시 꺼낸다
    bb = cell.getbbox()
    obj = cell.crop(bb)
    s = min(72 / obj.width, 72 / obj.height)
    obj = obj.resize((max(1, round(obj.width * s)), max(1, round(obj.height * s))), Image.NEAREST)
    sq = Image.new("RGBA", (72, 72), (0, 0, 0, 0))
    sq.paste(obj, ((72 - obj.width) // 2, (72 - obj.height) // 2), obj)
    return sq


def main2():
    log = []
    for s in STAGES:
        out = OUT / s
        p = IN / f"bg_{s}_water.png"
        if p.exists():
            log.append(f"{s} 물 웅덩이 {cut_water(p, s, out)}")
        p = IN / f"bg_{s}_hazards.png"
        if p.exists():
            parts, found = cut_blocks(p, 4)
            for i, part in enumerate(parts[:2], 1):
                to_square(part).save(out / f"obj_{s}_spike_{i}.png")
            for i, part in enumerate(parts[2:4], 1):
                to_cell(part).save(out / f"obj_{s}_fence_{i}.png")
            log.append(f"{s} 가시 · 울타리 {len(parts)}/4 (덩어리 {found})")
        p = IN / f"bg_{s}_apron.png"
        if p.exists():
            fit_exact(p, (720, 540), out / f"roomapron_{s}.png")
            log.append(f"{s} 방 밖 {Image.open(p).size}")
    for b in BOSSES:
        out = OUT / "boss"
        p = IN / f"bg_boss_{b}_floor.png"
        if p.exists():
            fit_exact(p, (720, 1152), out / f"roomfloor_{b}.png")
            log.append(f"보스 {b} 바닥 {Image.open(p).size}")
        p = IN / f"bg_boss_{b}_apron.png"
        if p.exists():
            fit_exact(p, (720, 540), out / f"roomapron_boss_{b}.png")
            log.append(f"보스 {b} 방 밖 {Image.open(p).size}")
    print("\n".join(log))


if __name__ == "__main__":
    main2()
    main()
