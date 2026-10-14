$env:OPENAI_API_hopper_smg = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 아래 3장만(도구 한도 5장).
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 와 Projects/AVSR/_exchange/guide_skill_mock_criteria.md(**PD 검수 기준 12개 — 반드시 지킨다**)를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720x1280, 탑다운) 인게임 — **호퍼(기관단총) 액티브 「백스텝 사격」 + 패시브 「충격 반동」 연출 시안**.
[참조 1 — 바탕] Projects/AVSR/_exchange/ref/skillmock/base_hopper_smg.png — 게임 화면. 아래 가운데가 **플레이어가 탄 호퍼(기관단총)**, 위쪽에 적들. HUD · 방 · 적 그림은 그대로 둔다.
[참조 2 — 캐릭터 원작, 반드시 이 사람 그대로] Projects/AVSR/_exchange/ref/skillmock/orig_hopper_smg_sheet.png (원작 9장). 얼굴 · 옷 · 색 · 무기 · 크기가 원작과 같아야 한다. 바탕 속 크기 그대로.
[참조 3 — 화풍 · 퀄 기준] Projects/AVSR/_exchange/in/mock_skill_baseball_v2.png (PD 가 고른 시안).
[이번 시안 — 액티브(일반 스킬) + 패시브] 호퍼(기관단총) 의 컨셉은 「돌파 특공」. 액티브 「백스텝 사격」: 뒤로 크게 뛰며 앞으로 쏟아붓는다(붙은 적에게서 빠지는 기술). (필살기보다 **작고 가벼운** 일반 스킬 — 컷인 없이 바로 나간다, 화려함은 필살기의 70% 정도). 패시브 「충격 반동」: 10% 로 맞은 적을 밀어낸다.
세 컷: 컷1 근접 적이 붙자 호퍼가 뒤로 크게 도약(잔상) / 컷2 공중에서 앞으로 기관단총 난사, 탄피 / 컷3 **패시브 「충격 반동」** — 맞은 적이 뒤로 튕겨 밀려난다
꼭 지킬 것: 캐릭터 몸 동작에서 이펙트가 생겨 나간다 · 원인 → 결과가 세 컷으로 읽힌다 · 바깥 곁가지 디테일 · 소환물은 몸의 1.5배 이내 · 이동은 잔상 · 방을 어둡게 하지 않는다 · HUD 를 가리지 않는다 · 글자 · 숫자 그리지 않는다.
그릴 것: 화면 세 장 나란히(각 720x1280, 합쳐 2160x1280). 그림 아래 여백에 한국어 메모 3줄 이내.
[저장] Projects/AVSR/_exchange/in/mock_active_hopper_smg.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
