$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지). 칸 비율이 안 맞게 그려 와도 지우지 말고 그대로 저장한다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 아웃게임 연출 **부품**. PD 가 통과시킨 시안에서 이 부분만 잘라 낸 기준 그림(이 모양 · 색 · 화풍 그대로): Projects/AVSR/_exchange/ref/ui_reborn_1023/fx_anchor/chest.png
공통: **검은 바탕(#000000)**(게임에서 더하기로 그린다) · 빛 가장자리는 검정으로 바로 사그라든다(바깥 어두운 판 금지) · 도트 결이 살아 있게 · 글자 · 숫자 · 캐릭터 · 창은 그리지 않는다(부품만) · 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
[만들 것] 상자가 열릴 때 **안에서 솟는 빛** — ① 은(희고 차가운 빛) ② 금(금빛) ③ 백금(보라 · 흰 빛 + 결정 빛살). 위로 퍼지는 빛 무리, 상자는 그리지 않는다
[시트] 한 장 320x320, 3장을 가로 3칸 x 1줄(왼쪽 위부터 시간 순) — 전체 960x320.
[저장] Projects/AVSR/_exchange/in/rbfx_chest_burst.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
