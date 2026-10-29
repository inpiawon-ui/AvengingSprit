"""10-29 밤 부품 자르기 — 기관총 필살기(검정 = 빛) · 호스트 해금(위 검정 / 아래 분홍) · 성장 화면 인연 판(분홍).

시트: Projects/AVSR/_exchange/in/parts1029_{ult,unlock,inyeon}.png
- 검정 바탕(빛): 밝기를 알파로, 색은 알파로 나눠 되살린다(더하기처럼 보이게 — 빛 셰이더 없이도 번짐이 남는다)
- 분홍 바탕(형태): partyui_cut.key_out 으로 뚫고 반쯤 남은 분홍 점을 지운다
덩어리는 「시트에서 놓인 가운데 점」으로 고른다(partyui_cut2 와 같은 방식).
결과: 인게임 Assets/BaseResource/InGameMainUI/fx_mg*.png · 로비 Assets/BaseResource/ReBornFx/rbfx_unlock_*.png ·
      Assets/BaseResource/ReBornKit/Components/rb_iy_*.png · 검수 한 장 Projects/AVSR/_exchange/ref/party_ui_1029/cut_check3.png
"""
import os
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

sys.path.insert(0, os.path.dirname(__file__))
from partyui_cut import key_out  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
GAME = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')
FX = os.path.join(ROOT, 'Assets', 'BaseResource', 'ReBornFx')
KIT = os.path.join(ROOT, 'Assets', 'BaseResource', 'ReBornKit', 'Components')
CHECK = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'party_ui_1029', 'cut_check3.png')

# (이름, 폴더, 가운데 x, 가운데 y, 배율, 바탕 'black'|'pink', 돌림)
PLAN = {
    'ult': [
        ('fx_mgtracer_1', GAME, 80, 280, 0.75, 'black', 90),     # 세로 그림 → 가로(빛줄기는 가로로 늘려 쓴다)
        ('fx_mgmuzzle_1', GAME, 235, 335, 0.75, 'black', 0), ('fx_mgmuzzle_2', GAME, 455, 320, 0.75, 'black', 0),
        ('fx_mgmuzzle_3', GAME, 675, 320, 0.75, 'black', 0), ('fx_mgmuzzle_4', GAME, 905, 325, 0.75, 'black', 0),
        ('fx_mghit_1', GAME, 150, 625, 0.6, 'black', 0), ('fx_mghit_2', GAME, 385, 620, 0.6, 'black', 0),
        ('fx_mghit_3', GAME, 630, 620, 0.6, 'black', 0), ('fx_mghit_4', GAME, 880, 625, 0.6, 'black', 0),
        ('fx_mgdust_1', GAME, 190, 905, 0.65, 'black', 0), ('fx_mgdust_2', GAME, 590, 895, 0.65, 'black', 0),
        ('fx_mgshell_1', GAME, 897, 890, 0.6, 'pinkbox', 0),
    ],
    'unlock': [
        ('rbfx_unlock_rays', FX, 290, 260, 1.0, 'black', 0), ('rbfx_unlock_ring', FX, 775, 265, 1.0, 'black', 0),
        ('rbfx_unlock_shard_gold', FX, 145, 600, 1.0, 'pink', 0), ('rbfx_unlock_shard_red', FX, 272, 603, 1.0, 'pink', 0),
        ('rbfx_unlock_shard_blue', FX, 400, 600, 1.0, 'pink', 0), ('rbfx_unlock_grade_plate', FX, 772, 600, 1.0, 'pink', 0),
        ('rbfx_unlock_name_plate', FX, 348, 752, 1.0, 'pink', 0),
        ('rbfx_unlock_btn_blue', FX, 265, 918, 1.0, 'pink', 0), ('rbfx_unlock_btn_gold', FX, 757, 918, 1.0, 'pink', 0),
    ],
    'inyeon': [
        ('rb_iy_panel', KIT, 262, 333, 1.0, 'pink', 0), ('rb_iy_box', KIT, 750, 262, 1.0, 'pink', 0),
        ('rb_iy_step', KIT, 658, 508, 0.6, 'pink', 0), ('rb_iy_link', KIT, 898, 500, 0.7, 'pink', 0),
        ('rb_iy_face_cyan', KIT, 160, 773, 0.85, 'pink', 0), ('rb_iy_face_gold', KIT, 433, 773, 0.85, 'pink', 0),
        ('rb_iy_chain', KIT, 748, 730, 0.6, 'pink', 0), ('rb_iy_toggle', KIT, 685, 890, 0.45, 'pink', 0),
        ('rb_iy_arrow', KIT, 880, 893, 0.5, 'pink', 0),
    ],
}


def black_alpha(rgb):
    a = rgb.max(axis=2).astype(np.float32)
    a = np.where(a < 18, 0, a)                     # 바닥 잡티(붉은 점)는 버린다
    out = np.zeros(rgb.shape[:2] + (4,), np.uint8)
    safe = np.maximum(a, 1)[..., None]
    out[..., :3] = np.clip(rgb.astype(np.float32) * 255.0 / safe, 0, 255).astype(np.uint8)
    out[..., 3] = a.astype(np.uint8)
    return out


def pink_alpha(rgb):
    a = key_out(rgb)
    a = np.where(a < 255, 0, a).astype(np.uint8)
    return np.dstack([rgb, a])


def pinkbox_alpha(rgb):
    # 분홍 칸 안 탄피 — 칸 자체가 번진 분홍이라 「분홍이 아닌 것」을 넓게 남긴다
    r, g, b = rgb[..., 0].astype(int), rgb[..., 1].astype(int), rgb[..., 2].astype(int)
    pinkish = (r > 120) & (b > 90) & (g < 110) & (np.abs(r - b) < 120)
    dark = rgb.max(axis=2) < 20
    a = np.where(pinkish | dark, 0, 255).astype(np.uint8)
    return np.dstack([rgb, a])


def pick(alpha_mask, cx, cy, grow):
    # 가운데 점에 가장 가까운 덩어리 — 빛 부스러기(점 수 1200 미만)는 건너뛴다. 큰 것을 고르면 옆 부품을 집었다
    lab, n = ndimage.label(ndimage.binary_dilation(alpha_mask, iterations=grow))
    counts = np.bincount(lab.ravel())
    best, bd = 0, 1e18
    for i, sl in enumerate(ndimage.find_objects(lab), start=1):
        if sl is None or counts[i] < 1200:
            continue
        d = ((sl[1].start + sl[1].stop) / 2 - cx) ** 2 + ((sl[0].start + sl[0].stop) / 2 - cy) ** 2
        if d < bd:
            best, bd = i, d
    return lab, best, bd


def main():
    tiles = []
    for sheet, plan in PLAN.items():
        path = os.path.join(IN, f'parts1029_{sheet}.png')
        rgb = np.asarray(Image.open(path).convert('RGB'))
        for name, folder, cx, cy, s, bg, rot in plan:
            if bg == 'black':
                rgba = black_alpha(rgb)
                mask = rgba[..., 3] > 30
                if sheet == 'unlock':
                    mask[500:, :] = False          # 아래 절반은 분홍(형태) 칸
                if sheet == 'ult':
                    mask[740:, 770:] = False       # 오른쪽 아래 분홍 탄피 칸
            elif bg == 'pinkbox':
                rgba = pinkbox_alpha(rgb)
                x0, y0 = 780, 760
                mask = np.zeros(rgb.shape[:2], bool)
                mask[y0:, x0:] = rgba[y0:, x0:, 3] > 0
            else:
                rgba = pink_alpha(rgb)
                mask = rgba[..., 3] > 0
            lab, idx, bd = pick(mask, cx, cy, 2 if bg != 'black' else 3)
            if idx == 0:
                print(f'[못 찾음] {name}')
                continue
            ys, xs = np.where(lab == idx)
            y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
            part = rgba[y0:y1, x0:x1].copy()
            part[..., 3] = np.where(lab[y0:y1, x0:x1] == idx, part[..., 3], 0)
            tile = Image.fromarray(part, 'RGBA')
            if rot:
                tile = tile.rotate(rot, expand=True)
            if s != 1.0:
                tile = tile.resize((max(1, round(tile.width * s)), max(1, round(tile.height * s))), Image.NEAREST)
            os.makedirs(folder, exist_ok=True)
            tile.save(os.path.join(folder, name + '.png'))
            tiles.append((name, tile))
            print(f'{name}: {tile.size}')
    W = 1200
    y, x, rowh = 10, 10, 0
    out = Image.new('RGBA', (W, 4000), (40, 44, 56, 255))
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
