$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 연출 부품 — PD 가 통과시킨 연출 시안 Projects/AVSR/_exchange/in/mock_fxstory_shop_v1.png 에 그려진 효과와
**똑같은 모양 · 색 · 도트 그림체**로. 앵커: 시안 mock_fxstory_shop_v1.png 의 컷3(왼쪽부터 컷 1 · 2 · 3, 각 720 폭)에 그려진 이 효과 — 그 모양 · 색을 그대로
[부품] fx_shop_coin_to_gold — 가로 띠(왼쪽 → 오른쪽으로 날아감). 둥근 금화 도트 5~7개가 서로 떨어져 한 줄로 날고, 꼬리 쪽(왼쪽)으로 갈수록 작아진다. 흰색 ~ 밝은 회색 중성(게임에서 금색을 곱함). 가로로 늘여도 도트가 동그랗게 남게 간격 넉넉히. 별 · 십자 금지. 4장 반복
[시트] 칸 384 x 72 가 2열로 4장, 왼쪽 위부터 시간 순(시트 전체 768 x 144). 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF) — 효과 바깥 · 판 안쪽 비울 곳은 전부 마젠타. 마젠타/분홍빛 바탕색을 효과에 섞지 않는다.
- 글자 · 숫자 · 화살표 · 주석 금지. 별 · 표창 · 십자 반짝이 금지.
[저장] Projects/AVSR/_exchange/in/fxs_shop_coin_to_gold.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
