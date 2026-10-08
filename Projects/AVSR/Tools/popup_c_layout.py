"""C 「원혼 회로」 팝업 4종 배치 — 단일 출처 (2026-10-08).

틀 그림(out/setc/{frame}.png) 안의 칸 위치를 재서(fit 뒤 실측) 화면 좌표로 옮긴 값.
python popup_c_layout.py          → popup_c_layout.json 쓰기 + 미리보기(_exchange/ref/popup_c_preview_*.png)
유니티 쪽 Assets/Scripts/Editor/InGamePopupCLayout.cs 가 같은 json 을 읽어 프리팹에 꽂는다 — 값은 여기만 고친다.

좌표: 패널(720x1280) 직속 노드는 화면 왼쪽 위 기준 (x, y, w, h). 자식 노드는 부모 왼쪽 위 기준.
글자: fs = 최대(자동 크기 상한), mn = 최소. 상의(consult_popup_restyle.md) 목표치에 최대한 붙이되 틀 칸 크기 안에서.
"""
import json
import os

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
OUT = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'out', 'setc')
REF = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref')

GOLD = '#F5C044'
CYAN = '#8FE8F0'
WHITE = '#F2F4F8'
DIM = '#B8C4D4'
RED = '#FF6A5A'


def at(frame_xy, fx, fy):
    return frame_xy[0] + fx, frame_xy[1] + fy


# 틀 자리(화면) — 위 HUD(~225) 아래, 아래 조작 버튼(~1060) 위
LV = (15, 226)      # levelupframe 690x790 — 위 유령 문장이 HUD 아랫단에 걸리지 않게
SH = (65, 400)      # shrineframe 590x530
EV = (30, 372)      # eventframe 660x595
SP = (15, 395)      # shopframe 690x545

# ── 글자 역할 (코덱스 합의 _exchange/consult_popup_typography.md, 2026-10-08) ─────────
# 로비와 한 집안: 진짜 굵은 폰트(NotoSans*-Bold SDF) + HeavyText(획 dilate · 외곽선), 가짜 굵게 금지, 고정 크기.
# 자동 줄임은 좁게(최대 → 최소). 같은 묶음(카드 3 · 제단 3 · 상점 6 · 보상/대가 한 쌍)은 같은 크기(group).
# heavy=False 는 본문 폰트 + Soft 재질(설명 · 이벤트 본문).
ROLES = {
    #            heavy  fs  mn  dilate outline outline색     글자색     줄간격
    'title':     (True, 34, 30, 0.30, 0.22, '#07101AF5', '#FFFFFF', 0),
    'title_s':   (True, 34, 26, 0.30, 0.22, '#07101AF5', '#FFFFFF', 0),   # 이름이 길 수 있는 제목(악마의 거래 18종)   # PD 「제목이 뿌옇다」 — 대표색 반투명 외곽선이 글자를 번지게 해 짙은 외곽선으로
    'hint':      (True, 18, 16, 0.18, 0.10, '#07101AD9', '#E8EDF2', 0),
    'name':      (True, 21, 18, 0.28, 0.12, '#07101AD9', '#F2F1E9', 0),
    'desc':      (False, 16, 14, 0.06, 0.08, '#07101ACC', '#C9D1D8', 2),
    'body':      (False, 19, 17, 0.06, 0.08, '#07101ACC', '#C9D1D8', 2),
    'value':     (True, 18, 16, 0.25, 0.12, '#07101AE6', '#F5E6A8', 0),
    # 거래 보상 · 대가 알약 — 일본어 보상 문구가 한 줄로 안 들어간다(「報酬・ショップ価格-40%（ラン終了まで）」 실측 312px / 칸 236).
    # 두 줄까지 접고 더 작게 줄인다(게임 전 문장 점검 2026-10-08)
    'pill':      (True, 17, 13, 0.25, 0.12, '#07101AE6', '#F5E6A8', -10),
    # 좁은 칸 이름(상점) — 일본어 「体 — ギャングスター」가 18 로는 130 칸을 넘었다(169px)
    'name_s':    (True, 21, 12, 0.28, 0.12, '#07101AD9', '#F2F1E9', 0),
    'chip':      (True, 16, 14, 0.22, 0.10, '#07101AE6', '#DCE8F0', 0),
    'primary':   (True, 26, 22, 0.34, 0.14, '#07101AE6', '#FFF3D0', 0),
    'secondary': (True, 20, 17, 0.24, 0.12, '#07101AD9', '#D7DCE0', 2),
    # 결과창 전용 — 「CHAPTER n CLEAR」 큰 제목 · 핵심 결과 값(한 화면 한 군데) · 노란 버튼 위 짙은 글자
    'result_title': (True, 44, 36, 0.35, 0.22, '#07101AF5', '#FFFFFF', 0),
    'hero':      (True, 32, 28, 0.38, 0.16, '#07101AE6', '#F5C044', 0),
    'ok_dark':   (True, 30, 24, 0.30, 0, None,             '#2A1A05', 0),
}
# 늘려 쓰는 판의 테두리(왼 · 아래 · 오른 · 위, 원본 px) — 테두리는 그대로, 안쪽만 늘어난다.
# ⚠ 제단 선택 칸을 통째로 늘였더니(위아래 빈 여백 16 px 포함) 글자 자리가 38 px 로 줄어 이름 · 설명이
#   위 · 아래 테두리에 걸쳤다(PD 2026-10-08 「회복의 제단 UI 랑 텍스트가 다 겹친다」). 그림은 빈 여백을 잘라 두고(fit 뒤 bbox),
#   테두리는 실측: 위 · 아래 베벨 9 px, 왼쪽 아이콘 칸 끝 104 px, 오른쪽 보석 칸 시작 352 px.
BORDERS = {
    'shrinechoiceslot': (104, 9, 88, 9),
    'eventcostpill': (24, 10, 24, 10),
    'eventrewardpill': (18, 10, 18, 10),   # 양끝 장식 16px — 넓은 한 줄 알약으로 늘린다(10-08)
    # 상품 칸 — 창을 세로로 늘리며 칸도 91 → 121. 위 37 · 아래 25 는 장식 줄이라 그대로, 가운데 민무늬만 늘린다(줄 차이 실측)
    'shopitemslot': (20, 25, 20, 37),
    'shopitemslot_off': (20, 25, 20, 37),
}

ACCENT = {'BuffChoicePanel': '#FFC83D', 'ShrinePanel': '#42EAF2', 'EventPanel': '#FF4B4B', 'ShopPanel': '#FFBE32',
          'ChapterResultPopup': '#75F3FF'}

NODES = []


def node(panel, name, x, y, w, h, parent=None, sprite=None, sliced=False, create=None, role=None, color=None,
         align=None, first=False, before=None, nowrap=None, group=None, font=None, skip=False, hide=False, post=False):
    # post — 좌표가 이미 **세로로 늘린 틀** 기준이다(stretch 가 옮기지 않고 새 틀 윗변만 따라간다)
    d = dict(panel=panel, name=name, parent=parent, x=x, y=y, w=w, h=h, sprite=sprite, sliced=sliced, create=create,
             fs=None, mn=None, color=color, align=align, first=first, before=before or '', nowrap=bool(nowrap),
             role=role or '', heavy=False, dilate=0.0, outline=0.0, outlineColor='', lineSpacing=0.0, group=group or '',
             font=font or '', skip=skip, hide=hide, post=post)
    if role:
        heavy, fs, mn, dil, ol, olc, col, ls = ROLES[role]
        d.update(fs=fs, mn=mn, heavy=heavy, dilate=dil, outline=ol, outlineColor=olc or '', lineSpacing=ls,
                 color=color or col, nowrap=nowrap if nowrap is not None else role not in ('desc', 'body', 'secondary', 'pill'))
    NODES.append(d)


# ── 레벨업 ────────────────────────────────────────────────
P = 'BuffChoicePanel'
node(P, 'BuffFrame', *LV, 690, 790, sprite='levelupframe', create='image', first=True)
node(P, 'BuffTitleText', *at(LV, 176, 162), 349, 80, role='title', color='#FFC83D', font='pixel')   # 번역 안 하는 장식 표식
node(P, 'BuffSubText', *at(LV, 180, 241), 329, 38, role='hint')
for i, fx in enumerate((52, 264, 477)):
    c = 'BuffCard%d' % i
    node(P, c, *at(LV, fx, 368), 162, 320, sliced=True)
    node(P, c + 'Chip', 14, 6, 134, 26, parent=c)
    node(P, c + 'ChipText', 0, 0, 134, 26, parent=c + 'Chip', role='chip', group='buff_chip')
    node(P, c + 'Icon', 21, 40, 120, 120, parent=c)
    node(P, c + 'Name', 2, 170, 158, 34, parent=c, role='name', group='buff_name')
    # 카드 그림 안쪽 테두리(좌우 약 12px)에 글자가 닿지 않게 — 일본어 두 줄이 테두리에 붙었다(게임 캡처 2026-10-08)
    node(P, c + 'Desc', 14, 206, 134, 92, parent=c, role='desc', align='center', group='buff_desc')

# ── 회복의 제단 ───────────────────────────────────────────
P = 'ShrinePanel'
node(P, 'ShrineBox', *SH, 590, 530, sprite='shrineframe')
node(P, 'ShrineTitleText', *at(SH, 128, 80), 334, 50, role='title')
node(P, 'ShrineHintPill', *at(SH, 155, 140), 280, 34, sprite='shrinehintpill')
node(P, 'ShrineHintText', *at(SH, 145, 140), 300, 34, role='hint', color='#9FEFF5')
for i, fy in enumerate((186, 282, 375)):
    c = 'ShrineChoice%d' % i
    # 칸 그림은 세로로 긴 판(out/setc_tall · 440x134)을 비율 그대로 그린다 — 9분할로 늘리면 오른쪽 보석이 길쭉해졌다(10-08)
    node(P, c, *at(SH, 92, fy - 6), 407, 88, sprite='shrinechoiceslot')
    # 글자 자리 = 아이콘 칸(왼 104) · 보석 칸(오른 88) · 위아래 베벨(9) 안쪽: x 112~311, y 13~75
    node(P, c + 'Text', 112, 14, 200, 30, parent=c, role='name', align='left', group='shrine_name')
    node(P, c + 'Desc', 112, 46, 200, 26, parent=c, create='text', role='desc', align='left', nowrap=True,
         color='#B9EEF2', group='shrine_desc')

# ── 악마의 거래 ───────────────────────────────────────────
P = 'EventPanel'
node(P, 'EventBox', *EV, 660, 595, sprite='eventframe')
# 제목판 안쪽 — 판 폭(288)과 같게 두었더니 긴 제목(「捨てられた補給品」)이 판 테두리에 닿았다. 긴 제목은 26 까지 줄어든다
node(P, 'EventTitleText', *at(EV, 204, 112), 252, 55, role='title_s')
# 늘린 틀(685) 기준 — 본문 4줄 · 보상 / 대가는 넓은 한 줄 알약을 위아래로(PD 10-08 「악마의 계약은 텍스트 공간이 부족」:
# 반폭 알약에 「보상 · 상점 값 -40% (판 끝까지)」가 두 줄로 접혀 꽉 끼었고 위 본문 칸은 비어 있었다)
node(P, 'EventBodyText', *at(EV, 130, 190), 400, 104, role='body', align='center', post=True)   # 좌우 사슬 장식을 피해(440 에선 끝 글자가 사슬에 걸렸다)
node(P, 'EventRewardPill', *at(EV, 80, 300), 500, 46, sprite='eventrewardpill', sliced=True, create='image', post=True)
node(P, 'EventRewardText', *at(EV, 104, 300), 452, 46, create='text', role='pill', color='#F5E6A8', group='event_pair',
     nowrap=True, post=True)
node(P, 'EventCostPill', *at(EV, 80, 352), 500, 46, sprite='eventcostpill', sliced=True, post=True)
node(P, 'EventCostText', *at(EV, 104, 352), 452, 46, role='pill', color='#FF7676', group='event_pair', nowrap=True,
     post=True)
node(P, 'EventAcceptButton', *at(EV, 100, 318), 460, 98, sprite='eventacceptbutton')
node(P, 'EventAcceptText', 0, 0, 460, 98, parent='EventAcceptButton', role='primary')
node(P, 'EventDeclineButton', *at(EV, 107, 411), 446, 66, sprite='eventdeclinebutton')
node(P, 'EventDeclineText', 0, 0, 446, 66, parent='EventDeclineButton', role='secondary', nowrap=True)
node(P, 'EventResultText', *at(EV, 100, 318), 460, 98, role='secondary')

# ── 상점 ──────────────────────────────────────────────────
P = 'ShopPanel'
node(P, 'ShopBox', *SP, 690, 545, sprite='shopframe')
node(P, 'ShopTitleText', *at(SP, 197, 65), 296, 51, role='title')
node(P, 'ShopGoldPill', *at(SP, 217, 116), 256, 34, sprite='shopgoldpill', create='image', before='ShopGoldText')
node(P, 'ShopGoldText', *at(SP, 214, 116), 262, 34, role='value')
node(P, 'ShopLimitText', *at(SP, 70, 148), 550, 30, role='hint', color='#C9D1D8')   # 안 띄운다(하나 사면 닫힘 — PD 10-08). 노드는 남겨 둔다
for i in range(6):
    c = 'ShopItem%d' % i
    fx = (75, 356)[i % 2]
    fy = (179, 277, 374)[i // 2]
    node(P, c, *at(SP, fx, fy), 270, 91, sprite='shopitemslot', sliced=True)
    node(P, c + 'Icon', 9, 10, 62, 62, parent=c)
    # 가격 = 칸 그림 속 가격 상자(267 폭 그림 x 190~255 · y 12~35 → 270 폭 칸 x 193~258) 안에 꼭 맞게.
    # 이름은 같은 줄 가운데(y 23) · 가격 상자 앞까지(10-08 게임 캡처: 「66 G」가 상자 위 테두리에 걸쳐 있었다)
    node(P, c + 'Name', 74, 8, 116, 30, parent=c, role='name_s', align='left', group='shop_name')
    node(P, c + 'Price', 193, 12, 65, 23, parent=c, role='value', group='shop_price')
    node(P, c + 'Desc', 76, 40, 184, 41, parent=c, role='desc', align='left', group='shop_desc')   # 이름 줄(8~38) 아래 · 칸 아래 테두리에 닿지 않게
node(P, 'ShopLeaveButton', *at(SP, 197, 472), 296, 64, sprite='shopleavebutton')
node(P, 'ShopLeaveText', 0, 0, 296, 64, parent='ShopLeaveButton', role='primary')
node(P, 'ShopResultText', *at(SP, 70, 528), 550, 30, role='secondary', nowrap=True)

# ── 챕터 클리어 결과창 (C 원혼 회로 틀 result_frame — 연출 시안 mock_fxstory_clear_v2 컷3) ─────
# 노드는 ChapterScreensBuilder 가 ResultContent(720x1280) 아래에 세운 것 — 자리 · 글자만 새 틀에 맞춘다.
# 틀 안 칸(틀 그림 실측, 틀 왼쪽 위 기준): 제목 판 106~553 x 176~262 · 이름 판 185~475 x 265~295 · 안쪽 판 114~543 x 326~648 · OK 자리 아래
P = 'ChapterResultPopup'
RF = (30, 220)
node(P, 'ResultContent', 0, 0, 720, 1280, skip=True)
node(P, 'ResultFrame', *at(RF, 0, 0), 660, 800, parent='ResultContent', sprite='result_frame')
node(P, 'ResultTitleText', *at(RF, 120, 182), 420, 74, parent='ResultContent', role='result_title', color='#FFFFFF')
node(P, 'ResultSubText', *at(RF, 190, 265), 280, 30, parent='ResultContent', role='hint')
node(P, 'ResultGoldRow', *at(RF, 124, 334), 410, 126, parent='ResultContent', sprite='result_rowpanel')
node(P, 'ResultGoldIcon', 14, 10, 150, 106, parent='ResultGoldRow')
node(P, 'ResultGoldLabelText', 176, 16, 220, 34, parent='ResultGoldRow', role='name', align='left')
node(P, 'ResultGoldValueText', 176, 54, 220, 56, parent='ResultGoldRow', role='hero', align='left')
node(P, 'ResultChestRow', *at(RF, 124, 466), 410, 126, parent='ResultContent', sprite='result_rowpanel')
node(P, 'ResultChestArt', 22, 9, 140, 108, parent='ResultChestRow')
node(P, 'ResultChestNameText', 176, 35, 220, 56, parent='ResultChestRow', role='name', align='left')
node(P, 'ResultWarnBar', *at(RF, 124, 600), 410, 42, parent='ResultContent', sprite='result_warnbar')
node(P, 'ResultWarnText', 52, 4, 346, 34, parent='ResultWarnBar', role='hint', color='#FF8A80')
# OK 판은 틀 그림에 그려져 있다 — 버튼은 그 자리에 투명하게 겹쳐 누르기만 받는다(판 그림을 또 얹으면 테두리가 두 겹)
node(P, 'ResultOkButton', *at(RF, 185, 680), 290, 76, parent='ResultContent', hide=True)
node(P, 'ResultOkText', 0, 0, 290, 76, parent='ResultOkButton', role='ok_dark')

# 텍스트 미리보기용 예시(지금 캡처의 일본어)
SAMPLE = {
    'BuffTitleText': 'LEVEL UP!', 'BuffSubText': 'カードを選択してください',
    'BuffCard0ChipText': 'RARE', 'BuffCard1ChipText': 'RARE', 'BuffCard2ChipText': 'COMMON',
    'BuffCard0Name': '成長加速', 'BuffCard1Name': '守護の盾', 'BuffCard2Name': '吸魂',
    'BuffCard0Desc': '獲得経験値が増える。', 'BuffCard1Desc': '盾が周囲を回って攻撃を防ぐ。', 'BuffCard2Desc': '敵を倒すと体力が回復する。',
    'ShrineTitleText': '回復の祭壇', 'ShrineHintText': 'ひとつだけ持っていける',
    'ShrineChoice0Text': '手が速くなる', 'ShrineChoice0Desc': '攻撃が速くなる',
    'ShrineChoice1Text': '遠くまで届く', 'ShrineChoice1Desc': '射程が伸びる',
    'ShrineChoice2Text': '器を広げる', 'ShrineChoice2Desc': '奪う体がより頑丈になる',
    'EventTitleText': '深淵の取引', 'EventBodyText': '底の見えない通路。下から声が昇ってくる。\n代価は魂で受け取ると言う。',
    'EventRewardText': '報酬・エピックカード1枚', 'EventCostText': 'ゴースト体力25%',
    'EventAcceptText': '魂を差し出す', 'EventDeclineText': '代わりに休んでいく（ゴースト30%回復）',
    'ShopTitleText': 'ショップ', 'ShopGoldText': '所持ゴールド 0',
    'ShopLeaveText': '店を出る',
    'ResultTitleText': 'CHAPTER 1 CLEAR', 'ResultSubText': 'ゴミ集積場', 'ResultGoldLabelText': 'ゴールド獲得',
    'ResultGoldValueText': '+1,240', 'ResultChestNameText': 'シルバー宝箱', 'ResultWarnText': '宝箱スロットがいっぱいです',
    'ResultOkText': 'OK',
}
SHOP_SAMPLE = [('火炎の刻印', '攻撃に燃焼を付与する。', '42 G', 'card_c009'), ('窮地', '体力が半分以下になると強くなる。', '42 G', 'card_c010'),
               ('守護の盾', '盾が周囲を回って攻撃を防ぐ。', '42 G', 'card_c003'), ('体 — ギャングスター', 'その場でこの体を奪う', '66 G', None),
               ('停止', '次の部屋で敵が5秒止まる', '14 G', 'shop_freeze'), ('治療', 'ホスト35%・ゴースト25%回復', '42 G', 'shop_heal')]
for i, (n, d, p, _) in enumerate(SHOP_SAMPLE):
    SAMPLE['ShopItem%dName' % i] = n
    SAMPLE['ShopItem%dDesc' % i] = d
    SAMPLE['ShopItem%dPrice' % i] = p


SAMPLE_KO = {
    'BuffTitleText': 'LEVEL UP!', 'BuffSubText': '카드를 선택하세요',
    'BuffCard0ChipText': 'RARE', 'BuffCard1ChipText': 'COMMON', 'BuffCard2ChipText': 'RARE',
    'BuffCard0Name': '추가 발사', 'BuffCard1Name': '흡혼', 'BuffCard2Name': '수호 방패',
    'BuffCard0Desc': '쏠 때마다 탄이 더 나간다.', 'BuffCard1Desc': '적을 잡으면 체력을 돌려받는다.', 'BuffCard2Desc': '방패가 내 주위를 돌며 막는다.',
    'ShrineTitleText': '회복의 제단', 'ShrineHintText': '하나만 가져갈 수 있다',
    'ShrineChoice0Text': '손이 빨라진다', 'ShrineChoice0Desc': '공격이 빨라진다',
    'ShrineChoice1Text': '몸을 아문다', 'ShrineChoice1Desc': '호스트 체력을 가득 채운다',
    'ShrineChoice2Text': '그릇을 넓힌다', 'ShrineChoice2Desc': '뺏는 몸이 더 튼튼해진다',
    'EventTitleText': '야전 의무병', 'EventBodyText': '부두 창고에 아직 숨이 붙은 의무병이 있다.\n말은 안 통하지만 손짓은 통한다.',
    'EventRewardText': '보상 · 호스트 체력 25% 회복', 'EventCostText': '고스트 체력 20%',
    'EventAcceptText': '값을 치른다', 'EventDeclineText': '내버려 둔다',
    'ShopTitleText': '상점', 'ShopGoldText': '보유 골드 0',
    'ShopLeaveText': '나간다',
    'ResultTitleText': 'CHAPTER 1 CLEAR', 'ResultSubText': '쓰레기 집적장', 'ResultGoldLabelText': '골드 획득',
    'ResultGoldValueText': '+1,240', 'ResultChestNameText': '은빛 상자', 'ResultWarnText': '상자 칸이 가득 찼습니다',
    'ResultOkText': 'OK',
}
for i, (n, dsc, p) in enumerate([('화염 각인', '공격에 화상을 붙인다.', '42 G'), ('궁지', '체력이 절반 아래면 세진다.', '42 G'),
                                 ('수호 방패', '방패가 내 주위를 돌며 막는다.', '42 G'), ('몸 — 갱스터', '그 자리에서 이 몸을 빼앗는다', '66 G'),
                                 ('정지', '다음 방에서 적이 5초 멈춘다', '14 G'), ('치료', '호스트 35% · 고스트 25% 회복', '42 G')]):
    SAMPLE_KO['ShopItem%dName' % i] = n
    SAMPLE_KO['ShopItem%dDesc' % i] = dsc
    SAMPLE_KO['ShopItem%dPrice' % i] = p

FONTS = {
    'ja': (os.path.join(ROOT, 'Assets', 'BaseResource', 'Fonts', 'NotoSansJP-Bold.ttf'),
           os.path.join(ROOT, 'Assets', 'BaseResource', 'Fonts', 'NotoSansJP-Regular.otf')),
    'ko': (os.path.join(ROOT, 'Assets', 'BaseResource', 'Fonts', 'NotoSansKR-Bold.ttf'),
           os.path.join(ROOT, 'Assets', 'BaseResource', 'Fonts', 'NotoSansKR.ttf')),
}


def nine(src, b, w, h):
    from PIL import Image
    l, bo, r, t = b
    W, H = src.size
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    xs = [(0, l, 0, l), (l, W - r, l, w - r), (W - r, W, w - r, w)]
    ys = [(0, t, 0, t), (t, H - bo, t, h - bo), (H - bo, H, h - bo, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if sx1 <= sx0 or sy1 <= sy0 or dx1 <= dx0 or dy1 <= dy0:
                continue
            out.alpha_composite(src.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS), (dx0, dy0))
    return out


def rgba(hexs, default=(242, 244, 248, 255)):
    if not hexs:
        return default
    h = hexs.lstrip('#')
    v = tuple(int(h[i:i + 2], 16) for i in range(0, len(h), 2))
    return v if len(v) == 4 else v + (255,)


def fit(d, n, text, path):
    """역할 크기에서 시작해 최소까지 줄인다 — 한 줄 칸은 줄바꿈 없이, 여러 줄 칸은 줄바꿈하며."""
    from PIL import ImageFont

    def layout(size):
        f = ImageFont.truetype(path, size)
        lines = []
        for para in text.split('\n'):
            if n['nowrap']:
                lines.append(para)
                continue
            cur = ''
            for ch in para:
                if d.textlength(cur + ch, font=f) > n['w'] - 8 and cur:
                    cut = cur.rfind(' ')
                    if cut > 0:
                        lines.append(cur[:cut])
                        cur = cur[cut + 1:] + ch
                    else:
                        lines.append(cur)
                        cur = ch
                else:
                    cur += ch
            lines.append(cur)
        lh = size * 1.22 + n['lineSpacing']
        ok = max(d.textlength(l, font=f) for l in lines) <= n['w'] - 8 and lh * len(lines) <= n['h'] - 4
        return f, lines, lh, ok

    size = int(n['fs'])
    while size > n['mn']:
        if layout(size)[3]:
            break
        size -= 1
    return size


def preview(lang='ja'):
    from PIL import Image, ImageDraw
    sample = SAMPLE if lang == 'ja' else SAMPLE_KO
    bold, regular = FONTS[lang]
    bg_path = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'skill_qx_check', 'robot.png')
    icons = os.path.join(OUT, 'icons')
    sprite_of = {'cardpanel': ['cardpanel_rare', 'cardpanel_rare', 'cardpanel_common']}
    for panel in ('BuffChoicePanel', 'ShrinePanel', 'EventPanel', 'ShopPanel', 'ChapterResultPopup'):
        img = Image.open(bg_path).convert('RGBA').resize((720, 1280))
        img.alpha_composite(Image.new('RGBA', img.size, (0, 0, 0, 175)))
        d = ImageDraw.Draw(img)
        nodes = [n for n in NODES if n['panel'] == panel]
        # 묶음 크기 — 묶음 안 가장 긴 글이 정한 크기를 다 같이 쓴다(게임과 같은 규칙)
        sizes = {}
        for n in nodes:
            t = sample.get(n['name'])
            if t and n['fs']:
                sizes[n['name']] = fit(d, n, t, bold if n['heavy'] else regular)
        for g in {n['group'] for n in nodes if n['group']}:
            m = min(sizes[n['name']] for n in nodes if n['group'] == g and n['name'] in sizes)
            for n in nodes:
                if n['group'] == g and n['name'] in sizes:
                    sizes[n['name']] = m
        absxy = {}
        for n in nodes:
            px, py = absxy.get(n['parent'], (0, 0)) if n['parent'] else (0, 0)
            if n['skip']:
                absxy[n['name']] = (0, 0)
                continue
            x, y = px + n['x'], py + n['y']
            absxy[n['name']] = (x, y)
            sp = n['sprite']
            if n['name'].startswith('BuffCard') and n['parent'] is None:
                sp = sprite_of['cardpanel'][int(n['name'][-1])]
            if n['name'].endswith('Chip') and n['parent']:
                sp = 'cardchip_rarity'
            if sp:
                tall = os.path.join(OUT + '_tall', sp + '.png')
                src = Image.open(tall if os.path.exists(tall) else os.path.join(OUT, sp + '.png')).convert('RGBA')
                art = nine(src, BORDERS[sp], n['w'], n['h']) if n['sliced'] and sp in BORDERS else                     src.resize((n['w'], n['h']), Image.LANCZOS)
                img.alpha_composite(art, (x, y))
            if n['name'].endswith('Icon'):
                key = None
                if panel == 'BuffChoicePanel':
                    key = ['card_c008', 'card_c006', 'card_c003'][int(n['name'][8])]
                elif panel == 'ShopPanel':
                    key = ['card_c009', 'card_c010', 'card_c003', None, 'shop_freeze', 'shop_heal'][int(n['name'][8])]
                if key:
                    ic = Image.open(os.path.join(icons, key + '.png')).convert('RGBA').resize((n['w'], n['h']), Image.LANCZOS)
                    img.alpha_composite(ic, (x, y))
            if n['name'] in ('ResultGoldIcon', 'ResultChestArt'):
                k = os.path.join(ROOT, 'Assets', 'BaseResource', 'ChapterScreens', 'reward_goldpile.png') if 'Gold' in n['name'] else os.path.join(ROOT, 'Assets', 'BaseResource', 'LobbyMainUI', 'chest_silver.png')
                ic = Image.open(k).convert('RGBA')
                sc = min(n['w'] / ic.width, n['h'] / ic.height)
                ic = ic.resize((int(ic.width * sc), int(ic.height * sc)), Image.LANCZOS)
                img.alpha_composite(ic, (x + (n['w'] - ic.width) // 2, y + (n['h'] - ic.height) // 2))
            if panel == 'ShrinePanel' and n['name'] in ('ShrineChoice0', 'ShrineChoice1', 'ShrineChoice2'):
                key = ['shrine_speed', 'shrine_full_heal', 'shrine_max_hp'][int(n['name'][-1])]
                ic = Image.open(os.path.join(icons, key + '.png')).convert('RGBA').resize((60, 60), Image.LANCZOS)
                img.alpha_composite(ic, (x + 22, y + (n['h'] - 60) // 2))
            text = sample.get(n['name'])
            if not (text and n['fs']):
                continue
            if n['font'] == 'pixel':
                cap = Image.open(os.path.join(REF, 'popup_c_ingame', 'levelup.png')).convert('RGBA')
                img.alpha_composite(cap.crop((x, y, x + n['w'], y + n['h'])), (x, y))
                continue
            from PIL import ImageFont
            path = bold if n['heavy'] else regular
            size = sizes[n['name']]
            f = ImageFont.truetype(path, size)
            lines = text.split('\n') if n['nowrap'] else fit_lines(d, n, text, f)
            lh = size * 1.22 + n['lineSpacing']
            ty = y + (n['h'] - lh * len(lines)) / 2 - size * 0.08
            stroke = max(1, round(n['outline'] * size * 0.32)) if n['outline'] else 0
            if n['role'] in ('title', 'title_s'):
                stroke = 3
                # 아래로 1 px 짙은 그림자 — 판 위에서 또렷하게 떠 보이게
                for l2, ty2 in [(l, ty + i * (size * 1.22)) for i, l in enumerate(lines)]:
                    tw2 = d.textlength(l2, font=f)
                    tx2 = x + 4 if n['align'] == 'left' else x + (n['w'] - tw2) / 2
                    d.text((tx2, ty2 + 2), l2, font=f, fill=(4, 8, 14, 230), stroke_width=stroke, stroke_fill=(4, 8, 14, 230))
            for l in lines:
                tw = d.textlength(l, font=f)
                tx = x + 4 if n['align'] == 'left' else x + (n['w'] - tw) / 2
                d.text((tx, ty), l, font=f, fill=rgba(n['color']), stroke_width=stroke, stroke_fill=rgba(n['outlineColor'], (7, 16, 26, 217)))
                ty += lh
        img.convert('RGB').save(os.path.join(REF, 'popup_c_type_%s_%s.png' % (lang, panel)))


# ── 창 세로 늘리기 (PD 2026-10-08 「위아래로 크게 해서 텍스트 영역을 더 확보」) ─────────────
# 위 node() 값은 원본 틀 기준 그대로 두고, 여기서 make_tall_frames.INSERTS 와 **같은 규칙**으로 옮긴다.
# 칸 안 설명 줄 높이에서 늘어나므로 제목 줄은 그대로, 설명 · 본문 칸만 커지고 아래 것은 내려간다.
# 원본 값은 oy · oh 로 남긴다 — 연출 표(fx_story.py)가 원본 기준으로 쓴 좌표를 같은 규칙으로 옮길 때 쓴다.
TALL = {'ShrinePanel': ('ShrineBox', 'shrineframe', 346), 'EventPanel': ('EventBox', 'eventframe', 327),
        'ShopPanel': ('ShopBox', 'shopframe', 350)}   # 새 틀 윗변(화면 y) — 늘어난 만큼 위로 올려 가운데를 지킨다


def stretch():
    from make_tall_frames import remap
    by = {n['name']: n for n in NODES}

    def absy(n):
        y, p = n['y'], n['parent']
        while p:
            y += by[p]['y']
            p = by[p]['parent']
        return y

    for panel, (box, frame, new_top) in TALL.items():
        nodes = [n for n in NODES if n['panel'] == panel]
        top0, h0 = by[box]['y'], by[box]['h']
        span = {}
        for n in nodes:
            a = absy(n)
            r0, r1 = a - top0, a - top0 + n['h']
            if n.get('post'):
                t, b = new_top + r0, new_top + r1
            elif 0 <= r0 and r1 <= h0:
                t, b = new_top + remap(frame, r0), new_top + remap(frame, r1 - 1) + 1
                if n['name'].endswith('Icon'):
                    # 아이콘은 늘리지 않는다(정사각 그림) — 늘어난 자리의 가운데에 같은 크기로
                    t = (t + b - n['h']) // 2
                    b = t + n['h']
            else:
                t, b = a + new_top - top0, a + new_top - top0 + n['h']
            span[n['name']] = (t, b)
        for n in nodes:
            n['oy'], n['oh'] = n['y'], n['h']
            t, b = span[n['name']]
            n['y'] = t - (span[n['parent']][0] if n['parent'] else 0)
            n['h'] = b - t


stretch()


def fit_lines(d, n, text, f):
    lines = []
    for para in text.split('\n'):
        cur = ''
        for ch in para:
            if d.textlength(cur + ch, font=f) > n['w'] - 8 and cur:
                cut = cur.rfind(' ')
                if cut > 0:
                    lines.append(cur[:cut])
                    cur = cur[cut + 1:] + ch
                else:
                    lines.append(cur)
                    cur = ch
            else:
                cur += ch
        lines.append(cur)
    return lines


if __name__ == '__main__':
    with open(os.path.join(HERE, 'popup_c_layout.json'), 'w', encoding='utf-8') as fp:
        json.dump({'nodes': NODES, 'borders': [dict(sprite=k, l=v[0], b=v[1], r=v[2], t=v[3]) for k, v in BORDERS.items()]},
                  fp, ensure_ascii=False, indent=1)
    preview('ja')
    preview('ko')
    print(len(NODES), 'nodes')
