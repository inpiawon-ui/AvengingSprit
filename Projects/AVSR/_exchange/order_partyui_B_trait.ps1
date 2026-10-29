$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/guide_party_ui.md 를 끝까지 읽는다. 참고 그림은 Projects/AVSR/_exchange/ref/party_ui_1029/ 의 네 장(특히 pd_ref_trait_tracker.png = 목표 느낌, base_ingame_now.png = 지금 전투 화면).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 「빙의 파티」 새 시스템의 **인게임 UI 시안** 한 장. 크기 720 x 1280.
[안 B — 시너지 열] 왼쪽 가장자리에 pd_ref_trait_tracker.png 처럼 **세로 시너지 열**(육각 문장 + 이름 + 「모은 수 / 다음 단계」, 켜진 것 금빛 · 아직인 것 어둡게)을 세운다. 위쪽 띠에 유령 HP(크게) · 레벨 줄(지금처럼 크게) · 스테이지 · 일시정지, 오른쪽 위에 파티 초상화 M · 1 · 2 를 세로로.
- 장면: 메인 「슬러거」, 보조 1 「폭력배」, 보조 2 「호퍼」. 시너지 = 무기 2/3(켜짐) · 갱단 2/3(켜짐) · 무도가 1/2 · 파워 1/2 · 균형 2/3. 유령 HP 60/100. Lv 4. 메인 초상화 아래 체력 막대, 보조 초상화에 스킬 쿨 고리.
[저장] Projects/AVSR/_exchange/in/mock_partyui_B_trait.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
