$env:OPENAI_API_dragon_blue = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 아래 3장만(도구 한도 5장).
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 와 Projects/AVSR/_exchange/guide_skill_mock_criteria.md(**PD 검수 기준 12개 — 반드시 지킨다**)를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720x1280, 탑다운) 인게임 — **호스트 액티브 스킬 연출 시안**(던전앤파이터 스킬을 참고해 새로 짠 스킬).
[참조 1 — 바탕] Projects/AVSR/_exchange/ref/skillmock/base_dragon_blue.png — 게임 화면. 아래 가운데가 **플레이어가 탄 청룡**, 위쪽에 적들. HUD · 방 · 적 그림은 그대로 둔다.
[참조 2 — 캐릭터 원작, 반드시 이 사람 그대로] Projects/AVSR/_exchange/ref/skillmock/orig_dragon_blue_sheet.png (원작 9장: 행 아래 · 옆 · 위, 열 서기 · 공격1 · 공격2). 새 자세를 그려도 얼굴 · 옷 · 색 · 무기 · 크기가 원작과 같아야 한다. 바탕 속 크기 그대로.
[참조 3 — 화풍 · 퀄 기준] Projects/AVSR/_exchange/in/mock_skill_baseball_v2.png (PD 가 고른 시안 — 이 정도 밀도 · 도트 결 · 빛).
[스킬] 청룡 「번개 구슬」 (던파 「나선의 넨(위성)」 참고). 5초간 몸 주위를 번개 구슬 3개가 돌며 닿는 적에게 번개를 치고, 끝에 낙뢰가 떨어진다.
세 컷: 왼쪽 = **기(시작)** · 가운데 = **승 · 전(한창 — 가장 볼거리 많은 순간)** · 오른쪽 = **결(마무리 한 방 — 가장 강한 박자)**.
기: 청룡이 손을 들자 손에서 번개 구슬 3개가 하나씩 튀어나와 몸 둘레 궤도에 오른다. / 승 · 전: 구슬이 몸 둘레를 돌며 가까운 적에게 번개 줄기를 꽂는다(구슬 궤적 · 맞은 적 하얀 섬광). / 결: 구슬 셋이 하늘로 솟아 합쳐지며 가장 많은 적 위로 큰 낙뢰.
꼭 지킬 것: 캐릭터 몸 동작에서 이펙트가 생겨 나간다(이펙트를 몸 위에 얹기만 하지 않는다) · 원인 → 결과가 세 컷으로 읽힌다 · 바깥 곁가지 디테일 · 소환물은 몸의 1.5배 이내 · 이동은 잔상 · 방을 어둡게 하지 않는다 · HUD 를 가리지 않는다 · 글자 · 숫자 그리지 않는다.
그릴 것: 화면 세 장 나란히(각 720x1280, 합쳐 2160x1280). 그림 아래 여백에 한국어 메모 3줄 이내(무엇이 · 몇 초 · 어디서 강하게).
[저장] Projects/AVSR/_exchange/in/mock_skill_dragon_blue.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
