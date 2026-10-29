$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
도구가 크게 그려 오면 칸 격자대로 지정 크기로 **축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지). 투명 바탕이 안 나오면 순수 마젠타(#FF00FF) 바탕도 받는다 — 지우지 말고 그대로 저장.
먼저 Projects/AVSR/_exchange/codex_brief_ui_reborn.md 를 읽는다(방향 · 9-slice 제약).
게임 「Avenging Spirit RE:BORN」 UI 리뉴얼 — 판 · 버튼 · 카드 리소스. 승인 디자인: Projects/AVSR/_handoff/reborn_ui_1023/04_검수기록/style_tile.png
기준 그림(배치 · 색 · 비율은 이대로, 재질 · 마감은 손으로 만든 출시 품질로): Projects/AVSR/_handoff/reborn_ui_1023/03_리소스_및_시안/REBORN_UI_FINAL/Screens/growth.png · Projects/AVSR/_handoff/reborn_ui_1023/03_리소스_및_시안/REBORN_UI_FINAL/Screens/pass.png
[만들 것] **진행 바** 160x24(가로로 늘려 쓴다) — 위부터 ① 빈 바(어두운 홈, 양끝 꺾쇠 + 10% 눈금) ② 시안 채움 ③ 초록 채움 ④ 노랑 채움 ⑤ 빨강 채움(채움 위 광택선)
[시트] 칸 320x48, 5칸을 가로 1칸 x 5줄(왼쪽 위부터) — 전체 320x240, **투명 바탕**. 칸마다 부품 하나, 칸 가운데.
⚠ **각 부품은 적힌 크기 그대로**(예: 320x160 이면 가로 320 · 세로 160) 칸을 채운다 — 칸 여백은 사방 8 px 정도. 납작하거나 작게 그리지 말 것
(지난 샘플은 320x160 버튼을 290x60 으로 그려 반려).
[저장] Projects/AVSR/_exchange/in/rb_bars.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
