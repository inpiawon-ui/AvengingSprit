"""RE:BORN 아웃게임 연출 부품 2차(2026-10-24) — 코덱스 자유 시안(mock_rbfx_*_free_*)이 목표가 된 뒤 다시 받는 것만.

10-23 부품(order_ui_reborn_fx.py) 중 그대로 쓰는 것: 6성 링 · 별 터짐 · 칸 열림 빛 · 금화 · 상자 빛 · 버튼 빛 띠 · 강화 빛 띠.
다시 받는 것:
| 부품 | 왜 |
|---|---|
| rbfx2_awaken10 | 10-23 납품은 바깥에 갈색 얼룩 판 · 가운데 빨강/초록 테가 묻었다(검은 바탕 아님) |
| rbfx2_chest_{silver,gold,platinum} | 상자 열기 1박자의 닫힌 상자는 키트 아이콘(rb_icon_chest_*) — 열림 그림이 그 모양에서 이어져야 한다 |
| rbfx2_rays | 새 몸 · 각성 · 상자에 같이 쓰는 도는 빛살 한 장(흰 — 등급색은 게임에서 곱한다) |

python order_rbfx_v2.py → _exchange/order_rbfx2_*.ps1 · 실행기 run_ui_reborn.ps1 -Sheets (한 줄로만 돌린다)
"""
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
KIT = 'Assets/BaseResource/ReBornKit/Icons/'
MOCK = 'Projects/AVSR/_exchange/in/'

# 시트: (기준 그림들, 칸 폭, 칸 높이, 열, 줄, 바탕, 무엇을)
ORDERS = {
    'rbfx2_awaken10': ([MOCK + 'mock_rbfx_awaken10_free_peak.png', MOCK + 'rbfx_awaken6.png'], 384, 384, 4, 2, 'black',
                       '**10성 각성 금빛 2겹 링 + 빛살** 8장(번쩍 → 넓어짐 → 문양 원이 돌며 머묾 → 사그라짐). 시안(첫 그림) 몸 뒤 금빛 고리 두 겹 · 마름모 반짝 별 · 문양 원 그대로. '
                       '크기 · 결은 6성 링 시트(둘째 그림)와 **같은 칸 · 같은 지름**(10성이라고 키우지 않는다 — 겹을 하나 더할 뿐). 가운데(몸 자리)는 비운다. '
                       '⚠ 지난 납품은 링 바깥에 갈색 안개 판이 깔리고 가운데 구멍 가장자리에 빨강 · 초록 테가 묻었다 — 빛 바깥은 순수 검정, 다른 색 테 금지'),
    'rbfx2_chest_silver': ([KIT + 'rb_icon_chest_silver_144.png'], 192, 192, 4, 1, 'pink',
                           '**이 은 상자 아이콘이 열리는** 4장(① 닫힘 = 기준 그림 그대로 ② 뚜껑 들썩 ③ 반쯤 ④ 활짝 — 안쪽은 어둡게, 내용물 없음). '
                           '기준 그림의 모양 · 색 · 검은 외곽선 · 도트 크기를 그대로(새로 디자인하지 않는다). 상자 몸통은 4장 모두 같은 자리 · 같은 크기(칸 아래 가운데), 뚜껑만 위로 열린다. 빛 · 반짝이 없이'),
    'rbfx2_chest_gold': ([KIT + 'rb_icon_chest_gold_144.png'], 192, 192, 4, 1, 'pink',
                         '**이 금 상자 아이콘이 열리는** 4장 — 은 상자 발주와 같은 4박자 · 같은 자리. 기준 그림의 모양 · 금 테 · 검은 외곽선 그대로. 빛 · 반짝이 없이'),
    'rbfx2_chest_platinum': ([KIT + 'rb_icon_chest_platinum_144.png'], 192, 192, 4, 1, 'pink',
                             '**이 백금 상자 아이콘이 열리는** 4장 — 은 상자 발주와 같은 4박자 · 같은 자리. 기준 그림의 모양 · 흰 금속 테 · 검은 외곽선 그대로. 빛 · 반짝이 없이'),
    'rbfx2_rays': ([MOCK + 'mock_rbfx_newbody_free_peak.png'], 512, 512, 1, 1, 'black',
                   '몸 뒤에서 **사방으로 뻗는 빛살** 1장 — 시안(새 몸 순간) 몸 뒤 별 빛살의 모양 그대로, 단 **흰색**(게임에서 시안 · 금 · 등급색을 곱한다). '
                   '굵은 살 4개 + 가는 살 사이사이, 끝이 가늘어지며 사그라짐, 도트 결 · 계단 모서리. 가운데 작은 흰 심. 돌려 쓰므로 둥글게 고르게'),
}

BG = {
    'black': '**검은 바탕(#000000)**(게임에서 더하기로 그린다) · 빛 가장자리는 검정으로 바로 사그라든다(바깥 어두운 판 금지)',
    'pink': '**순수 마젠타(#FF00FF) 바탕**(게임에서 뚫는다) — 도구가 투명 바탕으로 그려 오면 그대로 저장(지우지 말 것) · 그림에 마젠타 · 분홍 금지 · 테두리 또렷하게',
}

TEMPLATE = '''$env:OPENAI_API_KEY = $null
Set-Location "C:\\won\\UnityProject\\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지). 칸 비율이 안 맞게 그려 와도 지우지 말고 그대로 저장한다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(특히 0-2 RE:BORN 화면 · 3-1 등급 계단).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 아웃게임 연출 **부품**. 기준 그림(이 모양 · 색 · 화풍 그대로): {anchor}
공통: {bg} · 도트 결이 살아 있게 · 글자 · 숫자 · 캐릭터 · 창은 그리지 않는다(부품만) · 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
[만들 것] {what}
[시트] 한 장 {w}x{h}, {n}장을 가로 {cols}칸 x {rows}줄(왼쪽 위부터 시간 순) — 전체 {sw}x{sh}.
[저장] Projects/AVSR/_exchange/in/{sheet}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''


def main():
    for sheet, (anchors, w, h, cols, rows, bg, what) in ORDERS.items():
        text = TEMPLATE.format(anchor=' · '.join(anchors), bg=BG[bg], what=what, w=w, h=h, n=cols * rows,
                               cols=cols, rows=rows, sw=w * cols, sh=h * rows, sheet=sheet)
        with open(os.path.join(EX, 'order_%s.ps1' % sheet), 'w', encoding='utf-8-sig') as f:
            f.write(text)
    print(len(ORDERS), '건:', ','.join(ORDERS))


if __name__ == '__main__':
    main()
