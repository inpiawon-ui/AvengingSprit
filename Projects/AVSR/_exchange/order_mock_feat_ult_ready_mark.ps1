$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 「못 그렸다」고만 답하라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 탑다운, 세로 720x1280 모바일) **완성 화면 시안** 한 장. 바탕은 지금 게임 화면 그대로: Projects/AVSR/_exchange/ref/feature_mock/base_room_enemyhost.jpg (이 화면의 캐릭터 · 바닥 · HUD · 버튼은 그대로 두고 아래 요소만 더한다. 캐릭터 그림을 바꾸지 마라).
[더할 것] 방 안 **적 호스트(빼앗을 수 있는 몸 — 체력바 왼쪽에 파란 마크가 있는 캐릭터)** 중 「아직 필살기를 안 쓴 몸」 머리 위에 **「필살기 가능」 작은 표시** — 체력바 바로 위, 몸 크기의 1/3 이하의 보라 · 금빛 작은 별 문장(SPECIAL 버튼과 같은 보라 톤) + 은은하게 숨쉬는 작은 빛. 글자는 넣지 않는다. 체력바 · 숫자를 가리지 않는다. 잡몹(해골)에는 붙지 않는다. 과하지 않게 — 한눈에 「저 몸을 뺏으면 필살기를 쓸 수 있다」가 읽히는 정도.
[저장] Projects/AVSR/_exchange/in/mock_feat_ult_ready_mark.png (720x1280 세로)
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
