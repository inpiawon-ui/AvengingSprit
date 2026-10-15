$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 720x1280) — 새 모드 「망령의 밤 · 망령의 탑 (로비 입구)」의 UI 디자인 시안. **완성된 게임 화면 한 장**으로 그린다.

[먼저 볼 것]
- 배치(정답): Projects/AVSR/_exchange/ref/mode_ui_mock/layout_L.png — 기획이 정한 요소 · 순서 · 대략 위치 · 글자. **배치와 정보는 이대로**, 모양 · 재질 · 색 · 장식은 네가 새로 디자인한다(이 그림의 네모 칸 · 단색 버튼은 자리 표시일 뿐이다).
- 지금 게임 UI 한 벌(이 화풍으로 이어서): Projects/AVSR/_exchange/ref/mode_ui_mock/now_lobby.png(로비) · Projects/AVSR/_exchange/ref/mode_ui_mock/now_chapter_panel.png(챕터 · 호스트 패널) · Projects/AVSR/_exchange/ref/mode_ui_mock/now_result_popup.png(결과 창) · Projects/AVSR/_exchange/ref/mode_ui_mock/now_ingame_hud.png(인게임 HUD)
  — 남색 판 · 하늘색 네온 테두리 · 금 버튼 · 도트 글꼴 · 금속 볼트 디테일. 새 화면이 이 게임 안에 원래 있던 것처럼 보이게.

[공통]
- 글자는 한국어, 배치 그림의 문구를 그대로 쓴다(바꾸지 마라). 숫자 · 남은 횟수 · 층 · 시간이 한눈에 또렷하게.
- 글자는 칸 안에 넉넉히 — 칸 밖으로 넘치거나 잘리면 안 된다. 일본어 · 영어로 바뀌어도 들어갈 여백을 둔다.
- 이 게임 도트 화풍 그대로(매끈한 3D · 실사 · 벡터 평면 금지). 별 · 십자 반짝이를 뿌리지 않는다. 화면 전체를 한 색으로 물들이지 않는다.
- 캐릭터 초상 · 유닛 · 상성 보석 · 금화 · 젬 아이콘은 배치 그림 속 그림을 그대로 쓴다(새로 그리지 마라).
- 그림 아래 여백 없이 720x1280 한 화면. 메모는 그림에 쓰지 말고 출력 끝에 한국어 3줄로 답한다(무엇을 어떻게 · 왜).

[이 화면]
로비 PLAY 탭 — 지금 로비(now_lobby.png)에서 아래 두 모드 칸만 바뀐 모습. 나머지 로비는 그대로.
- 왼쪽 칸 「망령의 밤」: 지금 서바이벌 그림(몰려오는 적 떼)을 쓰고 자물쇠를 뗀다. 아래 「원거리의 날 · 입장 3/3」 칩 + 오른쪽 위 빨간 점.
- 오른쪽 칸 「망령의 탑」: **칸 그림을 새로** — 지금 포탑 그림 대신 「밤하늘로 솟은 유령 탑(층마다 창에 혼불, 꼭대기에 보스의 그림자), 탑 앞에 작은 유령」. 자물쇠를 뗀다.
  아래 「이번 주 12층 · 입장 2/3」 칩 + 오른쪽 위 빨간 점.
- 두 칸은 지금 칸과 같은 크기 · 같은 틀. 시나리오 칸과 한 벌로 보이게.

[저장] Projects/AVSR/_exchange/in/mock_mode_L.png

'@
codex exec --sandbox workspace-write --skip-git-repo-check -o "Projects/AVSR/_exchange/log_mode_ui_L.txt" $prompt
