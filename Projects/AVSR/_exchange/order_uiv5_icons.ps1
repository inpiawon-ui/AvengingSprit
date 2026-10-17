$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 UI 리뉴얼(혼합 컨셉 「픽셀 연대기 × 쇼케이스」) — 홈 화면 부품. 먼저 읽는다: Projects/AVSR/_exchange/plan_ui_v3.md (혼합 규칙 · 5차).
PD 가 고른 시안: Projects/AVSR/_exchange/in/mock_uiv5_home.png — 이 화풍 · 색 · 빛 그대로.
앵커(시안에서 자른 떠 있는 단추들): Projects/AVSR/_exchange/ref/ui_v5/home_icons.png
캔버스 2048 × 1024, 배경은 순수 마젠타(#FF00FF) 단색. 부품 사이 마젠타 여백 40px 이상. 빛 번짐 · 그림자를 부품 바깥으로 그리지 마라(마젠타가 뚫린다).
그릴 부품(두 줄로 나란히, 각 약 300 × 300):
 1. 단추 판 — 남색 정사각 판 + 고동금 1px 테(빈 판, 아이콘 없음)
 2. PASS 티켓 아이콘(안 산 상태) — 파란 티켓에 「PASS」 영문 글자
 3. 금 왕관 티켓 아이콘(산 상태) — 금색 티켓 + 왕관, 글자 없음
 4. 미션 아이콘 — 클립보드 + 체크
 5. 업적 아이콘 — 별 메달(리본 달린)
 6. 작은 금색 꼬리표 판(빈 판, 글자 없음 — 「구매」를 게임이 얹는다, 약 160 × 60)
 7. 빨간 알림 점(동그라미, 약 60 × 60)
 8. 넓은 단추 판(패스 산 상태 · 큰 왕관 단추 바탕, 남색 + 밝은 금 테, 약 300 × 260, 빈 판)
아이콘(2~5)은 판 없이 아이콘만(판은 1번을 게임이 깐다). 도트 질감은 시안처럼.
[저장] Projects/AVSR/_exchange/in/uiv5_icons.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
