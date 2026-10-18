$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 UI 리뉴얼(혼합 컨셉 「픽셀 연대기 × 쇼케이스」) — 홈 화면 부품. 먼저 읽는다: Projects/AVSR/_exchange/plan_ui_v3.md (혼합 규칙 · 5차).
PD 가 고른 시안: Projects/AVSR/_exchange/in/mock_uiv5_home.png — 이 화풍 · 색 · 빛 그대로.
앵커(PD 가 고른 탑 시안의 왼쪽 탑 기둥을 자른 것 — 이 그림 그대로, 깨끗하게): Projects/AVSR/_exchange/ref/ui_v5/tower_column.png
전체 시안: Projects/AVSR/_exchange/in/mock_uiv3_tower.png
그릴 것: **세로로 긴 망령의 탑 한 채** — 돌 탑에 층마다 불 켜진 창, 층 사이 사슬, 꼭대기 첨탑 · 붉은 불꽃, 둘레에 파란 유령불 몇 개, 뒤는 달밤 도시(어둡게), 맨 아래 문에 주황 불빛.
**층 이름표(「정예 10층」 「4층」 같은 판) · 글자는 그리지 마라** — 게임이 층 칸을 얹는다. 탑 가운데 세로 띠(층 칸이 놓일 자리)는 너무 복잡하지 않게.
크기 600 × 1700(세로로 길게), 꽉 찬 그림(투명 · 마젠타 없음). 도트 픽셀아트 질감.
[저장] Projects/AVSR/_exchange/in/uiv5_tower_column.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
