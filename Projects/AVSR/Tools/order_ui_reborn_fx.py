"""RE:BORN UI 리뉴얼(2026-10-23) — 아웃게임 연출 부품 발주(시안 11장 → 부품).

PD 2026-10-23 「시안은 다 괜찮은 것 같아 · 적용한 걸 보고 결정」. 규칙 08_fx: 빛 · 반짝임 · 알갱이 = 파티클(기존
Assets/BaseResource/ParticleFx 입자 그림), **모양이 있는 것만** 그림 프레임으로 받는다. 빛 모양 = 검은 바탕(가산) · 물건 = 분홍 바탕.
시안에서 그 부분을 잘라 기준 그림으로 박는다(_exchange/ref/ui_reborn_1023/fx_anchor/).

| 시안 | 그림으로 받는 것 | 파티클 · 코드로 하는 것 |
|---|---|---|
| 소환 빛기둥 | 등급별 빛기둥 3벌(6장 반복) | 위로 흐르는 알갱이 · 반짝 별 · SSR 화면 번쩍 |
| 카드 뒤집기 | 등급별 테두리 빛 3벌 | 뒤집힘(가로 크기) · 튀는 빛 조각 |
| 대박 도장 | 잉크 도장 3장(커짐 → 찍힘 → 먼지) | 화면 흔들림 |
| 상자 열기 | 등급별 상자 열림 4장 × 3 · 터지는 빛 3벌 | 금화 · 보석 튐 · 반짝 별 |
| 코인 날아가기 | 금화 회전 8장 | 날아가는 길(곡선) · 칩 반짝 |
| 별 채움 | 별 터짐 6장 | 반짝 |
| 6성 · 10성 각성 | 시안 링 폭발 8장 · 금빛 2겹 링 + 빛살 8장 | 영혼 알갱이 |
| 강화 펄스 | 초록 빛 띠 1장 | 지나가기(이동) |
| 보상 칸 열림 | 칸 열림 빛 6장(흰 심 — 등급색은 칸 그림이 낸다) | 알갱이 |
| 출전 버튼 빛 | 비스듬한 빛 띠 1장 | 지나가기 · 가끔 반복 |

python order_ui_reborn_fx.py → _exchange/order_rbfx_*.ps1 · 실행기 run_ui_reborn.ps1 재사용(-Sheets 로 이름)
"""
import os

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
ANC = os.path.join(EX, 'ref', 'ui_reborn_1023', 'fx_anchor')
REL = 'Projects/AVSR/_exchange/ref/ui_reborn_1023/fx_anchor/'

# 시안에서 자를 자리(720 x 1560 시안 기준)
CROPS = {
    'summon_beam': ('rb_mock_summon_beam', (0, 250, 720, 1150)),
    'card_flip': ('rb_mock_card_flip', (120, 380, 620, 860)),
    'stamp': ('rb_mock_stamp_jackpot', (180, 180, 560, 520)),
    'chest': ('rb_mock_chest_open', (40, 330, 690, 700)),
    'coin': ('rb_mock_coin_burst', (0, 80, 720, 900)),
    'star': ('rb_mock_star_fill', (240, 520, 560, 700)),
    'awaken6': ('rb_mock_awaken_6', (0, 250, 720, 900)),
    'awaken10': ('rb_mock_awaken_10', (0, 250, 720, 900)),
    'upgrade': ('rb_mock_upgrade_tick', (20, 760, 700, 920)),
    'reveal': ('rb_mock_reward_reveal', (220, 700, 500, 960)),
    'shine': ('rb_mock_button_shine', (0, 1180, 720, 1340)),
}

# 시트: (기준 그림, 칸 폭, 칸 높이, 열, 줄, 바탕, 무엇을)
ORDERS = {
    'rbfx_beam_r': ('summon_beam', 192, 512, 6, 1, 'black', '**R 등급 파랑 빛기둥**(시안 왼쪽 기둥) — 아래 책에서 위로 솟는 도트 빛줄기 다발. 6장 반복(줄기가 위로 흐르듯 일렁이게, 모양 · 폭은 같게)'),
    'rbfx_beam_sr': ('summon_beam', 192, 512, 6, 1, 'black', '**SR 등급 보라 빛기둥**(시안 가운데 기둥) — R 과 같은 모양 · 폭, 빛줄기 한 겹 더. 6장 반복'),
    'rbfx_beam_ssr': ('summon_beam', 192, 512, 6, 1, 'black', '**SSR 등급 금빛 기둥**(시안 오른쪽 기둥) — 같은 폭, 줄기 두 겹 더 + 가장자리 빛살. 6장 반복'),
    'rbfx_card_rim': ('card_flip', 256, 320, 3, 1, 'black', '소환 결과 카드(200x280 틀)를 **두르는 테두리 빛**만 — ① R 파랑 ② SR 보라 ③ SSR 금. 카드 자리는 비우고 테두리를 따라 도는 빛 · 모서리 반짝'),
    'rbfx_stamp': ('stamp', 320, 320, 3, 1, 'pink', '**붉은 잉크 원형 도장** 3장 — ① 크게 떠 있음(옅게) ② 쾅 찍힘(가장 진하게, 가장자리 거친 잉크 · 튄 방울) ③ 잉크 먼지가 흩어짐. 도장 안 글자 자리는 비운다'),
    'rbfx_chest_silver': ('chest', 192, 192, 4, 1, 'pink', '**은 상자가 열리는** 4장(닫힘 → 뚜껑 들썩 → 반쯤 → 활짝) — 도트 상자, 빛 · 반짝이 없이(빛은 따로). 상자 모양 · 크기 · 자리는 4장 같게'),
    'rbfx_chest_gold': ('chest', 192, 192, 4, 1, 'pink', '**금 상자가 열리는** 4장 — 은 상자와 같은 모양 · 같은 크기, 금 장식. 빛 · 반짝이 없이'),
    'rbfx_chest_platinum': ('chest', 192, 192, 4, 1, 'pink', '**백금 상자가 열리는** 4장 — 같은 모양 · 같은 크기, 흰 금속 + 결정 장식. 빛 · 반짝이 없이'),
    'rbfx_chest_burst': ('chest', 320, 320, 3, 1, 'black', '상자가 열릴 때 **안에서 솟는 빛** — ① 은(희고 차가운 빛) ② 금(금빛) ③ 백금(보라 · 흰 빛 + 결정 빛살). 위로 퍼지는 빛 무리, 상자는 그리지 않는다'),
    'rbfx_coin': ('coin', 64, 64, 8, 1, 'pink', '**금화가 도는** 8장(앞면 → 옆 → 뒷면 → 옆) — 도트 금화 하나, 크기 · 자리 같게'),
    'rbfx_star_burst': ('star', 192, 192, 6, 1, 'black', '빈 별이 금빛으로 **채워지는 순간의 작은 터짐** 6장 — 가운데 섬광 → 짧은 빛살 → 사그라짐. 별 모양은 그리지 않는다(별은 아이콘)'),
    'rbfx_awaken6': ('awaken6', 384, 384, 4, 2, 'black', '**6성 각성 시안 링 폭발** 8장 — 캐릭터 둘레(가운데 비움)에서 시안 빛 고리가 번쩍 → 넓어지며 깨진 빛 조각 · 영혼 불꽃 → 사라짐'),
    'rbfx_awaken10': ('awaken10', 384, 384, 4, 2, 'black', '**10성 각성 금빛 2겹 링 + 빛살** 8장 — 6성보다 크고 화려하게: 금빛 고리 두 겹 + 사방 빛살 + 금빛 조각. 가운데 비움'),
    'rbfx_upgrade_band': ('upgrade', 512, 128, 1, 1, 'black', '능력치 칸 위를 **지나가는 초록 빛 띠** 1장 — 세로로 선 부드러운 초록 빛 기둥(가운데 밝고 양옆 사그라짐), 도트 결'),
    'rbfx_reveal': ('reveal', 192, 192, 6, 1, 'black', '보상 칸이 **열리는 빛** 6장 — 칸 가운데에서 흰 빛이 확 터졌다 사그라짐(흰 심 — 등급색은 칸 그림이 낸다)'),
    'rbfx_shine': ('shine', 256, 128, 1, 1, 'black', '주요 버튼 위를 **비스듬히 지나가는 빛 띠** 1장 — 45도 기울어진 가는 흰 빛 줄 두 개(굵은 것 + 가는 것), 끝이 부드럽게 사그라짐'),
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
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 아웃게임 연출 **부품**. PD 가 통과시킨 시안에서 이 부분만 잘라 낸 기준 그림(이 모양 · 색 · 화풍 그대로): {anchor}
공통: {bg} · 도트 결이 살아 있게 · 글자 · 숫자 · 캐릭터 · 창은 그리지 않는다(부품만) · 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
[만들 것] {what}
[시트] 한 장 {w}x{h}, {n}장을 가로 {cols}칸 x {rows}줄(왼쪽 위부터 시간 순) — 전체 {sw}x{sh}.
[저장] Projects/AVSR/_exchange/in/{sheet}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''


def main():
    os.makedirs(ANC, exist_ok=True)
    for name, (mock, box) in CROPS.items():
        Image.open(os.path.join(EX, 'in', mock + '.png')).convert('RGB').crop(box).save(os.path.join(ANC, name + '.png'))
    for sheet, (anc, w, h, cols, rows, bg, what) in ORDERS.items():
        text = TEMPLATE.format(anchor=REL + anc + '.png', bg=BG[bg], what=what, w=w, h=h, n=cols * rows,
                               cols=cols, rows=rows, sw=w * cols, sh=h * rows, sheet=sheet)
        with open(os.path.join(EX, 'order_%s.ps1' % sheet), 'w', encoding='utf-8-sig') as f:
            f.write(text)
    print(len(ORDERS), 'orders:', ','.join(ORDERS))


if __name__ == '__main__':
    main()
