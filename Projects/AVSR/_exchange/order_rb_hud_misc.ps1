$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
도구가 크게 그려 오면 칸 격자대로 지정 크기로 **축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지). 투명 바탕이 안 나오면 순수 마젠타(#FF00FF) 바탕도 받는다 — 지우지 말고 그대로 저장.
먼저 Projects/AVSR/_exchange/codex_brief_ui_reborn.md 를 읽는다(방향 · 9-slice 제약).
게임 「Avenging Spirit RE:BORN」 UI 리뉴얼 — 전투 HUD 리소스. 승인 디자인: Projects/AVSR/_handoff/reborn_ui_1023/04_검수기록/style_tile.png
기준 그림(배치 · 색 · 비율은 이대로, 재질 · 마감은 손으로 만든 출시 품질로): Projects/AVSR/_exchange/ref/ui_reborn_1023/anchor/hud_joystick.png · Projects/AVSR/_exchange/ref/ui_reborn_1023/anchor/hud_buttons.png
[만들 것] ① 조이스틱 바탕 280x280(반투명 링 + 옅은 네온 테두리) ② 조이스틱 손잡이 112x112 ③ 일시정지 버튼 120x120(기호 없음) ④ 일시정지 누름 ⑤ READY 칩 바탕 160x56(시안 테두리 알약, 글자 없음) ⑥ 칸은 비운다
[시트] 칸 280x280, 6칸을 가로 3칸 x 2줄(왼쪽 위부터) — 전체 840x560, **투명 바탕**. 칸마다 부품 하나, 칸 가운데.
⚠ **각 부품은 적힌 크기 그대로**(예: 320x160 이면 가로 320 · 세로 160) 칸을 채운다 — 칸 여백은 사방 8 px 정도. 납작하거나 작게 그리지 말 것
(지난 샘플은 320x160 버튼을 290x60 으로 그려 반려).
[저장] Projects/AVSR/_exchange/in/rb_hud_misc.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
