$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 연출 부품 — PD 가 통과시킨 연출 시안 Projects/AVSR/_exchange/in/mock_fxstory_levelup_v1.png 에 그려진 효과와
**똑같은 모양 · 색 · 도트 그림체**로. 앵커: 시안 mock_fxstory_levelup_v1.png 의 컷2(왼쪽부터 컷 1 · 2 · 3, 각 720 폭)에 그려진 이 효과 — 그 모양 · 색을 그대로
[부품] fx_levelup_emblem_rays — (v4 재발주 — v3 는 굵은 직사각 막대가 부채꼴로 솟음) 가운데 빈 원 둘레로 가늘고 부드러운 금빛 방사광 14~18줄을 원 전체에 고르게 — 긴 줄 6~8 + 짧은 보조. 각 줄은 안쪽 폭 5~8px 에서 바깥 1~3px 로 가늘어지며 끝이 흐려진다. 직사각 끝 · 톱니 · 픽셀 덩어리 · 별 · 십자 금지. 4장 모양 고정, 밝기만 65 → 85 → 100 → 80%
[시트] 칸 360 x 360 가 2열로 4장, 왼쪽 위부터 시간 순(시트 전체 720 x 720). 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF) — 효과 바깥 · 판 안쪽 비울 곳은 전부 마젠타. 마젠타/분홍빛 바탕색을 효과에 섞지 않는다.
- 글자 · 숫자 · 화살표 · 주석 금지. 별 · 표창 · 십자 반짝이 금지.
[저장] Projects/AVSR/_exchange/in/fxs_levelup_emblem_rays.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
