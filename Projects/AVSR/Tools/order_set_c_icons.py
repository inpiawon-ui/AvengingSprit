"""C 「원혼 회로」 아이콘 발주 — 카드 그림 10 · 제단 선물 6 · 상점 캡슐 4 (2026-10-08).

PD: 「시안의 스킬 아이콘 · 상점 아이템도 괜찮다」. 시안 C 의 카드 그림(유령 + 원혼 빛 효과) · 상점 물건(유리 캡슐 안 상징) 그림체로.
시트 1024 x 1024 마젠타, 칸 512 x 512 가 2 x 2. 자르기는 fit_set_c_icons.py.
지금 게임에 쓰는 카드는 c001~c010(CardImporter.Cards) — 나머지 card_c011~ 은 옛 그림이라 손대지 않는다.
제단 선물은 지금 옛 카드 그림(buffcard_c017 등)을 빌려 뜻이 안 맞아 제 아이콘을 새로 준다(shrine_{key}).
python order_set_c_icons.py [시트 ...]
"""
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

CARD_STYLE = ('시안 levelup 카드 그림처럼 — 어두운 바탕 없이(마젠타), 가운데 작은 유령(흰 몸 · 노란/청록 눈빛)이 효과의 주인공이 되고 '
              '효과(전기 · 방패 · 불꽃 등)가 원혼 빛으로 둘러싼다. 굵은 외곽선 도트, 160 px 로 줄여도 읽히게 큰 덩어리.')
SHRINE_STYLE = ('시안 altar 선택 칸의 아이콘처럼 — 청록 원혼 빛 선으로 그린 상징 하나(단색 청록 + 흰 하이라이트). '
                '64 px 로 줄여도 읽히게 굵고 단순하게.')
SHOP_STYLE = ('시안 shop 상품 칸의 아이콘처럼 — 흑철 뚜껑 · 받침이 달린 세로 유리 캡슐 안에 상징이 빛난다. 64 px 로 줄여도 읽히게.')

SHEETS = {
    'cards1': (CARD_STYLE, [('card_c001', '감전 — 유령이 내뿜은 번개가 옆으로 튄다(청백 전기)'),
                            ('card_c002', '성장 가속 — 위로 솟는 금빛 결정 위의 유령(시안 그대로)'),
                            ('card_c003', '수호 방패 — 유령 둘레를 도는 청색 방패 셋(시안 그대로)'),
                            ('card_c004', '처형 — 유령이 든 큰 붉은 낫, 아래 금 간 해골')]),
    'cards2': (CARD_STYLE, [('card_c005', '번개 사슬 — 여러 갈래로 이어지는 번개 고리 속 유령'),
                            ('card_c006', '흡혼 — 쓰러진 기계 잔해에서 피어오르는 초록 유령 기운(시안 그대로)'),
                            ('card_c007', '찰나의 불사 — 금빛 보호막 껍질에 감싸인 유령, 모래시계 문양'),
                            ('card_c008', '추가 발사 — 유령 앞에서 갈라져 나가는 탄 세 줄기(호박빛)')]),
    'cards3': (CARD_STYLE, [('card_c009', '화염 각인 — 불꽃 문장을 새긴 유령(주황 불)'),
                            ('card_c010', '궁지 — 금 간 하트를 움켜쥔 붉게 달아오른 유령')]),
    'shrine1': (SHRINE_STYLE, [('shrine_full_heal', '몸을 아문다 — 십자 없는 둥근 붕대 · 하트'),
                               ('shrine_soul_heal', '영혼을 채운다 — 물방울 안의 작은 유령'),
                               ('shrine_max_hp', '그릇을 넓힌다 — 근육 펴는 몸 실루엣(시안 그대로)'),
                               ('shrine_atk', '힘을 받는다 — 위로 솟는 주먹')]),
    'shrine2': (SHRINE_STYLE, [('shrine_speed', '손이 빨라진다 — 빠르게 뻗는 손 · 속도선(시안 그대로)'),
                               ('shrine_range', '멀리 닿는다 — 길게 날아가는 탄 · 궤적(시안 그대로)')]),
    'shop1': (SHOP_STYLE, [('shop_bomb', '폭탄 — 캡슐 안 둥근 폭탄 · 불꽃 심지(주황)'),
                           ('shop_freeze', '정지 — 캡슐 안 보라 모래시계(시안 그대로)'),
                           ('shop_ally', '동료 — 캡슐 안 초록 유령 포대'),
                           ('shop_heal', '치료 — 캡슐 안 붉은 바탕 흰 회복 표식(시안 그대로)')]),
}

TEMPLATE = r'''$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 아이콘 시트 — PD 가 통과시킨 시안 C 「원혼 회로」(Projects/AVSR/_exchange/in/mock_set_C_levelup.png ·
mock_set_C_altar.png · mock_set_C_shop.png)의 아이콘과 **같은 그림체 · 같은 도트 밀도**로. 상점 아이콘 앵커: Projects/AVSR/_exchange/in/anchor_c/shop_icons.png
[그림체] {style}
시트: 1024 x 1024, 순수 마젠타(#FF00FF) 바탕, 칸 512 x 512 가 2열 x 2줄(왼쪽 위부터 차례로). 그림은 칸 가운데, 칸의 80% 정도, 칸 밖으로 넘치지 않게.
{cells}
- 글자 · 숫자 금지. 마젠타/분홍 색을 그림에 쓰지 않는다. 별 · 표창 · 십자 반짝이 금지. 빈 칸은 마젠타.
[저장] Projects/AVSR/_exchange/in/setc_icons_{sheet}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

RUN = r'''Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @(KEYS)
$ps = foreach ($k in $keys) {
    Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_setc_icons_$k.ps1' *> 'Projects/AVSR/_exchange/log_setc_icons_$k.txt'" -WindowStyle Hidden -PassThru
    Start-Sleep 3
}
$ps | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/setc_icons_$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
'''


def main(only=None):
    keys = []
    for sheet, (style, items) in SHEETS.items():
        if only and sheet not in only:
            continue
        cells = '\n'.join('- 칸 %d (%s): %s' % (i + 1, name, desc) for i, (name, desc) in enumerate(items))
        with open(os.path.join(EX, 'order_setc_icons_%s.ps1' % sheet), 'w', encoding='utf-8-sig') as f:
            f.write(TEMPLATE.format(style=style, cells=cells, sheet=sheet))
        keys.append(sheet)
    with open(os.path.join(EX, 'run_setc_icons.ps1'), 'w', encoding='utf-8-sig') as f:
        f.write(RUN.replace('KEYS', ','.join("'%s'" % k for k in keys)))
    print(len(keys), 'sheets')


if __name__ == '__main__':
    import sys
    main(sys.argv[1:] or None)
