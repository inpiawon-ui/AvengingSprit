# -*- coding: utf-8 -*-
"""2단계 새 몬스터 · 원소판의 **방향 판**(3 x 3 · 마젠타) 발주문을 만든다 — 받으면 `trash_cut.py` 로 자른다.

  새 모양 6종 : 라인업 시안 `in/mock_lineup_v3.png` 에서 그 몬스터만 잘라 생김새 앵커로 쓴다.
  원소판 6종  : 지금 게임 그림 8장을 3 x 3 판으로 붙여(3배) 「같은 자세 · 같은 칸, 원소 색만」 앵커로 쓴다.
                원소 생김새는 `in/mock_elements_v1.png` 가 정답이다.

순서 : 동쪽(e) 판을 먼저 받아 검수 → 통과하면 나머지 네 방향은 그 동쪽 판을 앵커로(`--dir se,ne,s,n`).

쓰는 법 : python order_monsters_step2_sheets.py [--dir e] [--only watcher_eye,gorilla_fire]
실행    : powershell -File Tools/wa-game-harness-kit/files/Tools/harness/run_codex_orders.ps1 -Sheets <이름들> -Exchange Projects/AVSR/_exchange
"""
import argparse
import os

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
REF = os.path.join(EX, 'ref', 'trash_v3')
P = 'C:/won/UnityProject/AvengingSprit/'

HEAD = ('이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 내장 이미지 생성 기능만 써라. '
        '스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.')

DIRS = {
    'e': '오른쪽(동쪽)을 보는 옆모습',
    'se': '오른쪽 아래(남동쪽, 화면 앞 오른쪽)를 보는 비스듬한 앞모습',
    'ne': '오른쪽 위(북동쪽, 화면 안쪽 오른쪽)를 보는 비스듬한 뒷모습',
    's': '아래(남쪽, 화면 앞쪽)를 보는 앞모습',
    'n': '위(북쪽, 화면 안쪽)를 보는 뒷모습',
}

SHEET_RULE = ('  · 배경은 **반드시 마젠타 #FF00FF 단색** — 몸이 어두워도 검정 · 회색 바탕 금지(자르는 도구가 마젠타만 뚫는다).\n'
              '  · 1 px 짙은 외곽선 · 12~16색 · 2~3단 끊기는 명암. 질감 노이즈 금지. 몸에 빛 · 아우라 · 반짝이를 굽지 않는다.\n'
              '  · 여덟 칸 같은 몸 · 같은 크기 · 같은 색. 칸마다 몸을 칸 가운데 · 바닥에 붙여 그린다.')

# 라인업 시안에서 자를 자리(1672 x 941 기준, 4배 그림) — 몬스터 하나씩
CROPS = {
    'watcher_eye': (30, 140, 270, 430),
    'shadow_cat': (270, 160, 780, 430),
    'saw_monkey': (780, 170, 1165, 420),
    'mimic': (30, 505, 620, 800),
    'lantern': (620, 500, 790, 780),
    'soul_hound': (795, 570, 1170, 800),
}

# 새 모양 — 생김새 · 하는 일 · 칸 차례 · 몸 크기(96 칸 안)
NEW = {
    'watcher_eye': dict(
        name='감시 눈', look='회색 금속 공 몸 · 큰 붉은 렌즈 하나 · 짧은 안테나 둘 · 금색 추진구. 바닥에서 조금 떠 있고 밑에 작은 그림자.',
        job='떠다니다 멈춰 렌즈로 **예고선 → 레이저**를 쏜다(레이저 빛은 게임이 따로 그린다 — 판에는 그리지 않는다).',
        cells='1 대기(떠 있음) · 2 떠다님1(조금 위) · 3 떠다님2(조금 아래) / 4 공격1(렌즈 덮개가 열리고 몸이 뒤로 살짝) · '
              '5 공격2(쏘는 반동 — 몸이 뒤로 밀림, 렌즈 활짝) · 6 피격(찌그러짐 · 렌즈 깜빡) / 7 부서짐1(기울며 연기 없이 금이 감) · '
              '8 부서짐2(바닥에 떨어진 깨진 공) · 9 빈 칸',
        size='몸 지름 46~52 px(떠 있으니 바닥에서 10 px 띄운다)'),
    'shadow_cat': dict(
        name='그림자 살쾡이', look='검보라 그림자 몸의 고양잇과 요괴 · 노란 눈 둘 · 길게 휜 흰 앞발톱 · 꼬리 끝이 연기처럼 흩어짐.',
        job='멀어지면 **연기로 흩어져 사라졌다가** 내 옆에 나타나 앞발로 벤다. 벤 뒤 잠깐 선다.',
        cells='1 대기 · 2 걷기1 · 3 걷기2 / 4 공격1(웅크려 앞발을 치켜듦) · 5 공격2(앞발로 크게 할퀴며 뻗음) · 6 피격 / '
              '7 쓰러짐1 · 8 쓰러짐2(연기로 흩어지며 반쯤 사라짐) · 9 **사라짐**(몸이 연기 덩어리로 흩어지는 순간 — 눈만 남음)',
        size='바닥에서 등까지 44~50 px, 길이 70~80 px(낮고 길게)'),
    'saw_monkey': dict(
        name='톱날 원숭이', look='갈색 원숭이 · 붉은 눈 · 녹슨 갑옷 조끼 · 등에 녹슨 톱날 원반 서너 장.',
        job='거리를 두고 손의 톱날을 던진다 — 톱날은 돌아온다(날아가는 톱날은 게임이 따로 그린다). 던진 뒤엔 빈손.',
        cells='1 대기(손에 톱날 하나) · 2 걷기1 · 3 걷기2 / 4 공격1(톱날을 뒤로 젖혀 감음) · 5 공격2(던지고 팔을 뻗음 — **손이 빔**) · 6 피격 / '
              '7 쓰러짐1 · 8 쓰러짐2 · 9 **빈손 대기**(톱날을 던지고 기다리는 자세 — 손이 빔)',
        size='바닥에서 머리 끝까지 56~62 px'),
    'mimic': dict(
        name='미믹 상자', look=f'방 상자와 **같은 겉모습**의 철 덧댄 상자({P}Assets/BaseResource/InGameMainUI/obj_crate_1.png). 깨면 뚜껑이 입처럼 벌어져 이빨 · 붉은 혀, 밑에 짧은 다리 넷.',
        job='물건인 척 가만히 있다가 가까이 가거나 맞으면 깨어나 깨문다.',
        cells='1 대기(깨어남 — 입을 조금 벌림) · 2 걷기1(다리) · 3 걷기2 / 4 공격1(입을 크게 벌림) · 5 공격2(앞으로 덮쳐 깨묾) · 6 피격 / '
              '7 쓰러짐1(뚜껑이 털썩) · 8 쓰러짐2(부서진 상자) · 9 **잠든 상자**(완전히 닫힌 상자 — 방 상자와 구별되지 않게, 다리 · 이빨 안 보임)',
        size='방 상자와 같은 크기 — 폭 56~62 px'),
    'lantern': dict(
        name='수호 등불', look='낡은 청동 랜턴이 떠다니는 정령 · 안에 푸른 불꽃 · 작은 검은 눈 둘. 위에 고리.',
        job='곁의 적에게 방패를 씌운다(방패 빛은 게임이 따로 그린다). 겁이 많아 뒤로 숨는다.',
        cells='1 대기(떠 있음) · 2 떠다님1(왼쪽으로 살짝 기욺) · 3 떠다님2(오른쪽으로) / 4 공격1(문이 열리며 불꽃이 커짐 — 빛 번짐 없이 도트 불꽃으로) · '
              '5 공격2(불꽃을 내뿜는 자세) · 6 피격(흔들림) / 7 부서짐1(유리 금) · 8 부서짐2(떨어져 꺼진 랜턴) · 9 빈 칸',
        size='고리부터 바닥까지 50~56 px(바닥에서 8 px 띄운다)'),
    'soul_hound': dict(
        name='혼 사냥개', look='마른 검은 개 · 해골 머리 · 드러난 등뼈 · 눈과 꼬리 끝이 푸른 혼불 색(도트 색만, 빛 번짐 금지).',
        job='몸이 있을 땐 어슬렁거리고, 유령이 되면 빨라져 쫓아와 문다.',
        cells='1 대기 · 2 달리기1 · 3 달리기2 / 4 공격1(웅크려 이빨을 드러냄) · 5 공격2(앞으로 뛰어들며 물기) · 6 피격 / '
              '7 쓰러짐1 · 8 쓰러짐2(뼈가 흩어짐) · 9 빈 칸',
        size='바닥에서 등까지 40~46 px, 길이 68~76 px'),
}

# 원소판 — 몸 키 · 원소 그림 앵커 줄 · 바꿀 것
ELEMENTS = {
    'gorilla_fire': dict(body='actor_enforcer', name='불 고릴라', row='1줄 오른쪽(불 고릴라)',
                         how='돌 몸 틈이 용암처럼 주황 · 빨강으로 갈라지고 어깨 · 머리에 작은 도트 불꽃 몇 개.'),
    'roadwarden_fire': dict(body='roadwarden', name='불 순찰기', row='1줄 오른쪽(불 고릴라)의 색 · 불꽃 표현',
                            how='몸 판이 달군 쇠처럼 주황 · 빨강, 경광등 자리가 불꽃, 이음매에 용암색 틈.'),
    'scrapgunner_ice': dict(body='scrapgunner', name='얼음 전갈', row='2줄 오른쪽(얼음 전갈)',
                            how='고철 껍데기에 서리 · 하늘색, 다리 끝 고드름, 꼬리 포신이 얼음 결정.'),
    'coilwalker_ice': dict(body='coilwalker', name='얼음 코일', row='2줄 오른쪽(얼음 전갈)의 색 · 얼음 표현',
                           how='코일 · 다리에 서리와 고드름, 하늘색 · 흰색, 코일 끝에 얼음 결정.'),
    'skeleton_poison': dict(body='skeleton', name='독 해골', row='3줄 오른쪽(독 해골)',
                            how='뼈가 연두 · 보라로 물들고 갈비뼈 사이에 작은 독 거품.'),
    'bat_poison': dict(body='bat', name='독 박쥐', row='3줄 오른쪽(독 해골)의 색 · 독 표현',
                       how='몸이 보라, 날개막이 연두, 송곳니에 독 방울.'),
}

ACTS = ['', '_walk1', '_walk2', '_atk1', '_atk2', '_hit', '_die1', '_die2']


def crop_concept(key):
    src = os.path.join(EX, 'in', 'mock_lineup_v3.png')
    os.makedirs(REF, exist_ok=True)
    out = os.path.join(REF, f'ref_{key}_concept.png')
    Image.open(src).convert('RGB').crop(CROPS[key]).save(out)
    return out


def compose_game_sheet(body, d):
    """지금 게임 그림 8장을 3 x 3 판으로 — 3배(가장 가까운 이웃), 마젠타 바탕. 원소판의 자세 · 칸 앵커."""
    cell = 96 * 3
    sheet = Image.new('RGB', (cell * 3, cell * 3), (255, 0, 255))
    for i, act in enumerate(ACTS):
        p = os.path.join(ROOT, 'Assets', 'BaseResource', 'Unit', body, f'unit_{body}_{d}{act}.png')
        if not os.path.exists(p):
            continue
        im = Image.open(p).convert('RGBA').resize((cell, cell), Image.NEAREST)
        r, c = divmod(i, 3)
        sheet.paste(im, (c * cell, r * cell), im)
    os.makedirs(REF, exist_ok=True)
    out = os.path.join(REF, f'game_{body}_{d}_x3.png')
    sheet.save(out)
    return out


def anchor_line(key, d):
    """동쪽이 아니면 먼저 통과한 동쪽 판을 앵커로 박는다."""
    if d == 'e':
        return ''
    east = os.path.join(EX, 'in', f'trash_{key}_e.png').replace('\\', '/')
    return (f'동쪽 판(통과) : {east} — **같은 몸 · 같은 색 · 같은 크기 · 같은 그림체**로 방향만 바꾼다. 칸 차례도 같다.\n')


def order_new(key, d):
    m = NEW[key]
    concept = crop_concept(key).replace('\\', '/')
    name = f'trash_{key}_{d}'
    return name, f"""{HEAD}

AVENGING SPIRIT: RE:BORN — 새 잡몹 「{m['name']}」 게임 그림 ({d} 방향 · 9칸)

생김새 정답 : {concept} (라인업 3차 시안 {P}Projects/AVSR/_exchange/in/mock_lineup_v3.png 에서 자른 것) — 생김새 · 색을 바꾸지 않는다.
  {m['look']}
하는 일 : {m['job']}
칸 · 동작 형식 · 그림체는 먼저 통과한 잡몹과 같게 : {P}Projects/AVSR/_exchange/ref/trash_v2/boar_e_raw.png · {P}Projects/AVSR/_exchange/ref/trash_v2/mantis_e_raw.png
{anchor_line(key, d)}
■ 만들 것 : {P}Projects/AVSR/_exchange/in/{name}.png — 정사각(864 x 864 쯤, 96 x 96 칸 3 x 3 을 3배), 배경 **마젠타 #FF00FF 단색**
  · 방향 : {DIRS[d]}. 아홉 칸 모두 같은 방향.
  · 칸 차례 : {m['cells']}
  · 크기 : {m['size']}.
{SHEET_RULE}
끝나면 경로만 알려라.
"""


def order_element(key, d):
    m = ELEMENTS[key]
    # 박쥐 게임 그림은 날개가 칸 밖으로 잘려 있었다(2026-10-22) — 앵커는 잘리기 전 납품 판으로
    raw = os.path.join(EX, 'ref', 'char_up', f"out_{m['body']}_{d}_raw.png")
    game = (raw if m['body'] == 'bat' and os.path.exists(raw) else compose_game_sheet(m['body'], d)).replace('\\', '/')
    name = f'trash_{key}_{d}'
    return name, f"""{HEAD}

AVENGING SPIRIT: RE:BORN — 잡몹 원소판 「{m['name']}」 게임 그림 ({d} 방향 · 8칸)

원래 몸(지금 게임 그림 8장을 3배로 붙인 판) : {game}
  → **칸 · 자세 · 실루엣 · 크기를 그대로 두고 색 · 작은 부속만** 바꾼다. 한 칸이라도 자세가 달라지면 안 된다.
원소 생김새 정답 : {P}Projects/AVSR/_exchange/in/mock_elements_v1.png 의 {m['row']}.
바꿀 것 : {m['how']}
{anchor_line(key, d)}
■ 만들 것 : {P}Projects/AVSR/_exchange/in/{name}.png — 정사각(864 x 864, 원래 판과 같은 칸), 배경 **마젠타 #FF00FF 단색**
  · 칸 차례는 원래 판과 같다 : 1 대기 · 2 걷기1 · 3 걷기2 / 4 공격1 · 5 공격2 · 6 피격 / 7 쓰러짐1 · 8 쓰러짐2 · 9 빈 칸
{SHEET_RULE}
끝나면 경로만 알려라.
"""


EXTRA = {
    # 톱날 원숭이가 던지는 톱날 — 게임 칸 48 x 48 네 장(돌아가는 모습). 빛 없이 쇠 톱날만
    'shot_saw': f"""{HEAD}

AVENGING SPIRIT: RE:BORN — 톱날 원숭이가 던지는 **톱날 탄** 그림 (4장 · 돌아가는 모습)

생김새 정답 : {P}Projects/AVSR/_exchange/ref/trash_v3/ref_saw_monkey_concept.png 의 등에 멘 녹슨 톱날 원반과 같은 것.
지금 게임의 탄 크기 · 화풍 참조 : {P}Assets/BaseResource/InGameMainUI/shot_spider_1.png (48 x 48 칸)

■ 만들 것 : {P}Projects/AVSR/_exchange/in/shot_saw.png — 가로로 긴 판(768 x 192 쯤, 48 x 48 칸 네 개를 4배), 배경 **마젠타 #FF00FF 단색**
  · 네 칸 모두 위에서 내려다본 둥근 톱날 하나 — 칸마다 45°씩 더 돌아간 모습(이어 돌리면 회전으로 보이게).
  · 칸의 80% 를 채우게 가운데. 빛 · 잔상 · 속도선을 그리지 않는다(게임이 돌리고 날린다).
  · 1 px 짙은 외곽선 · 녹슨 쇠색 6~8색 · 2단 명암.
끝나면 경로만 알려라.
""",
    # 구르는 아르마딜로 — 말아서 구르는 공 넷 + 휘청(배가 드러남) 둘, 동쪽 하나만(공은 돌리면 방향이 안 보인다)
    'trash_armadillo_roll': f"""{HEAD}

AVENGING SPIRIT: RE:BORN — 잡몹 「갑옷 아르마딜로」 구르기 그림 (6칸)

원래 몸(게임 그림, 동쪽 판) : {P}Projects/AVSR/_exchange/ref/trash_v2/armadillo_e_raw.png — **같은 몸 · 같은 색 · 같은 크기 · 같은 그림체.**
하는 일 : 몸을 말아 점점 빨라지며 굴러온다 → 벽에 박으면 뒤집혀 휘청(배가 드러난다 — 이때는 정면도 맞는다).

■ 만들 것 : {P}Projects/AVSR/_exchange/in/trash_armadillo_roll.png — 정사각(864 x 864 쯤, 96 x 96 칸 3 x 3 을 3배), 배경 **마젠타 #FF00FF 단색**
  · 칸 차례 : 1 말기(반쯤 말린 몸 — 머리를 넣는 중) · 2 공1 · 3 공2 / 4 공3 · 5 공4(공은 칸마다 90°씩 더 돌아간 모습 — 마디 무늬로 회전이 보이게) · 6 휘청1(뒤집혀 배를 보임) /
            7 휘청2(배를 보인 채 발을 버둥) · 8 빈 칸 · 9 빈 칸
  · 공 크기 : 96 칸에서 지름 48~54 px, 바닥에 붙여. 빛 · 속도선 · 먼지를 그리지 않는다.
{SHEET_RULE}
끝나면 경로만 알려라.
""",
}


def write(name, prompt):
    ps = ('$env:OPENAI_API_KEY = $null\n'
          'Set-Location "C:\\won\\UnityProject\\AvengingSprit"\n'
          '$prompt = @"\n' + prompt.replace('"@', '" @') + '\n"@\n'
          'codex exec --sandbox workspace-write --skip-git-repo-check $prompt\n')
    with open(os.path.join(EX, f'order_{name}.ps1'), 'w', encoding='utf-8-sig') as f:
        f.write(ps)
    return name


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--dir', default='e')
    ap.add_argument('--only', default='')
    ap.add_argument('--extra', action='store_true', help='톱날 탄 · 아르마딜로 구르기 발주문만')
    a = ap.parse_args()
    only = set(filter(None, a.only.split(',')))
    names = []
    if a.extra:
        print(','.join(write(k, v) for k, v in EXTRA.items()))
        return
    for d in a.dir.split(','):
        for key in NEW:
            if not only or key in only:
                names.append(write(*order_new(key, d)))
        for key in ELEMENTS:
            if not only or key in only:
                names.append(write(*order_element(key, d)))
    print(','.join(names))


if __name__ == '__main__':
    main()
