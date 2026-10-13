$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 와 Projects/AVSR/_exchange/guide_skill_cast.md(맨 위 「2차 방향」) 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720x1280) 인게임 — **호스트 스킬 시전 순간 연출 시안 2차**.
[바탕] Projects/AVSR/_exchange/ref/enemycast_mock/base_now.png — 지금 실제 게임 화면. 가운데 초록 옷 · 모자 남자 = **적 호스트 갱스터**, 아래 주황 머리 여자 = **플레이어가 탄 아마존**. HUD · 버튼 · 방 · 캐릭터 그림은 그대로(HUD 글자도 바꾸지 마라). 캐릭터 자세를 새로 그리지 않는다.
[스킬 아이콘 — 이 그림을 그대로 쓴다] Assets/BaseResource/HostSelectPanel/ultimateicon_gangster.png(갱스터 「일제 표식」) · ultimateicon_amazon.png(아마존 「도약 강타」)
[1차 시안 — 반려된 방향] Projects/AVSR/_exchange/ref/enemycast_mock/compare_cast_4.png — 발밑 마법진 · 차오르는 원은 **쓰지 않는다**.
PD 말: 「스킬 시전 시간은 따로 필요하지 않고, 먼가 포커싱이 되면서 스킬 아이콘 띄우면서 나가면 좋을 것 같은데, 마법진 말고 다른 식으로 · 다른 게임 참고해서」.
조건: 쓰는 순간 0.2~0.3초 안에 끝난다(기다리는 시간 없음) · 게임을 멈춘 듯 방 전체를 어둡게 하지 않는다 · 적과 플레이어가 **같은 방식**, 편은 색으로(적 = 붉은 위험색, 플레이어 = 푸른 · 금빛).
[방식] **몸 옆 미니 컷인**(쿠키런 · 원신 궁극기 컷인을 캐릭터 옆에 작게): 시전자 옆으로 작은 도트 띠 패널(스킬 아이콘 + 속도선, 글자 없음)이 0.3초 휙 지나간다. 플레이어는 지금 화면 큰 띠(초상 + 이름, 참고 Projects/AVSR/_exchange/ref/skillfx_now/cast_1.png)가 있으니, 적은 **그 큰 띠의 작은 판**이 몸 옆에 붙는 느낌. 오른쪽 플레이어 장면은 큰 띠가 걷히며 같은 미니 띠 · 아이콘이 몸으로 들어가는 순간.
그릴 것: 화면 두 장 나란히(각 720x1280, 합쳐 1440x1280) — 왼쪽 = **적 갱스터가 스킬을 쓰는 순간**, 오른쪽 = **플레이어 아마존이 스킬을 쓰는 순간**(같은 방식, 색만 다르게). 그림 아래 여백에 한국어 메모 3줄 이내(무엇이 어떻게 움직이나 · 참고한 게임).
[저장] Projects/AVSR/_exchange/in/mock_cast2_minicutin.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
