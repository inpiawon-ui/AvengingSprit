"""빙의 파티 UI 부품 시트 자르기 — 분홍 바탕을 뚫고 덩어리마다 잘라 이름을 붙인다.

시트: Projects/AVSR/_exchange/in/parts_partyui_{hud,syn,popup}.png (1024 x 1024, 분홍 #FF00FF 바탕)
결과: Assets/BaseResource/InGameMainUI/<부품 이름>.png  · 검수용 한 장 Projects/AVSR/_exchange/ref/party_ui_1029/cut_check.png
덩어리는 위 → 아래, 왼쪽 → 오른쪽 순서로 세고, 부품 표(guide_party_ui.md) 순서대로 이름을 붙인다.
"""
import os
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')
CHECK = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'party_ui_1029', 'cut_check.png')

# 시트마다 덩어리 이름(읽는 순서) — 부품 표 UI-PARTY-01~11
NAMES = {
    'hud': ['partyhud_panel', 'partyhud_main_frame', 'partyhud_sub_frame', 'partyhud_empty_frame',
            'partyhud_bar_bg', 'partyhud_bar_ghost', 'partyhud_bar_hp'],
    'syn': ['syn_cell_on', 'syn_cell_off', 'syn_cell_preview',
            'syn_icon_force', 'syn_icon_blade', 'syn_icon_magic',
            'syn_icon_gang', 'syn_icon_commando', 'syn_icon_martial',
            'syn_icon_monster', 'syn_icon_mystic', 'syn_icon_balance'],
    'popup': ['party_popup_panel', 'party_row', 'party_row_best', 'party_row_off',
              'party_timer_ring', 'possess_bubble_syn', 'possess_bubble_none'],
}


# 게임에서 쓸 크기로 줄이는 배율(시트는 2~3배로 그려져 온다). 9분할 부품은 줄인 뒤 Unity 에서 늘린다
SCALE = {
    'partyhud_panel': 0.75, 'partyhud_main_frame': 0.41, 'partyhud_sub_frame': 0.37, 'partyhud_empty_frame': 0.35,
    'partyhud_bar_bg': 0.30, 'partyhud_bar_ghost': 0.30, 'partyhud_bar_hp': 0.30,
    'syn_cell_on': 0.28, 'syn_cell_off': 0.28, 'syn_cell_preview': 0.28,
    'party_popup_panel': 0.70, 'party_row': 0.65, 'party_row_best': 0.65, 'party_row_off': 0.65,
    'party_timer_ring': 0.50, 'possess_bubble_syn': 0.50, 'possess_bubble_none': 0.50,
}
ICON_SCALE = 0.26   # 시너지 문장 9종


def key_out(rgb):
    """분홍 바탕을 투명으로. 테두리에 번진 분홍 기운도 조금 걷는다."""
    r, g, b = rgb[..., 0].astype(int), rgb[..., 1].astype(int), rgb[..., 2].astype(int)
    pink = (r > 200) & (b > 200) & (g < 90)
    near = (r > 170) & (b > 170) & (g < 130) & (np.abs(r - b) < 60)
    alpha = np.where(pink, 0, np.where(near, 90, 255)).astype(np.uint8)
    return alpha


def blobs(alpha):
    # 점선 · 왕관처럼 떨어진 조각을 한 덩어리로 묶으려고 조금 부풀려서 센다(자르는 칸은 원래 그림 기준)
    lab, n = ndimage.label(ndimage.binary_dilation(alpha > 0, iterations=3))
    boxes = ndimage.find_objects(lab)
    out = []
    for i, sl in enumerate(boxes):
        if sl is None:
            continue
        h = sl[0].stop - sl[0].start
        w = sl[1].stop - sl[1].start
        if w * h < 400:   # 점 부스러기
            continue
        out.append((sl[0].start, sl[1].start, sl[0].stop, sl[1].stop))
    # 다른 덩어리 칸 안에 들어 있는 조각(빈 자리 칸의 「+」 등)은 바깥 덩어리에 합친다
    out = [b for b in out if not any(o != b and o[0] <= b[0] and o[1] <= b[1] and o[2] >= b[2] and o[3] >= b[3] for o in out)]
    # 줄 묶기: 위쪽 기준이 비슷하면 같은 줄로 보고 왼쪽부터
    out.sort(key=lambda b: (b[0], b[1]))
    rows, cur = [], []
    for b in out:
        if cur and b[0] > cur[0][0] + 40 and b[0] > max(c[2] for c in cur) - 20:
            rows.append(sorted(cur, key=lambda c: c[1])); cur = []
        cur.append(b)
    if cur:
        rows.append(sorted(cur, key=lambda c: c[1]))
    return [b for r in rows for b in r]


def main():
    os.makedirs(OUT, exist_ok=True)
    tiles = []
    for sheet, names in NAMES.items():
        im = Image.open(os.path.join(IN, f'parts_partyui_{sheet}.png')).convert('RGB')
        rgb = np.asarray(im)
        alpha = key_out(rgb)
        bs = blobs(alpha)
        if len(bs) != len(names):
            print(f'[주의] {sheet}: 덩어리 {len(bs)} · 이름 {len(names)}')
        for name, (y0, x0, y1, x1) in zip(names, bs):
            pad = 2
            y0, x0 = max(0, y0 - pad), max(0, x0 - pad)
            y1, x1 = min(rgb.shape[0], y1 + pad), min(rgb.shape[1], x1 + pad)
            rgba = np.dstack([rgb[y0:y1, x0:x1], alpha[y0:y1, x0:x1]])
            tile = Image.fromarray(rgba, 'RGBA')
            s = SCALE.get(name, ICON_SCALE if name.startswith('syn_icon_') else 1.0)
            if s != 1.0:
                tile = tile.resize((max(1, round(tile.width * s)), max(1, round(tile.height * s))), Image.LANCZOS)
            tile.save(os.path.join(OUT, name + '.png'))
            tiles.append((name, tile))
            print(f'{name}: {tile.size}')
    # 검수 한 장 — 어두운 바닥 위
    W = 1100
    y, x, rowh = 10, 10, 0
    sheet = Image.new("RGBA", (W, 5000), (40, 44, 56, 255))
    from PIL import ImageDraw, ImageFont
    d = ImageDraw.Draw(sheet)
    f = ImageFont.truetype(r'C:\Windows\Fonts\malgun.ttf', 14)
    for name, t in tiles:
        tw, th = t.size
        if x + tw > W - 10:
            x, y, rowh = 10, y + rowh + 30, 0
        sheet.alpha_composite(t, (x, y + 18))
        d.text((x, y), name, font=f, fill=(255, 214, 106))
        x += tw + 20
        rowh = max(rowh, th + 18)
    sheet = sheet.crop((0, 0, W, y + rowh + 20))
    sheet.save(CHECK)
    print('검수', CHECK)


if __name__ == '__main__':
    main()
