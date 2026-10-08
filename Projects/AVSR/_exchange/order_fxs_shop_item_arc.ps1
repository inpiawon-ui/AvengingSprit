$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 연출 부품 — PD 가 통과시킨 연출 시안 Projects/AVSR/_exchange/in/mock_fxstory_shop_v1.png 에 그려진 효과와
**똑같은 모양 · 색 · 도트 그림체**로. 앵커: 시안 mock_fxstory_shop_v1.png 의 컷3(왼쪽부터 컷 1 · 2 · 3, 각 720 폭)에 그려진 이 효과 — 그 모양 · 색을 그대로
[부품] fx_shop_item_arc — 아래 가운데에서 출발해 오른쪽으로 불룩한 원호를 타고 위 가운데로 솟는 구매 물건 아이콘(56~64px, 크기 유지) + 뒤에 이어지는 호박빛 꼬리(폭 8~14px)와 작은 파편 4~7개. 첫 장의 출발점은 캔버스 아래 가운데. 별 · 십자 금지. 1 → 6 시간 순
[시트] 칸 250 x 390 가 3열로 6장, 왼쪽 위부터 시간 순(시트 전체 750 x 780). 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF) — 효과 바깥 · 판 안쪽 비울 곳은 전부 마젠타. 마젠타/분홍빛 바탕색을 효과에 섞지 않는다.
- 글자 · 숫자 · 화살표 · 주석 금지. 별 · 표창 · 십자 반짝이 금지.
[저장] Projects/AVSR/_exchange/in/fxs_shop_item_arc.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
