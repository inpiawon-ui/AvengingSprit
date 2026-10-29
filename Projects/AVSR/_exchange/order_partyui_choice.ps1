$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/guide_party_ui.md 를 끝까지 읽는다. 참고 그림은 Projects/AVSR/_exchange/ref/party_ui_1029/ 의 네 장(특히 pd_ref_trait_tracker.png = 목표 느낌, base_ingame_now.png = 지금 전투 화면).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 「빙의 파티」 새 시스템의 **인게임 UI 시안** 한 장. 크기 720 x 1280.
[빙의 선택창] 전투가 잠깐 멈춘 위에 뜨는 창. 적 호스트 「청룡」(마법 · 괴물)을 터치한 순간. 지금 파티: 메인 「드라군」 · 보조 1 「샐러맨더」 · 보조 2 「설녀」. 유령 HP 55/100.
- 창 위쪽에 청룡 얼굴 · 이름 · 태그 두 개(마법 · 괴물) · 보조일 때 자동 스킬 이름.
- 고를 것 네 줄: 「메인과 바꾸기 −25 (남은 2회)」 · 「보조 1 과 바꾸기 −15 (남은 1회)」 · 「보조 2 와 바꾸기 −15」 · 「놔두기」. 각 줄에 바뀔 자리의 얼굴과 **바뀌는 시너지 미리보기**(예: 「+ 괴물 3/3 · − 마도 1/2」 를 육각 문장으로).
- 남은 유령 HP 가 각 비용을 치르면 얼마가 되는지도 보인다. 창은 화면의 아래 2/3 안쪽, 위쪽 HUD 는 보이게 둔다.
[저장] Projects/AVSR/_exchange/in/mock_partyui_choice.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
