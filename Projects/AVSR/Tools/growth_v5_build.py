"""호스트 육성 v5 — 한 화면(몸 정보 · 성급 · 능력치 5줄 · 스킬 2칸 · 가로 목록) 판과 부품 (2026-10-06).

통과 시안: Projects/AVSR/_exchange/in/growth_starup_mock_v4.png (왼쪽) → 720 폭으로 다시 그린 growth_host_v5.png
빈 판:     growth_host_v5_plate.png (코덱스가 글자 · 바뀌는 칸을 지운 것)

growth_build.py 와 같은 원칙이다 — 그림은 시안 · 판 픽셀이고 나는 자르고 붙이기만 한다.
  - 바탕은 **빈 판**이다. 맨 위 줄(로고 · 유령 · 간판)만 시안 픽셀을 붙인다(판이 그것까지 지웠다).
  - 부품(Lv 배지 · 자물쇠 꼬리표 · ACTIVE/PASSIVE 꼬리표 · 정렬 칸)은 시안에서 떼고 속 글자만 지운다.
    투명도는 빈 판과의 차이로 낸다.
  - 글자 칸 · 크기 · 색은 시안 글자를 재서 growth_v5_spec.json 에 둔다(GrowthBuilder 가 읽는다).

좌표는 720×1280 그대로다. 출력: Assets/BaseResource/Growth/v5/*.png + growth_v5_spec.json
"""
import os, json, importlib.util
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Growth', 'v5')

spec_ = importlib.util.spec_from_file_location('growth_build', os.path.join(HERE, 'growth_build.py'))
gb = importlib.util.module_from_spec(spec_)
spec_.loader.exec_module(gb)
load, paste, erase_texts, measure, diff_alpha, crop = gb.load, gb.paste, gb.erase_texts, gb.measure, gb.diff_alpha, gb.crop
NAV_Y = gb.NAV_Y   # 1170 — 그 아래는 공통 하단 바

# ── 시안 좌표 (v5, 720×1280) ─────────────────────────────────
TOP_KEEP = [(0, 0, 325, 145), (325, 64, 720, 145),   # 로고 · 떠다니는 유령 · 간판 — 시안 그대로
            (330, 12, 380, 62), (606, 0, 642, 32)]          # 골드 칸 동전 · 우편 알림 점(빈 판이 지웠다)

CARD = (16, 234, 706, 443)
PORTRAIT = (30, 245, 240, 425)                         # 초상 칸 — 시안 캐릭터 크기(발이 판 바닥 위)
T_NAME_EN = (272, 252, 450, 288)
T_NAME = (272, 288, 360, 318)
T_DESC = (272, 322, 456, 378)

STAR_BOX = (460, 243, 698, 434)
T_STAR_LABEL = (540, 248, 630, 270)
STARS = [(498 + 32 * i, 270, 528 + 32 * i, 299) for i in range(5)]
STAR_MARK = (531, 302, 549, 322)                       # 「★ 1.5 / 5」 앞 작은 별
T_STAR_VALUE = (550, 300, 632, 324)
SHARD_BAR_IN = (516, 331, 682, 353)                    # 조각 막대 속(채움 자리)
T_SHARD_VALUE = (520, 330, 684, 356)
STARUP_BTN = (467, 365, 688, 428)
T_STARUP = (500, 368, 660, 394)
STARUP_COIN = (525, 397, 552, 420)
T_STARUP_COST = (556, 396, 640, 420)

T_STAT_HEADER = (34, 448, 175, 480)
HELP = (183, 453, 212, 476)
CAP_MARK = (540, 456, 560, 476)
T_CAP = (562, 455, 696, 477)
ROW_PITCH = 39.4
ROW0 = (27, 482, 695, 520)
ROW_BAND = (24, 481, 698, 521)                         # 줄 하나 + 줄 사이 틈(넘기는 줄 그림)
STAT_VIEW = (24, 481, 698, 678)                        # 5줄이 보이는 창
ROW_ICON = (44, 484, 82, 518)
T_ROW_NAME = (110, 488, 232, 512)
T_ROW_LV_LABEL = (245, 488, 272, 512)
T_ROW_LV_NUM = (276, 484, 293, 514)
T_ROW_LV_MAX = (296, 488, 332, 512)
T_ROW_PCT = (418, 486, 495, 514)
ROW_BTN = (520, 484, 693, 519)
ROW_COIN = (545, 487, 573, 516)
T_ROW_COST = (596, 488, 660, 512)

T_SKILL_HEADER = (34, 690, 92, 720)
SKILL_A, SKILL_P = (16, 722, 706, 820), (16, 826, 706, 930)
TAG_A, TAG_P = (30, 728, 213, 755), (30, 831, 152, 859)
STAR_A, STAR_P = (26, 768, 56, 800), (26, 873, 56, 905)
ICON_A, ICON_P = (58, 748, 137, 822), (58, 853, 137, 927)   # 스킬 아이콘 그림은 위아래가 비어 있어 틀보다 크게
BADGE_A, BADGE_P = (140, 758, 214, 798), (140, 863, 214, 903)
T_BADGE_A = (150, 764, 206, 794)
LOCK = (528, 736, 697, 778)
T_LOCK = (566, 744, 692, 770)
T_NAME_A, T_DESC_A = (226, 758, 330, 784), (226, 786, 520, 808)
T_NAME_P, T_DESC_P = (226, 862, 322, 888), (226, 890, 640, 930)

T_LIST_HEADER = (34, 940, 165, 972)
SORT = (564, 938, 706, 974)
T_SORT = (578, 944, 642, 968)
LIST = (16, 976, 706, 1114)                            # 가로로 넘기는 목록 창
CARD0 = (18, 978, 152, 1110)                           # 칸 134 · 간격 144 — 칸 사이 10
CARD_PITCH = 144.0
T_CARD_NAME = (40, 1052, 140, 1080)


def key_out(im):
    """마젠타 바탕 빼기 — popup_parts_fit.key_out 과 같다(그 파일은 불러오면 작업을 다 돌려서 옮겨 둔다)."""
    a = np.asarray(im.convert('RGBA')).astype(np.int32).copy()
    h, w = a.shape[:2]
    bg = np.mean([a[2, 2, :3], a[2, w - 3, :3], a[h - 3, 2, :3], a[h - 3, w - 3, :3]], axis=0)
    d = np.sqrt(((a[..., :3] - bg) ** 2).sum(axis=2))
    t = np.clip((d - 60) / 90.0, 0, 1)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    m = np.minimum(r, b)
    spill = np.where((m > g) & (t < 1), (m - g) * (1 - t), 0)
    a[..., 0] = np.clip(r - spill, 0, 255)
    a[..., 2] = np.clip(b - spill, 0, 255)
    a[..., 3] = (a[..., 3] * t).astype(np.int32)
    return Image.fromarray(a.astype(np.uint8))


def main():
    os.makedirs(OUT, exist_ok=True)
    M = load(os.path.join(IN, 'growth_host_v5.png'))
    P = gb.align(load(os.path.join(IN, 'growth_host_v5_plate.png')), M)
    T = {}

    # ── 글자 재기 ─────────────────────────────────────────
    T['H5NameEn'] = measure(M, T_NAME_EN, 'L', 'GANGSTER')
    T['H5Name'] = measure(M, T_NAME, 'L', '갱스터')
    T['H5Desc'] = measure(M, T_DESC, 'L', '권총 한 자루로 길을 연다.', lines=2)
    T['H5StarLabel'] = measure(M, T_STAR_LABEL, 'C', '성급')
    T['H5StarValue'] = measure(M, T_STAR_VALUE, 'C', '1.5 / 5')
    T['H5ShardValue'] = measure(M, T_SHARD_VALUE, 'C', '16 / 16', thresh=160)
    T['H5StarUp'] = measure(M, T_STARUP, 'C', '성급 UP', thresh=160)
    T['H5StarUpCost'] = measure(M, T_STARUP_COST, 'L', '1,200', thresh=160)
    T['H5StatHeader'] = measure(M, T_STAT_HEADER, 'L', '능력치 강화')
    T['H5Cap'] = measure(M, T_CAP, 'R', '강화 상한 Lv 30')
    T['H5RowName'] = measure(M, T_ROW_NAME, 'L', '공격력')
    T['H5RowLvLabel'] = measure(M, T_ROW_LV_LABEL, 'L', 'Lv.')
    T['H5RowLvNum'] = measure(M, T_ROW_LV_NUM, 'L', '0')
    T['H5RowLvMax'] = measure(M, T_ROW_LV_MAX, 'L', '/ 30')
    T['H5RowPct'] = measure(M, T_ROW_PCT, 'R', '+0.0%')
    T['H5RowCost'] = measure(M, T_ROW_COST, 'C', '60', thresh=160)
    T['H5SkillHeader'] = measure(M, T_SKILL_HEADER, 'L', '스킬')
    T['H5Badge'] = measure(M, T_BADGE_A, 'C', 'Lv.3')
    T['H5Lock'] = measure(M, T_LOCK, 'L', 'Lv.5 특수 효과')
    T['H5SkillName'] = measure(M, T_NAME_A, 'L', '일제 표식')
    T['H5SkillDesc'] = measure(M, T_DESC_A, 'L', '방 안 모든 적에게 3초간 표식을 새긴다.')
    T['H5PassiveName'] = measure(M, T_NAME_P, 'L', '처형 계약')
    T['H5PassiveDesc'] = measure(M, T_DESC_P, 'L', '표식이 붙은 적을 20% 로 즉사. 엘리트는 최대 체력 50%,', lines=2)
    T['H5ListHeader'] = measure(M, T_LIST_HEADER, 'L', '호스트 목록')
    T['H5Sort'] = measure(M, T_SORT, 'L', '기본순')
    T['H5CardName'] = measure(M, T_CARD_NAME, 'C', '갱스터')

    # ── 바탕 — 빈 판 + 맨 위 줄은 시안 ───────────────────────
    bg = P.copy()
    for r in TOP_KEEP:
        paste(bg, M, r)
    # 시안은 하단 바를 1150 에 그렸고 게임 하단 바는 1170 에서 시작한다 — 그 사이에 시안 하단 바 윗변이 비친다.
    # 목록 판 바로 아래 빈 띠(1114~1132)를 되풀이해 덮는다(판 픽셀 그대로).
    for y in range(1134, NAV_Y):
        bg[y] = bg[1114 + (y - 1134) % 18]
    Image.fromarray(bg[:NAV_Y]).save(os.path.join(OUT, 'bg_host_v5.png'))

    # ── 부품 — 시안에서 떼고 속 글자만 지운다. 투명도는 빈 판과의 차이 ──
    def part(name, rect, erase=(), solid=True, thresh=110, grow=5):
        img = erase_texts(M.copy(), list(erase), thresh=thresh, grow=grow) if erase else M
        rgb = crop(img, rect)
        al = diff_alpha(rgb, crop(P, rect), solid=solid)
        Image.fromarray(np.dstack([rgb, al]).astype(np.uint8)).save(os.path.join(OUT, f'{name}.png'))

    part('h5_badge', BADGE_A, [(150, 767, 204, 792)], thresh=70, grow=5)
    part('h5_lock', LOCK, [T_LOCK])
    part('h5_tag_active', TAG_A)
    part('h5_tag_passive', TAG_P)
    part('h5_sort', SORT, [T_SORT])
    # 노란 버튼 둘 — 글자만 지우고 동전 그림은 남긴다(값 · 「성급 UP」 은 게임이 찍는다)
    part('h5_gold_button', ROW_BTN, [(590, 487, 670, 516)], thresh=80, grow=7)
    part('h5_starup_button', STARUP_BTN, [(490, 368, 672, 393), (556, 398, 660, 424)], thresh=80, grow=7)

    # 능력치 한 줄 — 빈 판의 첫 줄 띠를 통째로(불투명) 뗀다. 몸 능력치는 8종이라 5줄 창에서 넘긴다 —
    # 줄 띠가 불투명하면 판에 박힌 줄 무늬를 덮으며 넘어간다.
    band = crop(P, ROW_BAND)
    Image.fromarray(band).save(os.path.join(OUT, 'h5_row.png'))

    # 성급 올리기 창 — 코덱스가 마젠타 바탕에 그린 빈 창. 바탕만 뺀다(popup_parts_fit 와 같은 방식)
    win = key_out(Image.open(os.path.join(IN, 'starup_popup_plate.png')))
    win = win.crop(win.getbbox())
    win.save(os.path.join(OUT, 'h5_starup_window.png'))

    rects = {
        'CARD': CARD, 'PORTRAIT': PORTRAIT, 'STAR_BOX': STAR_BOX, 'STAR_MARK': STAR_MARK,
        'SHARD_BAR_IN': SHARD_BAR_IN, 'STARUP_BTN': STARUP_BTN, 'STARUP_COIN': STARUP_COIN,
        'HELP': HELP, 'CAP_MARK': CAP_MARK, 'ROW0': ROW0, 'ROW_BAND': ROW_BAND, 'STAT_VIEW': STAT_VIEW, 'ROW_ICON': ROW_ICON, 'ROW_BTN': ROW_BTN, 'ROW_COIN': ROW_COIN,
        'SKILL_A': SKILL_A, 'SKILL_P': SKILL_P, 'TAG_A': TAG_A, 'TAG_P': TAG_P, 'STAR_A': STAR_A, 'STAR_P': STAR_P,
        'ICON_A': ICON_A, 'ICON_P': ICON_P, 'BADGE_A': BADGE_A, 'BADGE_P': BADGE_P, 'LOCK': LOCK, 'SORT': SORT,
        'LIST': LIST, 'CARD0': CARD0,
    }
    for i, r in enumerate(STARS):
        rects[f'STAR{i}'] = r
    out = {
        'rowPitch': ROW_PITCH, 'cardPitch': CARD_PITCH,
        'texts': [dict(name=k, **v) for k, v in T.items()],
        'rects': [{'name': k, 'box': [float(x) for x in v]} for k, v in rects.items()],
    }
    json.dump(out, open(os.path.join(OUT, 'growth_v5_spec.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    for k, v in T.items():
        print(f"{k:16s} {v['box']} {v['color']} size {v['size']}")


if __name__ == '__main__':
    main()
