$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 연출 부품 — PD 가 통과시킨 연출 시안 Projects/AVSR/_exchange/in/mock_fxstory_devil_v1.png 에 그려진 효과와
**똑같은 모양 · 색 · 도트 그림체**로. 앵커(시안에서 이 효과만 잘라 둔 것): Projects/AVSR/_exchange/in/anchor_fx/devil/soul_beam.png
[부품] fx_devil_soul_beam — 가로로 뻗은 붉은 원혼 번개 — **2~3가닥**이 꼬이며 나란히, 가운데 흰 심선, 둘레 붉은 외광과 작은 불티(시안 3 수락 컷의 굵고 화려한 번개). 단선 금지. 3장이 모양만 바뀌는 반복 — 칸 가로 가득 (v2 재발주: v1 은 가는 단선이라 시안과 달랐다)
[시트] 칸 1024 x 128 가 1열로 3장, 왼쪽 위부터 시간 순(시트 전체 1024 x 384). 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF) — 효과 바깥 · 판 안쪽 비울 곳은 전부 마젠타. 마젠타/분홍빛 바탕색을 효과에 섞지 않는다.
- 글자 · 숫자 · 화살표 · 주석 금지. 별 · 표창 · 십자 반짝이 금지.
[저장] Projects/AVSR/_exchange/in/fxs_devil_soul_beam.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
