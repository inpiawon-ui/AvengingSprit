$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 연출 부품 — PD 가 통과시킨 연출 시안 Projects/AVSR/_exchange/in/mock_fxstory_altar_v1.png 에 그려진 효과와
**똑같은 모양 · 색 · 도트 그림체**로. 앵커: 시안 mock_fxstory_altar_v1.png 의 컷1(왼쪽부터 컷 1 · 2 · 3, 각 720 폭)에 그려진 이 효과 — 그 모양 · 색을 그대로
[부품] fx_altar_drop_ripple — 세로 캔버스. 위쪽 60%에서 지름 18~26px 청록 물방울 하나가 수직으로 떨어지고, 아래 40%에서 닿은 자리부터 얇은 동심 타원 2~3겹이 퍼진다. 1 낙하 시작 → 2 닿음 → 3 물결 최대 → 4 잔광 사라짐. 삼각형 · 룬 · 격자 · 거미줄 · 방사 빛살 전부 금지, 발광 반경은 칸 폭의 40% 이하
[시트] 칸 192 x 256 가 2열로 4장, 왼쪽 위부터 시간 순(시트 전체 384 x 512). 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF) — 효과 바깥 · 판 안쪽 비울 곳은 전부 마젠타. 마젠타/분홍빛 바탕색을 효과에 섞지 않는다.
- 글자 · 숫자 · 화살표 · 주석 금지. 별 · 표창 · 십자 반짝이 금지.
[저장] Projects/AVSR/_exchange/in/fxs_altar_drop_ripple.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
