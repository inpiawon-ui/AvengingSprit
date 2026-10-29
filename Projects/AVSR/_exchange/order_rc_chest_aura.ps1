$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 결과창 부품. 기준 그림: Assets/BaseResource/LobbyMainUI/chest_silver.png (이 파일을 열어 그림체 · 색 · 크기를 맞춘다).
[부품] chest_aura — 기준 그림(상자)에 붙어 있는 **아우라만** 떼어 움직이게 — 상자 둘레를 감싸는 빛 번짐과 바깥으로 뻗은 짧은 빛살을 기준 그림과 **같은 자리 · 같은 크기**로 그리되 상자 몸은 그리지 않는다(상자 자리는 비우지 말고 빛으로 채워도 된다 — 게임에서 상자가 위에 덮인다). 8장이 숨 쉬듯 반복: 빛 번짐이 조금 커졌다 작아지고, 빛살이 한 가닥씩 길어졌다 짧아지며 천천히 돈다(8 다음 1로 끊김 없이). 흰색 ~ 밝은 회색만(게임에서 상자 등급색을 곱한다). 칸 가장자리 4px 안에서 끝난다 — 잘린 빛 금지
[시트] 칸 206 x 142 가 4열로 8장, 왼쪽 위부터 시간 순(시트 전체 824 x 284). 각 그림은 칸 안에, 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF). 마젠타/분홍을 그림에 쓰지 않는다. 글자 · 숫자 금지.
[저장] Projects/AVSR/_exchange/in/rc_chest_aura.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
