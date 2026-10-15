$env:OPENAI_API_medium = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 아래 4장만(도구 한도 5장).
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 와 Projects/AVSR/_exchange/guide_skill_mock_criteria.md(**PD 검수 기준 12개 — 반드시 지킨다**)를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720x1280, 탑다운) 인게임 — **영매 필살기 「골렘 강령」 연출 시안**.
[참조 1 — 바탕] Projects/AVSR/_exchange/ref/skillmock/base_medium.png — 게임 화면. 아래 가운데가 **플레이어가 탄 영매**, 위쪽에 적들. H영매가 지팡이로 바닥을 크게 짚어 거대 골렘을 불러낸다. 골렘은 Assets/BaseResource/Unit/golem 원작 골렘 그대로(크기도 원작 그대로 — 참조 2 시트 대신 원작 골렘 그림을 보고 그린다). 8초간 영매 앞에 서서 내리찍기로 둘레 적을 부수고 영매를 지킨다. · 방 · 적 그림은 그대로 둔다.
[참조 2 — 캐릭터 원작, 반드시 이 사람 그대로] Projects/AVSR/_exchange/ref/skillmock/orig_medium_sheet.png (원작 9장). 얼굴 · 옷 · 색 · 무기 · 크기가 원작과 같아야 한다. 바탕 속 크기 그대로.
[참조 3 — 화풍 · 퀄 기준] Projects/AVSR/_exchange/in/mock_skill_baseball_v2.png (PD 가 고른 시안).
[참조 4 — 소환되는 골렘 원작, 반드시 이 골렘 그대로] Projects/AVSR/_exchange/ref/skillmock/orig_golem_sheet.png (원작 골렘 6장 — 서기 · 공격 · 내리찍기). 모양 · 색 · 크기 그대로.
[이번 시안 — 필살기] 영매 「골렘 강령」(빙의할 때 1번 쓰는 필살기 — 가장 강한 연출). 영매가 지팡이로 바닥을 크게 짚어 거대 골렘을 불러낸다. 골렘은 Assets/BaseResource/Unit/golem 원작 골렘 그대로(크기도 원작 그대로 — 참조 2 시트 대신 원작 골렘 그림을 보고 그린다). 8초간 영매 앞에 서서 내리찍기로 둘레 적을 부수고 영매를 지킨다.
세 컷: 왼쪽 = 기 · 가운데 = 승 · 전 · 오른쪽 = 결.
기: 영매 앞 바닥에 푸른 강령 문양이 펼쳐지고, 그 안에서 원작 골렘이 바닥을 깨며 솟아오른다(돌 파편 · 푸른 빛). / 승 · 전: 골렘이 두 팔로 바닥을 내리찍어 둘레 적이 튕겨 나가고, 영매는 뒤에서 지팡이로 같이 쏜다(골렘과 영매가 같이 싸우는 그림). / 결: 8초가 끝나며 골렘이 마지막으로 크게 내리찍고 푸른 빛 가루로 부서져 사라진다.
꼭 지킬 것: 캐릭터 몸 동작에서 이펙트가 생겨 나간다 · 원인 → 결과가 세 컷으로 읽힌다 · 바깥 곁가지 디테일 · 이동은 잔상 · 방을 어둡게 하지 않는다 · H영매가 지팡이로 바닥을 크게 짚어 거대 골렘을 불러낸다. 골렘은 Assets/BaseResource/Unit/golem 원작 골렘 그대로(크기도 원작 그대로 — 참조 2 시트 대신 원작 골렘 그림을 보고 그린다). 8초간 영매 앞에 서서 내리찍기로 둘레 적을 부수고 영매를 지킨다. 를 가리지 않는다 · 글자 · 숫자 그리지 않는다.
그릴 것: 화면 세 장 나란히(각 720x1280, 합쳐 2160x1280). 그림 아래 여백에 한국어 메모 3줄 이내.
[저장] Projects/AVSR/_exchange/in/mock_skill_medium_golem.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
