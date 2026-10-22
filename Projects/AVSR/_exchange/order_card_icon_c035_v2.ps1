$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 레벨업 카드 아이콘 한 장 — 「사거리 — 근접 공격이 닿는 거리가 늘어난다」.
[화풍 앵커 — 이 아이콘들과 같은 화풍 · 크기감 · 외곽선 · 색 밀도] Projects/AVSR/_exchange/in/card_icon_c014.png (관통탄 — 통과한 샘플, 이것이 기준) , Assets/BaseResource/Card/card_c008.png (추가 발사) , Assets/BaseResource/Card/card_c012.png (속사)
- 그림: 유령이 오른쪽으로 **굵은 영혼 팔**을 쭉 늘려, 끝의 **큰 주먹**(주먹 모양이 분명하게, 손가락 마디까지)으로 멀리 있는 해골을 때린다. 팔은 빛줄기가 아니라 굵은 반투명 하늘빛 팔(유령 몸과 같은 흰색 테두리). 팔 아래에 양끝 화살표 자(↔)로 「길이」를 보여 준다. 지난 그림은 팔이 가는 빛줄기라 빔으로 읽혀 반려됐다.
- 유령 · 해골은 앵커 아이콘의 유령(흰 몸 · 큰 검은 눈 · 짧은 꼬리) · 해골과 같은 모양.
- 글자 · 테두리 · 판 없이 아이콘 그림만. 그림이 칸을 90% 정도 꽉 채우게 가운데에(샘플보다 조금 크게).
- 바탕 **분홍(#FF00FF)** 단색 — 게임에서 뚫는다. 그림 가장자리에 분홍이 섞인 흐림 금지(도트 경계 그대로).
- 크기 320 x 320.
[저장] Projects/AVSR/_exchange/in/card_icon_c035_v2.png
"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
