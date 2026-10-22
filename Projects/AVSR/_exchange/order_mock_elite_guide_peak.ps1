$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — elite 연출 **시안**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다** — 이 게임의 빛은 「도트 모양 + 셰이더 광원 · 파티클」을 섞는다(툰 느낌만 그리지 말 것), 강약 · 연출 원칙.
[바탕] Projects/AVSR/_exchange/ref/elite_mock/base_ingame.png — 지금 게임 화면. 창 · 칸 · 글자 · 캐릭터 · 물건 그림은 **그대로** 두고 그 위에 연출을 덧그린다.
PD 말: 「엘리트는 제안대로 — 같은 몬스터의 강한 판, 크기 1.25배」 · 공통: 「약하게 할 때는 약하게, 힘줄 때는 힘을 줘야지 · 게임 정보(글자 · 숫자)는 가리지 마 · 되도 않는 그림자 넣지 마」.
먼저 Projects/AVSR/_exchange/guide_elite.md 를 읽는다(목표 모습 · 박자 · 강약).
[장면] mock_elite_guide_peak — 가이드의 박자표대로. 엘리트 고릴라가 방에 나타나는 순간 — 발밑 고리가 한 번 퍼지며 켜진다
- 크기는 바탕과 같은 720x1280.
[저장] Projects/AVSR/_exchange/in/mock_elite_guide_peak.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
