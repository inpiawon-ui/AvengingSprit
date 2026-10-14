$env:OPENAI_API_ninja_chain = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 아래 3장만(도구 한도 5장).
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 와 Projects/AVSR/_exchange/guide_skill_mock_criteria.md(**PD 검수 기준 12개 — 반드시 지킨다**)를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720x1280, 탑다운) 인게임 — **닌자(사슬) 필살기 「사슬 처형」 연출 시안**.
[참조 1 — 바탕] Projects/AVSR/_exchange/ref/skillmock/base_ninja_chain.png — 게임 화면. 아래 가운데가 **플레이어가 탄 닌자(사슬)**, 위쪽에 적들. HUD · 방 · 적 그림은 그대로 둔다.
[참조 2 — 캐릭터 원작, 반드시 이 사람 그대로] Projects/AVSR/_exchange/ref/skillmock/orig_ninja_chain_sheet.png (원작 9장). 얼굴 · 옷 · 색 · 무기 · 크기가 원작과 같아야 한다. 바탕 속 크기 그대로.
[참조 3 — 화풍 · 퀄 기준] Projects/AVSR/_exchange/in/mock_skill_baseball_v2.png (PD 가 고른 시안).
[이번 시안 — 필살기] 닌자(사슬) 「사슬 처형」(빙의할 때 1번 쓰는 필살기 — 가장 강한 연출). 가장 강한 적 하나에게 사슬을 던져 감고, 닌자 앞으로 끌어와 연타한 뒤 내려찍는다.
세 컷: 왼쪽 = 기 · 가운데 = 승 · 전 · 오른쪽 = 결.
기: 닌자가 사슬낫을 던져 가장 강한 적 하나에게 감는다(사슬이 쭉 뻗는 궤적 · 감기는 금속 불꽃). / 승 · 전: 사슬을 당기자 그 적이 잔상을 끌며 닌자 앞으로 끌려오고, 닌자가 낫으로 연타(분홍 베기 궤적 여러 줄). / 결: 뛰어올라 끌어온 적을 바닥에 내리찍는다 — 바닥 균열 · 충격 고리. 다른 적은 끼어들지 않는다.
꼭 지킬 것: 캐릭터 몸 동작에서 이펙트가 생겨 나간다 · 원인 → 결과가 세 컷으로 읽힌다 · 바깥 곁가지 디테일 · 소환물은 몸의 1.5배 이내 · 이동은 잔상 · 방을 어둡게 하지 않는다 · HUD 를 가리지 않는다 · 글자 · 숫자 그리지 않는다.
그릴 것: 화면 세 장 나란히(각 720x1280, 합쳐 2160x1280). 그림 아래 여백에 한국어 메모 3줄 이내.
[저장] Projects/AVSR/_exchange/in/mock_skill_ninja_chain_v2.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
