$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지). 칸 비율이 안 맞게 그려 와도 지우지 말고 그대로 저장한다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(특히 0-2 RE:BORN 화면 · 3-1 등급 계단).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 아웃게임 연출 **부품**. 기준 그림(이 모양 · 색 · 화풍 그대로): Assets/BaseResource/ReBornKit/Icons/rb_icon_chest_gold_144.png
공통: **순수 마젠타(#FF00FF) 바탕**(게임에서 뚫는다) — 도구가 투명 바탕으로 그려 오면 그대로 저장(지우지 말 것) · 그림에 마젠타 · 분홍 금지 · 테두리 또렷하게 · 도트 결이 살아 있게 · 글자 · 숫자 · 캐릭터 · 창은 그리지 않는다(부품만) · 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
[만들 것] **이 금 상자 아이콘이 열리는** 4장 — 은 상자 발주와 같은 4박자 · 같은 자리. 기준 그림의 모양 · 금 테 · 검은 외곽선 그대로. 빛 · 반짝이 없이
[시트] 한 장 192x192, 4장을 가로 4칸 x 1줄(왼쪽 위부터 시간 순) — 전체 768x192.
[저장] Projects/AVSR/_exchange/in/rbfx2_chest_gold.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
