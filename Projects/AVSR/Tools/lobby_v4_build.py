"""로비 v4 — 시안(Reference/Mockups/lobby_hub_v3_jp.png) 픽셀로 로비 부품을 만든다 (2026-09-21 지시: 「이 이미지랑 똑같이」).

v3(lobby_v3_build.py)와 같은 원칙이다.
  - 그림은 **시안 픽셀 그대로** 쓴다. 새로 그리지 않는다.
  - 바뀌는 글자(숫자 · 언어별 글자)만 지우고, 그 자리에 게임이 글자를 찍는다.
    판 위 글자는 줄 잇기로 지운다. **그림 위 글자**(게임 모드 구역)는 줄 잇기로는 번진 자국이 남아
    코덱스가 글자만 지운 판(out_modes_clean.png)에서 **글자 자리만** 떠 온다.
  - 상자 카드 속(제목 · 상자 · 시간 판)은 상태마다 바뀐다. 빈 카드는 코덱스 판(out_plate.png)의 **카드 안쪽만** 떠 온다.

코덱스 그림은 통째로 다시 그린 것이라 글자 밖도 조금씩 다르다 — **통째로 쓰지 않는다.** 필요한 자리만 떠서 가장자리를 녹인다.

출력: Assets/BaseResource/LobbyV4/*.png + lobby_v4_spec.json (글자 자리 · 크기 · 색, 부품 자리)
좌표는 전부 시안 좌표(941×1672). 게임 캔버스(720×1280)로는 × 720/941.
"""
import os, json
import numpy as np
import cv2
from PIL import Image, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
REF = os.path.join(HERE, '..', '_exchange', 'ref', 'lobby_v4')
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'LobbyV4')
FONT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Fonts', 'NotoSansJP-Bold.ttf')
W, H = 941, 1672
SPLIT_Y = 632          # 위판 / 가운데판 경계 — 파란 상자 띠 윗선(636) 바로 위
NAV_Y = 1455           # 가운데판 / 하단 바 판 경계 — 하단 바 위 가로 땅 선(1456) 바로 위
# 긴 화면(20:9 등)에서 남는 세로 공간은 위판 아래 · 하단 바 위로 반씩 나눈다(2026-09-21 지적: 「아래로 치우친다」).
# 가운데판은 그 사이 한가운데에 선다. 두 틈은 코덱스가 이어 그린 바닥 · 도시가 메운다 —
# 틈 끝은 상자 띠 윗선 · 하단 바 땅 선에 닿으므로 이음매가 UI 선 뒤로 숨는다.

# ── 판 위 글자 — 줄 잇기로 지운다 (이름 = 게임 노드 이름, 넉넉한 칸, 정렬, 시안 글자) ──
FLAT_TEXTS = [
    ('GoldText', (392, 28, 494, 72), 'C', '1,357'),
    ('GemText', (616, 28, 718, 72), 'C', '1,000,000'),
    ('SeasonPassTitleText', (566, 122, 692, 156), 'C', 'シーズンパス'),
    ('SeasonPassSubText', (566, 154, 692, 190), 'C', '残り12日'),
    ('EventTitleText', (800, 122, 902, 156), 'C', 'イベント'),
    ('EventSubText', (800, 154, 902, 190), 'C', '開催中'),
    ('GhostSearchTitleText', (132, 272, 268, 306), 'L', 'ゴースト観察'),
    ('GhostSearchTimerText', (136, 308, 302, 352), 'L', '04:32:18'),
    ('GhostSearchGoldText', (116, 360, 302, 408), 'L', '+12,640 G'),
    ('GhostSearchDescText', (50, 426, 314, 498), 'L', 'ゴーストが街を巡って'),
    ('HostButtonSubText', (86, 1614, 226, 1652), 'C', 'ホスト育成'),
    ('ChapterButtonSubText', (390, 1619, 548, 1652), 'C', 'ゲームモード'),
    ('ShopButtonSubText', (728, 1614, 848, 1652), 'C', 'ショップ'),
]

# ── 그림 위 글자 — 코덱스가 지운 판에서 글자 자리만 떠 온다 ──
ART_TEXTS = [
    ('GameModeLabel', (56, 920, 274, 968), 'L', 'ゲームモード'),
    ('ModeScenarioTitleText', (154, 1016, 412, 1070), 'L', 'シナリオモード'),
    ('ModeScenarioSubText', (158, 1070, 382, 1106), 'L', '魂が蘇る新たな物語'),
    ('ModePlayButtonText', (690, 1122, 812, 1174), 'C', 'プレイ'),
    ('ModeSurvivalTitleText', (136, 1328, 364, 1366), 'C', 'サバイバルモード'),
    ('ModeSurvivalSubText', (150, 1366, 350, 1400), 'C', '最後まで生き残れ'),
    ('ModeDefenseTitleText', (576, 1328, 806, 1366), 'C', 'ディフェンスモード'),
    ('ModeDefenseSubText', (576, 1366, 806, 1400), 'C', '押し寄せる敵を撃退せよ'),
]
# 「プレイ ▶」 의 ▶ — 시안 글꼴 삼각형이라 그림으로 뗀다. 코덱스 판에선 함께 지워져 있다
ARROW = (810, 1128, 844, 1170)

# ── 상자 카드 ────────────────────────────────────────────────
CARDS = [(37, 652, 320, 902), (332, 652, 610, 902), (622, 652, 905, 902)]
INSET = 9                       # 카드 틀 두께 — 이 안쪽만 빈 카드로 갈아 끼운다
GRADES = ['silver', 'gold', 'platinum']   # 시안 카드 순서 = 등급
TITLE_Y = (662, 706)            # 카드 제목 줄
CHEST_Y = (700, 831)            # 상자 그림 줄 — 832 부터는 안쪽 판 아랫변이라 섞이면 상자 밑에 줄이 붙는다
PLATE_Y = (832, 896)            # 시간 판 줄
CLOCK = (82, 840, 128, 884)     # 1번 카드 시계
TITLES_JP = ['シルバーチェスト', 'ゴールドチェスト', 'プラチナチェスト']


def load(p):
    return np.asarray(Image.open(p).convert('RGB')).copy()


def align(img):
    """코덱스 그림을 시안 크기 · 자리에 맞춘다(크기 다르면 늘리고, 위상 상관으로 밀린 만큼 되민다)."""
    im = Image.fromarray(img)
    if im.size != (W, H):
        im = im.resize((W, H), Image.LANCZOS)
    a = np.asarray(im).astype(np.float32)
    ref = np.asarray(Image.open(os.path.join(REF, 'mockup_full.png')).convert('L')).astype(np.float32)
    (sx, sy), _ = cv2.phaseCorrelate(ref, a.mean(axis=2))
    print(f'  코덱스 판 어긋남 {sx:+.2f}, {sy:+.2f}')
    if abs(sx) > 0.3 or abs(sy) > 0.3:
        a = cv2.warpAffine(a, np.float32([[1, 0, -sx], [0, 1, -sy]]), (W, H), borderMode=cv2.BORDER_REPLICATE)
    return a


def fit_size(text, box_w, box_h):
    """글자 실제 높이가 시안 칸 높이와 같아지는 폰트 크기(시안 px). 폭도 넘지 않게."""
    best = 8
    for size in range(8, 90):
        f = ImageFont.truetype(FONT, size)
        bb = f.getbbox(text)
        if bb[3] - bb[1] > box_h + 0.5 or bb[2] - bb[0] > box_w * 1.03 + 2:
            break
        best = size
    return best


def text_mask(img, box):
    x0, y0, x1, y1 = box
    roi = img[y0:y1, x0:x1].astype(np.int16)
    edge = np.concatenate([roi[0], roi[-1], roi[:, 0], roi[:, -1]])
    bg = np.median(edge, axis=0)
    dist = np.abs(roi - bg).sum(axis=2)
    m = (dist > 110).astype(np.uint8)
    red = (roi[..., 0] > 150) & (roi[..., 1] < 90) & (roi[..., 2] < 90)
    m[red] = 0
    m = cv2.morphologyEx(m, cv2.MORPH_OPEN, np.ones((2, 2), np.uint8))
    cnt, lab, stats, _ = cv2.connectedComponentsWithStats(m, connectivity=8)
    h, w = m.shape
    for i in range(1, cnt):
        x, y, cw, ch, area = stats[i]
        if (x == 0 or y == 0 or x + cw >= w or y + ch >= h) and area < 80:
            m[lab == i] = 0
    return m, bg


def diff_mask(a, b, box, thresh=70):
    """두 그림의 차이로 글자 잉크를 잡는다 — 그림 위 글자는 바탕이 고르지 않아 이쪽이 맞다."""
    x0, y0, x1, y1 = box
    d = np.abs(a[y0:y1, x0:x1].astype(np.int16) - b[y0:y1, x0:x1].astype(np.int16)).max(axis=2)
    m = (d > thresh).astype(np.uint8)
    m = cv2.morphologyEx(m, cv2.MORPH_OPEN, np.ones((2, 2), np.uint8))
    cnt, lab, stats, _ = cv2.connectedComponentsWithStats(m, connectivity=8)
    for i in range(1, cnt):
        if stats[i, cv2.CC_STAT_AREA] < 12:
            m[lab == i] = 0
    return m, d


def row_fill(img, mask, box, reach=14):
    """판 위 글자 지우기 — 같은 줄의 글자 없는 픽셀 사이를 곧게 잇는다."""
    x0, y0, x1, y1 = box
    H_, W_ = mask.shape
    lx0, lx1 = max(0, x0 - reach), min(W_, x1 + reach)
    blur = cv2.blur(img.astype(np.float32), (3, 3))
    out = img.copy()
    for y in range(max(0, y0), min(H_, y1)):
        row = mask[y, lx0:lx1]
        if not row.any():
            continue
        xs = np.arange(lx0, lx1)
        good = ~row.astype(bool)
        if good.sum() < 2:
            continue
        gx = xs[good]
        for ch in range(3):
            out[y, lx0:lx1, ch][~good] = np.interp(xs[~good], gx, blur[y, gx, ch]).astype(np.uint8)
    return out


def ink_color(img, m, box, bg):
    x0, y0, x1, y1 = box
    roi = img[y0:y1, x0:x1].astype(np.int16)
    if bg is None:
        # 그림 위 글자는 외곽선이 둘려 있다 — 획 **속**(깎아 낸 안쪽)만 재야 본색이 나온다
        core = cv2.erode(m.astype(np.uint8), np.ones((3, 3), np.uint8))
        if core.sum() < 20:
            core = m
        return '#%02X%02X%02X' % tuple(int(c) for c in np.median(roi[core > 0], axis=0))
    dist = np.abs(roi - bg).sum(axis=2)
    sel = (m > 0) & (dist >= np.percentile(dist[m > 0], 75))
    return '#%02X%02X%02X' % tuple(int(c) for c in np.median(roi[sel], axis=0))


def text_spec(img, m, box, align, jp, bg, lines=1):
    x0, y0, x1, y1 = box
    ys, xs = np.nonzero(m)
    tb = [x0 + int(xs.min()), y0 + int(ys.min()), x0 + int(xs.max()) + 1, y0 + int(ys.max()) + 1]
    line_h = (tb[3] - tb[1]) if lines == 1 else (tb[3] - tb[1]) * 0.42
    return {'box': tb, 'color': ink_color(img, m, box, bg), 'align': align, 'area': list(box),
            'size': fit_size(jp, tb[2] - tb[0], line_h)}


# 색으로 재지 않는 그림 위 글자 — 「ゲームモード」 는 칸 아래 흰 무늬까지 같은 색이라 차이로 잰 값이 맞다
COLOR_BOX_SKIP = {'GameModeLabel'}


def color_box(img, box, hex_color, tol=70, pad=6):
    """칸 안에서 글자 본색에 가까운 픽셀의 테두리 상자(외곽선 제외)."""
    x0, y0, x1, y1 = box
    ya, yb = max(0, y0 - pad), min(img.shape[0], y1 + pad)
    roi = img[ya:yb, x0:x1].astype(np.int16)
    col = np.array([int(hex_color[i:i + 2], 16) for i in (1, 3, 5)])
    m = np.abs(roi - col).sum(axis=2) < tol
    ys, xs = np.nonzero(m)
    if len(xs) < 20:
        return None
    return [x0 + int(xs.min()), ya + int(ys.min()), x0 + int(xs.max()) + 1, ya + int(ys.max()) + 1]


def feather_paste(dst, src, mask, grow=4, soft=5):
    """src 를 mask(글자 잉크) 둘레만큼 dst 에 붙인다. 가장자리는 녹인다."""
    m = cv2.dilate(mask.astype(np.uint8), np.ones((2 * grow + 1, 2 * grow + 1), np.uint8)).astype(np.float32)
    m = cv2.GaussianBlur(m, (0, 0), soft / 2.0)
    m = np.clip(m * 1.6, 0, 1)[..., None]
    return (dst.astype(np.float32) * (1 - m) + src.astype(np.float32) * m).astype(np.uint8)


def solid_alpha(a):
    """바깥에서 들이부어 닿지 않는 곳 = 속 — 불투명하게 채운다."""
    body = cv2.morphologyEx((a > 90).astype(np.uint8), cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    flood = np.pad(body, 1)
    ff = np.zeros((flood.shape[0] + 2, flood.shape[1] + 2), np.uint8)
    cv2.floodFill(flood, ff, (0, 0), 2)
    inside = (flood[1:-1, 1:-1] != 2)
    return np.where(inside, 255, a).astype(np.uint8)


def save_rgba(rgb, a, name):
    Image.fromarray(np.dstack([rgb, a]).astype(np.uint8)).save(os.path.join(OUT, f'{name}.png'))


def main():
    os.makedirs(OUT, exist_ok=True)
    M = load(os.path.join(REF, 'mockup_full.png'))
    P = align(load(os.path.join(REF, 'out_plate.png'))).astype(np.uint8)
    C = align(load(os.path.join(REF, 'out_modes_clean.png'))).astype(np.uint8)
    clean = M.copy()
    texts, parts, icons = {}, {}, {}

    # 1) ▶ 아이콘 — 시안에서 뗀다(코덱스 판과의 차이 = ▶)
    x0, y0, x1, y1 = ARROW
    m, d = diff_mask(M, C, ARROW, 60)
    a = cv2.GaussianBlur(cv2.dilate(m * 255, np.ones((3, 3), np.uint8)), (3, 3), 0)
    ys, xs = np.nonzero(a > 8)
    tx0, ty0, tx1, ty1 = int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1
    save_rgba(M[y0:y1, x0:x1][ty0:ty1, tx0:tx1], a[ty0:ty1, tx0:tx1], 'playarrow')
    icons['playarrow'] = [x0 + tx0, y0 + ty0, x0 + tx1, y0 + ty1]

    # 2) 판 위 글자 — 재고 지운다
    full = np.zeros((H, W), np.uint8)
    for name, box, al, jp in FLAT_TEXTS:
        m, bg = text_mask(M, box)
        texts[name] = text_spec(M, m, box, al, jp, bg, 2 if name == 'GhostSearchDescText' else 1)
        bx0, by0, bx1, by1 = box
        full[by0:by1, bx0:bx1] |= m
    full = cv2.dilate(full, np.ones((7, 7), np.uint8))
    for name, (bx0, by0, bx1, by1), al, jp in FLAT_TEXTS:
        clean = row_fill(clean, full, (bx0 - 4, by0 - 4, bx1 + 4, by1 + 4))

    # 3) 그림 위 글자 — 코덱스 판에서 글자 자리만
    #    ⚠ 시안은 글자 뒤에 어두운 반투명 띠를 깔았는데, 코덱스는 그 자리를 밝은 그림으로 다시 그렸다
    #      (잠긴 모드 카드 좀비, 2026-09-21). 그대로 붙이면 글자 뒤만 환해져 글자가 묻힌다.
    #      글자 둘레(글자 아닌 곳)의 시안 밝기에 맞춰 코덱스 그림을 곱해 맞춘 뒤 붙인다.
    C_fit = C.astype(np.float32).copy()
    for name, box, al, jp in ART_TEXTS:
        m, d = diff_mask(M, C, box)
        bx0, by0, bx1, by1 = box
        g = 14
        ex0, ey0, ex1, ey1 = max(0, bx0 - g), max(0, by0 - g), min(W, bx1 + g), min(H, by1 + g)
        ink = np.zeros((ey1 - ey0, ex1 - ex0), np.uint8)
        ink[by0 - ey0:by1 - ey0, bx0 - ex0:bx1 - ex0] = m
        wgt = (cv2.dilate(ink, np.ones((7, 7), np.uint8)) == 0).astype(np.float32)
        def local_mean(img):
            num = cv2.GaussianBlur(img * wgt[..., None], (0, 0), 7)
            den = cv2.GaussianBlur(wgt, (0, 0), 7)[..., None] + 1e-3
            return num / den
        mm = local_mean(M[ey0:ey1, ex0:ex1].astype(np.float32))
        cm = local_mean(C[ey0:ey1, ex0:ex1].astype(np.float32))
        gain = np.clip((mm + 4) / (cm + 4), 0.25, 1.6)
        C_fit[ey0:ey1, ex0:ex1] = np.clip(C[ey0:ey1, ex0:ex1] * gain, 0, 255)
    C_fit = C_fit.astype(np.uint8)
    for name, box, al, jp in ART_TEXTS:
        m, d = diff_mask(M, C, box)
        texts[name] = text_spec(M, m, box, al, jp, None)
        # ⚠ 잉크 칸은 **글자 색**으로 다시 잰다. 코덱스 판과의 차이로 재면 코덱스가 글자 밑 그림까지
        #   조금 바꿔 그린 탓에 칸이 부풀어, 제목 · 설명이 맞붙은 칸으로 잡혀 게임에서 겹쳤다(2026-09-21).
        cb = color_box(M, box, texts[name]['color'])
        if cb is not None and name not in COLOR_BOX_SKIP:
            texts[name]['box'] = cb
            texts[name]['size'] = fit_size(jp, cb[2] - cb[0], cb[3] - cb[1])
        bx0, by0, bx1, by1 = box
        region = np.zeros((H, W), np.uint8)
        region[by0:by1, bx0:bx1] = m
        clean = feather_paste(clean, C_fit, region)
    # ▶ 자리도
    region = np.zeros((H, W), np.uint8)
    m, _ = diff_mask(M, C, ARROW, 60)
    region[ARROW[1]:ARROW[3], ARROW[0]:ARROW[2]] = m
    clean = feather_paste(clean, C, region)

    # 4) 상자 카드 — 글자 먼저 잰다
    for i, (cx0, cy0, cx1, cy1) in enumerate(CARDS):
        tbox = (cx0 + INSET, TITLE_Y[0], cx1 - INSET, TITLE_Y[1])
        m, bg = text_mask(M, tbox)
        texts[f'_title{i + 1}'] = text_spec(M, m, tbox, 'C', TITLES_JP[i], bg)
    # 시간 글자(카드마다) — 흰 판 위라 text_mask 로. 시계는 뺀다
    times = []
    for i, (cx0, cy0, cx1, cy1) in enumerate(CARDS):
        dx = cx0 - CARDS[0][0]
        tbox = (CLOCK[2] + dx + 6, PLATE_Y[0] + 10, cx1 - INSET - 16, PLATE_Y[1] - 10)
        m, bg = text_mask(M, tbox)
        texts[f'_time{i + 1}'] = text_spec(M, m, tbox, 'L', '3時間 12分', bg)
        times.append((tbox, m))
    # 글자 보정(lobby_calib.py)이 카드 속 글자 잉크를 잴 수 있게 debug_clean 에서도 지운다 — 바탕판과는 무관하다
    for i, (cx0, cy0, cx1, cy1) in enumerate(CARDS):
        for nm in (f'_title{i + 1}', f'_time{i + 1}'):
            ax0, ay0, ax1, ay1 = texts[nm]['area']
            m, _ = text_mask(M, (ax0, ay0, ax1, ay1))
            full = np.zeros((H, W), np.uint8)
            full[ay0:ay1, ax0:ax1] = cv2.dilate(m, np.ones((5, 5), np.uint8))
            clean = row_fill(clean, full, (ax0 - 2, ay0 - 2, ax1 + 2, ay1 + 2), reach=8)

    # 빈 카드 바탕 = 코덱스 판의 카드 안쪽(가장자리 녹임)
    base = clean.copy()
    for cx0, cy0, cx1, cy1 in CARDS:
        ix0, iy0, ix1, iy1 = cx0 + INSET, cy0 + INSET, cx1 - INSET, cy1 - INSET
        a = np.ones((iy1 - iy0, ix1 - ix0), np.float32)
        for k in range(5):
            f = (k + 1) / 6
            a[k, :] = np.minimum(a[k, :], f); a[-1 - k, :] = np.minimum(a[-1 - k, :], f)
            a[:, k] = np.minimum(a[:, k], f); a[:, -1 - k] = np.minimum(a[:, -1 - k], f)
        roi = base[iy0:iy1, ix0:ix1].astype(np.float32)
        base[iy0:iy1, ix0:ix1] = (roi * (1 - a[..., None]) + P[iy0:iy1, ix0:ix1] * a[..., None]).astype(np.uint8)

    # 상자 그림 — 시안 − 빈 카드의 차이
    for i, (cx0, cy0, cx1, cy1) in enumerate(CARDS):
        box = (cx0 + INSET + 4, CHEST_Y[0], cx1 - INSET - 4, CHEST_Y[1])
        bx0, by0, bx1, by1 = box
        d = np.abs(M[by0:by1, bx0:bx1].astype(np.int16) - base[by0:by1, bx0:bx1].astype(np.int16)).sum(axis=2)
        a = np.clip((d - 30) * 5, 0, 255).astype(np.uint8)
        a = solid_alpha(a)
        cnt, lab, stats, _ = cv2.connectedComponentsWithStats((a > 60).astype(np.uint8), connectivity=8)
        if cnt > 1:
            keep = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))   # 상자 한 덩어리만 — 판 무늬 차이는 버린다
            a = np.where(cv2.dilate((lab == keep).astype(np.uint8), np.ones((5, 5), np.uint8)) > 0, a, 0)
        a = cv2.GaussianBlur(a, (3, 3), 0)
        ys, xs = np.nonzero(a > 8)
        tx0, ty0, tx1, ty1 = int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1
        save_rgba(M[by0:by1, bx0:bx1][ty0:ty1, tx0:tx1], a[ty0:ty1, tx0:tx1], f'chest_{GRADES[i]}')
        parts[f'chest_{GRADES[i]}'] = [bx0 + tx0, by0 + ty0, bx0 + tx1, by0 + ty1, i]

    # 시계 — 흰 판 위 검은 그림
    m, bg = text_mask(M, CLOCK)
    a = cv2.GaussianBlur(cv2.dilate(m * 255, np.ones((3, 3), np.uint8)), (3, 3), 0)
    ys, xs = np.nonzero(a > 8)
    tx0, ty0, tx1, ty1 = int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1
    x0, y0 = CLOCK[0], CLOCK[1]
    save_rgba(M[y0:CLOCK[3], x0:CLOCK[2]][ty0:ty1, tx0:tx1], a[ty0:ty1, tx0:tx1], 'clock')
    icons['clock'] = [x0 + tx0, y0 + ty0, x0 + tx1, y0 + ty1]

    # 시간 판 — 1번 카드에서, 시계 · 글자를 지운 뒤 빈 카드와의 차이로
    plate_src = M.copy()
    pm = np.zeros((H, W), np.uint8)
    cm, _ = text_mask(M, CLOCK)
    pm[CLOCK[1]:CLOCK[3], CLOCK[0]:CLOCK[2]] |= cm
    (tb, tm) = times[0]
    pm[tb[1]:tb[3], tb[0]:tb[2]] |= tm
    pm = cv2.dilate(pm, np.ones((5, 5), np.uint8))
    plate_src = row_fill(plate_src, pm, (CARDS[0][0], PLATE_Y[0], CARDS[0][2], PLATE_Y[1]), reach=10)
    bx0, by0, bx1, by1 = CARDS[0][0] + INSET, PLATE_Y[0], CARDS[0][2] - INSET, PLATE_Y[1]
    d = np.abs(plate_src[by0:by1, bx0:bx1].astype(np.int16) - base[by0:by1, bx0:bx1].astype(np.int16)).sum(axis=2)
    a = solid_alpha(np.clip((d - 24) * 6, 0, 255).astype(np.uint8))
    a = cv2.GaussianBlur(a, (3, 3), 0)
    ys, xs = np.nonzero(a > 8)
    tx0, ty0, tx1, ty1 = int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1
    save_rgba(plate_src[by0:by1, bx0:bx1][ty0:ty1, tx0:tx1], a[ty0:ty1, tx0:tx1], 'timeplate')
    parts['timeplate'] = [bx0 + tx0, by0 + ty0, bx0 + tx1, by0 + ty1, 0]

    # 5) 긴 화면 틈 — 코덱스가 이어 그린 것. 틈은 판 **뒤**에 깐다(앞 판이 불투명이라 이음 줄을 덮는다)
    spec_extra = {}

    def continuation(path, ref_rows, ref_y0, start_y):
        """코덱스 그림에서 시안 줄(ref_rows)과 가장 잘 맞는 세로 자리를 찾아, start_y 부터 아래를 떼어 온다."""
        raw = Image.open(path).convert('RGB')
        raw = raw.resize((W, round(raw.height * W / raw.width)), Image.LANCZOS)
        R = np.asarray(raw).astype(np.float32)
        ref = ref_rows.astype(np.float32).mean(axis=2)
        best, off = 1e9, 0
        for dy in range(-60, 61):
            yy = ref_y0 + dy
            if yy < 0 or yy + ref.shape[0] > R.shape[0]:
                continue
            e = np.abs(R[yy:yy + ref.shape[0]].mean(axis=2) - ref).mean()
            if e < best:
                best, off = e, dy
        print('  ', os.path.basename(path), '세로 어긋남', off, '평균 차', round(float(best), 1))
        return np.clip(R[start_y + off:], 0, 255).astype(np.uint8)

    # 위 틈 — 유령 장면 아래 광장(다시 발주한 것이 있으면 그것)
    ext_path = os.path.join(REF, 'out_extend2_raw.png')
    if not os.path.exists(ext_path):
        ext_path = os.path.join(REF, 'out_extend_raw.png')
    if os.path.exists(ext_path):
        ext = continuation(ext_path, M[300:SPLIT_Y], 300, SPLIT_Y)
        Image.fromarray(ext).save(os.path.join(OUT, 'base_extend.png'))
        spec_extra['extendH'] = int(ext.shape[0])

    # 아래 틈 — 게임 모드 아래 밤 도시. 입력은 시안 1100~1454 줄 + 빈 곳
    city_path = os.path.join(REF, 'out_city_raw.png')
    if os.path.exists(city_path):
        city = continuation(city_path, M[1100:NAV_Y], 0, NAV_Y - 1100)
        Image.fromarray(city).save(os.path.join(OUT, 'base_city.png'))
        # 태블릿 양옆 — 도시 가장자리 줄을 늘여 쓴다(예전엔 하단 바 판이 이 높이까지 덮었다)
        Image.fromarray(city[:, :6]).save(os.path.join(OUT, 'city_side_l.png'))
        Image.fromarray(city[:, -6:]).save(os.path.join(OUT, 'city_side_r.png'))
        spec_extra['cityH'] = int(city.shape[0])

    # 6) 태블릿 양옆
    sides_path = os.path.join(REF, 'out_sides_raw.png')
    if os.path.exists(sides_path):
        SIDE = 170
        raw = Image.open(sides_path).convert('RGB').resize((W + 2 * SIDE, H), Image.LANCZOS)
        R = np.asarray(raw).astype(np.float32)
        best, off = 1e9, 0
        for dx in range(-30, 31):
            a0 = SIDE + dx
            if a0 < 0 or a0 + W > R.shape[1]:
                continue
            e = np.abs(R[:, a0:a0 + 40] - M[:, :40]).mean() + np.abs(R[:, a0 + W - 40:a0 + W] - M[:, W - 40:]).mean()
            if e < best:
                best, off = e, dx
        left = R[:, off:SIDE + off]
        right = R[:, SIDE + off + W:SIDE + off + W + SIDE]
        for nm, strip in (('side_left', left), ('side_right', right)):
            s8 = np.clip(strip, 0, 255).astype(np.uint8)
            Image.fromarray(s8[:SPLIT_Y]).save(os.path.join(OUT, f'{nm}_top.png'))
            Image.fromarray(s8[SPLIT_Y:NAV_Y]).save(os.path.join(OUT, f'{nm}_mid.png'))
            # 하단 바는 로비 · 육성 공통이라 growth_build.py 가 만든다(2026-09-21)
        spec_extra['sideW'] = SIDE
        print('sides: 가로 어긋남', off, '평균 차', round(float(best) / 2, 1))

    Image.fromarray(base[:SPLIT_Y]).save(os.path.join(OUT, 'base_top.png'))
    Image.fromarray(base[SPLIT_Y:NAV_Y]).save(os.path.join(OUT, 'base_mid.png'))
    Image.fromarray(clean).save(os.path.join(REF, 'debug_clean.png'))
    Image.fromarray(base).save(os.path.join(REF, 'debug_base.png'))

    flat = {
        'mockupW': W, 'mockupH': H, 'splitY': SPLIT_Y, 'navY': NAV_Y,
        'extendH': spec_extra.get('extendH', 0), 'cityH': spec_extra.get('cityH', 0),
        'sideW': spec_extra.get('sideW', 0),
        'texts': [dict(name=k, **v) for k, v in texts.items()],
        'parts': [dict(name=k, box=v[:4], card=v[4]) for k, v in parts.items()],
        'icons': [dict(name=k, box=v) for k, v in icons.items()],
        'slots': [dict(name=f'ChestSlot{i + 1}', box=list(c)) for i, c in enumerate(CARDS)],
    }
    json.dump(flat, open(os.path.join(OUT, 'lobby_v4_spec.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    for k, v in texts.items():
        print(f"{k:24s} {v['box']} {v['color']} size {v['size']}")
    print('parts', {k: v for k, v in parts.items()})


if __name__ == '__main__':
    main()
