$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 연출 부품 — PD 가 통과시킨 연출 시안 Projects/AVSR/_exchange/in/mock_fxstory_altar_v1.png 에 그려진 효과와
**똑같은 모양 · 색 · 도트 그림체**로. 앵커: 시안 mock_fxstory_altar_v1.png 의 컷2(왼쪽부터 컷 1 · 2 · 3, 각 720 폭)에 그려진 이 효과 — 그 모양 · 색을 그대로
[부품] fx_altar_gem_pulse — 칸 오른쪽 청록 물방울 보석이 맥박처럼 빛남 4장 반복
[시트] 칸 128 x 128 가 4열로 4장, 왼쪽 위부터 시간 순(시트 전체 512 x 128). 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF) — 효과 바깥 · 판 안쪽 비울 곳은 전부 마젠타. 마젠타/분홍빛 바탕색을 효과에 섞지 않는다.
- 글자 · 숫자 · 화살표 · 주석 금지. 별 · 표창 · 십자 반짝이 금지.
[저장] Projects/AVSR/_exchange/in/fxs_altar_gem_pulse.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
