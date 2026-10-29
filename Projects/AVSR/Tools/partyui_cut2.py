"""빙의 파티 창 안 부품(2차) 자르기 — 분홍 바탕을 뚫고, 덩어리를 「시트에서 놓인 자리(가운데 점)」로 이름 붙인다.

시트: Projects/AVSR/_exchange/in/parts_partyui_popup{2,3,4,5,6}.png  (부품 표: _exchange/guide_party_ui.md 「창 안 부품 — 2차」)
결과: Assets/BaseResource/InGameMainUI/<이름>.png · 검수 한 장 Projects/AVSR/_exchange/ref/party_ui_1029/cut_check2.png
읽는 순서로 세면 위아래로 어긋난 부품(사슬 · 육각)이 뒤바뀐다 — 시트를 보고 적은 가운데 점에 가장 가까운 덩어리를 고른다.
"""
import os
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

sys.path.insert(0, os.path.dirname(__file__))
from partyui_cut import key_out, blobs  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')
CHECK = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'party_ui_1029', 'cut_check2.png')

# 시트 → [(이름, 가운데 x, 가운데 y, 게임 배율)] — 게임 배율은 720 판 기준 크기 / 시트 크기
SHEETS = {
    'popup2': [
        ('party_tag_gold', 340, 95, 0.32), ('party_tag_red', 685, 95, 0.32),
        ('party_box_cyan', 355, 305, 0.30), ('party_box_gray', 665, 305, 0.30),
        ('party_divider_v', 80, 700, 0.5), ('party_death_splat', 510, 720, 0.42),
        ('party_best_hex', 880, 875, 0.42),
    ],
    'popup3': [
        ('syn_pop_panel', 420, 220, 0.6),
        ('syn_tier_on', 420, 497, 0.55), ('syn_tier_off', 420, 600, 0.55), ('syn_more_chip', 888, 600, 0.45),
        ('syn_member_gold', 200, 785, 0.38), ('syn_member_purple', 423, 785, 0.38),
        ('syn_member_gray', 645, 785, 0.38), ('syn_member_badge', 852, 757, 0.36),
        ('party_divider_h', 512, 942, 0.5),
    ],
    'popup4': [
        ('party_row_gold', 512, 422, 0.65), ('party_ghost_mini', 508, 760, 0.42),
    ],
    'popup5': [
        ('possess_pill', 495, 300, 0.2), ('possess_bubble_light', 510, 590, 0.25),
    ],
    'popup7': [   # 자물쇠 뺀 못 고름 줄 + 자물쇠 따로(시안: 자물쇠는 사슬 왼쪽 끝)
        ('party_row_locked', 512, 340, 1.0), ('party_lock', 512, 540, 0.9),
    ],
    'popup6': [   # 시안과 달라 다시 받은 셋(테 없는 남은 HP 칸 · 진한 금 딱지 · 모서리를 감는 사슬)
        ('party_hp_pill', 400, 315, 0.33), ('party_badge_best', 975, 312, 0.25), ('party_chain', 630, 850, 0.21),
    ],
}


def cut(sheet, plan, tiles):
    path = os.path.join(IN, f'parts_partyui_{sheet}.png')
    if not plan or not os.path.exists(path):
        return
    rgb = np.asarray(Image.open(path).convert('RGB'))
    alpha = key_out(rgb)
    alpha = np.where(alpha < 255, 0, alpha).astype(np.uint8)   # 반쯤 남은 분홍 테 점(칸 모서리 분홍 티)을 지운다
    bs = blobs(alpha)
    lab, _ = ndimage.label(ndimage.binary_dilation(alpha > 0, iterations=3))
    used = set()
    for name, cx, cy, s in plan:
        best, bd = None, 1e9
        for i, (y0, x0, y1, x1) in enumerate(bs):
            if i in used:
                continue
            d = ((x0 + x1) / 2 - cx) ** 2 + ((y0 + y1) / 2 - cy) ** 2
            if d < bd:
                best, bd = i, d
        if best is None or bd > 120 ** 2:
            print(f'[못 찾음] {sheet} {name}')
            continue
        used.add(best)
        y0, x0, y1, x1 = bs[best]
        pad = 2
        y0, x0 = max(0, y0 - pad), max(0, x0 - pad)
        y1, x1 = min(rgb.shape[0], y1 + pad), min(rgb.shape[1], x1 + pad)
        # 상자가 옆 부품과 겹치면(피 얼룩 상자 안에 사슬 · 육각 끝) 이 덩어리 점만 남긴다
        sub = lab[y0:y1, x0:x1]
        ids, cnt = np.unique(sub[sub > 0], return_counts=True)
        keep = sub == ids[np.argmax(cnt)] if len(ids) else np.ones_like(sub, bool)
        tile = Image.fromarray(np.dstack([rgb[y0:y1, x0:x1], np.where(keep, alpha[y0:y1, x0:x1], 0).astype(np.uint8)]), 'RGBA')
        # 도트 그림 — 가장 가까운 점으로 줄인다(번지지 않게)
        tile = tile.resize((max(1, round(tile.width * s)), max(1, round(tile.height * s))), Image.NEAREST)
        tile.save(os.path.join(OUT, name + '.png'))
        tiles.append((name, tile))
        print(f'{name}: {tile.size}')
    left = [b for i, b in enumerate(bs) if i not in used]
    if left:
        print(f'[남은 덩어리] {sheet}: {len(left)} {left[:4]}')


def main():
    tiles = []
    for sheet, plan in SHEETS.items():
        cut(sheet, plan, tiles)
    W = 1100
    y, x, rowh = 10, 10, 0
    out = Image.new('RGBA', (W, 3000), (40, 44, 56, 255))
    d = ImageDraw.Draw(out)
    f = ImageFont.truetype(r'C:\Windows\Fonts\malgun.ttf', 14)
    for name, t in tiles:
        tw, th = t.size
        if x + tw > W - 10:
            x, y, rowh = 10, y + rowh + 30, 0
        out.alpha_composite(t, (x, y + 18))
        d.text((x, y), name, font=f, fill=(255, 214, 106))
        x += tw + 20
        rowh = max(rowh, th + 18)
    out.crop((0, 0, W, y + rowh + 20)).save(CHECK)
    print('검수', CHECK)


if __name__ == '__main__':
    main()
