$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 연출 부품 — PD 가 통과시킨 연출 시안 Projects/AVSR/_exchange/in/mock_fxstory_clear_v2.png 에 그려진 효과와
**똑같은 모양 · 색 · 도트 그림체**로. 앵커(시안에서 이 효과만 잘라 둔 것): Projects/AVSR/_exchange/in/anchor_fx/clear/coin_rain.png
[부품] fx_clear_coin_rain — (v3 — v2 는 꼬리가 불꽃이었고 더미까지 그렸다) 2열 x 2줄 4장. **동그란 금화 7~10개**(앞면 보이는 동전, 지름 26~40px, 기울기 제각각, 흰 반사광 한 점)가 위에서 아래로 떨어진다. 꼬리는 **불꽃이 아니라** 동전 위로 이어지는 가는 금빛 세로 빛줄기(폭 2~3px, 길이 30~60px, 위로 갈수록 투명). 1 위쪽 → 2 가운데 → 3 아래쪽 → 4 맨 아래에 닿으며 작은 반짝임 몇 개만 남고 동전은 사라짐. **금화 더미는 그리지 않는다**(게임에 더미 그림이 따로 있다). 불꽃 · 주황 불길 · 빨강 · 분홍 · 별 · 십자 금지
[시트] 칸 320 x 320 가 2열로 4장, 왼쪽 위부터 시간 순(시트 전체 640 x 640). 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF) — 효과 바깥 · 판 안쪽 비울 곳은 전부 마젠타. 마젠타/분홍빛 바탕색을 효과에 섞지 않는다.
- 글자 · 숫자 · 화살표 · 주석 금지. 별 · 표창 · 십자 반짝이 금지.
[저장] Projects/AVSR/_exchange/in/fxs_clear_coin_rain.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
