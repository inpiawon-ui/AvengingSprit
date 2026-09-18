"""로비 v3 — 시안(lobby_hub_v2.png) 픽셀로 로비 부품을 만든다 (2026-09-18 지시: 시안과 100% 똑같이).

원칙
  - 그림은 **시안 픽셀 그대로** 쓴다. 새로 그리지 않는다.
  - 바뀌는 글자(숫자 · 언어별 글자)만 시안에서 지우고, 그 자리에 게임이 글자를 찍는다.
  - 상자 세 칸 안쪽만 상태마다 바뀌므로, 빈 칸 바탕은 코덱스가 비워 준 배경판(out_plate.png)에서 가져온다.
  - 그 밖(카드 · 하단 바 · 상단)은 시안 그대로라 기본 상태에서 게임 화면 = 시안이다.

출력: Assets/BaseResource/LobbyV3/*.png + lobby_v3_spec.json (글자 자리 · 크기 · 색, 부품 자리)
좌표는 전부 시안 좌표(941×1672). 게임 캔버스(720×1280)로는 × 720/941.
"""
import os, json
import numpy as np
import cv2
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
REF = os.path.join(HERE, '..', '_exchange', 'ref', 'lobby_v3')
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'LobbyV3')
W, H = 941, 1672
SPLIT_Y = 620          # 위판 / 아래판 경계 — 긴 화면에서 위판은 위에, 아래판은 아래에 붙는다

# ── 지울 글자 칸 (시안 좌표, 넉넉하게) ──────────────────────────────
# (이름 = 게임 노드 이름, 칸, 정렬)
TEXTS = [
    ('GoldText', (368, 28, 512, 72), 'C'),
    ('GemText', (672, 28, 768, 72), 'C'),
    ('SeasonPassTitleText', (585, 126, 690, 160), 'C'),
    ('SeasonPassSubText', (585, 160, 690, 195), 'C'),
    ('EventTitleText', (812, 126, 895, 160), 'C'),
    ('EventSubText', (812, 160, 895, 195), 'C'),
    ('GhostSearchTitleText', (130, 250, 260, 294), 'L'),
    ('GhostSearchTimerText', (128, 296, 302, 340), 'L'),
    ('GhostSearchGoldText', (106, 356, 288, 402), 'L'),
    ('GhostSearchDescText', (40, 420, 312, 488), 'L'),
    ('GhostSearchClaimText', (740, 526, 892, 576), 'C'),
    ('GameModeLabel', (50, 912, 208, 958), 'L'),
    ('ModeSideLeftTitleText', (58, 1262, 232, 1300), 'C'),
    ('ModeSideLeftSubText', (58, 1302, 232, 1336), 'C'),
    ('ModeCenterTitleText', (400, 1204, 622, 1254), 'L'),
    ('ModeCenterSubText', (402, 1258, 622, 1292), 'C'),
    ('ModePlayButtonText', (378, 1308, 545, 1356), 'C'),   # ▶ 는 그림(playarrow)으로 뗀다
    ('ModeSideRightTitleText', (706, 1262, 880, 1300), 'C'),
    ('ModeSideRightSubText', (706, 1302, 880, 1336), 'C'),
    ('HostButtonTitleText', (156, 1518, 252, 1560), 'L'),
    ('HostButtonSubText', (156, 1562, 278, 1596), 'L'),
    ('ChapterButtonTitleText', (464, 1515, 570, 1560), 'L'),
    ('ChapterButtonSubText', (464, 1562, 568, 1596), 'L'),
    ('ShopButtonTitleText', (772, 1518, 872, 1560), 'L'),
    ('ShopButtonSubText', (776, 1562, 830, 1596), 'L'),
    # 상자 칸 속 — 부품으로 떼기 전에 지운다
    ('_time1', (130, 758, 266, 798), 'L'), ('_time2', (430, 758, 566, 798), 'L'),
    ('_cost1', (158, 810, 240, 846), 'C'), ('_cost2', (456, 810, 530, 846), 'C'),
    ('_label1', (128, 844, 228, 874), 'C'), ('_label2', (428, 844, 526, 874), 'C'),
    ('_ready', (718, 634, 808, 670), 'C'), ('_claim', (680, 824, 860, 868), 'C'),
]

# 아이콘 — 글자와 한 줄로 가운데 맞추려고 판에서 떼어 따로 둔다
ICONS = [
    ('clock', (90, 760, 128, 796)),      # 1번 칸 시계
    ('gem', (112, 810, 156, 846)),       # 1번 칸 젬
    ('playarrow', (550, 1314, 578, 1350)),   # 「플레이하기 ▶」 의 ▶ — 시안 글꼴의 좁은 삼각형이라 우리 글꼴로는 모양이 안 나온다
]

# 상자 칸 (시안 틀 기준) — 안쪽(inset)만 빈 칸 바탕으로 갈아 끼운다
SLOTS = [(27, 622, 318, 897), (333, 622, 613, 897), (627, 622, 913, 897)]
INSET = 11
TOP_STRIP = (612, 640)   # 칸 윗변 줄 — 2번 칸은 이 줄에 상자가 안 닿는다

# 줄 잇기 대신 번짐으로 메울 글자 — 판이 아니라 그림 위에 있다
INPAINT_TEXTS = {'GameModeLabel'}
# 1번 칸 시간 판 윗변 중 상자 발에 덮인 곳
PLATE_FEET = (90, 746, 272, 760)
# 속이 찬 판 — 빈 칸 바탕과 색이 비슷해 뚫린 안쪽을 채운다
SOLID_PARTS = {'timeplate', 'gembutton', 'goldbutton', 'readybanner'}

# 칸 속 부품 (시안 좌표) — 어느 칸에서 떼는지
PARTS = [
    ('chest_blue', 0, (40, 626, 305, 760)),     # 옆으로 번진 빛까지
    ('chest_purple', 1, (346, 626, 600, 760)),
    ('chest_black', 2, (650, 684, 890, 807)),   # 위는 완료 띠 월계수, 아래는 금색 버튼 윗변 — 빼고 뗀다
    ('timeplate', 0, (48, 753, 302, 804)),
    ('gembutton', 0, (46, 802, 304, 884)),
    ('goldbutton', 2, (642, 806, 894, 884)),
    ('readybanner', 2, (660, 622, 880, 684)),
]


# 시안 글자(한국어) — 글자 크기를 이 글자로 잰다. 게임은 언어 표에서 같은 글자를 읽는다.
KOREAN = {
    'GoldText': '125,680', 'GemText': '2,340',
    'SeasonPassTitleText': '시즌 패스', 'SeasonPassSubText': '12일 남음',
    'EventTitleText': '이벤트', 'EventSubText': '진행 중',
    'GhostSearchTitleText': '유령 수색', 'GhostSearchTimerText': '04:32:18',
    'GhostSearchGoldText': '+ 12,640 G', 'GhostSearchDescText': '유령이 도시 곳곳을 떠돌며',
    'GhostSearchClaimText': '보상 받기', 'GameModeLabel': '게임 모드',
    'ModeSideLeftTitleText': '서바이벌 모드', 'ModeSideLeftSubText': '끝까지 살아남아라',
    'ModeCenterTitleText': '시나리오 모드', 'ModeCenterSubText': '영혼이 깃든 새로운 이야기',
    'ModePlayButtonText': '플레이하기',
    'ModeSideRightTitleText': '디펜스 모드', 'ModeSideRightSubText': '몰려오는 적을 막아라',
    'HostButtonTitleText': 'HOST', 'HostButtonSubText': '호스트 육성',
    'ChapterButtonTitleText': 'PLAY', 'ChapterButtonSubText': '게임 모드',
    'ShopButtonTitleText': 'SHOP', 'ShopButtonSubText': '상점',
    '_time1': '3시간 12분', '_cost1': '1,000', '_label1': '즉시 열기', '_ready': '완료!', '_claim': '보상 획득하기',
}
FONT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Fonts', 'NotoSansKR-Bold.ttf')


def fit_size(text, box_w, box_h):
    """PIL 로 찍어 글자 실제 높이가 시안 칸 높이와 같아지는 폰트 크기(시안 픽셀). 폭도 넘지 않게."""
    from PIL import ImageFont
    best = 8
    for size in range(8, 90):
        f = ImageFont.truetype(FONT, size)
        bb = f.getbbox(text)
        if bb[3] - bb[1] > box_h + 0.5 or bb[2] - bb[0] > box_w * 1.03 + 2:
            break
        best = size
    return best


def row_fill(img, mask, box, reach=14):
    """판 위 글자 지우기 — 같은 줄의 글자 없는 픽셀 사이를 곧게 잇는다.
    판(버튼 · 띠)은 가로로 고른 색이라 줄마다 잇는 것이 번짐(inpaint)보다 자국이 안 남는다."""
    x0, y0, x1, y1 = box
    H_, W_ = mask.shape
    lx0, lx1 = max(0, x0 - reach), min(W_, x1 + reach)
    src = img.astype(np.float32)
    # 끝점 잡음을 줄이려 3×3 평균을 쓴다
    blur = cv2.blur(src, (3, 3))
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


def load(p):
    return np.asarray(Image.open(p).convert('RGB')).copy()


def text_mask(img, box):
    x0, y0, x1, y1 = box
    roi = img[y0:y1, x0:x1].astype(np.int16)
    edge = np.concatenate([roi[0], roi[-1], roi[:, 0], roi[:, -1]])
    bg = np.median(edge, axis=0)
    dist = np.abs(roi - bg).sum(axis=2)
    m = (dist > 110).astype(np.uint8)
    # 빨간 알림 뱃지(!)는 글자가 아니다 — 칸 모서리에 걸쳐 있어 섞이면 글자 크기가 크게 잡힌다
    red = (roi[..., 0] > 150) & (roi[..., 1] < 90) & (roi[..., 2] < 90)
    m[red] = 0
    m = cv2.morphologyEx(m, cv2.MORPH_OPEN, np.ones((2, 2), np.uint8))
    # 칸 경계에 걸친 작은 조각은 이웃 글자 · 판 빛의 끝자락이다 — 섞이면 글자 칸이 커진다(「즉시 열기」 2026-09-18)
    cnt, lab, stats, _ = cv2.connectedComponentsWithStats(m, connectivity=8)
    h, w = m.shape
    for i in range(1, cnt):
        x, y, cw, ch, area = stats[i]
        touches = x == 0 or y == 0 or x + cw >= w or y + ch >= h
        if touches and area < 80:
            m[lab == i] = 0
    return m, bg


def main():
    os.makedirs(OUT, exist_ok=True)
    M = load(os.path.join(REF, 'mockup_full.png'))
    P = load(os.path.join(REF, 'out_plate.png'))
    clean = M.copy()
    spec = {'mockup': [W, H], 'split_y': SPLIT_Y, 'texts': {}, 'parts': {}, 'icons': {}, 'slots': SLOTS}

    # 1) 아이콘 먼저 떼어 둔다(시안 그대로)
    for name, (x0, y0, x1, y1) in ICONS:
        roi = M[y0:y1, x0:x1]
        m, bg = text_mask(M, (x0, y0, x1, y1))
        a = cv2.dilate(m * 255, np.ones((3, 3), np.uint8))
        a = cv2.GaussianBlur(a, (3, 3), 0)
        # 그림은 알파가 닿는 만큼(가장자리 번짐 포함)으로 자르고, 그 상자를 그대로 자리로 쓴다
        # ⚠ 넉넉한 칸으로 저장하고 잉크 상자에 꽂으면 아이콘이 찌그러지며 작아진다(2026-09-18)
        ys, xs = np.nonzero(a > 8)
        tx0, ty0, tx1, ty1 = int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1
        Image.fromarray(np.dstack([roi, a])[ty0:ty1, tx0:tx1]).save(os.path.join(OUT, f'{name}.png'))
        spec['icons'][name] = [x0 + tx0, y0 + ty0, x0 + tx1, y0 + ty1]

    # 2) 글자 · 아이콘 자리를 지운다(시안 픽셀을 둘레에서 메운다)
    full = np.zeros((H, W), np.uint8)
    for name, box, align in TEXTS:
        m, bg = text_mask(M, box)
        x0, y0, x1, y1 = box
        ys, xs = np.nonzero(m)
        if len(xs):
            tb = [x0 + int(xs.min()), y0 + int(ys.min()), x0 + int(xs.max()) + 1, y0 + int(ys.max()) + 1]
            # 글자 색 = 바탕과 가장 먼 픽셀들(상위 25%)의 가운뎃값 — 가장자리 섞인 색을 빼야 글자 본색이 나온다
            roi = M[y0:y1, x0:x1].astype(np.int16)
            dist = np.abs(roi - bg).sum(axis=2)
            sel = (m > 0) & (dist >= np.percentile(dist[m > 0], 75))
            col = np.median(roi[sel], axis=0)
            # 두 줄 글자(유령 수색 설명)는 한 줄 높이로 크기를 잰다
            lines = 2 if name == 'GhostSearchDescText' else 1
            line_h = (tb[3] - tb[1]) if lines == 1 else (tb[3] - tb[1]) * 0.42
            size = fit_size(KOREAN.get(name, ''), tb[2] - tb[0], line_h) if name in KOREAN else 0
            spec['texts'][name] = {'box': tb, 'color': '#%02X%02X%02X' % tuple(int(c) for c in col),
                                   'align': align, 'area': list(box), 'size': size}
        full[y0:y1, x0:x1] |= m
    for name, (x0, y0, x1, y1) in ICONS:
        m, _ = text_mask(M, (x0, y0, x1, y1))
        full[y0:y1, x0:x1] |= m
    full = cv2.dilate(full, np.ones((7, 7), np.uint8))
    # 판 위 글자는 줄 잇기, 골목 그림 위 글자(게임 모드)만 번짐으로 메운다
    telea = np.zeros_like(full)
    for name, (x0, y0, x1, y1), align in TEXTS:
        if name in INPAINT_TEXTS:
            telea[y0 - 4:y1 + 4, x0 - 4:x1 + 4] = full[y0 - 4:y1 + 4, x0 - 4:x1 + 4]
    clean = cv2.inpaint(clean, telea, 6, cv2.INPAINT_TELEA)
    rows = full & ~telea
    for name, box, align in TEXTS:
        if name not in INPAINT_TEXTS:
            x0, y0, x1, y1 = box
            clean = row_fill(clean, rows, (x0 - 4, y0 - 4, x1 + 4, y1 + 4))
    for name, (x0, y0, x1, y1) in ICONS:
        clean = row_fill(clean, rows, (x0 - 4, y0 - 4, x1 + 4, y1 + 4))
    # 1번 칸 시간 판 윗변을 상자 발이 덮고 있다 — 상자가 바뀌면 파란 발이 남는다. 윗변을 줄 잇기로 되살린다
    feet = np.zeros_like(full)
    fx0, fy0, fx1, fy1 = PLATE_FEET
    feet[fy0:fy1, fx0:fx1] = 1
    parts_src_feet = row_fill(clean, feet, (fx0, fy0, fx1, fy1), reach=3)
    parts_src = clean.copy()   # 띠를 덮기 전 — 부품은 여기서 뗀다

    # 3번 칸 윗변은 「완료!」 띠가 틀을 덮고 있다 — 2번 칸 윗변(상자가 안 닿는 줄)을 떠서 폭만 맞춰 붙인다
    s2, s3 = SLOTS[1], SLOTS[2]
    strip = clean[TOP_STRIP[0]:TOP_STRIP[1], s2[0] - 4:s2[2] + 4]
    strip = cv2.resize(strip, (s3[2] - s3[0] + 8, TOP_STRIP[1] - TOP_STRIP[0]), interpolation=cv2.INTER_CUBIC)
    clean[TOP_STRIP[0]:TOP_STRIP[1], s3[0] - 4:s3[2] + 4] = strip

    # 3) 칸 속 부품을 떼어 낸다 — 글자가 지워진 시안에서, 빈 칸 바탕과 다른 곳만
    #    빈 칸 바탕: 시안 칸 안쪽을 줄마다 양 끝 바탕색으로 잇는다.
    #    ⚠ 코덱스가 비워 준 판은 시안 바탕과 색이 달라, 그 위에 뗀 상자 빛 가장자리에 네모 경계가 보였다(2026-09-18).
    #      시안 제 바탕색으로 비우면 부품 가장자리가 바탕에 그대로 녹는다.
    base = clean.copy()
    empties = []
    #    줄마다 칸 안쪽 양 끝(각 6px)의 바탕색을 재서 **어두운 쪽**을 그 줄 색으로 쓰고, 세로로만 부드럽게 한다.
    #    ⚠ 양 끝을 잇기만 하면 판 테두리 빛이 한쪽 끝에 걸려 가로 줄무늬가 남는다. 빛은 밝으니 어두운 쪽이 바탕이다.
    for i, (x0, y0, x1, y1) in enumerate(SLOTS):
        ix0, iy0, ix1, iy1 = x0 + INSET, y0 + INSET, x1 - INSET, y1 - INSET
        roi = clean[iy0:iy1, ix0:ix1].astype(np.float32)
        h_, w_ = roi.shape[:2]
        left = np.median(roi[:, 1:7], axis=1)
        right = np.median(roi[:, -7:-1], axis=1)
        prof = np.where((left.sum(1) <= right.sum(1))[:, None], left, right)
        prof = cv2.GaussianBlur(prof[:, None, :], (1, 0), sigmaX=0.1, sigmaY=6)[:, 0, :]
        empty = np.repeat(prof[:, None, :], w_, axis=1)
        # 시안 바탕의 잔 무늬(노이즈)를 조금 되살린다 — 너무 매끈하면 칸만 플라스틱처럼 뜬다
        rng = np.random.default_rng(7 + i)
        empty += rng.normal(0, 1.2, empty.shape[:2])[..., None]        # 가장자리 6px 는 시안 안쪽과 섞어 틀과의 이음매를 없앤다
        a = np.ones((h_, w_), np.float32)
        for k in range(6):
            f = (k + 1) / 7
            a[k, :] = np.minimum(a[k, :], f); a[-1 - k, :] = np.minimum(a[-1 - k, :], f)
            a[:, k] = np.minimum(a[:, k], f); a[:, -1 - k] = np.minimum(a[:, -1 - k], f)
        empty = roi * (1 - a[..., None]) + empty * a[..., None]
        empties.append(np.clip(empty, 0, 255).astype(np.uint8))

    for name, si, (x0, y0, x1, y1) in PARTS:
        sx0, sy0, sx1, sy1 = SLOTS[si]
        e = empties[si]
        bg = np.zeros_like(clean)
        bg[:] = clean
        bg[sy0 + INSET:sy1 - INSET, sx0 + INSET:sx1 - INSET] = e
        srcimg = parts_src_feet if name == 'timeplate' else parts_src
        roi = srcimg[y0:y1, x0:x1].astype(np.int16)
        d = np.abs(roi - bg[y0:y1, x0:x1].astype(np.int16)).sum(axis=2)
        a = np.clip((d - 18) * 6, 0, 255).astype(np.uint8)
        a = cv2.morphologyEx(a, cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
        # 칸 틀(바깥 INSET)은 부품에 넣지 않는다 — 틀은 바탕에 있다
        ly0 = 0 if name == 'readybanner' else max(0, sy0 + INSET - y0); ly1 = min(y1 - y0, sy1 - INSET - y0)
        lx0 = max(0, sx0 + INSET - x0); lx1 = min(x1 - x0, sx1 - INSET - x0)
        clip = np.zeros_like(a); clip[ly0:ly1, lx0:lx1] = 1
        a = a * clip
        if name in SOLID_PARTS:
            # 바깥에서 들이부어 닿지 않는 곳 = 판 안쪽 — 불투명하게 채운다
            solid = (a > 60).astype(np.uint8)
            flood = np.pad(solid, 1)
            ff = np.zeros((flood.shape[0] + 2, flood.shape[1] + 2), np.uint8)
            cv2.floodFill(flood, ff, (0, 0), 2)
            inside = (flood[1:-1, 1:-1] != 2)
            a = np.where(inside, 255, a).astype(np.uint8) * clip
        if name.startswith('chest_'):
            # 상자 몸통의 어두운 면은 빈 칸 바탕과 색이 비슷해 구멍이 난다 — 몸통 윤곽 안을 채운다
            body = cv2.morphologyEx((a > 100).astype(np.uint8), cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
            flood = np.pad(body, 1)
            ff = np.zeros((flood.shape[0] + 2, flood.shape[1] + 2), np.uint8)
            cv2.floodFill(flood, ff, (0, 0), 2)
            inside = (flood[1:-1, 1:-1] != 2).astype(np.uint8)
            a = np.maximum(a, inside * 255).astype(np.float32)
            # 부품 상자 가장자리에서 빛이 칼같이 잘리면 네모가 보인다 — 둘레 10px 을 0 으로 흘린다
            h_, w_ = a.shape
            ramp = np.ones((h_, w_), np.float32)
            r = 10
            for k in range(r):
                f = k / r
                ramp[k, :] = np.minimum(ramp[k, :], f)
                ramp[:, k] = np.minimum(ramp[:, k], f); ramp[:, -1 - k] = np.minimum(ramp[:, -1 - k], f)
                if name == 'chest_black':
                    ramp[-1 - k, :] = np.minimum(ramp[-1 - k, :], f)
            a = np.maximum(a * ramp, inside * 255)
            if name == 'chest_black':
                # 빛살이 부품 상자 끝까지 차 있어 속 채우기가 사각형 전체를 덮는다 — 둘레는 무조건 흘린다
                a = a * ramp
            a = a.astype(np.uint8)
        Image.fromarray(np.dstack([srcimg[y0:y1, x0:x1], a])).save(os.path.join(OUT, f'{name}.png'))
        spec['parts'][name] = [x0, y0, x1, y1]

    for i, (x0, y0, x1, y1) in enumerate(SLOTS):
        base[y0 + INSET:y1 - INSET, x0 + INSET:x1 - INSET] = empties[i]

    Image.fromarray(base[:SPLIT_Y]).save(os.path.join(OUT, 'base_top.png'))
    Image.fromarray(base[SPLIT_Y:]).save(os.path.join(OUT, 'base_bottom.png'))
    Image.fromarray(clean).save(os.path.join(REF, 'debug_clean.png'))
    Image.fromarray(base).save(os.path.join(REF, 'debug_base.png'))
    # 유니티 JsonUtility 가 읽게 목록으로 편다
    flat = {
        'mockupW': W, 'mockupH': H, 'splitY': SPLIT_Y,
        'texts': [dict(name=k, **v) for k, v in spec['texts'].items()],
        'parts': [dict(name=k, box=v) for k, v in spec['parts'].items()],
        'icons': [dict(name=k, box=v) for k, v in spec['icons'].items()],
        'slots': [dict(name=f'ChestSlot{i + 1}', box=list(s)) for i, s in enumerate(SLOTS)],
    }
    json.dump(flat, open(os.path.join(OUT, 'lobby_v3_spec.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print('parts', list(spec['parts']), 'texts', len(spec['texts']))


if __name__ == '__main__':
    main()











