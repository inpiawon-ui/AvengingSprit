$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 연출 부품 — PD 가 통과시킨 연출 시안 Projects/AVSR/_exchange/in/mock_fxstory_levelup_v1.png 에 그려진 효과와
**똑같은 모양 · 색 · 도트 그림체**로. 앵커: 시안 mock_fxstory_levelup_v1.png 의 컷3(왼쪽부터 컷 1 · 2 · 3, 각 720 폭)에 그려진 이 효과 — 그 모양 · 색을 그대로
[부품] fx_levelup_pick_stream — (v2 재발주 — 게임은 그림의 **가로축**을 줄기 방향으로 늘인다. v1 은 세로 그림이라 가로선 묶음으로 보였다) 가로 캔버스: 왼쪽 끝 = 고른 카드(넓고 밝음) → 오른쪽 끝 = 문장(가늘게 모임). 완만한 S자로 굽이치는 금빛 원혼 줄기 2~3가닥이 서로 꼬이며 흐른다 — 심 3~5px, 번짐 16~24px, 줄기 사이 작은 불티 몇 개. 직선 묶음 · 지그재그 번개 · 별 · 십자 · 사각 입자 금지. 4장이 흐르는 반복
[시트] 칸 512 x 128 가 2열로 4장, 왼쪽 위부터 시간 순(시트 전체 1024 x 256). 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF) — 효과 바깥 · 판 안쪽 비울 곳은 전부 마젠타. 마젠타/분홍빛 바탕색을 효과에 섞지 않는다.
- 글자 · 숫자 · 화살표 · 주석 금지. 별 · 표창 · 십자 반짝이 금지.
[저장] Projects/AVSR/_exchange/in/fxs_levelup_pick_stream.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
