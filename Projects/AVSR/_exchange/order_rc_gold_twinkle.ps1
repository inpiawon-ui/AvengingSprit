$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 결과창 부품. 기준 그림: Assets/BaseResource/ParticleFx/star4.png (이 파일을 열어 그림체 · 색 · 크기를 맞춘다).
[부품] gold_twinkle — 금화 더미 위에서 반짝 빛났다 사라지는 별 하나 — 가는 십자(4갈래) 빛 + 가운데 작은 흰 점. 1 아주 작은 점 → 3 가장 크고 밝음(갈래 끝이 칸 가장자리 4px 안쪽) → 6 사라짐. 흰색 ~ 연한 금색. 게임에서 금화 더미 둘레 여러 자리에 시간차를 두고 띄운다
[시트] 칸 64 x 64 가 6열로 6장, 왼쪽 위부터 시간 순(시트 전체 384 x 64). 각 그림은 칸 안에, 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF). 마젠타/분홍을 그림에 쓰지 않는다. 글자 · 숫자 금지.
[저장] Projects/AVSR/_exchange/in/rc_gold_twinkle.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
