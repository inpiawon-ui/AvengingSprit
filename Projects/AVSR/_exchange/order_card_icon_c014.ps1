$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 레벨업 카드 아이콘 한 장 — 「관통탄 — 탄이 적을 뚫고 지나간다」.
[화풍 앵커 — 이 아이콘들과 같은 화풍 · 크기감 · 외곽선 · 색 밀도] Assets/BaseResource/Card/card_c008.png (추가 발사) , Assets/BaseResource/Card/card_c012.png (속사) , Assets/BaseResource/Card/card_c001.png
- 그림: 왼쪽의 유령이 길쭉한 하늘빛 탄 하나를 쏘고, 그 탄이 오른쪽에 나란히 선 작은 해골 두 마리를 차례로 꿰뚫고 계속 날아간다. 꿰뚫린 자리에 작은 구멍 · 파편이 튄다. 「뚫고 지나간다」가 한눈에 읽혀야 한다.
- 유령은 앵커 아이콘의 유령(흰 몸 · 큰 검은 눈 · 짧은 꼬리)과 같은 모양.
- 글자 · 테두리 · 판 없이 아이콘 그림만. 그림이 칸을 85% 정도 채우게 가운데에.
- 바탕 **분홍(#FF00FF)** 단색 — 게임에서 뚫는다. 그림 가장자리에 분홍이 섞인 흐림 금지(도트 경계 그대로).
- 크기 320 x 320.
[저장] Projects/AVSR/_exchange/in/card_icon_c014.png
"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt