$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
도구가 크게 그려 오면 칸 격자대로 지정 크기로 **축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지). 투명 바탕이 안 나오면 순수 마젠타(#FF00FF) 바탕도 받는다 — 지우지 말고 그대로 저장.
먼저 Projects/AVSR/_exchange/codex_brief_ui_reborn.md 를 읽는다(방향 · 9-slice 제약).
게임 「Avenging Spirit RE:BORN」 UI 리뉴얼 — 전투 HUD 리소스. 승인 디자인: Projects/AVSR/_handoff/reborn_ui_1023/04_검수기록/style_tile.png
기준 그림(배치 · 색 · 비율은 이대로, 재질 · 마감은 손으로 만든 출시 품질로): Projects/AVSR/_exchange/ref/ui_reborn_1023/anchor/hud_top.png · Projects/AVSR/_handoff/reborn_ui_1023/03_리소스_및_시안/REBORN_UI_FINAL/Screens/fight.png
[만들 것] ① 위 반투명 띠 520x160(판 재질, 양끝 꺾쇠, 9-slice) ② 모드 판 460x144(밤 KILLS · 탑 층 표시용) ③ 보스 체력 바 **틀만** 520x100(왼쪽 끝 해골 장식 + 비어 있는 홈 — 빨간 채움은 그리지 않는다, 유령 얼굴 금지 · 지난 납품 반려) ④ 경고 띠 520x144(빨강 사선 줄무늬 띠). 글자 · 숫자 없음. **방 진행 칸은 그리지 않는다**(없앴다)
[시트] 칸 520x160, 4칸을 가로 2칸 x 2줄(왼쪽 위부터) — 전체 1040x320, **투명 바탕**. 칸마다 부품 하나, 칸 가운데.
⚠ **각 부품은 적힌 크기 그대로**(예: 320x160 이면 가로 320 · 세로 160) 칸을 채운다 — 칸 여백은 사방 8 px 정도. 납작하거나 작게 그리지 말 것
(지난 샘플은 320x160 버튼을 290x60 으로 그려 반려).
[저장] Projects/AVSR/_exchange/in/rb_hud_bars.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
