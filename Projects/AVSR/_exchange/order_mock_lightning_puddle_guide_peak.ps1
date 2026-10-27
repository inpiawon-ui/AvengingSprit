$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — lightning_puddle 연출 **시안**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다** — 이 게임의 빛은 「도트 모양 + 셰이더 광원 · 파티클」을 섞는다(툰 느낌만 그리지 말 것), 강약 · 연출 원칙.
[바탕] Projects/AVSR/_exchange/ref/base_lightning_puddle.png — 지금 게임 화면. 창 · 칸 · 글자 · 캐릭터 · 물건 그림은 **그대로** 두고 그 위에 연출을 덧그린다.
PD 말: 「큰 범위 번개가 아니라 부분적으로 떨어지는 번개 · 스킬도 아니고 너무 과하다(1차 반려) · 기믹은 적도 맞게」 · 공통: 「약하게 할 때는 약하게, 힘줄 때는 힘을 줘야지 · 게임 정보(글자 · 숫자)는 가리지 마 · 되도 않는 그림자 넣지 마」.
먼저 Projects/AVSR/_exchange/guide_lightning_puddle.md 를 읽는다(목표 모습 · 박자 · 강약).
[장면] mock_lightning_puddle_guide_peak — 가이드의 박자표대로. 작은 낙뢰 2곳이 따로 떨어지는 순간 — 하나는 맨바닥 작은 원(캐릭터 1명 크기), 하나는 작은 물웅덩이에 떨어져 그 웅덩이만 지직 감전되고 안에 선 적 1명이 경련. 나머지 한 곳은 빨간 작은 예고 원이 차오르는 중
- 크기는 바탕과 같은 920 x 690.
[저장] Projects/AVSR/_exchange/in/mock_lightning_puddle_guide_peak.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
