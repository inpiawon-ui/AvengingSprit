$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
도구가 크게 그려 오면 칸 격자대로 지정 크기로 **축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지). 투명 바탕이 안 나오면 순수 마젠타(#FF00FF) 바탕도 받는다 — 지우지 말고 그대로 저장.
먼저 Projects/AVSR/_exchange/codex_brief_ui_reborn.md 를 읽는다(방향 · 9-slice 제약).
게임 「Avenging Spirit RE:BORN」 UI 리뉴얼 — 판 · 버튼 · 카드 리소스. 승인 디자인: Projects/AVSR/_handoff/reborn_ui_1023/04_검수기록/style_tile.png
기준 그림(배치 · 색 · 비율은 이대로, 재질 · 마감은 손으로 만든 출시 품질로): Projects/AVSR/_handoff/reborn_ui_1023/03_리소스_및_시안/REBORN_UI_FINAL/Screens/settings.png · Projects/AVSR/_handoff/reborn_ui_1023/03_리소스_및_시안/REBORN_UI_FINAL/Screens/shop.png · Projects/AVSR/_handoff/reborn_ui_1023/03_리소스_및_시안/REBORN_UI_FINAL/Screens/fragments-exchange.png
[만들 것] ① 구간 선택 바탕 320x120(일일 · 주간 같은 탭) ② 구간 선택 켜진 칸 320x120 ③ 토글 꺼짐 140x76 ④ 토글 켜짐 140x76(시안) ⑤ 토글 손잡이 64x64 ⑥ 수량 − 버튼 144x144 ⑦ 수량 + 버튼 144x144(기호는 그리지 않는다) ⑧ 칸은 비운다
[시트] 칸 320x144, 8칸을 가로 4칸 x 2줄(왼쪽 위부터) — 전체 1280x288, **투명 바탕**. 칸마다 부품 하나, 칸 가운데.
⚠ **각 부품은 적힌 크기 그대로**(예: 320x160 이면 가로 320 · 세로 160) 칸을 채운다 — 칸 여백은 사방 8 px 정도. 납작하거나 작게 그리지 말 것
(지난 샘플은 320x160 버튼을 290x60 으로 그려 반려).
[저장] Projects/AVSR/_exchange/in/rb_controls.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
