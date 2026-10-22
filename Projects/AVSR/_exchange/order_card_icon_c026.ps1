$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 레벨업 카드 아이콘 한 장 — 「폭발 메아리 — 내 탄이 터지면 같은 자리에 작은 폭발이 한 번 더」.
[화풍 앵커 — 이 아이콘들과 같은 화풍 · 크기감 · 외곽선 · 색 밀도] Projects/AVSR/_exchange/in/card_icon_c014.png (관통탄 — 통과한 샘플, 이것이 기준) , Assets/BaseResource/Card/card_c008.png (추가 발사) , Assets/BaseResource/Card/card_c012.png (속사)
- 그림: 큰 주황 폭발 하나와, 그 옆에 바로 이어 터지는 작은 두 번째 폭발(메아리). 유령은 왼쪽 아래에서 작게 바라본다.
- 유령 · 해골은 앵커 아이콘의 유령(흰 몸 · 큰 검은 눈 · 짧은 꼬리) · 해골과 같은 모양.
- 글자 · 테두리 · 판 없이 아이콘 그림만. 그림이 칸을 90% 정도 꽉 채우게 가운데에(샘플보다 조금 크게).
- 바탕 **분홍(#FF00FF)** 단색 — 게임에서 뚫는다. 그림 가장자리에 분홍이 섞인 흐림 금지(도트 경계 그대로).
- 크기 320 x 320.
[저장] Projects/AVSR/_exchange/in/card_icon_c026.png
"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
