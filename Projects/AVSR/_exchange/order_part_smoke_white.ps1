$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 「못 그렸다」고만 답하라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 탑다운) 파티클 입자 그림 — **흰 연기 덩이 한 개**. 미사일이 날아가며 뒤에 남기는 하얀 연기 꼬리를 이 입자 수십 개로 만든다.
시안의 연기: Projects/AVSR/_exchange/ref/skillmock/sr/cm_1.png (미사일 뒤로 길게 끌리는 흰 · 연회색 뭉게 연기).
[만들 것] 둥글게 뭉친 **흰 · 연회색 연기 덩이** 하나(가운데가 가장 밝은 흰색, 바깥으로 갈수록 연회색 · 가장자리는 투명하게 흩어짐, 뭉게뭉게 세 · 네 덩이가 붙은 모양, 도트 결). 그림자 · 판 · 테두리 선 금지. 192x192 한 장, 연기 덩이가 칸 가운데 지름 70% 정도.
**투명 바탕(알파)** PNG. 투명이 안 되면 **분홍 바탕(#FF00FF) 단색**.
[저장] Projects/AVSR/_exchange/in/part_smoke_white.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
