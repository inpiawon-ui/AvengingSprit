$env:OPENAI_API_snowwoman = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 아래 3장만(도구 한도 5장).
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 와 Projects/AVSR/_exchange/guide_skill_mock_criteria.md(**PD 검수 기준 12개 — 반드시 지킨다**)를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720x1280, 탑다운) 인게임 — **호스트 액티브 스킬 연출 시안**(던전앤파이터 스킬을 참고해 새로 짠 스킬).
[참조 1 — 바탕] Projects/AVSR/_exchange/ref/skillmock/base_snowwoman.png — 게임 화면. 아래 가운데가 **플레이어가 탄 설녀**, 위쪽에 적들. HUD · 방 · 적 그림은 그대로 둔다.
[참조 2 — 캐릭터 원작, 반드시 이 사람 그대로] Projects/AVSR/_exchange/ref/skillmock/orig_snowwoman_sheet.png (원작 9장: 행 아래 · 옆 · 위, 열 서기 · 공격1 · 공격2). 새 자세를 그려도 얼굴 · 옷 · 색 · 무기 · 크기가 원작과 같아야 한다. 바탕 속 크기 그대로.
[참조 3 — 화풍 · 퀄 기준] Projects/AVSR/_exchange/in/mock_skill_baseball_v2.png (PD 가 고른 시안 — 이 정도 밀도 · 도트 결 · 빛).
[스킬] 설녀 「얼음 진」 (던파 「아크틱 피스트(설치 빙결)」 참고). 앞에 얼음 진을 깔아 얼음 기둥이 솟고 적을 얼린다. 깔고 바로 움직일 수 있다.
세 컷: 왼쪽 = **기(시작)** · 가운데 = **승 · 전(한창 — 가장 볼거리 많은 순간)** · 오른쪽 = **결(마무리 한 방 — 가장 강한 박자)**.
기: 설녀가 소매를 휘두르자 냉기가 앞 바닥으로 퍼지며 얼음 문양이 깔린다. / 승 · 전: 문양에서 얼음 기둥이 차례로 솟아 적을 가두고, 적이 얼어붙는다(서리 · 하얀 김). / 결: 얼음 기둥이 한꺼번에 깨지며 얼음 파편이 튄다.
꼭 지킬 것: 캐릭터 몸 동작에서 이펙트가 생겨 나간다(이펙트를 몸 위에 얹기만 하지 않는다) · 원인 → 결과가 세 컷으로 읽힌다 · 바깥 곁가지 디테일 · 소환물은 몸의 1.5배 이내 · 이동은 잔상 · 방을 어둡게 하지 않는다 · HUD 를 가리지 않는다 · 글자 · 숫자 그리지 않는다.
그릴 것: 화면 세 장 나란히(각 720x1280, 합쳐 2160x1280). 그림 아래 여백에 한국어 메모 3줄 이내(무엇이 · 몇 초 · 어디서 강하게).
[저장] Projects/AVSR/_exchange/in/mock_skill_snowwoman.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
