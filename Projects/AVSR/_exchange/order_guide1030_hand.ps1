$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 튜토리얼에서 「여기를 눌러요」 를 가리키는 **손가락 아이콘** 한 개.
[화풍 앵커] Projects/AVSR/_exchange/in/guide1030_possess.png 오른쪽 위의 손가락(소매 + 집게손가락으로 누르는 손) — 이 손과 같은 화풍 · 같은 도트 크기.
손은 **오른쪽 아래에서 왼쪽 위를 가리킨다**(집게손가락 끝이 그림의 왼쪽 위 모서리 근처). 굵은 검은 외곽선, 밝은 살색, 소매는 진한 남색.
[바탕] 완전한 분홍(#FF00FF) 단색. 투명 바탕으로 나오면 그대로 저장해도 된다. 그림자 · 빛 · 글자 금지.
[크기] 1024 x 1024 한 장, 손이 가운데 크게(캔버스의 70% 정도).
[저장] Projects/AVSR/_exchange/in/guide1030_hand.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
