"""스킬 손질(2026-10-21) — 드라군 메테오 · 백마법사 중력진 부품 발주문.

PD 2026-10-21 「둘 다 그 시안대로 발주해서 적용해줘」 — 시안 in/mock_dragoon_meteor.png · in/mock_wizard_gravity.png.
시안에서 잘라 낸 기준 그림(ref/rework1021/anchor_*.png)을 앵커로 박는다.
자르기는 Tools/cut_skill_rework_parts.py 의 표(PARTS)에 같은 이름으로 넣어 둔다.
"""
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

# 시트 이름: (스킬, 시안, 기준 그림, 칸 폭, 칸 높이, 열, 행, 바탕, 무엇을)
ORDERS = {
    'part_mt_rock': ('드라군 필살기 「메테오」', 'mock_dragoon_meteor.png', 'anchor_mt_rock.png', 256, 256, 6, 1, 'pink',
                     '하늘에서 비스듬히(오른쪽 위 → 왼쪽 아래) 떨어지는 **불타는 운석** — 검게 갈라진 바위 덩어리(틈이 주황으로 빛남) + '
                     '뒤로 끌리는 불꽃 꼬리 · 검은 연기. 6장 반복(불꽃 꼬리만 일렁이고 바위 모양 · 자리 · 크기는 같게). '
                     '바위는 칸 왼쪽 아래(지름 약 90 px), 꼬리는 오른쪽 위 모서리까지'),
    'part_mt_blast': ('드라군 필살기 「메테오」', 'mock_dragoon_meteor.png', 'anchor_mt_blast.png', 320, 320, 4, 2, 'black',
                      '운석 착탄 **대폭발의 빛**만 — 가운데 흰 · 노랑 심이 번쩍 → 주황 불덩이가 부풀며 터짐 → 바닥을 따라 퍼지는 '
                      '주황 충격파 고리(탑뷰 타원) · 사방으로 튀는 불티 → 사그라짐. 8장(1 번쩍 · 2~3 최대 · 4~8 고리가 넓어지며 사라짐). '
                      '바위 파편은 그리지 않는다(따로 받는다)'),
    'part_mt_debris': ('드라군 필살기 「메테오」', 'mock_dragoon_meteor.png', 'anchor_mt_blast.png', 320, 320, 4, 2, 'pink',
                       '착탄 때 사방으로 튀는 **검은 바위 파편**만(틈이 주황으로 살짝 빛나는 검은 돌 조각 12~16개, 크기 8~28 px). '
                       '8장 — 1 가운데 뭉침 → 2~5 바깥으로 흩어지며 날아감(위쪽은 조금 높이 솟음) → 6~8 바닥에 떨어져 작아지며 사라짐. '
                       '불 · 빛 · 연기는 그리지 않는다(빛은 따로 받는다)'),
    'part_mt_lava': ('드라군 필살기 「메테오」', 'mock_dragoon_meteor.png', 'anchor_mt_lava.png', 384, 256, 3, 2, 'pink',
                     '착탄 자리에 4초 남는 **용암 지대** 바닥 그림(탑뷰 타원, 가로 약 340 · 세로 약 200 px) — 검게 식은 바위 판들 사이 '
                     '틈이 주황 · 노랑으로 빛나고 작은 불꽃 · 끓는 거품. 6장 반복(바위 판 모양 · 자리는 같게, 틈의 빛 · 거품 · 불꽃만 '
                     '숨쉬듯 바뀌게). 피 웅덩이 같은 검붉은 액체 금지'),
    'part_gv_circle': ('백마법사 액티브 「중력진」', 'mock_wizard_gravity.png', 'anchor_gv_circle.png', 384, 320, 3, 2, 'black',
                       '바닥에 깔리는 **황금 룬 마법진**(탑뷰 타원, 가로 약 360 · 세로 약 290 px) — 겹 원 2~3개 · 테두리를 도는 룬 문자 · '
                       '안쪽 문양, 가운데는 비워 둔다. 6장 반복(룬이 조금씩 돌고 빛이 흐르듯 반짝이게, 모양 · 크기는 같게)'),
    'part_gv_swirl': ('백마법사 액티브 「중력진」', 'mock_wizard_gravity.png', 'anchor_gv_swirl.png', 384, 320, 3, 2, 'black',
                      '마법진 위에서 가운데로 빨려 드는 **금빛 소용돌이**(탑뷰, 가로 약 360 · 세로 약 290 px) — 바깥에서 안으로 휘어 드는 '
                      '빛줄기 6~8가닥 + 줄기를 따라 빨려 드는 작은 빛 알갱이, 가운데 밝은 심. 6장 반복(가닥이 안쪽으로 감겨 들며 돌게)'),
    'part_gv_burst': ('백마법사 액티브 「중력진」', 'mock_wizard_gravity.png', 'anchor_gv_burst.png', 384, 384, 4, 2, 'black',
                      '끌어모은 뒤 **마무리 빛 폭발** — 가운데 흰 · 금빛 섬광이 확 터지며 사방으로 빛살 · 바닥을 따라 퍼지는 금빛 '
                      '고리(탑뷰 타원) · 반짝이는 빛 알갱이. 8장(1 모임 · 2 번쩍 최대 · 3~8 빛살 · 고리가 넓어지며 사라짐)'),
}

TEMPLATE = '''$env:OPENAI_API_KEY = $null
Set-Location "C:\\won\\UnityProject\\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 아래에 적은 것만(2장). 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지).
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 탑다운) {skill} 이펙트 **부품**. PD 가 고른 시안: Projects/AVSR/_exchange/in/{mock}
그 시안에서 이 부분만 잘라 낸 기준 그림(이 모양 · 색 · 화풍 그대로): Projects/AVSR/_exchange/ref/rework1021/{anchor}
공통: {bg_text} · 도트 결이 살아 있게 · 캐릭터 · 적 · 바닥 타일 · 글자 · 숫자는 그리지 않는다(부품만) · 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
[만들 것] {what}
[시트] 한 장 {w}x{h}, {n}장을 가로 {cols}칸 x {rows}줄(왼쪽 위부터 시간 순) — 전체 {sw}x{sh}.
[저장] Projects/AVSR/_exchange/in/{sheet}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

BG = {
    'black': '**검은 바탕(#000000)**(게임에서 더하기로 그린다) · 빛 가장자리는 검정으로 바로 사그라든다(바깥 어두운 판 금지)',
    # ⚠ 이미지 도구가 투명 바탕으로 그려 오면 코덱스가 「바탕이 다르다」며 지우고 「못 그렸다」로 끝냈다(파편 · 용암, 2026-10-21)
    'pink': '**순수 마젠타(#FF00FF) 바탕**(게임에서 뚫는다) — 도구가 **투명 바탕**으로 그려 오면 그것도 그대로 저장한다(지우지 말 것) · '
            '그림에 마젠타 · 분홍 색 금지 · 테두리를 바탕과 섞지 말고 또렷하게',
}

if __name__ == '__main__':
    for sheet, (skill, mock, anchor, w, h, cols, rows, bg, what) in ORDERS.items():
        text = TEMPLATE.format(skill=skill, mock=mock, anchor=anchor, bg_text=BG[bg], what=what,
                               w=w, h=h, n=cols * rows, cols=cols, rows=rows, sw=w * cols, sh=h * rows, sheet=sheet)
        with open(os.path.join(EX, 'order_%s.ps1' % sheet), 'w', encoding='utf-8-sig') as f:
            f.write(text)
        print(sheet)
