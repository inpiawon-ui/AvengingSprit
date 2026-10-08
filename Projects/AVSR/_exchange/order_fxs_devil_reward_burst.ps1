$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 연출 부품 — PD 가 통과시킨 연출 시안 Projects/AVSR/_exchange/in/mock_fxstory_devil_v1.png 에 그려진 효과와
**똑같은 모양 · 색 · 도트 그림체**로. 앵커(시안에서 이 효과만 잘라 둔 것): Projects/AVSR/_exchange/in/anchor_fx/devil/reward_burst.png
[부품] fx_devil_reward_burst — 알약 판 둘레에서 노란 불꽃 · 짧은 빛살이 확 퍼졌다 사라짐(판은 그리지 않는다, 가운데는 비움) 1 → 4. 별 · 십자 모양 금지
[시트] 칸 512 x 256 가 2열로 4장, 왼쪽 위부터 시간 순(시트 전체 1024 x 512). 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF) — 효과 바깥 · 판 안쪽 비울 곳은 전부 마젠타. 마젠타/분홍빛 바탕색을 효과에 섞지 않는다.
- 글자 · 숫자 · 화살표 · 주석 금지. 별 · 표창 · 십자 반짝이 금지.
[저장] Projects/AVSR/_exchange/in/fxs_devil_reward_burst.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
