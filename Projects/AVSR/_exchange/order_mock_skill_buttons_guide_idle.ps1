$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — skill_buttons 연출 **시안**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다** — 이 게임의 빛은 「도트 모양 + 셰이더 광원 · 파티클」을 섞는다(툰 느낌만 그리지 말 것), 강약 · 연출 원칙.
[바탕] Projects/AVSR/_exchange/ref/skill_buttons_mock/base_fight.png — 지금 게임 화면. 창 · 칸 · 글자 · 캐릭터 · 물건 그림은 **그대로** 두고 그 위에 연출을 덧그린다.
[가이드] Projects/AVSR/_exchange/guide_skill_buttons.md — 먼저 읽는다(액티브 = 가볍고 빠른 기술 버튼 · 필살기 = 무겁고 특별한 결정타 버튼, 하나의 묶음).
[아이콘] 버튼 속 아이콘 · 장식은 **지금 우리 게임 바탕에 어울리는 것**(PD 「우리 게임 베이스에 어울릴 만한 아이콘이 들어가야 해」) — 도트 픽셀 화풍 · RE:BORN 키트 그림(Assets/BaseResource/ReBornKit/Hud · Icons)과 바탕 캡처 속 지금 스킬 아이콘을 기준으로. 매끈한 고해상도 · 다른 게임풍 아이콘 금지.
PD 말: 「액티브 스킬이랑 필살기가 너무 그냥 떨어져 있어서 뭐가 뭔지 느낌이 약하다 · 뭔가 액티브 스킬과 필살기라는 느낌의 시안이 필요하다」 · 공통: 「약하게 할 때는 약하게, 힘줄 때는 힘을 줘야지 · 게임 정보(글자 · 숫자)는 가리지 마 · 되도 않는 그림자 넣지 마」.
먼저 Projects/AVSR/_exchange/guide_skill_buttons.md 를 읽는다(목표 모습 · 박자 · 강약).
[장면] mock_skill_buttons_guide_idle — 가이드의 박자표대로. 같은 디자인으로 액티브를 막 쓴 직후 한 장 — 액티브 아이콘 원 전체가 어둡게 덮이고 시계 방향으로 걷히는 중, 가운데 남은 초 「5」. 필살기는 그대로 준비 상태
- 크기는 바탕과 같은 720 x 1280.
[저장] Projects/AVSR/_exchange/in/mock_skill_buttons_guide_idle.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
