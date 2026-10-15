"""2차 부품 모음판 — 묶음마다 [정답 장면 | 잘라 낸 부품 + 이름] 한 줄. 연출 프레임은 GIF 로."""
import json, os, codecs
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = 'C:/won/UnityProject/AvengingSprit/'
EX = ROOT + 'Projects/AVSR/_exchange/'
OUT = EX + 'out/parts2/'
REF = EX + 'ref/store_mock/'
FONT = ROOT + 'Assets/BaseResource/Fonts/NotoSansKR-Bold.ttf'
man = json.load(codecs.open(EX + 'parts2_manifest.json', 'r', 'utf-8'))

TITLE = {
    'p2_bottombar': '하단 바 네 칸', 'p2_topbar': '상단 바(행동력 · 패스)', 'p2_startmode': '출전 방식 · 시작 버튼',
    'p2_shop_a': '상점 ① 머리 · 탭 · 오늘의 상점 · 상자', 'p2_shop_b': '상점 ② 계약 · 입문 · 젬 · 골드',
    'p2_products_gem': '상품 그림 ① 젬 6단', 'p2_products_misc': '상품 그림 ② 골드 · 계약 · 입문 · 소환권 · 행동력',
    'p2_popups': '팝업', 'p2_summon_bg': '소환 제단', 'p2_summon_ui': '소환 UI', 'p2_pass': '시즌 패스',
    'p2_mission_icons': '미션 아이콘', 'p2_profile': '프로필',
}
ANCHOR = {
    'p2_bottombar': 'scene_s01_lobby', 'p2_topbar': 'scene_s20_stamina', 'p2_startmode': 'scene_s22a_host',
    'p2_shop_a': 'scene_s02_daily', 'p2_shop_b': 'scene_s04_currency', 'p2_products_gem': 'scene_s04_currency',
    'p2_products_misc': 'scene_s04_currency', 'p2_popups': 'scene_s05_confirm', 'p2_summon_bg': 'mock_summon_a',
    'p2_summon_ui': 'mock_summon_a', 'p2_pass': 'scene_s16_pass', 'p2_mission_icons': 'scene_s17_mission',
    'p2_profile': 'mock_store_profile',
}
f_t = ImageFont.truetype(FONT, 26)
f_n = ImageFont.truetype(FONT, 13)
BG, CHK = (26, 30, 46), ((52, 56, 74), (66, 70, 90))
SCENE_H = 420
W = 1800


def checker(w, h):
    im = Image.new('RGB', (w, h), CHK[0])
    d = ImageDraw.Draw(im)
    for y in range(0, h, 10):
        for x in range(0, w, 10):
            if (x // 10 + y // 10) % 2:
                d.rectangle([x, y, x + 9, y + 9], fill=CHK[1])
    return im


rows = []
for k, title in TITLE.items():
    folder = OUT + k.replace('p2_', '') + '/'
    names = man[k]['parts']
    parts = [(n, Image.open(folder + n + '.png').convert('RGBA')) for n in names if os.path.exists(folder + n + '.png')]
    sc = Image.open(EX + 'in/' + ANCHOR[k] + '.png').convert('RGB')
    sc = sc.resize((int(sc.width * SCENE_H / sc.height), SCENE_H))
    # 부품은 긴 변 기준으로 줄여 줄지어 놓는다
    area_w = W - sc.width - 60
    tiles = []
    for n, im in parts:
        s = min(1.0, 210 / max(im.width, im.height))
        t = im.resize((max(1, int(im.width * s)), max(1, int(im.height * s))), Image.NEAREST)
        tiles.append((n, t))
    x, y, lh, placed = 0, 0, 0, []
    for n, t in tiles:
        tw = max(t.width, 120)
        if x + tw > area_w:
            x, y, lh = 0, y + lh + 26, 0
        placed.append((n, t, x, y))
        x += tw + 16
        lh = max(lh, t.height)
    ph = max(SCENE_H, y + lh + 26)
    row = Image.new('RGB', (W, ph + 50), BG)
    d = ImageDraw.Draw(row)
    d.text((14, 8), f'{title}  ·  {len(parts)}개  ·  정답 장면 {ANCHOR[k]}', font=f_t, fill=(255, 220, 120))
    row.paste(sc, (14, 46))
    ox = sc.width + 40
    for n, t, px, py in placed:
        cb = checker(t.width, t.height)
        cb.paste(t, (0, 0), t)
        row.paste(cb, (ox + px, 46 + py + 18))
        d.text((ox + px, 46 + py), n, font=f_n, fill=(200, 210, 235))
    rows.append(row)

H = sum(r.height for r in rows)
board = Image.new('RGB', (W, H), BG)
yy = 0
for r in rows:
    board.paste(r, (0, yy))
    yy += r.height
board.save(REF + 'parts2_board.png')
# 한 장이 너무 길어 보기 좋게 반으로도 나눈다
half = len(rows) // 2 + 1
for i, chunk in enumerate([rows[:half], rows[half:]], 1):
    b = Image.new('RGB', (W, sum(r.height for r in chunk)), BG)
    yy = 0
    for r in chunk:
        b.paste(r, (0, yy)); yy += r.height
    b.save(REF + f'parts2_board_{i}.png')
print('board', board.size)

# 소환 연출 — 프레임을 GIF 로(검정 바탕, 실제 재생 순서)
fx = [k for k in man if man[k]['bg'] == 'black']
_alt = Image.open(OUT + 'summon_bg/summon_bg.png').convert('RGBA')
ALTAR = Image.new('RGB', _alt.size, (10, 6, 24)); ALTAR.paste(_alt, (0, 0), _alt)
# 제단 아래쪽(마법진 자리)만 쓴다
ALTAR = ALTAR.crop((0, int(ALTAR.height * 0.35), ALTAR.width, ALTAR.height))
strips = []
for k in fx:
    folder = OUT + k.replace('p2_', '') + '/'
    if not os.path.isdir(folder):
        continue
    fr = sorted(f for f in os.listdir(folder) if f.endswith('.png'))
    imgs = [Image.open(folder + f).convert('RGBA') for f in fr]
    if not imgs:
        continue
    frames = []
    for im in imgs:
        # 실제와 같게 — 소환 제단 바닥 위에 가산으로 얹는다(검정 = 아무것도 안 더함)
        bg = ALTAR.resize((int(ALTAR.width * im.height / ALTAR.height * 1.0), im.height), Image.NEAREST)
        cx = bg.width // 2
        bg = np.array(bg.crop((cx - im.width // 2, 0, cx - im.width // 2 + im.width, im.height))).astype(np.float32)
        a = np.array(im).astype(np.float32)
        add = a[..., :3] * (a[..., 3:4] / 255.0)
        frames.append(Image.fromarray(np.clip(bg + add, 0, 255).astype(np.uint8)))
    frames[0].save(REF + f'gif_{k}.gif', save_all=True, append_images=frames[1:], duration=120, loop=0)
    strips.append((k, frames))
# 연출 전부 한 GIF 에 나란히(같은 높이로 맞춤)
if strips:
    th = 360
    sized = [(k, [f.resize((int(f.width * th / f.height), th), Image.NEAREST) for f in fs]) for k, fs in strips]
    n = max(len(fs) for _, fs in sized)
    tw = sum(fs[0].width for _, fs in sized) + 12 * (len(sized) + 1)
    out = []
    for i in range(n):
        c = Image.new('RGB', (tw, th + 34), (8, 6, 18))
        d = ImageDraw.Draw(c)
        x = 12
        for k, fs in sized:
            c.paste(fs[min(i, len(fs) - 1)], (x, 30))
            d.text((x, 6), k.replace('p2_fx_', ''), font=f_n, fill=(255, 220, 120))
            x += fs[0].width + 12
        out.append(c)
    out[0].save(REF + 'gif_summon_fx_all.gif', save_all=True, append_images=out[1:], duration=130, loop=0)
    print('fx gif', len(sized))
