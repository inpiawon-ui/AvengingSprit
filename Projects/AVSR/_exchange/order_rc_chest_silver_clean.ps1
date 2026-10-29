$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 결과창 부품. 기준 그림: Assets/BaseResource/LobbyMainUI/chest_silver.png (이 파일을 열어 그림체 · 색 · 크기를 맞춘다).
[부품] chest_silver_clean — 결과창 은 상자 — 기준 그림과 **완전히 같은 그림**(모양 · 색 · 도트 · 크기 · 자리)에서 둘레의 빛 · 아우라 · 반짝이 별만 뺀다. 빛이 그림 가장자리에 번져 밝아진 테두리 픽셀도 원래 재질 색으로 되돌린다. 새로 그리지 말고 기준을 그대로 옮긴다. 둘레의 파란 아우라 · 빛살을 뺀 상자만.
[시트] 칸 206 x 142 가 1열로 1장, 왼쪽 위부터 시간 순(시트 전체 206 x 142). 각 그림은 칸 안에, 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF). 마젠타/분홍을 그림에 쓰지 않는다. 글자 · 숫자 금지.
[저장] Projects/AVSR/_exchange/in/rc_chest_silver_clean.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
