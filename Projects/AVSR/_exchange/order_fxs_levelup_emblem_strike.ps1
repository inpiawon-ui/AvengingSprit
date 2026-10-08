$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 연출 부품 — PD 가 통과시킨 연출 시안 Projects/AVSR/_exchange/in/mock_fxstory_levelup_v1.png 에 그려진 효과와
**똑같은 모양 · 색 · 도트 그림체**로. 앵커: 시안 mock_fxstory_levelup_v1.png 의 컷1(왼쪽부터 컷 1 · 2 · 3, 각 720 폭)에 그려진 이 효과 — 그 모양 · 색을 그대로
[부품] fx_levelup_emblem_strike — (v4 재발주 — v3 는 가늘게 꺾인 번개) 캔버스 가운데 세로축을 따라 위에서 아래로 내려와 아래쪽 문장 원 안까지 이어지는 굵고 부드러운 금빛 한 줄 — 백금색 심 4~7px + 금색 번짐 14~22px. 위쪽 끝에 짧고 완만한 갈래 1~2개만. 지그재그 번개 · 고리 · 별 · 십자 · 사각 입자 금지. 1 점등 → 2 · 3 최대 → 4 소멸, 흔들림 3px 이내
[시트] 칸 110 x 250 가 4열로 4장, 왼쪽 위부터 시간 순(시트 전체 440 x 250). 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF) — 효과 바깥 · 판 안쪽 비울 곳은 전부 마젠타. 마젠타/분홍빛 바탕색을 효과에 섞지 않는다.
- 글자 · 숫자 · 화살표 · 주석 금지. 별 · 표창 · 십자 반짝이 금지.
[저장] Projects/AVSR/_exchange/in/fxs_levelup_emblem_strike.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
