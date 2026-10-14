$env:OPENAI_API_death = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 아래 3장만(도구 한도 5장).
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 와 Projects/AVSR/_exchange/guide_skill_mock_criteria.md(**PD 검수 기준 12개 — 반드시 지킨다**)를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720x1280, 탑다운) 인게임 — **사신 (히든) 액티브 「사신의 시간」 + 패시브 「망자 소집」 연출 시안**.
[참조 1 — 바탕] Projects/AVSR/_exchange/ref/skillmock/base_death.png — 게임 화면. 아래 가운데가 **플레이어가 탄 사신 (히든)**, 위쪽에 적들. HUD · 방 · 적 그림은 그대로 둔다.
[참조 2 — 캐릭터 원작, 반드시 이 사람 그대로] Projects/AVSR/_exchange/ref/skillmock/orig_death_sheet.png (원작 9장). 얼굴 · 옷 · 색 · 무기 · 크기가 원작과 같아야 한다. 바탕 속 크기 그대로.
[참조 3 — 화풍 · 퀄 기준] Projects/AVSR/_exchange/in/mock_skill_baseball_v2.png (PD 가 고른 시안).
[이번 시안 — 액티브(일반 스킬) + 패시브] 사신 (히든) 의 컨셉은 「죽음의 군주」. 액티브 「사신의 시간」: 3초간 때리는 적이 죽는다. 중간 보스는 절반, 보스는 열에 하나. (필살기보다 **작고 가벼운** 일반 스킬 — 컷인 없이 바로 나간다, 화려함은 필살기의 70% 정도). 패시브 「망자 소집」: 적을 잡을 때마다 그 자리에서 해골이 일어선다.
세 컷: 컷1 사신 둘레에 검푸른 시계 문양, 낫이 빛난다(3초) / 컷2 낫이 닿는 적이 한 방에 영혼째 베여 쓰러진다 / 컷3 **패시브 「망자 소집」** — 쓰러진 자리마다 해골이 일어나 사신 편에 선다
꼭 지킬 것: 캐릭터 몸 동작에서 이펙트가 생겨 나간다 · 원인 → 결과가 세 컷으로 읽힌다 · 바깥 곁가지 디테일 · 소환물은 몸의 1.5배 이내 · 이동은 잔상 · 방을 어둡게 하지 않는다 · HUD 를 가리지 않는다 · 글자 · 숫자 그리지 않는다.
그릴 것: 화면 세 장 나란히(각 720x1280, 합쳐 2160x1280). 그림 아래 여백에 한국어 메모 3줄 이내.
[저장] Projects/AVSR/_exchange/in/mock_active_death.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
