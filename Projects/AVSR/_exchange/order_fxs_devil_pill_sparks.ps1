$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 연출 부품 — PD 가 통과시킨 연출 시안 Projects/AVSR/_exchange/in/mock_fxstory_devil_v1.png 에 그려진 효과와
**똑같은 모양 · 색 · 도트 그림체**로. 앵커(시안에서 이 효과만 잘라 둔 것): Projects/AVSR/_exchange/in/anchor_fx/devil/pill_embers.png
[부품] fx_devil_pill_sparks — 알약 모양 판의 **테두리만** 따라 타오르는 불씨 · 작은 불꽃 띠(판 안쪽은 비운다 = 마젠타). 판 크기는 칸의 85% 가로 · 45% 세로, 밝은 금 ~ 흰 노랑(게임에서 보상은 금, 대가는 붉게 색을 곱한다). 4장이 불씨 위치만 바뀌며 이어지는 반복
[시트] 칸 512 x 128 가 2열로 4장, 왼쪽 위부터 시간 순(시트 전체 1024 x 256). 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF) — 효과 바깥 · 판 안쪽 비울 곳은 전부 마젠타. 마젠타/분홍빛 바탕색을 효과에 섞지 않는다.
- 글자 · 숫자 · 화살표 · 주석 금지. 별 · 표창 · 십자 반짝이 금지.
[저장] Projects/AVSR/_exchange/in/fxs_devil_pill_sparks.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
