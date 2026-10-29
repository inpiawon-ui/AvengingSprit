$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/guide_party_ui.md 를 끝까지 읽는다. 참고 그림은 Projects/AVSR/_exchange/ref/party_ui_1029/ 의 네 장(특히 pd_ref_trait_tracker.png = 목표 느낌, base_ingame_now.png = 지금 전투 화면).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 「빙의 파티」 새 시스템의 **인게임 UI 시안** 한 장. 크기 720 x 1280.
[안 C — 파티 띠] 아래 조작 띠(이동 패드 · 스킬 · 필살기) 바로 위에 **파티 띠**: 메인 M(크게, 왕관 · 체력) + 보조 1 · 2(작게, 번호 · 스킬 쿨 고리)를 가로로. 시너지 추적은 그 띠 위쪽에 작은 육각 문장 줄(켜진 것 밝게 + 「2/3」). 위쪽 띠는 유령 HP(크게) · 레벨 줄(지금처럼 크게) · 스테이지 · 일시정지만.
- 장면: 메인 「드라군」(마법 · 괴물), 보조 1 「청룡」(마법 · 괴물), 보조 2 「설녀」(마법 · 마도). 시너지 = 마법 3/3(강, 크게 빛남) · 괴물 2/3(켜짐) · 마도 1/2. 유령 HP 45/100. Lv 6.
[저장] Projects/AVSR/_exchange/in/mock_partyui_C_band.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
