$env:OPENAI_API_gangster = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 아래 3장만(도구 한도 5장).
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 와 Projects/AVSR/_exchange/guide_skill_mock_criteria.md(**PD 검수 기준 12개 — 반드시 지킨다**)를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720x1280, 탑다운) 인게임 — **갱스터 액티브 「일제 표식」 + 패시브 「처형 계약」 연출 시안**.
[참조 1 — 바탕] Projects/AVSR/_exchange/ref/skillmock/base_gangster.png — 게임 화면. 아래 가운데가 **플레이어가 탄 갱스터**, 위쪽에 적들. HUD · 방 · 적 그림은 그대로 둔다.
[참조 2 — 캐릭터 원작, 반드시 이 사람 그대로] Projects/AVSR/_exchange/ref/skillmock/orig_gangster_sheet.png (원작 9장). 얼굴 · 옷 · 색 · 무기 · 크기가 원작과 같아야 한다. 바탕 속 크기 그대로.
[참조 3 — 화풍 · 퀄 기준] Projects/AVSR/_exchange/in/mock_skill_baseball_v2.png (PD 가 고른 시안).
[이번 시안 — 액티브(일반 스킬) + 패시브] 갱스터 의 컨셉은 「청부 저격수 — 보스 킬러」. 액티브 「일제 표식」: 방 안 모든 적에게 3초간 표식을 새긴다. (필살기보다 **작고 가벼운** 일반 스킬 — 컷인 없이 바로 나간다, 화려함은 필살기의 70% 정도). 패시브 「처형 계약」: 표식이 붙은 적을 20% 로 즉사. 엘리트는 최대 체력 50%, 보스는 30%.
세 컷: 컷1 갱스터가 모자를 눌러 쓰며 권총을 휘두르자 방 안 적 머리 위마다 붉은 표식이 동시에 찍힌다 / 컷2 표식 붙은 적을 평타로 쏜다 / 컷3 **패시브 「처형 계약」** — 표식 붙은 적이 평타 한 발에 붉은 십자 섬광과 함께 즉사
꼭 지킬 것: 캐릭터 몸 동작에서 이펙트가 생겨 나간다 · 원인 → 결과가 세 컷으로 읽힌다 · 바깥 곁가지 디테일 · 소환물은 몸의 1.5배 이내 · 이동은 잔상 · 방을 어둡게 하지 않는다 · HUD 를 가리지 않는다 · 글자 · 숫자 그리지 않는다.
그릴 것: 화면 세 장 나란히(각 720x1280, 합쳐 2160x1280). 그림 아래 여백에 한국어 메모 3줄 이내.
[저장] Projects/AVSR/_exchange/in/mock_active_gangster.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
