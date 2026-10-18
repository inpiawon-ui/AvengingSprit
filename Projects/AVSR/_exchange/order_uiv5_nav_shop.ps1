$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 UI 리뉴얼(혼합 컨셉 「픽셀 연대기 × 쇼케이스」) — **아래 바 아이콘 4개**.
먼저 읽는다: Projects/AVSR/_exchange/plan_ui_v3.md
앵커(PD 가 고른 시안의 아래 바를 자른 것 — 이 네 아이콘의 모양 · 색 · 도트 질감 그대로, 깨끗하게): Projects/AVSR/_exchange/ref/ui_v5/home_nav.png
원작 캐릭터 기준: 갱스터 Assets/BaseResource/Growth/Portraits/portrait_gangster.png · 유령 Assets/BaseResource/Growth/Portraits/portrait_ghost.png
이번엔 **4번 SHOP 하나만** 다시 그린다 — 지난 납품(Projects/AVSR/_exchange/in/uiv5_nav_icons.png 오른쪽 끝)이 캔버스 끝에 잘렸다. 그 그림 결 · 색 그대로.
캔버스 1024 × 1024, 배경은 순수 마젠타(#FF00FF) 단색. 가판대를 **가운데**, 크기 약 600 × 600, 사방 여백 200px 이상(어디도 캔버스 끝에 닿지 않게).
 SHOP — 줄무늬 차양이 달린 작은 상점 가판대(정면, 좌우 대칭). 가판대 안쪽 빈 곳은 어두운 나무색으로 채운다(마젠타가 비치지 않게).
**판 · 테두리 · 글자는 그리지 마라 — 아이콘만.** 빛 번짐 · 그림자를 아이콘 바깥으로 그리지 마라.
[저장] Projects/AVSR/_exchange/in/uiv5_nav_shop.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
