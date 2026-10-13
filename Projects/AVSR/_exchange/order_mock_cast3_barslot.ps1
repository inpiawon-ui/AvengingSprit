$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 와 Projects/AVSR/_exchange/guide_skill_cast.md(맨 위 「3차 방향」) 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720x1280) 인게임 — **적 호스트가 스킬을 쓸 때의 작은 표시** 시안 3차.
[바탕] Projects/AVSR/_exchange/ref/enemycast_mock/base_multi.png — 게임 화면. 적 호스트가 셋: 가운데 초록 옷 **갱스터**, 오른쪽 위 보라 옷 **폭력배**, 왼쪽 파란 **닌자**(각자 머리 위 체력바). 아래 주황 머리 여자 = 플레이어. HUD · 방 · 캐릭터 그림은 그대로(글자 바꾸지 마라). 캐릭터 자세를 새로 그리지 않는다.
[스킬 아이콘 — 이 그림을 그대로 작게 쓴다] Assets/BaseResource/HostSelectPanel/ultimateicon_gangster.png · ultimateicon_thug.png · ultimateicon_ninja.png (64x64 도트)
[반려된 2차 시안] Projects/AVSR/_exchange/ref/enemycast_mock/compare_cast2_4.png — 집중선 · 조준 괄호 · 큰 말풍선은 **너무 과하다**. 그보다 훨씬 작고 조용하게.
PD 말: 「적으로 나올 때 한 마리 이상일 수 있어서 저렇게 하면 안 될 것 같고, 캐릭터 반짝이고 캐릭터 위에 저거보다 작은 스킬 아이콘으로 식별할 정도로 · 저건 너무 과하네」.
조건: 몸 크기(약 50x60) 둘레에서만 일어난다 · 화면을 덮지 않는다 · 아이콘은 작게(지름 26~34px 정도, 체력바 근처) · 적이 둘이 동시에 써도 시끄럽지 않다 · 평타 예고(몸이 노랗게 물듦)와는 구별된다. 플레이어 쪽은 그리지 않는다.
[방식] **체력바 옆 칸이 스킬 아이콘으로**: 머리 위에 새로 띄우지 않는다. 체력바 왼쪽 작은 아이콘 칸(지금 속성 표시 자리)이 쓰는 순간 그 호스트의 스킬 아이콘으로 바뀌며 반짝이고, 몸은 0.1초 하얗게 번쩍. 1초 뒤 원래 표시로 돌아간다. 가장 조용한 안.
그릴 것: 화면 두 장 나란히(각 720x1280, 합쳐 1440x1280) — 왼쪽 = **갱스터 혼자 스킬을 쓰는 순간**, 오른쪽 = **갱스터와 닌자가 거의 동시에 쓰는 순간**(둘이 함께 떠도 깔끔한지). 그림 아래 여백에 **쓰는 몸을 3배로 키운 작은 확대 칸** 하나와 한국어 메모 3줄 이내(무엇이 · 몇 초 · 어떻게 사라지나).
[저장] Projects/AVSR/_exchange/in/mock_cast3_barslot.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
