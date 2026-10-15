$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 720x1280 비율) — 상점 · 소환 · 패스 **장면 기획 시안**.
[반드시 먼저 읽는다] Projects/AVSR/_exchange/store_scenes.md — 이 장면(S22)의 구성이 정답이다(무엇이 · 어디에 · 어떤 상태로). 0절 판매 구성표도 본다.
[화풍 기준 — 이어서 그린다]
- 소환(보라 제단): Projects/AVSR/_exchange/in/mock_summon_a.png — 하단 바 네 칸 HOST · PLAY · SUMMON · SHOP 이 이 시안 모양.
- 상점(남색 · 하늘 네온): Projects/AVSR/_exchange/in/mock_store_currency.png · 패스: Projects/AVSR/_exchange/in/mock_store_pass.png · 로비: Projects/AVSR/_exchange/in/mock_store_lobby.png
- 유령은 Projects/AVSR/Reference/Hosts/idle/ghost_anchor_user_x8.png 모습 그대로. 호스트 얼굴은 Assets/BaseResource/Growth/Faces/face_{키}.png 를 그대로 쓴다.
[공통] 글자 한국어 · 값 원화(₩) · 젬. 이 게임 도트 화풍. 값 · 수량 · 남은 횟수 · 상태(구매 완료 · 잠김 · 받기)가 또렷하게. 그림 아래 여백에 한국어 메모 3줄 이내.
[이번 장면 공통] store_scenes.md 끝의 「출전 방식」 절을 반드시 읽는다. 상단 바는 Projects/AVSR/_exchange/in/scene_s20_stamina.png(행동력 칸 있음) · 하단 바는 scene_s01_lobby.png 네 칸.
[장면 S22] 로비 자유안 — store_scenes.md S22 그대로. 지금 로비 배치에 얽매이지 말고 더 나은 배치를 제안해도 된다(단 챕터 정보 · 보스 · 클리어 보상 · 호스트 선택 · 설명 · 시작 버튼은 다 있어야 한다).
[저장] Projects/AVSR/_exchange/in/scene_s22_lobbyFree.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
