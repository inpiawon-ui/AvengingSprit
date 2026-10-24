"""RE:BORN 아웃게임 연출 부품 반영(2026-10-24) — 코덱스 시트(_exchange/in) → 프레임 PNG(Assets/BaseResource/ReBornFx).

빛(검은 바탕)은 밝기를 투명도로 옮긴다 — 게임에서 가산(UI/AdditiveSprite, SrcAlpha One)으로 그려 보이는 것은 같다.
물건(마젠타 바탕)은 바탕을 뚫는다. 칸 격자는 시트 크기 ÷ 열 · 줄로 나누고, 칸이 목표보다 크면 최근접 축소만 한다.
python rbfx_import.py            # 받은 것 전부
python rbfx_import.py rbfx2_rays # 이름만
"""
import os
import sys

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
SRC = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
DST = os.path.join(ROOT, 'Assets', 'BaseResource', 'ReBornFx')

# 시트 → (게임 이름, 열, 줄, 칸 목표 크기, 바탕)
SHEETS = {
    'rbfx_awaken6': ('rbfx_ring6', 4, 2, 384, 'black'),
    'rbfx2_awaken10': ('rbfx_ring10', 4, 2, 384, 'black:90'),   # 두 번 다 갈색 안개 · 빨강 점을 달고 왔다 — 어두운 곳을 내린다
    'rbfx_star_burst': ('rbfx_star', 6, 1, 192, 'black'),
    'rbfx_reveal': ('rbfx_flash', 6, 1, 192, 'black'),
    'rbfx_chest_burst': ('rbfx_chestlight', 3, 1, 320, 'black'),
    'rbfx_coin': ('rbfx_coin', 8, 1, 64, 'pink'),
    'rbfx_shine': ('rbfx_shine', 1, 1, 256, 'black'),
    'rbfx_upgrade_band': ('rbfx_band', 1, 1, 512, 'black'),
    'rbfx2_rays': ('rbfx_rays', 1, 1, 512, 'black'),
    'rbfx2_chest_silver': ('rbfx_chest_silver', 4, 1, 192, 'pink'),
    'rbfx2_chest_gold': ('rbfx_chest_gold', 4, 1, 192, 'pink'),
    'rbfx2_chest_platinum': ('rbfx_chest_platinum', 4, 1, 192, 'pink'),
}


def cut_pink(im):
    """마젠타 바탕 → 투명. 이미 투명한 곳은 그대로."""
    im = im.convert('RGBA')
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            # 가장자리 섞인 분홍(외곽선과 바탕 사이)까지 — 그림에 분홍은 없다(발주 조건)
            if a == 0 or (r - g > 70 and b - g > 70):
                px[x, y] = (0, 0, 0, 0)
    return im


def floor_dark(im, floor):
    """검은 바탕 빛의 바탕 정리 — 가장 밝은 채널이 floor 아래(안개 판)이거나 빨강 · 초록만 튄 점(테)은 검정으로.
    가산으로 그리므로 검정 = 아무것도 안 더한다. 빛 모양은 건드리지 않는다."""
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            m = max(r, g, b)
            # 빨강 점 · 초록 테 · 올리브 얼룩(금빛은 늘 빨강 > 초록이라 초록 ≥ 빨강인 어두운 곳은 얼룩이다)
            stray = (r > 120 and g < r * 0.45) or (g > 60 and r < g * 0.7 and b < g * 0.7) or (g >= r and b < g * 0.5 and m < 150)
            if m < floor or stray:
                px[x, y] = (0, 0, 0, 255)
    return im


def fit_to_kit(frames, kit_path, size):
    """상자 열림 프레임을 키트 닫힌 상자(144 칸)에 맞춘다 — 첫 장(닫힘)의 몸통 폭 · 바닥 가운데가 키트와 같게.
    게임은 열림 칸(192)을 키트 칸의 192/144 배로 그리므로 원본 px 가 1:1 로 맞으면 바꿔 끼워도 튀지 않는다."""
    kit = Image.open(kit_path).convert('RGBA')
    kb = kit.getbbox()
    fb = frames[0].getbbox()
    if not kb or not fb:
        return frames
    k = (kb[2] - kb[0]) / (fb[2] - fb[0])
    bottom = kb[3] / kit.height * size          # 키트 바닥 높이를 192 칸으로
    out = []
    for f in frames:
        g = f.resize((max(1, round(f.width * k)), max(1, round(f.height * k))), Image.NEAREST) if abs(k - 1) > 0.03 else f
        cx, by = (fb[0] + fb[2]) / 2 * (g.width / f.width), fb[3] * (g.height / f.height)
        canvas = Image.new('RGBA', (size, size), (0, 0, 0, 0))
        canvas.alpha_composite(g, (round(size / 2 - cx), round(bottom - by)))
        out.append(canvas)
    return out


def black_to_alpha(im):
    """검은 바탕 빛 → 투명 바탕. 투명도 = 가장 밝은 채널, 색은 그만큼 키운다.
    가산(SrcAlpha One)으로 그리면 색 × 투명도 = 원래 색이라 보이는 것은 같고, 바탕이 비어 아틀라스 자리를 덜 먹는다."""
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, _ = px[x, y]
            m = max(r, g, b)
            px[x, y] = (0, 0, 0, 0) if m == 0 else (r * 255 // m, g * 255 // m, b * 255 // m, m)
    return im


def run(sheet):
    name, cols, rows, size, bg = SHEETS[sheet]
    path = os.path.join(SRC, sheet + '.png')
    if not os.path.exists(path):
        print('없음', sheet)
        return 0
    im = Image.open(path)
    bg, _, floor = bg.partition(':')
    im = cut_pink(im) if bg == 'pink' else im.convert('RGB').convert('RGBA')
    if floor:
        im = floor_dark(im, int(floor))
    if bg == 'black':
        im = black_to_alpha(im)
    cw, ch = im.width / cols, im.height / rows
    os.makedirs(DST, exist_ok=True)
    cells = []
    for r in range(rows):
        for c in range(cols):
            cell = im.crop((round(c * cw), round(r * ch), round((c + 1) * cw), round((r + 1) * ch)))
            # 칸 비율은 그대로 — 긴 변을 목표 크기로(크면 최근접 축소, 작으면 그대로)
            k = size / max(cell.width, cell.height)
            if k < 1:
                cell = cell.resize((max(1, round(cell.width * k)), max(1, round(cell.height * k))), Image.NEAREST)
            cells.append(cell)
    if name.startswith('rbfx_chest_'):
        grade = name[len('rbfx_chest_'):]
        cells = fit_to_kit(cells, os.path.join(ROOT, 'Assets', 'BaseResource', 'ReBornKit', 'Icons', 'rb_icon_chest_%s_144.png' % grade), size)
    n = 0
    for cell in cells:
        cell.save(os.path.join(DST, name + ('_%d' % n if cols * rows > 1 else '') + '.png'))
        n += 1
    print(sheet, '→', name, n, '장')
    return n


def main():
    names = sys.argv[1:] or list(SHEETS)
    for s in names:
        run(s)


if __name__ == '__main__':
    main()
