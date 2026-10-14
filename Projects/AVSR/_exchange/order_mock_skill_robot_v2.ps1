$env:OPENAI_API_robot = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 아래 3장만(도구 한도 5장).
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 와 Projects/AVSR/_exchange/guide_skill_mock_criteria.md(**PD 검수 기준 12개 — 반드시 지킨다**)를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720x1280, 탑다운) 인게임 — **로봇 필살기 「요새 모드」 연출 시안**.
[참조 1 — 바탕] Projects/AVSR/_exchange/ref/skillmock/base_robot.png — 게임 화면. 아래 가운데가 **플레이어가 탄 로봇**, 위쪽에 적들. HUD · 방 · 적 그림은 그대로 둔다.
[참조 2 — 캐릭터 원작, 반드시 이 사람 그대로] Projects/AVSR/_exchange/ref/skillmock/orig_robot_sheet.png (원작 9장). 얼굴 · 옷 · 색 · 무기 · 크기가 원작과 같아야 한다. 바탕 속 크기 그대로.
[참조 3 — 화풍 · 퀄 기준] Projects/AVSR/_exchange/in/mock_skill_baseball_v2.png (PD 가 고른 시안).
[이번 시안 — 필살기] 로봇 「요새 모드」(빙의할 때 1번 쓰는 필살기 — 가장 강한 연출). 로봇이 제자리에 다리를 박고 포대로 변형한다. 포탑 3대가 둘레에 내려앉고 로봇과 함께 사방 일제 사격.
세 컷: 왼쪽 = 기 · 가운데 = 승 · 전 · 오른쪽 = 결.
기: 로봇이 바닥에 발을 박으며 어깨 · 팔이 열려 포구가 드러난다(원작 로봇 모습 그대로, 변형은 열리는 정도) · 둘레 세 자리에 포탑이 떨어져 펼쳐진다. / 승 · 전: 로봇과 포탑 셋이 사방으로 일제 사격 — 탄줄이 방 전체로 퍼진다 · 탄피 · 열기. / 결: 마지막에 로봇 가슴에서 굵은 빔 한 발 + 포탑이 접히며 연기.
꼭 지킬 것: 캐릭터 몸 동작에서 이펙트가 생겨 나간다 · 원인 → 결과가 세 컷으로 읽힌다 · 바깥 곁가지 디테일 · 소환물은 몸의 1.5배 이내 · 이동은 잔상 · 방을 어둡게 하지 않는다 · HUD 를 가리지 않는다 · 글자 · 숫자 그리지 않는다.
그릴 것: 화면 세 장 나란히(각 720x1280, 합쳐 2160x1280). 그림 아래 여백에 한국어 메모 3줄 이내.
[저장] Projects/AVSR/_exchange/in/mock_skill_robot_v2.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
