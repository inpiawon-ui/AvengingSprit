$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/guide_party_ui.md 를 끝까지 읽는다. 참고 그림은 Projects/AVSR/_exchange/ref/party_ui_1029/ 의 네 장(특히 pd_ref_trait_tracker.png = 목표 느낌, base_ingame_now.png = 지금 전투 화면).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 「빙의 파티」 새 시스템의 **인게임 UI 시안** 한 장. 크기 720 x 1280.
[메인 사망 선택창] 메인 「드라군」이 쓰러져 유령이 튕겨 나온 순간. 유령 HP 60 → 30(−30). 8초 안에 고른다 — 원형 시계 「5.2초」.
- 고를 것 세 줄: 「A. 근처 호스트에 새로 빙의 −20」 · 「B. 보조 1 「청룡」 을 메인으로 −5」(가장 추천처럼 금빛) · 「C. 그 자리에서 드라군 부활 −40」(유령 HP 가 모자라 어둡게 잠김). 줄마다 얼굴과 남는 유령 HP.
- 뒤 필드는 어둡게 멈춰 있고 쓰러진 드라군 자리에서 유령이 떠 있다. 위쪽 유령 HP 막대가 크게 빨간 감소 표시.
[저장] Projects/AVSR/_exchange/in/mock_partyui_death.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
