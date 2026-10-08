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

NODES = []


def node(panel, name, x, y, w, h, parent=None, sprite=None, sliced=False, create=None, fs=None, mn=None,
         color=None, align=None, first=False, before=None, nowrap=False):
    NODES.append(dict(panel=panel, name=name, parent=parent, x=x, y=y, w=w, h=h, sprite=sprite, sliced=sliced,
                      create=create, fs=fs, mn=mn, color=color, align=align, first=first, before=before or '',
                      nowrap=nowrap))


# ── 레벨업 ────────────────────────────────────────────────
P = 'BuffChoicePanel'
node(P, 'BuffFrame', *LV, 690, 790, sprite='levelupframe', create='image', first=True)
node(P, 'BuffTitleText', *at(LV, 176, 162), 349, 80, fs=52, mn=28, color=GOLD, nowrap=True)
node(P, 'BuffSubText', *at(LV, 190, 241), 309, 38, fs=22, mn=16, color=WHITE, nowrap=True)
for i, fx in enumerate((52, 264, 477)):
    c = 'BuffCard%d' % i
    node(P, c, *at(LV, fx, 368), 162, 320, sliced=True)
    node(P, c + 'Chip', 14, 6, 134, 26, parent=c)
    node(P, c + 'ChipText', 0, 0, 134, 26, parent=c + 'Chip', fs=18, mn=12, nowrap=True)
    node(P, c + 'Icon', 21, 40, 120, 120, parent=c)
    node(P, c + 'Name', 6, 170, 150, 34, parent=c, fs=26, mn=16, color=WHITE, nowrap=True)
    node(P, c + 'Desc', 8, 206, 146, 92, parent=c, fs=19, mn=13, color=DIM)

# ── 회복의 제단 ───────────────────────────────────────────
P = 'ShrinePanel'
node(P, 'ShrineBox', *SH, 590, 530, sprite='shrineframe')
node(P, 'ShrineTitleText', *at(SH, 138, 81), 314, 48, fs=34, mn=22, color=WHITE, nowrap=True)
node(P, 'ShrineHintPill', *at(SH, 155, 140), 280, 34, sprite='shrinehintpill')
node(P, 'ShrineHintText', *at(SH, 155, 140), 280, 34, fs=19, mn=13, color=CYAN, nowrap=True)
for i, fy in enumerate((186, 282, 375)):
    c = 'ShrineChoice%d' % i
    node(P, c, *at(SH, 92, fy - 4), 407, 78, sprite='shrinechoiceslot', sliced=True)
    node(P, c + 'Text', 76, 7, 280, 32, parent=c, fs=26, mn=18, color=WHITE, align='left', nowrap=True)
    node(P, c + 'Desc', 76, 37, 280, 26, parent=c, create='text', fs=20, mn=14, color=CYAN, align='left', nowrap=True)

# ── 악마의 거래 ───────────────────────────────────────────
P = 'EventPanel'
node(P, 'EventBox', *EV, 660, 595, sprite='eventframe')
node(P, 'EventTitleText', *at(EV, 206, 112), 248, 55, fs=34, mn=20, color=WHITE, nowrap=True)
node(P, 'EventBodyText', *at(EV, 88, 186), 484, 78, fs=21, mn=14, color=WHITE)
node(P, 'EventRewardPill', *at(EV, 80, 268), 246, 48, sprite='eventrewardpill', create='image')
node(P, 'EventRewardText', *at(EV, 92, 268), 222, 48, create='text', fs=20, mn=12, color=GOLD, nowrap=True)
node(P, 'EventCostPill', *at(EV, 350, 268), 230, 48, sprite='eventcostpill', sliced=True)
node(P, 'EventCostText', *at(EV, 362, 268), 206, 48, fs=20, mn=12, color=RED, nowrap=True)
node(P, 'EventAcceptButton', *at(EV, 100, 318), 460, 98, sprite='eventacceptbutton')
node(P, 'EventAcceptText', 0, 0, 460, 98, parent='EventAcceptButton', fs=32, mn=20, color=WHITE, nowrap=True)
node(P, 'EventDeclineButton', *at(EV, 107, 411), 446, 66, sprite='eventdeclinebutton')
node(P, 'EventDeclineText', 0, 0, 446, 66, parent='EventDeclineButton', fs=22, mn=14, color=WHITE, nowrap=True)
node(P, 'EventResultText', *at(EV, 100, 318), 460, 98, fs=24, mn=16, color=WHITE)

# ── 상점 ──────────────────────────────────────────────────
P = 'ShopPanel'
node(P, 'ShopBox', *SP, 690, 545, sprite='shopframe')
node(P, 'ShopTitleText', *at(SP, 207, 65), 276, 51, fs=34, mn=22, color=GOLD, nowrap=True)
node(P, 'ShopGoldPill', *at(SP, 217, 116), 256, 34, sprite='shopgoldpill', create='image', before='ShopGoldText')
node(P, 'ShopGoldText', *at(SP, 224, 116), 242, 34, fs=20, mn=13, color=WHITE, nowrap=True)
node(P, 'ShopLimitText', *at(SP, 70, 150), 550, 28, fs=18, mn=12, color=DIM, nowrap=True)
for i in range(6):
    c = 'ShopItem%d' % i
    fx = (75, 356)[i % 2]
    fy = (179, 277, 374)[i // 2]
    node(P, c, *at(SP, fx, fy), 260, 83, sprite='shopitemslot', sliced=True)
    node(P, c + 'Icon', 9, 10, 62, 62, parent=c)
    node(P, c + 'Name', 78, 6, 112, 28, parent=c, fs=21, mn=13, color=WHITE, align='left', nowrap=True)
    node(P, c + 'Price', 190, 6, 62, 28, parent=c, fs=19, mn=12, color=GOLD, nowrap=True)
    node(P, c + 'Desc', 78, 36, 176, 42, parent=c, fs=16, mn=11, color=DIM, align='left')
node(P, 'ShopLeaveButton', *at(SP, 207, 460), 276, 64, sprite='shopleavebutton')
node(P, 'ShopLeaveText', 0, 0, 276, 64, parent='ShopLeaveButton', fs=26, mn=16, color=GOLD, nowrap=True)
node(P, 'ShopResultText', *at(SP, 70, 530), 550, 26, fs=18, mn=14, color=WHITE)

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
    'ShopTitleText': 'ショップ', 'ShopGoldText': '所持ゴールド 0', 'ShopLimitText': '残り購入2回・カード1枚',
    'ShopLeaveText': '店を出る',
}
SHOP_SAMPLE = [('火炎の刻印', '攻撃に燃焼を付与する。', '42 G', 'card_c009'), ('窮地', '体力が半分以下になると強くなる。', '42 G', 'card_c010'),
               ('守護の盾', '盾が周囲を回って攻撃を防ぐ。', '42 G', 'card_c003'), ('体 — ギャングスター', 'その場でこの体を奪う', '66 G', None),
               ('停止', '次の部屋で敵が5秒止まる', '14 G', 'shop_freeze'), ('治療', 'ホスト35%・ゴースト25%回復', '42 G', 'shop_heal')]
for i, (n, d, p, _) in enumerate(SHOP_SAMPLE):
    SAMPLE['ShopItem%dName' % i] = n
    SAMPLE['ShopItem%dDesc' % i] = d
    SAMPLE['ShopItem%dPrice' % i] = p


def preview():
    from PIL import Image, ImageDraw, ImageFont
    font_path = os.path.join(ROOT, 'Assets', 'BaseResource', 'Fonts', 'NotoSansJP-Bold.ttf')
    bg_path = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'skill_qx_check', 'robot.png')
    icons = os.path.join(OUT, 'icons')
    sprite_of = {'cardpanel': ['cardpanel_rare', 'cardpanel_rare', 'cardpanel_common']}
    for panel in ('BuffChoicePanel', 'ShrinePanel', 'EventPanel', 'ShopPanel'):
        img = Image.open(bg_path).convert('RGBA').resize((720, 1280))
        img.alpha_composite(Image.new('RGBA', img.size, (0, 0, 0, 175)))
        d = ImageDraw.Draw(img)
        absxy = {}
        for n in [n for n in NODES if n['panel'] == panel]:
            px, py = (0, 0) if n['parent'] is None else absxy[n['parent']]
            x, y = px + n['x'], py + n['y']
            absxy[n['name']] = (x, y)
            sp = n['sprite']
            if n['name'].startswith('BuffCard') and n['parent'] is None:
                sp = sprite_of['cardpanel'][int(n['name'][-1])]
            if n['name'].endswith('Chip') and n['parent']:
                sp = 'cardchip_rarity'
            if sp:
                art = Image.open(os.path.join(OUT, sp + '.png')).convert('RGBA').resize((n['w'], n['h']), Image.LANCZOS)
                img.alpha_composite(art, (x, y))
            if n['name'].endswith('Icon'):
                key = None
                if panel == 'BuffChoicePanel':
                    key = ['card_c002', 'card_c003', 'card_c006'][int(n['name'][8])]
                elif panel == 'ShopPanel':
                    key = SHOP_SAMPLE[int(n['name'][8])][3]
                if key:
                    ic = Image.open(os.path.join(icons, key + '.png')).convert('RGBA').resize((n['w'], n['h']), Image.LANCZOS)
                    img.alpha_composite(ic, (x, y))
            if panel == 'ShrinePanel' and n['name'] in ('ShrineChoice0', 'ShrineChoice1', 'ShrineChoice2'):
                key = ['shrine_speed', 'shrine_range', 'shrine_max_hp'][int(n['name'][-1])]
                ic = Image.open(os.path.join(icons, key + '.png')).convert('RGBA').resize((56, 56), Image.LANCZOS)
                img.alpha_composite(ic, (x + 10, y + 11))
            text = SAMPLE.get(n['name'])
            if text and n['fs']:
                size = n['fs']
                while size > n['mn']:
                    f = ImageFont.truetype(font_path, size)
                    lines = text.split('\n')
                    wmax = max(d.textlength(l, font=f) for l in lines)
                    if wmax <= n['w'] - 4 and len(lines) * size * 1.25 <= n['h'] + 2:
                        break
                    size -= 1
                f = ImageFont.truetype(font_path, size)
                lines = text.split('\n')
                # 넘치면 줄바꿈(최소 크기에서)
                if max(d.textlength(l, font=f) for l in lines) > n['w'] - 4:
                    wrapped = []
                    for l in lines:
                        cur = ''
                        for ch in l:
                            if d.textlength(cur + ch, font=f) > n['w'] - 4:
                                wrapped.append(cur)
                                cur = ch
                            else:
                                cur += ch
                        wrapped.append(cur)
                    lines = wrapped
                lh = size * 1.25
                ty = y + (n['h'] - lh * len(lines)) / 2
                for l in lines:
                    tw = d.textlength(l, font=f)
                    tx = x + 2 if n['align'] == 'left' else x + (n['w'] - tw) / 2
                    d.text((tx, ty), l, font=f, fill=n['color'] or WHITE)
                    ty += lh
        img.convert('RGB').save(os.path.join(REF, 'popup_c_preview_%s.png' % panel))


if __name__ == '__main__':
    with open(os.path.join(HERE, 'popup_c_layout.json'), 'w', encoding='utf-8') as fp:
        json.dump({'nodes': NODES}, fp, ensure_ascii=False, indent=1)
    preview()
    print(len(NODES), 'nodes')
