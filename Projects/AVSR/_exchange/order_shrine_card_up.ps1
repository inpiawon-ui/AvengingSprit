$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 천사 제단 선물 아이콘 한 장 — 「카드를 벼린다 — 가진 카드 하나의 레벨이 오른다」.
[화풍 앵커 — 이 제단 아이콘들과 같은 화풍: 하늘빛으로 빛나는 단색 도트 실루엣 · 같은 외곽선 · 같은 크기감] Assets/BaseResource/InGameMainUI/shrine_atk.png , Assets/BaseResource/InGameMainUI/shrine_speed.png , Assets/BaseResource/InGameMainUI/shrine_full_heal.png
- 그림: 카드 한 장 위에 위쪽을 가리키는 굵은 화살표, 카드 둘레에 작은 반짝임.
- 글자 · 테두리 · 판 없이 아이콘 그림만. 그림이 칸을 85% 정도 채우게 가운데에.
- 바탕 **분홍(#FF00FF)** 단색 — 게임에서 뚫는다. 그림 가장자리에 분홍이 섞인 흐림 금지(도트 경계 그대로).
- 크기 256 x 256.
[저장] Projects/AVSR/_exchange/in/shrine_card_up.png
"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
