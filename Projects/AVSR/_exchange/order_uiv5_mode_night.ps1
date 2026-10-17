$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 UI 리뉴얼(혼합 컨셉 「픽셀 연대기 × 쇼케이스」) — 홈 화면 부품. 먼저 읽는다: Projects/AVSR/_exchange/plan_ui_v3.md (혼합 규칙 · 5차).
PD 가 고른 시안: Projects/AVSR/_exchange/in/mock_uiv5_home.png — 이 화풍 · 색 · 빛 그대로.
앵커(시안에서 자른 망령의 밤 카드): Projects/AVSR/_exchange/ref/ui_v5/home_night.png
그릴 것: **망령의 밤 모드 카드 그림 한 장** — 보라 밤하늘 아래 몰려오는 해골 · 좀비 무리, 오른쪽 위 보라 불꽃 악령 얼굴, 작은 파란 유령들.
**글자(망령의 밤 · 오늘 수요일 · 최고 기록) · 글자 판 · 카드 테두리는 그리지 마라** — 게임이 얹는다. 위쪽 1/4 과 아래쪽 1/4 은 글자가 얹히므로 비교적 어둡고 차분하게.
크기 900 × 860, 꽉 찬 그림(투명 · 마젠타 없음). 도트 픽셀아트 질감.
[저장] Projects/AVSR/_exchange/in/uiv5_mode_night.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
