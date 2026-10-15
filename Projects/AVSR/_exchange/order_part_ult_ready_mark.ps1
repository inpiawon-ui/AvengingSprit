$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 탑다운). PD 가 통과시킨 시안(이 모양 · 색 그대로): Projects/AVSR/_exchange/ref/feature_mock/anchor_ultready_mark.png (적 호스트 체력바 옆 작은 보라 별)
[만들 것] 적 호스트 머리 위에 붙는 **「필살기 가능」 작은 표시** — 보라 원 안의 금빛 별(SPECIAL 버튼 보라 톤) + 은은한 보라 빛. 4장 반복(숨쉬듯 별이 살짝 반짝 · 빛이 커졌다 작아짐). 한 장 32x32, 4장 가로 한 줄(128x32). **투명 바탕(알파)**(안 되면 분홍 #FF00FF 단색).
[저장] Projects/AVSR/_exchange/in/part_ult_ready_mark.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
