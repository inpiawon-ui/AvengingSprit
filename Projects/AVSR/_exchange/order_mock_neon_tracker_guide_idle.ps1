$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — neon_tracker 연출 **시안**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다** — 이 게임의 빛은 「도트 모양 + 셰이더 광원 · 파티클」을 섞는다(툰 느낌만 그리지 말 것), 강약 · 연출 원칙.
[바탕] Projects/AVSR/_exchange/ref/base_neon_tracker.png — 지금 게임 화면. 창 · 칸 · 글자 · 캐릭터 · 물건 그림은 **그대로** 두고 그 위에 연출을 덧그린다.
[참고 그림] Projects/AVSR/_exchange/in/mock_lightning_puddle_guide_peak.png
PD 말: 「부분적으로 떨어지는 위험 · 스킬도 아니고 너무 과하면 안 된다(4챕터 번개 1차 반려) · 기믹은 적도 맞게 · 무대마다 모양이 달라야 한다」 · 공통: 「약하게 할 때는 약하게, 힘줄 때는 힘을 줘야지 · 게임 정보(글자 · 숫자)는 가리지 마 · 되도 않는 그림자 넣지 마」.
먼저 Projects/AVSR/_exchange/guide_neon_tracker.md 를 읽는다(목표 모습 · 박자 · 강약).
[장면] mock_neon_tracker_guide_idle — 가이드의 박자표대로. 방전 전 — 내 발밑으로 미끄러져 오는 빨간 원 1개(끌린 자국) + 제자리 원 2개가 차오르고, 원 위 공중에 끊어진 네온 전깃줄이 흔들리며 끝에서 분홍 불티가 조금씩 떨어지는 장면
- 크기는 바탕과 같은 720 x 1280.
[저장] Projects/AVSR/_exchange/in/mock_neon_tracker_guide_idle.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
