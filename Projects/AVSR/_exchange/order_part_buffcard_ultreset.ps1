$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 탑다운). PD 가 통과시킨 시안(이 모양 · 색 그대로): Projects/AVSR/_exchange/ref/feature_mock/anchor_ultreset_icon.png (레벨업 카드 「필살기 초기화」 아이콘)
[만들 것] 레벨업 카드 아이콘 한 장 — 가운데 **흰 유령**(통통한 몸 · 큰 눈 · 짧은 꼬리, 같은 화풍의 기존 카드 아이콘: C:/won/UnityProject/AvengingSprit/Assets/BaseResource/InGameMainUI/buffcard_c008.png 의 유령과 같은 유령) 둘레를 **보라 둥근 화살표 셋**이 감아 도는 「다시 채움」 문양, 보라 빛. 카드 틀 · 배경 판 · 글자는 그리지 않는다(아이콘만). 굵은 외곽선 · 또렷한 도트. 64x64 한 장, **투명 바탕(알파)**(안 되면 분홍 #FF00FF 단색).
[저장] Projects/AVSR/_exchange/in/part_buffcard_ultreset.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
