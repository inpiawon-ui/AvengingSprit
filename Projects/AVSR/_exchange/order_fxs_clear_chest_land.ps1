$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 연출 부품 — PD 가 통과시킨 연출 시안 Projects/AVSR/_exchange/in/mock_fxstory_clear_v2.png 에 그려진 효과와
**똑같은 모양 · 색 · 도트 그림체**로. 앵커: 시안 mock_fxstory_clear_v2.png 의 컷2(왼쪽부터 컷 1 · 2 · 3, 각 720 폭)에 그려진 이 효과 — 그 모양 · 색을 그대로
[부품] fx_clear_chest_land — 상자가 떨어진 자리의 바닥 고리(가로:세로 약 2.7:1 납작한 타원, 가운데는 비움). 고리는 청록 #42EAF2 ~ 흰색, 먼지는 중성 회백색. 녹색 픽셀 · 검은 테두리 금지, 상자는 그리지 않음. 1 → 4 퍼지며 사라짐
[시트] 칸 512 x 192 가 2열로 4장, 왼쪽 위부터 시간 순(시트 전체 1024 x 384). 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF) — 효과 바깥 · 판 안쪽 비울 곳은 전부 마젠타. 마젠타/분홍빛 바탕색을 효과에 섞지 않는다.
- 글자 · 숫자 · 화살표 · 주석 금지. 별 · 표창 · 십자 반짝이 금지.
[저장] Projects/AVSR/_exchange/in/fxs_clear_chest_land.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
