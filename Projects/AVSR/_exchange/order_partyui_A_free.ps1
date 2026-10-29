$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/guide_party_ui.md 를 끝까지 읽는다. 참고 그림은 Projects/AVSR/_exchange/ref/party_ui_1029/ 의 네 장(특히 pd_ref_trait_tracker.png = 목표 느낌, base_ingame_now.png = 지금 전투 화면).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 「빙의 파티」 새 시스템의 **인게임 UI 시안** 한 장. 크기 720 x 1280.
[안 A — 자유안] 네가 이 게임의 UI 아트 디렉터라면 이 시스템(유령 HP 예산 · 파티 메인 1 + 보조 2 · 시너지 9종)을 전투 화면에 어떻게 담겠나. 가이드의 정보 순서를 지키고, 롤토체스식 시너지 추적(육각 문장 + 「2/3」)을 반드시 넣는다. 그 밖의 배치는 자유.
- 장면: 1챕터 전투 중. 메인 「슬러거」(파워 · 무도가), 보조 1 「폭력배」(무기 · 갱단), 보조 2 「호퍼」(무기 · 갱단). 켜진 시너지 = 무기 2/3 · 갱단 2/3. 하나만 더 모으면 켜질 것 = 무도가 1/2 · 파워 1/2. 유령 HP 60/100. Lv 4.
- 필드에 보조 둘은 비치는 보랏빛 혼으로 메인 곁에 떠 있다. 적 호스트 하나가 「빙의 가능」 표시(유령 아이콘 + 터치 표시)를 달고 있다.
[저장] Projects/AVSR/_exchange/in/mock_partyui_A_free.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
