$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — lvpopup 연출 **시안**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다** — 이 게임의 빛은 「도트 모양 + 셰이더 광원 · 파티클」을 섞는다(툰 느낌만 그리지 말 것), 강약 · 연출 원칙.
[바탕] Projects/AVSR/_exchange/ref/levelup_mock/base_popup_idle.png — 지금 게임 화면. 창 · 칸 · 글자 · 캐릭터 · 물건 그림은 **그대로** 두고 그 위에 연출을 덧그린다.
[참고 그림] Projects/AVSR/_exchange/ref/levelup_mock/base_pick.png
PD 말: 「레벨업 팝업 — 머묾은 아주 은은하게(창 그림이 이미 화려하다), 힘은 카드를 고르는 순간에. 카드 테두리를 도는 빛 금지 · 고른 카드는 제자리에서 커진다」 · 공통: 「약하게 할 때는 약하게, 힘줄 때는 힘을 줘야지 · 게임 정보(글자 · 숫자)는 가리지 마 · 되도 않는 그림자 넣지 마」.
먼저 Projects/AVSR/_exchange/guide_levelup_fx.md 를 읽는다(목표 모습 · 박자 · 강약).
[장면] mock_lvpopup_guide_peak — 가이드의 박자표대로. **카드를 고르는 순간**(강) 한 장 — 왼쪽 카드를 골랐다고 치고, 그 카드 뒤에서 등급색(파랑) 빛이 확 터지고 반짝임 · 알갱이가 튐, 나머지 두 장은 흐려짐.
- 크기는 바탕과 같은 720 x 1280.
[저장] Projects/AVSR/_exchange/in/mock_lvpopup_guide_peak.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
