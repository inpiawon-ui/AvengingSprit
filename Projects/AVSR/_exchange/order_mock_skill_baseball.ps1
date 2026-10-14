$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 와 Projects/AVSR/_exchange/guide_skill_rework.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720x1280, 탑다운) 인게임 — **호스트 액티브 스킬 연출 시안**(던전앤파이터 스킬을 참고해 새로 짠 스킬).
[바탕] Projects/AVSR/_exchange/ref/skillmock/base_baseball.png — 게임 화면. 아래 가운데가 **플레이어가 탄 슬러거**, 위쪽에 적들(해골 · 박쥐 · 고철 사수 · 적 호스트). HUD · 방 · 적 그림은 그대로 둔다(HUD 글자는 신경 쓰지 마라).
[캐릭터 원작 — 반드시 이 사람 그대로] Assets/BaseResource/Unit/baseball 폴더의 스프라이트(걷기 · 공격 · 방향별). 새 자세를 그려도 얼굴 · 옷 · 색 · 무기가 원작과 같아야 한다. 크기도 바탕 속 크기 그대로.
PD 말: 「던파 스킬(레인저 난사 · 스트라이커 라이트닝 댄스 등)을 접목하면 재밌을 것 같다 · 게임 퀄을 위해서라면 동작 애니도 추가해도 괜찮다 · 그림이 그려져야 결정할 수 있다」.
[스킬] 슬러거 「풀스윙 회오리」 — 던파 퇴마사 「대회전격」. 배트를 크게 휘두르며 회전하면서 이동해 주변 적을 쓸고, 날아오는 적 탄을 쳐 내 되받아친다(되받아친 탄은 금빛). 마지막에 풀스윙 홈런 — 맞은 적이 하늘 멀리 날아가고 「딱!」 충격.
그릴 것: 화면 세 장 나란히(각 720x1280, 합쳐 2160x1280) — 왼쪽 = **시작**, 가운데 = **한창(가장 볼거리 많은 순간)**, 오른쪽 = **마무리 한 방(가장 강한 박자)**. 캐릭터가 실제로 움직이는 경로 · 잔상이 보이게. 맞은 적은 하얀 섬광 · 밀려남.
그림 아래 여백에 한국어 메모 3줄 이내(무엇이 · 몇 초 · 어디서 강하게).
방을 어둡게 하지 않는다 · 화면 전체를 덮지 않는다(캐릭터 둘레와 맞는 적 위주).
[저장] Projects/AVSR/_exchange/in/mock_skill_baseball.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
