$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
도구가 크게 그려 오면 칸 격자대로 지정 크기로 **축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지). 투명 바탕이 안 나오면 순수 마젠타(#FF00FF) 바탕도 받는다 — 지우지 말고 그대로 저장.
먼저 Projects/AVSR/_exchange/codex_brief_ui_reborn.md 를 읽는다(방향 · 9-slice 제약).
게임 「Avenging Spirit RE:BORN」 UI 리뉴얼 — 판 · 버튼 · 카드 리소스. 승인 디자인: Projects/AVSR/_handoff/reborn_ui_1023/04_검수기록/style_tile.png
기준 그림(배치 · 색 · 비율은 이대로, 재질 · 마감은 손으로 만든 출시 품질로): Projects/AVSR/_exchange/ref/ui_reborn_1023/anchor/style_panels.png · Projects/AVSR/_handoff/reborn_ui_1023/03_리소스_및_시안/REBORN_UI_FINAL/Screens/mail.png · Projects/AVSR/_handoff/reborn_ui_1023/03_리소스_및_시안/REBORN_UI_FINAL/Screens/missions.png
[만들 것] 칸 순서대로 실제 크기로 — ① 목록 줄 400x144(투명에 가까운 면 + 아래 구분선) ② 목록 줄 선택(왼쪽 시안 막대 + 옅은 시안 바탕) ③ 정보 칩 220x64(누를 수 없는 정보, 두께 없음) ④ 재화 칩 260x100(상단 재화 바 — 그림 위에 얹는 유리 느낌) ⑤ 말풍선 320x144 ⑥ 알림 띠 400x144 ⑦ 경고 알림 띠 400x144(왼쪽 빨간 기) ⑧ 보상 알림 띠 520x144(왼쪽 골드 기). 말풍선 꼬리는 그리지 않는다
[시트] 칸 520x160, 8칸을 가로 4칸 x 2줄(왼쪽 위부터) — 전체 2080x320, **투명 바탕**. 칸마다 부품 하나, 칸 가운데.
⚠ **각 부품은 적힌 크기 그대로**(예: 320x160 이면 가로 320 · 세로 160) 칸을 채운다 — 칸 여백은 사방 8 px 정도. 납작하거나 작게 그리지 말 것
(지난 샘플은 320x160 버튼을 290x60 으로 그려 반려).
[저장] Projects/AVSR/_exchange/in/rb_rows.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
