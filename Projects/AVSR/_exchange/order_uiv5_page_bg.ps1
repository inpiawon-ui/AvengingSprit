$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 UI 리뉴얼(혼합 컨셉 「픽셀 연대기 × 쇼케이스」) — 홈 화면 부품. 먼저 읽는다: Projects/AVSR/_exchange/plan_ui_v3.md (혼합 규칙 · 5차).
PD 가 고른 시안: Projects/AVSR/_exchange/in/mock_uiv5_home.png — 이 화풍 · 색 · 빛 그대로.
앵커(시안 홈 전체 — 판 사이로 비치는 바탕): Projects/AVSR/_exchange/in/mock_uiv5_home.png
그릴 것: **홈 화면 바탕 한 장** — 잉크 남색 바탕에 아주 옅은 밤 도시 실루엣 · 별 몇 개 · 은은한 결(양피지 · 천 질감). 위에 판들이 덮이므로 **아주 차분하고 어둡게**, 무늬가 판 사이 틈으로 살짝 보일 정도.
글자 · 캐릭터 · 아이콘 · 테두리 · 판은 그리지 마라. 위아래로 이어 늘려도 티 나지 않게 위 · 아래 가장자리는 단색에 가깝게.
크기 1080 × 1920, 꽉 찬 그림(투명 · 마젠타 없음).
[저장] Projects/AVSR/_exchange/in/uiv5_page_bg.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
