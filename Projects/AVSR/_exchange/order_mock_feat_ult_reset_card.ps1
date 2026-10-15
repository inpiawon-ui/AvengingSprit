$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 「못 그렸다」고만 답하라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 탑다운, 세로 720x1280 모바일) **완성 화면 시안** 한 장. 바탕은 지금 게임 화면 그대로: Projects/AVSR/_exchange/ref/feature_mock/base_levelup.jpg (이 화면의 캐릭터 · 바닥 · HUD · 버튼은 그대로 두고 아래 요소만 더한다. 캐릭터 그림을 바꾸지 마라).
[더할 것] 레벨업 카드 셋 중 **가운데 카드를 「필살기 초기화」 희귀(RARE) 카드**로 바꾼 화면 — 카드 틀 · 레이아웃 · 글자 칸 위치는 지금 카드와 같게, 등급 띠는 RARE(파랑). 아이콘: SPECIAL 버튼의 보라 필살기 문양에 둥근 화살표(다시 채움)가 감긴 도트 아이콘. 카드 이름 칸 「필살기 초기화」, 설명 칸 「지금 몸의 필살기를 다시 쓸 수 있다」(한국어, 칸 안에 다 들어가게).
[저장] Projects/AVSR/_exchange/in/mock_feat_ult_reset_card.png (720x1280 세로)
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
