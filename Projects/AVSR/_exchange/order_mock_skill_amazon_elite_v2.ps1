$env:OPENAI_API_amazon_elite = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 아래 3장만(도구 한도 5장).
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 와 Projects/AVSR/_exchange/guide_skill_mock_criteria.md(**PD 검수 기준 12개 — 반드시 지킨다**)를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720x1280, 탑다운) 인게임 — **아마존 정예 필살기 「역린」 연출 시안**.
[참조 1 — 바탕] Projects/AVSR/_exchange/ref/skillmock/base_amazon_elite.png — 게임 화면. 아래 가운데가 **플레이어가 탄 아마존 정예**, 위쪽에 적들. HUD · 방 · 적 그림은 그대로 둔다.
[참조 2 — 캐릭터 원작, 반드시 이 사람 그대로] Projects/AVSR/_exchange/ref/skillmock/orig_amazon_elite_sheet.png (원작 9장). 얼굴 · 옷 · 색 · 무기 · 크기가 원작과 같아야 한다. 바탕 속 크기 그대로.
[참조 3 — 화풍 · 퀄 기준] Projects/AVSR/_exchange/in/mock_skill_baseball_v2.png (PD 가 고른 시안).
[이번 시안 — 필살기] 아마존 정예 「역린」(빙의할 때 1번 쓰는 필살기 — 가장 강한 연출). 3초 동안 맞는 피해를 몸에 모은다(맞아도 버틴다). 3초가 끝나면 모은 만큼 주변으로 되돌려 터뜨린다.
세 컷: 왼쪽 = 기 · 가운데 = 승 · 전 · 오른쪽 = 결.
기: 아마존 정예가 자세를 낮추고 버틴다 — 몸 둘레에 붉은 기운이 감기기 시작. / 승 · 전: 적들의 탄 · 주먹이 맞을 때마다 붉은 빛 조각이 몸으로 빨려 들어 쌓인다(몸의 붉은 기운이 점점 커짐, 몸의 1.5배 이내). / 결: 아마존 정예가 땅을 내리치자 모은 붉은 기운이 둘레로 폭발 — 둘레 적들이 튕겨 나간다 · 바닥 균열.
꼭 지킬 것: 캐릭터 몸 동작에서 이펙트가 생겨 나간다 · 원인 → 결과가 세 컷으로 읽힌다 · 바깥 곁가지 디테일 · 소환물은 몸의 1.5배 이내 · 이동은 잔상 · 방을 어둡게 하지 않는다 · HUD 를 가리지 않는다 · 글자 · 숫자 그리지 않는다.
그릴 것: 화면 세 장 나란히(각 720x1280, 합쳐 2160x1280). 그림 아래 여백에 한국어 메모 3줄 이내.
[저장] Projects/AVSR/_exchange/in/mock_skill_amazon_elite_v2.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
