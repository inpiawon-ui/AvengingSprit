$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 연출 부품 — PD 가 통과시킨 연출 시안 Projects/AVSR/_exchange/in/mock_fxstory_room_devil_v1.png 에 그려진 효과와
**똑같은 모양 · 색 · 도트 그림체**로. 앵커: 시안 mock_fxstory_room_devil_v1.png 의 컷3(왼쪽부터 컷 1 · 2 · 3, 각 720 폭)에 그려진 이 효과 — 그 모양 · 색을 그대로
[부품] fx_room_devil_chain_release — (v2 재발주 — v1 은 좌우 대칭 U자라 합치면 하트 고리로 보임) 가운데 190x210 은 비움(제단 자리). 그 좌우 가장자리 두 곳(구속점)에서 검고 굵은 쇠사슬이 끊겨 각각 **바깥 · 위쪽**으로 길고 거칠게 튀어 나간다. 좌우를 복사 · 반전하지 말고 길이 · 굴곡 · 끝 방향을 다르게. 사슬 끝은 가운데로 말리지 않고, 두 가닥이 원 · 타원 · 하트를 만들지 않는다. 1 장력 걸린 짧은 사슬 → 2~4 바깥으로 급히 펼쳐짐(굵은 S자 · 꺾인 호) → 5~6 캔버스 밖으로 빠지며 붉은 가장자리 잔광만 약하게. 쇠는 검정 · 암회색, 붉은빛은 쇠 가장자리 얇은 열광만. 연기 · 불덩이 · 별 · 십자 금지
[시트] 칸 512 x 256 가 3열로 6장, 왼쪽 위부터 시간 순(시트 전체 1536 x 512). 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF) — 효과 바깥 · 판 안쪽 비울 곳은 전부 마젠타. 마젠타/분홍빛 바탕색을 효과에 섞지 않는다.
- 글자 · 숫자 · 화살표 · 주석 금지. 별 · 표창 · 십자 반짝이 금지.
[저장] Projects/AVSR/_exchange/in/fxs_room_devil_chain_release.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
