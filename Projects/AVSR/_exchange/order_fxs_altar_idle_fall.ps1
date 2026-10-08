$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 연출 부품 — PD 가 통과시킨 연출 시안 Projects/AVSR/_exchange/in/mock_fxstory_altar_v1.png 에 그려진 효과와
**똑같은 모양 · 색 · 도트 그림체**로. 앵커: 시안 mock_fxstory_altar_v1.png 의 컷2(왼쪽부터 컷 1 · 2 · 3, 각 720 폭)에 그려진 이 효과 — 그 모양 · 색을 그대로
[부품] fx_altar_idle_fall — (v2 재발주 — v1 은 큰 방울 두 개뿐이라 끊겨 보임) 세로 캔버스 위 끝에서 아래 끝까지 이어지는 가느다란 청록 물결 줄기 1개(심 2~4px + 아주 옅은 번짐, 장이 바뀌어도 세로축은 늘 보임) + 크기가 다른 낙하 물방울 5~7개를 높이 전체에 흩어 둔다. 방울은 장마다 아래로 내려가되 간격을 엇갈려 이음새가 안 보이게. 굵은 기둥 · 별 · 십자 · 스파크 금지. 4장 반복
[시트] 칸 128 x 512 가 4열로 4장, 왼쪽 위부터 시간 순(시트 전체 512 x 512). 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF) — 효과 바깥 · 판 안쪽 비울 곳은 전부 마젠타. 마젠타/분홍빛 바탕색을 효과에 섞지 않는다.
- 글자 · 숫자 · 화살표 · 주석 금지. 별 · 표창 · 십자 반짝이 금지.
[저장] Projects/AVSR/_exchange/in/fxs_altar_idle_fall.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
