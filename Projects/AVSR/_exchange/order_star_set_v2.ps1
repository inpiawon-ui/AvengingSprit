$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 부품 발주 — 성급 별 두 개.
PD 가 고른 표기(이 그림 그대로): Projects/AVSR/_exchange/in/mock_growth_v6_stars_v2.png — 1~4성 작은 은별, 5성마다 큰 금별.
앵커(시안에서 자른 두 별): Projects/AVSR/_exchange/ref/ulticon/mock_star_6sung.png — 왼쪽 큰 금별, 오른쪽 작은 은별. 이 모양 · 색 · 빛 그대로 깨끗하게.
- 한 장에 **두 칸**: 왼쪽 칸 = 큰 금별(반짝임 · 밝은 테두리, 둘레 작은 반짝 점 두세 개는 별 몸에 붙여서), 오른쪽 칸 = 작은 은별(담백, 반짝 점 없음).
- 두 별은 **같은 크기로** 그린다(게임에서 큰 별을 1.6배로 키워 쓴다 — 그래서 각 별이 자기 칸을 거의 꽉 채운다). 같은 별 모양 · 같은 외곽선 굵기, 색과 빛만 다르다.
- 별은 정면 · 대칭, 다섯 꼭짓점이 또렷하게. 32 × 32 로 줄여도 별로 읽히게 굵은 도트.
- 배경은 순수 마젠타(#FF00FF) 단색 — 별 바깥에 번짐 · 그림자를 그리지 마라(뚫어서 쓴다). 둘레 반짝 점은 별 외곽선에 닿게.
- 캔버스 1024 × 512, 칸 둘(512 × 512)에 별 하나씩 가운데.
[저장] Projects/AVSR/_exchange/in/star_set_v2.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
