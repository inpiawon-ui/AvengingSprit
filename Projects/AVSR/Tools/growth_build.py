"""육성 화면(유령 · 호스트) — 시안(Reference/Mockups/growth/*.png, 720×1280) 픽셀로 부품을 만든다 (2026-09-21).

로비 v4(lobby_v4_build.py)와 같은 원칙이다.
  - 바탕은 **시안 픽셀 그대로**. 바뀌는 칸(글자 · 카드 · 줄 · 버튼)만 코덱스가 비운 판에서 떠 붙인다.
    코덱스 판은 통째로 다시 그린 것이라 칸 밖도 조금씩 다르다(호스트 판은 목록 둘레에 없던 테두리까지 그렸다) —
    **정한 칸 안만** 쓴다.
  - 부품(탭 버튼 · 능력치 줄 · 금색 버튼 · 별 · 막대 · 카드 틀 · 성장 경로 노드 · 하단 바 칸)은 시안에서 뗀다.
    부품 속 글자는 언어마다 달라 지우고 게임이 찍는다.
  - 새 능력치 아이콘 3종(공격속도 · 사거리 · 이동속도)과 하단 바 「선택된 PLAY · SHOP」 은 코덱스 발주본.

좌표는 시안 = 게임 캔버스(720×1280) 그대로다.
출력: Assets/BaseResource/Growth/*.png + growth_spec.json
"""
import os, json
import numpy as np
import cv2
from PIL import Image, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
REF = os.path.join(HERE, '..', '_exchange', 'ref', 'growth_v1')
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Growth')
FONT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Fonts', 'NotoSansKR-Bold.ttf')
W, H = 720, 1280
NAV_Y = 1170            # 하단 바 판 윗변 — 위는 페이지, 아래는 공통 하단 바


# ── 기본 도구 ─────────────────────────────────────────────────

def load(p):
    return np.asarray(Image.open(p).convert('RGB')).copy()


def align(img, ref):
    """코덱스 그림을 720×1280 에 맞추고 위상 상관으로 밀린 만큼 되민다(위쪽 470 줄 — 머리 · 카드로 잰다)."""
    im = Image.fromarray(img)
    if im.size != (W, H):
        im = im.resize((W, H), Image.LANCZOS)
    a = np.asarray(im).astype(np.float32)
    (sx, sy), _ = cv2.phaseCorrelate(ref.astype(np.float32).mean(2)[:470], a.mean(2)[:470])
    if abs(sx) > 0.3 or abs(sy) > 0.3:
        a = cv2.warpAffine(a, np.float32([[1, 0, -sx], [0, 1, -sy]]), (W, H), borderMode=cv2.BORDER_REPLICATE)
    return np.clip(a, 0, 255).astype(np.uint8)


def paste(dst, src, rect, feather=3):
    """src 의 rect 칸을 dst 에 붙인다. 가장자리 feather px 는 녹인다."""
    x0, y0, x1, y1 = rect
    h, w = y1 - y0, x1 - x0
    a = np.ones((h, w), np.float32)
    for k in range(feather):
        f = (k + 1) / (feather + 1)
        a[k, :] = np.minimum(a[k, :], f); a[-1 - k, :] = np.minimum(a[-1 - k, :], f)
        a[:, k] = np.minimum(a[:, k], f); a[:, -1 - k] = np.minimum(a[:, -1 - k], f)
    roi = dst[y0:y1, x0:x1].astype(np.float32)
    dst[y0:y1, x0:x1] = (roi * (1 - a[..., None]) + src[y0:y1, x0:x1].astype(np.float32) * a[..., None]).astype(np.uint8)


def text_mask(img, box, thresh=110):
    x0, y0, x1, y1 = box
    roi = img[y0:y1, x0:x1].astype(np.int16)
    edge = np.concatenate([roi[0], roi[-1], roi[:, 0], roi[:, -1]])
    bg = np.median(edge, axis=0)
    m = (np.abs(roi - bg).sum(axis=2) > thresh).astype(np.uint8)
    red = (roi[..., 0] > 150) & (roi[..., 1] < 90) & (roi[..., 2] < 90)
    m[red] = 0
    m = cv2.morphologyEx(m, cv2.MORPH_OPEN, np.ones((2, 2), np.uint8))
    cnt, lab, stats, _ = cv2.connectedComponentsWithStats(m, connectivity=8)
    h, w = m.shape
    for i in range(1, cnt):
        x, y, cw, ch, area = stats[i]
        if (x == 0 or y == 0 or x + cw >= w or y + ch >= h) and area < 40:
            m[lab == i] = 0
    return m, bg


def row_fill(img, mask, box, reach=10):
    """판 위 글자 지우기 — 같은 줄의 글자 없는 픽셀 사이를 곧게 잇는다."""
    x0, y0, x1, y1 = box
    Hh, Ww = mask.shape
    lx0, lx1 = max(0, x0 - reach), min(Ww, x1 + reach)
    blur = cv2.blur(img.astype(np.float32), (3, 3))
    out = img.copy()
    for y in range(max(0, y0), min(Hh, y1)):
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


def erase_texts(img, boxes, thresh=110, grow=5):
    """글자 칸들을 지운다(글자 잉크를 재서 넓힌 뒤 줄 잇기)."""
    full = np.zeros(img.shape[:2], np.uint8)
    for b in boxes:
        m, _ = text_mask(img, b, thresh)
        x0, y0, x1, y1 = b
        full[y0:y1, x0:x1] |= m
    full = cv2.dilate(full, np.ones((grow, grow), np.uint8))
    out = img
    for x0, y0, x1, y1 in boxes:
        out = row_fill(out, full, (x0 - 2, y0 - 2, x1 + 2, y1 + 2))
    return out


def fit_size(text, box_w, box_h):
    best = 8
    for size in range(8, 90):
        f = ImageFont.truetype(FONT, size)
        bb = f.getbbox(text)
        if bb[3] - bb[1] > box_h + 0.5 or bb[2] - bb[0] > box_w * 1.03 + 2:
            break
        best = size
    return best


def measure(img, box, align_, sample, lines=1, thresh=110):
    """글자 칸 — 잉크 상자 · 색 · 크기(시안 글자 기준)."""
    m, bg = text_mask(img, box, thresh)
    x0, y0, x1, y1 = box
    ys, xs = np.nonzero(m)
    if len(xs) == 0:
        raise RuntimeError(f'글자 없음 {box} {sample}')
    tb = [x0 + int(xs.min()), y0 + int(ys.min()), x0 + int(xs.max()) + 1, y0 + int(ys.max()) + 1]
    roi = img[y0:y1, x0:x1].astype(np.int16)
    dist = np.abs(roi - bg).sum(axis=2)
    sel = (m > 0) & (dist >= np.percentile(dist[m > 0], 75))
    col = np.median(roi[sel], axis=0)
    line_h = (tb[3] - tb[1]) if lines == 1 else (tb[3] - tb[1]) / (lines + (lines - 1) * 0.35)
    return {'box': tb, 'area': list(box), 'align': align_, 'color': '#%02X%02X%02X' % tuple(int(c) for c in col),
            'size': fit_size(sample, tb[2] - tb[0], line_h), 'lines': lines}


def save_part(name, rgb, alpha=None):
    if alpha is None:
        alpha = np.full(rgb.shape[:2], 255, np.uint8)
    Image.fromarray(np.dstack([rgb, alpha]).astype(np.uint8)).save(os.path.join(OUT, f'{name}.png'))


def diff_alpha(a, b, thresh=26, gain=6, solid=False):
    d = np.abs(a.astype(np.int16) - b.astype(np.int16)).sum(axis=2)
    al = np.clip((d - thresh) * gain, 0, 255).astype(np.uint8)
    if solid:
        body = cv2.morphologyEx((al > 90).astype(np.uint8), cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
        flood = np.pad(body, 1)
        ff = np.zeros((flood.shape[0] + 2, flood.shape[1] + 2), np.uint8)
        cv2.floodFill(flood, ff, (0, 0), 2)
        al = np.where(flood[1:-1, 1:-1] != 2, 255, al).astype(np.uint8)
    return cv2.GaussianBlur(al, (3, 3), 0)


def crop(img, r):
    x0, y0, x1, y1 = r
    return img[y0:y1, x0:x1]


# ── 시안 좌표 (720×1280) ──────────────────────────────────────
GOLD_TEXT = (384, 20, 502, 52)
TAB_GHOST, TAB_HOST = (16, 136, 360, 230), (362, 136, 706, 230)
TAB_GHOST_TEXT, TAB_HOST_TEXT = (166, 162, 300, 204), (486, 162, 650, 204)

CARD_IN = (22, 240, 700, 466)                    # 카드 판 안쪽(테두리 제외)
CARD_TEXT = (286, 246, 698, 464)                 # 카드 오른쪽 글자 · 별 · 막대 구역
HOST_PORTRAIT_AREA = (24, 242, 284, 466)

T_NAME_EN = (288, 248, 470, 286)
T_NAME = (288, 286, 420, 312)
T_DESC_G = (288, 314, 520, 382)                  # 유령 설명 3줄
T_DESC_H = (288, 316, 520, 364)                  # 호스트 설명 2줄
T_LV_LABEL = (288, 390, 334, 422)
T_LV_NUM = (334, 382, 386, 422)
T_LV_MAX_G = (386, 390, 442, 422)
T_LV_MAX_H = (366, 390, 432, 422)
STARS_BIG = [(462 + 43 * i, 383, 507 + 43 * i, 424) for i in range(5)]
EXP_LABEL = (290, 436, 334, 458)
EXP_BAR_G = (342, 431, 560, 461)
EXP_VAL_G = (564, 436, 684, 458)
SHARD_ICON = (298, 421, 340, 461)
EXP_BAR_H = (345, 428, 596, 457)
EXP_VAL_H = (600, 436, 684, 458)

STAT_TAB, SKILL_TAB = (20, 480, 358, 534), (362, 480, 702, 534)
STAT_TAB_TEXT, SKILL_TAB_TEXT = (150, 490, 230, 524), (500, 490, 570, 524)
STAT_HEADER = (38, 540, 172, 572)
HELP_ICON = (180, 546, 210, 576)
ROWS = [(34, 578 + 50.6 * i, 688, 626 + 50.6 * i) for i in range(4)]
ROW0 = (34, 578, 688, 627)
ROW_ICON = (46, 581, 96, 624)
ROW_NAME = (120, 590, 240, 616)
ROW_LV_LABEL = (272, 592, 300, 616)
ROW_LV_NUM = (300, 588, 332, 618)
ROW_LV_MAX = (333, 592, 368, 616)
ROW_PCT = (424, 590, 500, 616)
ROW_BTN = (519, 581, 685, 624)
ROW_BTN_COIN = (538, 586, 576, 620)
ROW_BTN_TEXT = (588, 590, 656, 616)
STAT_PANEL_HOST = (22, 536, 700, 782)            # 호스트 능력치 판 안 — 줄이 스크롤되는 창
STAT_PANEL_GHOST = (22, 486, 700, 782)           # 유령은 탭 줄이 없어 판이 위로 늘었다

LIST_HEADER_TEXT = (38, 796, 168, 828)
SORT_TEXT = (556, 800, 616, 824)
GRID = (16, 834, 706, 1160)
CARDS = [(18 + 174.3 * c, 836 + 162 * r, 186 + 174.3 * c, 994 + 162 * r) for r in range(2) for c in range(4)]
CARD_SEL, CARD_NORMAL = (16, 834, 188, 996), (190, 834, 360, 996)
CARD_NAME = (30, 944, 176, 966)
STAR_SMALL_ON, STAR_SMALL_OFF = (43, 966, 63, 988), (113, 966, 133, 988)

SKILL_NAME_A, SKILL_DESC_A = (184, 584, 420, 616), (184, 620, 560, 676)
SKILL_NAME_P, SKILL_DESC_P = (184, 732, 420, 766), (184, 770, 640, 800)
SKILL_ICON_A, SKILL_ICON_P = (66, 585, 156, 668), (66, 732, 156, 812)

PATH_TITLE = (38, 806, 204, 840)
PATH_HELP = (212, 812, 242, 842)
PATH_Y = 977
NODE_X = [83.5, 207, 425, 533, 639]              # 시안 노드 가운데(Lv10 · 20 · 30 · 40 · 50)
MARK_X = 318.6                                   # 지금 레벨 표시(유령) — Lv28
NODE_DONE, NODE_NEXT, NODE_LOCK = (58, 952, 110, 1003), (398, 950, 452, 1004), (506, 952, 560, 1003)
MARKER = (292, 948, 346, 1006)
LINE_CYAN, LINE_GOLD, LINE_GREY = (116, 973, 176, 982), (348, 973, 396, 982), (458, 973, 504, 982)
PATH_LABELS = [(58, 1006, 110, 1032), (180, 1006, 234, 1032), (290, 1006, 348, 1032), (398, 1006, 454, 1032),
               (506, 1006, 562, 1032), (612, 1006, 668, 1032)]
PATH_TRACK_AREA = (30, 944, 690, 1036)
BOX = (28, 1042, 692, 1144)
SLOTS = [(68, 1056, 144, 1130), (158, 1056, 234, 1130), (248, 1056, 324, 1130)]
BOX_LV_LABEL = (360, 1068, 396, 1090)
BOX_LV_NUM = (396, 1060, 434, 1092)
BOX_LV_MAX = (434, 1068, 478, 1090)
BOX_BAR = (360, 1099, 560, 1125)
BOX_VAL = (564, 1102, 674, 1122)

NAV_BTN = {'host': (10, 1172, 248, 1280), 'play': (244, 1172, 480, 1280), 'shop': (476, 1172, 712, 1280)}
NAV_SUB = {'host': (116, 1232, 234, 1266), 'play': (352, 1232, 464, 1266), 'shop': (588, 1232, 662, 1266)}


def main():
    os.makedirs(OUT, exist_ok=True)
    Mg = load(os.path.join(REF, 'growth_ghost_stats_v4.png'))
    Mh = load(os.path.join(REF, 'growth_host_stats_v4.png'))
    Ms = load(os.path.join(REF, 'growth_host_skill_v4.png'))
    Pg = align(load(os.path.join(REF, 'out_plate_ghost.png')), Mg)
    Ph = align(load(os.path.join(REF, 'out_plate_host.png')), Mh)
    skill_plate = os.path.join(REF, 'out_plate_skill.png')
    Ps = align(load(skill_plate), Ms) if os.path.exists(skill_plate) else None
    spec = {'texts': {}, 'parts': {}}
    T = spec['texts']

    # ── 글자 재기(시안 글자 기준) ──────────────────────────
    T['GoldText'] = measure(Mg, GOLD_TEXT, 'C', '125,680')
    T['TabOnText'] = measure(Mg, TAB_GHOST_TEXT, 'L', '유령 육성')
    T['TabOffText'] = measure(Mg, TAB_HOST_TEXT, 'L', '호스트 육성')
    T['CardNameEn'] = measure(Mg, T_NAME_EN, 'L', 'GHOST')
    T['CardName'] = measure(Mg, T_NAME, 'L', '유령')
    T['CardDesc'] = measure(Mg, T_DESC_G, 'L', '아직 끝나지 않은 이야기를 위해', lines=3)
    T['CardLvLabel'] = measure(Mg, T_LV_LABEL, 'L', 'Lv.')
    T['CardLvNum'] = measure(Mg, T_LV_NUM, 'L', '28')
    T['CardLvMax'] = measure(Mg, T_LV_MAX_G, 'L', '/ 50')
    T['ExpLabel'] = measure(Mg, EXP_LABEL, 'L', 'EXP')
    T['ExpValue'] = measure(Mg, EXP_VAL_G, 'R', '1,820 / 2,400')
    T['ShardValue'] = measure(Mh, EXP_VAL_H, 'R', '27 / 50')
    T['StatTabOn'] = measure(Mh, STAT_TAB_TEXT, 'C', '능력치')
    T['StatTabOff'] = measure(Ms, STAT_TAB_TEXT, 'C', '능력치')
    T['StatHeader'] = measure(Mg, STAT_HEADER, 'L', '능력치 강화')
    T['RowName'] = measure(Mg, ROW_NAME, 'L', '공격력')
    T['RowLvLabel'] = measure(Mg, ROW_LV_LABEL, 'L', 'Lv.')
    T['RowLvNum'] = measure(Mg, ROW_LV_NUM, 'L', '15')
    T['RowLvMax'] = measure(Mg, ROW_LV_MAX, 'L', '/ 50')
    T['RowPct'] = measure(Mg, ROW_PCT, 'R', '+30.0%')
    T['RowCost'] = measure(Mg, ROW_BTN_TEXT, 'C', '2,800', thresh=160)
    T['ListHeader'] = measure(Mh, LIST_HEADER_TEXT, 'L', '호스트 목록')
    T['SortText'] = measure(Mh, SORT_TEXT, 'L', '기본순')
    T['CardListName'] = measure(Mh, CARD_NAME, 'C', 'Commando')
    T['SkillName'] = measure(Ms, SKILL_NAME_A, 'L', '跳躍強襲')
    T['SkillDesc'] = measure(Ms, SKILL_DESC_A, 'L', '最も遠い敵に飛び込み、', lines=2)
    T['PathTitle'] = measure(Mg, PATH_TITLE, 'L', '유령 성장 경로')
    T['PathLabelDone'] = measure(Mg, PATH_LABELS[0], 'C', 'Lv. 10')
    T['PathLabelNow'] = measure(Mg, PATH_LABELS[2], 'C', 'Lv. 28')
    T['PathLabelNext'] = measure(Mg, PATH_LABELS[3], 'C', 'Lv. 30')
    T['PathLabelLock'] = measure(Mg, PATH_LABELS[4], 'C', 'Lv. 40')
    T['BoxLvLabel'] = measure(Mg, BOX_LV_LABEL, 'L', 'Lv.')
    T['BoxLvNum'] = measure(Mg, BOX_LV_NUM, 'L', '28')
    T['BoxLvMax'] = measure(Mg, BOX_LV_MAX, 'L', '/ 50')
    T['BoxValue'] = measure(Mg, BOX_VAL, 'R', '1,820 / 2,400')

    # ── 바탕 셋 ───────────────────────────────────────────
    def base(M, P, rects, text_boxes):
        b = M.copy()
        for r in rects:
            paste(b, P, r)
        return erase_texts(b, text_boxes) if text_boxes else b

    common = [GOLD_TEXT, (TAB_GHOST[0], TAB_GHOST[1], TAB_HOST[2], TAB_HOST[3])]
    bg_ghost = base(Mg, Pg, common + [CARD_TEXT, (STAT_PANEL_GHOST[0], 478, STAT_PANEL_GHOST[2], STAT_PANEL_GHOST[3]),
                                      PATH_TITLE, PATH_HELP, PATH_TRACK_AREA, BOX], [])
    bg_host = base(Mh, Ph, common + [CARD_IN, (STAT_TAB[0], STAT_TAB[1], SKILL_TAB[2], SKILL_TAB[3]), STAT_PANEL_HOST,
                                     LIST_HEADER_TEXT, SORT_TEXT] + [tuple(int(v) for v in c) for c in CARDS], [])
    Image.fromarray(bg_ghost[:NAV_Y]).save(os.path.join(OUT, 'bg_ghost.png'))
    Image.fromarray(bg_host[:NAV_Y]).save(os.path.join(OUT, 'bg_host.png'))

    # 스킬 탭 바탕 — 스킬 카드 틀은 시안 그대로 두고 글자 · 아이콘 속만 지운다. 탭 · 목록은 스킬 판에서
    if Ps is not None:
        sk = Ms.copy()
        for r in common + [CARD_IN, (STAT_TAB[0], STAT_TAB[1], SKILL_TAB[2], SKILL_TAB[3])]:
            paste(sk, Ps, r)
        dy = 48   # 스킬 탭은 목록이 48 아래에 있다
        for r in [(LIST_HEADER_TEXT[0], LIST_HEADER_TEXT[1] + dy, LIST_HEADER_TEXT[2], LIST_HEADER_TEXT[3] + dy),
                  (SORT_TEXT[0], SORT_TEXT[1] + dy, SORT_TEXT[2], SORT_TEXT[3] + dy)] + \
                 [(int(c[0]), int(c[1]) + dy, int(c[2]), min(NAV_Y, int(c[3]) + dy)) for c in CARDS]:
            paste(sk, Ps, r)
        sk = erase_texts(sk, [SKILL_NAME_A, SKILL_DESC_A, SKILL_NAME_P, SKILL_DESC_P])
        for r in (SKILL_ICON_A, SKILL_ICON_P):   # 아이콘 속 — 게임이 스킬 아이콘을 얹는다. 둘레 색으로 메운다
            x0, y0, x1, y1 = r
            ring = np.concatenate([sk[y0, x0:x1], sk[y1 - 1, x0:x1], sk[y0:y1, x0], sk[y0:y1, x1 - 1]])
            sk[y0:y1, x0:x1] = np.median(ring, axis=0)
        Image.fromarray(sk[:NAV_Y]).save(os.path.join(OUT, 'bg_skill.png'))
        spec['skillListDy'] = dy

    # ── 부품 ─────────────────────────────────────────────
    P = spec['parts']

    def part(name, M, rect, erase=(), alpha_ref=None, solid=True, thresh=26):
        img = M.copy()
        if erase:
            img = erase_texts(img, list(erase))
        rgb = crop(img, rect)
        al = diff_alpha(rgb, crop(alpha_ref, rect), thresh=thresh, solid=solid) if alpha_ref is not None else None
        save_part(name, rgb, al)
        P[name] = list(rect)

    part('tab_ghost_on', Mg, TAB_GHOST, [TAB_GHOST_TEXT], Pg)
    part('tab_ghost_off', Mh, TAB_GHOST, [TAB_GHOST_TEXT], Ph)
    part('tab_host_on', Mh, TAB_HOST, [TAB_HOST_TEXT], Ph)
    part('tab_host_off', Mg, TAB_HOST, [TAB_HOST_TEXT], Pg)
    T['TabOnText'] = measure(Mh, TAB_HOST_TEXT, 'L', '호스트 육성')   # 켜진 탭 글자 색은 노란 판 위 — 호스트 탭에서 잰다
    T['TabOffText'] = measure(Mg, TAB_HOST_TEXT, 'L', '호스트 육성')
    part('stattab_on', Mh, STAT_TAB, [STAT_TAB_TEXT], Ph)
    part('stattab_off', Ms, STAT_TAB, [STAT_TAB_TEXT], Ps if Ps is not None else Ph)
    part('skilltab_on', Ms, SKILL_TAB, [SKILL_TAB_TEXT], Ps if Ps is not None else Ph)
    part('skilltab_off', Mh, SKILL_TAB, [SKILL_TAB_TEXT], Ph)
    for i, nm in ((0, 'star_big_on'), (4, 'star_big_off')):
        part(nm, Mg, STARS_BIG[i], (), Pg)
    part('help', Mg, HELP_ICON, (), Pg)
    part('shard_icon', Mh, SHARD_ICON, (), Ph)
    part('slot_star_on', Mh, STAR_SMALL_ON, (), Ph)
    part('slot_star_off', Mh, STAR_SMALL_OFF, (), Ph)

    # 막대 — 틀(속 비움)과 채움. 채움은 가로로 늘여 쓴다
    for nm, M, r in (('bar_g', Mg, EXP_BAR_G), ('bar_h', Mh, EXP_BAR_H), ('bar_box', Mg, BOX_BAR)):
        rgb = crop(M, r).copy()
        cyan = (rgb[..., 1] > 170) & (rgb[..., 2] > 170) & (rgb[..., 0] < 140)
        ys, xs = np.nonzero(cyan)
        fx0, fx1, fy0, fy1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
        fill = rgb[fy0:fy1, fx0 + 3:fx1 - 3]
        save_part(nm + '_fill', fill)
        # 틀 — 채움 자리를 오른쪽 빈 곳 색으로 메운다(줄마다 빈 곳 가운뎃값)
        # 줄마다 채움이 아닌 속 픽셀의 가운뎃값. 채움이 끝까지 차 있으면 어두운 남색
        for yy in range(fy0, fy1):
            rowpx = rgb[yy, fx0:min(rgb.shape[1] - 3, fx1 + 30)]
            keep = rowpx[~cyan[yy, fx0:fx0 + len(rowpx)]]
            col = np.median(keep, axis=0) if len(keep) >= 3 else np.array([8, 22, 52])
            rgb[yy, fx0:fx1][cyan[yy, fx0:fx1]] = col
        save_part(nm + '_frame', rgb, diff_alpha(rgb, crop(Pg if M is Mg else Ph, r), solid=True))
        P[nm + '_frame'] = list(r)
        P[nm + '_fill'] = [int(r[0] + fx0), int(r[1] + fy0), int(r[0] + fx1), int(r[1] + fy1)]

    # 능력치 줄 — 틀만(아이콘 · 글자 · 버튼 지움), 금색 버튼(글자 지움), 아이콘 4 + 새 3
    row = crop(erase_texts(Mg, [ROW_NAME, ROW_LV_LABEL, ROW_LV_NUM, ROW_LV_MAX, ROW_PCT]), ROW0).copy()
    rx0, ry0 = ROW0[0], ROW0[1]
    def local(r):
        return (r[0] - rx0, r[1] - ry0, r[2] - rx0, r[3] - ry0)
    # 아이콘 · 버튼 자리는 줄 속 색(아이콘 왼쪽 · 버튼 왼쪽 빈 곳)으로 줄마다 메운다
    for r in (ROW_ICON, ROW_BTN):
        x0, y0, x1, y1 = local(r)
        left = row[y0:y1, max(0, x0 - 12):x0 - 2]
        right = row[y0:y1, x1 + 2:min(row.shape[1], x1 + 12)] if x1 + 12 < row.shape[1] - 4 else left
        colr = np.median(np.concatenate([left, right], axis=1), axis=1)
        row[y0:y1, x0:x1] = colr[:, None, :]
    save_part('stat_row', row)
    P['stat_row'] = list(ROW0)
    btn = crop(erase_texts(Mg, [ROW_BTN_TEXT], thresh=160), ROW_BTN)
    save_part('gold_button', btn)
    P['gold_button'] = list(ROW_BTN)
    P['gold_button_text'] = list(ROW_BTN_TEXT)
    for i, nm in ((0, 'icon_atk'), (2, 'icon_hp'), (3, 'icon_crit')):
        r = (ROW_ICON[0], int(ROW_ICON[1] + 50.6 * i), ROW_ICON[2], int(ROW_ICON[3] + 50.6 * i))
        rr = crop(Mg, r)
        ref = np.repeat(np.repeat(np.median(np.concatenate([rr[0], rr[-1], rr[:, 0], rr[:, -1]]), axis=0)[None, None, :],
                                  rr.shape[0], 0), rr.shape[1], 1)
        save_part(nm, rr, diff_alpha(rr, ref.astype(np.uint8), thresh=40, solid=True))
    P['icon'] = list(ROW_ICON)
    sheet = load(os.path.join(REF, 'out_stat_icons.png'))
    dist = np.abs(sheet.astype(np.int16) - np.array([255, 0, 255])).sum(axis=2)
    ink = ((dist > 120) & ~((sheet[..., 0] > 150) & (sheet[..., 2] > 150) & (sheet[..., 1] < 110))).astype(np.uint8)
    ink = cv2.morphologyEx(ink, cv2.MORPH_OPEN, np.ones((3, 3), np.uint8))
    cols = np.nonzero(ink.any(axis=0))[0]
    # 아이콘끼리는 수백 px 떨어져 있다 — 사거리 아이콘의 점선(작은 틈)을 따로 세지 않게 넉넉히 가른다
    groups, start = [], cols[0]
    for a, b in zip(cols, cols[1:]):
        if b - a > 150:
            groups.append((start, a + 1)); start = b
    groups.append((start, cols[-1] + 1))
    ref_h = ROW_ICON[3] - ROW_ICON[1] - 6
    for (gx0, gx1), nm in zip(groups[:3], ('icon_atkspeed', 'icon_range', 'icon_movespeed')):
        sub = ink[:, gx0:gx1]
        ys = np.nonzero(sub.any(axis=1))[0]
        gy0, gy1 = ys.min(), ys.max() + 1
        rgba = np.dstack([sheet[gy0:gy1, gx0:gx1], cv2.GaussianBlur(sub[gy0:gy1] * 255, (3, 3), 0)])
        im = Image.fromarray(rgba.astype(np.uint8))
        s = ref_h / max(im.height, im.width * 0.9)
        im = im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.LANCZOS)
        im.save(os.path.join(OUT, f'{nm}.png'))

    # 호스트 카드 — 틀(시안) + 속(판). 속은 판에서, 테두리 고리만 시안에서
    for nm, r in (('hostcard_sel', CARD_SEL), ('hostcard', CARD_NORMAL)):
        rgb = crop(Mh, r).copy()
        inner = crop(Ph, r)
        h, w = rgb.shape[:2]
        ring = np.zeros((h, w), bool)
        ring[:9, :] = ring[-9:, :] = True
        ring[:, :9] = ring[:, -9:] = True
        rgb[~ring] = inner[~ring]
        al = diff_alpha(crop(Mh, r), inner, thresh=40, solid=True)
        al = np.maximum(al, (~ring).astype(np.uint8) * 0)   # 속은 아래에서 채운다
        # 틀 안쪽은 전부 불투명 — 틀 윤곽 안을 채운다
        body = (al > 90).astype(np.uint8)
        flood = np.pad(body, 1)
        ff = np.zeros((flood.shape[0] + 2, flood.shape[1] + 2), np.uint8)
        cv2.floodFill(flood, ff, (0, 0), 2)
        al = np.where(flood[1:-1, 1:-1] != 2, 255, al).astype(np.uint8)
        save_part(nm, rgb, al)
        P[nm] = list(r)
    P['cards'] = [[round(v, 1) for v in c] for c in CARDS]

    # 성장 경로 — 노드 셋 · 표시 · 줄 셋 · 상자 속(보상 칸 속 비움)
    for nm, r in (('node_done', NODE_DONE), ('node_next', NODE_NEXT), ('node_lock', NODE_LOCK), ('marker', MARKER)):
        part(nm, Mg, r, (), Pg, thresh=40)
    for nm, r in (('line_cyan', LINE_CYAN), ('line_gold', LINE_GOLD), ('line_grey', LINE_GREY)):
        rgb = crop(Mg, r)
        col = np.median(rgb, axis=1)
        save_part(nm, np.repeat(col[:, None, :], 8, axis=1).astype(np.uint8))
        P[nm] = list(r)
    P['node_x'] = NODE_X; P['mark_x'] = MARK_X; P['path_y'] = PATH_Y
    P['path_labels'] = [list(r) for r in PATH_LABELS]
    box = Mg.copy()
    box = erase_texts(box, [BOX_LV_LABEL, BOX_LV_NUM, BOX_LV_MAX, BOX_VAL])
    for s in SLOTS:   # 보상 칸 속 — 아이콘 자리를 칸 안 둘레 색으로
        x0, y0, x1, y1 = s[0] + 6, s[1] + 6, s[2] - 6, s[3] - 6
        ringc = np.concatenate([box[y0, x0:x1], box[y1 - 1, x0:x1]])
        box[y0:y1, x0:x1] = np.median(ringc, axis=0)
    bx = crop(box, BOX).copy()
    b0 = BOX
    x0, y0, x1, y1 = BOX_BAR
    bx[y0 - b0[1]:y1 - b0[1], x0 - b0[0]:x1 - b0[0]] = crop(Pg, BOX_BAR)   # 막대 자리는 비워 둔다(막대 부품이 얹힌다)
    save_part('path_box', bx, diff_alpha(bx, crop(Pg, BOX), solid=True))
    P['path_box'] = list(BOX)
    P['slots'] = [list(s) for s in SLOTS]
    part('reward_flame', Mg, (SLOTS[0][0] + 8, SLOTS[0][1] + 8, SLOTS[0][2] - 8, SLOTS[0][3] - 8), (), Pg, thresh=40)
    # 금화 — 바탕판에도 금화가 남아 차이로는 못 뜬다. 금색 버튼 속 금화를 노란 판에서 떼어 낸다
    cm, _ = text_mask(Mg, ROW_BTN_COIN, 90)
    body = cv2.morphologyEx(cm, cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    flood = np.pad(body, 1)
    ff = np.zeros((flood.shape[0] + 2, flood.shape[1] + 2), np.uint8)
    cv2.floodFill(flood, ff, (0, 0), 2)
    inside = (flood[1:-1, 1:-1] != 2)
    cc = crop(Mg, ROW_BTN_COIN).astype(np.int16)
    # 버튼 노란 바탕(밝은 노랑 · 파랑 적음)이 모서리에 딸려 온다 — 금화 테두리(주황 · 빨강)만 남긴다
    yellow_bg = (cc[..., 0] > 220) & (cc[..., 1] > 190) & (cc[..., 2] < 110)
    lab_n, lab = cv2.connectedComponents((yellow_bg & inside).astype(np.uint8))
    h_, w_ = yellow_bg.shape
    for i in range(1, lab_n):
        ys_, xs_ = np.nonzero(lab == i)
        if xs_.min() == 0 or ys_.min() == 0 or xs_.max() == w_ - 1 or ys_.max() == h_ - 1:
            inside[lab == i] = False
    cal = cv2.GaussianBlur(inside.astype(np.uint8) * 255, (3, 3), 0)
    save_part('reward_coin', crop(Mg, ROW_BTN_COIN), cal)
    P['reward_coin'] = list(ROW_BTN_COIN)
    # 젬 — 육성 시안엔 젬이 없다. 로비 시안(같은 화풍) 상단 젬 칸의 보석을 뗀다
    Lm = load(os.path.join(ROOT, 'Projects', 'AVSR', 'Reference', 'Mockups', 'lobby_hub_v3_jp.png'))
    gbox = (572, 26, 622, 72)
    gm, _ = text_mask(Lm, gbox, 90)
    gm = cv2.morphologyEx(gm, cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    flood = np.pad(gm, 1)
    ff = np.zeros((flood.shape[0] + 2, flood.shape[1] + 2), np.uint8)
    cv2.floodFill(flood, ff, (0, 0), 2)
    gal = cv2.GaussianBlur((flood[1:-1, 1:-1] != 2).astype(np.uint8) * 255, (3, 3), 0)
    ys, xs = np.nonzero(gal > 8)
    g = np.dstack([crop(Lm, gbox), gal])[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    Image.fromarray(g.astype(np.uint8)).save(os.path.join(OUT, 'reward_gem.png'))

    # ── 하단 바 — 시안 + 코덱스 선택 변형(칸마다 자리 맞춤) ──────────
    def locate(img, tmpl):
        """코덱스 띠에서 시안 칸(tmpl)이 있는 자리 · 배율을 찾는다."""
        best = (-1, None)
        for s in np.linspace(2.6, 3.4, 33):
            t = cv2.resize(tmpl, (round(tmpl.shape[1] * s), round(tmpl.shape[0] * s)), interpolation=cv2.INTER_AREA)
            if t.shape[0] >= img.shape[0] or t.shape[1] >= img.shape[1]:
                continue
            r = cv2.matchTemplate(img, t, cv2.TM_CCOEFF_NORMED)
            _, mx, _, loc = cv2.minMaxLoc(r)
            if mx > best[0]:
                best = (mx, (s, loc))
        return best

    nav_src = {'host_on': (Mh, None), 'play_off': (Mh, None), 'shop_off': (Mh, None)}
    for key, fn, unchanged in (('play_on', 'out_nav_play.png', 'shop'), ('shop_on', 'out_nav_shop.png', 'play')):
        img = load(os.path.join(REF, fn))
        # 끝 칸(HOST)은 예상 자리가 그림 밖으로 조금 나간다 — 둘레를 덧대 창이 비지 않게
        PADN = 80
        img = cv2.copyMakeBorder(img, PADN, PADN, PADN, PADN, cv2.BORDER_REPLICATE)
        score, (s, loc) = locate(cv2.cvtColor(img, cv2.COLOR_RGB2GRAY),
                                 cv2.cvtColor(crop(Mh, NAV_BTN[unchanged]), cv2.COLOR_RGB2GRAY))
        # 칸 자리 → 코덱스 그림 자리: (x - 칸x0) × s + loc
        ux0, uy0 = NAV_BTN[unchanged][0], NAV_BTN[unchanged][1]
        gray = cv2.cvtColor(img, cv2.COLOR_RGB2GRAY)

        found = {}

        def grab(btn, from_btn=None):
            if from_btn is not None and from_btn in found:
                # 이웃 칸에서 잰 자리 · 배율로 옮긴다 — 상태가 달라 제 칸 맞춤이 약할 때(HOST 보통본)
                _, fx, fy, fs = found[from_btn]
                x0, y0, x1, y1 = NAV_BTN[btn]
                bx = fx + (x0 - NAV_BTN[from_btn][0]) * fs
                by = fy + (y0 - NAV_BTN[from_btn][1]) * fs
                sub = img[int(by):int(by + (y1 - y0) * fs), int(bx):int(bx + (x1 - x0) * fs)]
                return np.asarray(Image.fromarray(sub).resize((x1 - x0, y1 - y0), Image.LANCZOS))
            return grab_search(btn)

        def grab_search(btn):
            """칸마다 따로 맞춘다 — 코덱스 그림은 칸마다 배율 · 자리가 조금씩 달라, 한 칸으로 잰 값을
            옆 칸에 쓰면 이웃 테두리가 딸려 온다(PLAY 선택본 왼쪽, 2026-09-21).
            시안의 같은 칸(상태는 달라도 아이콘 · 글자 · 틀 모양이 같다)을 예상 자리 둘레에서 찾는다."""
            x0, y0, x1, y1 = NAV_BTN[btn]
            X0, Y0 = loc[0] + (x0 - ux0) * s, loc[1] + (y0 - uy0) * s
            tmpl = cv2.cvtColor(crop(Mh, NAV_BTN[btn]), cv2.COLOR_RGB2GRAY)
            best = (-1, X0, Y0, s)
            for ss in s * np.linspace(0.95, 1.05, 11):
                t = cv2.resize(tmpl, (round(tmpl.shape[1] * ss), round(tmpl.shape[0] * ss)), interpolation=cv2.INTER_AREA)
                wx0, wy0 = max(0, int(X0 - 40)), max(0, int(Y0 - 40))
                win = gray[wy0:int(Y0 + t.shape[0] + 40), wx0:int(X0 + t.shape[1] + 40)]
                if win.shape[0] < t.shape[0] or win.shape[1] < t.shape[1]:
                    continue
                r = cv2.matchTemplate(win, t, cv2.TM_CCOEFF_NORMED)
                _, mx, _, lc = cv2.minMaxLoc(r)
                if mx > best[0]:
                    best = (mx, wx0 + lc[0], wy0 + lc[1], ss)
            _, bx, by, bs = best
            found[btn] = best
            sub = img[int(by):int(by + (y1 - y0) * bs), int(bx):int(bx + (x1 - x0) * bs)]
            print(f'    {btn}: 맞춤 {best[0]:.3f} 배율 {bs:.3f}')
            return np.asarray(Image.fromarray(sub).resize((x1 - x0, y1 - y0), Image.LANCZOS))
        print(f'  {fn}: 맞춤 {score:.3f} 배율 {s:.3f}')
        nav_src[key] = (None, grab(key.split('_')[0]))
        if key == 'play_on':
            nav_src['host_off'] = (None, grab('host', from_btn='play'))
    for key, (M, arr) in nav_src.items():
        btn = key.split('_')[0]
        rgb = crop(M, NAV_BTN[btn]).copy() if arr is None else arr.copy()
        # 부제 글자 지움 — 언어마다 다르다
        full = np.zeros(Mh.shape[:2], np.uint8)
        canvas = Mh.copy()
        canvas[NAV_BTN[btn][1]:NAV_BTN[btn][3], NAV_BTN[btn][0]:NAV_BTN[btn][2]] = rgb
        sub_col = measure(canvas, NAV_SUB[btn], 'L', {'host': '호스트 육성', 'play': '게임 모드', 'shop': '상점'}[btn])
        T[f'Nav_{key}'] = sub_col
        canvas = erase_texts(canvas, [NAV_SUB[btn]])
        save_part(f'nav_{key}', crop(canvas, NAV_BTN[btn]))
        P[f'nav_{btn}'] = list(NAV_BTN[btn])
    Image.fromarray(Mh[NAV_Y:]).save(os.path.join(OUT, 'nav_bg.png'))
    # 태블릿 양옆 · 긴 화면 아래 — 판 가장자리 줄을 늘여 쓴다(폰 9:16 에서는 안 보인다)
    Image.fromarray(Mh[NAV_Y:, :6]).save(os.path.join(OUT, 'nav_side_l.png'))
    Image.fromarray(Mh[NAV_Y:, -6:]).save(os.path.join(OUT, 'nav_side_r.png'))
    Image.fromarray(bg_host[:NAV_Y, :6]).save(os.path.join(OUT, 'page_side_l.png'))
    Image.fromarray(bg_host[:NAV_Y, -6:]).save(os.path.join(OUT, 'page_side_r.png'))
    Image.fromarray(bg_host[NAV_Y - 24:NAV_Y]).save(os.path.join(OUT, 'page_filler.png'))
    # 패시브 아이콘 — 시안의 방패 · 섬광(호스트마다 따로 그린 것이 없어 공통으로 쓴다)
    x0, y0, x1, y1 = SKILL_ICON_P
    save_part('passive_icon', Ms[y0:y1, x0:x1])
    P['passive_icon'] = list(SKILL_ICON_P)
    P['navY'] = NAV_Y

    # 자리 목록(빌더가 읽는다) — 유니티 JsonUtility 가 읽게 전부 「이름 + 네 칸」 목록으로 편다
    extra = {}
    for i, r in enumerate(STARS_BIG):
        extra[f'@STAR{i}'] = r
    for k, r in NAV_SUB.items():
        extra[f'@NAV_SUB_{k}'] = r
    for i, c in enumerate(CARDS):
        extra[f'@CARD{i}'] = c
    for i, r in enumerate(PATH_LABELS):
        extra[f'@LABEL{i}'] = r
    for i, r in enumerate(SLOTS):
        extra[f'@SLOT{i}'] = r
    for k, v in {
        'CARD_TEXT': CARD_TEXT, 'HOST_PORTRAIT_AREA': HOST_PORTRAIT_AREA, 'STARS_BIG': STARS_BIG,
        'EXP_LABEL': EXP_LABEL, 'SHARD_ICON': SHARD_ICON, 'STAT_TAB': STAT_TAB, 'SKILL_TAB': SKILL_TAB,
        'STAT_PANEL_HOST': STAT_PANEL_HOST, 'STAT_PANEL_GHOST': STAT_PANEL_GHOST, 'HELP_ICON': HELP_ICON,
        'GRID': GRID, 'ROW_ICON': ROW_ICON, 'ROW_NAME': ROW_NAME, 'ROW_LV_LABEL': ROW_LV_LABEL,
        'ROW_LV_NUM': ROW_LV_NUM, 'ROW_LV_MAX': ROW_LV_MAX, 'ROW_PCT': ROW_PCT, 'ROW_BTN_COIN': ROW_BTN_COIN,
        'SKILL_ICON_A': SKILL_ICON_A, 'SKILL_ICON_P': SKILL_ICON_P, 'SKILL_NAME_P': SKILL_NAME_P,
        'SKILL_DESC_P': SKILL_DESC_P, 'PATH_HELP': PATH_HELP, 'TAB_GHOST': TAB_GHOST, 'TAB_HOST': TAB_HOST,
        'TAB_GHOST_TEXT': TAB_GHOST_TEXT, 'TAB_HOST_TEXT': TAB_HOST_TEXT, 'NAV_SUB': NAV_SUB,
        'CARD_NAME': CARD_NAME, 'STAR_SMALL_ON': STAR_SMALL_ON, 'ROW0': ROW0, 'T_DESC_H': T_DESC_H,
        'T_LV_MAX_H': T_LV_MAX_H, 'EXP_VAL_H': EXP_VAL_H,
    }.items():
        P['@' + k] = v if not isinstance(v, tuple) else list(v)
    P.update(extra)
    rects = []
    for k, v in P.items():
        if isinstance(v, (list, tuple)) and len(v) == 4 and all(isinstance(x, (int, float)) for x in v):
            rects.append({'name': k, 'box': [float(x) for x in v]})
    spec_out = {
        'navY': NAV_Y, 'skillListDy': spec.get('skillListDy', 48), 'nodeX': NODE_X, 'markX': MARK_X, 'pathY': PATH_Y,
        'texts': [dict(name=k, **v) for k, v in T.items()],
        'rects': rects,
    }
    json.dump(spec_out, open(os.path.join(OUT, 'growth_spec.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    return
    json.dump(spec, open(os.path.join(OUT, 'growth_spec.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    for k, v in T.items():
        print(f"{k:16s} {v['box']} {v['color']} size {v['size']}")
    print('parts', len(P))


if __name__ == '__main__':
    main()
