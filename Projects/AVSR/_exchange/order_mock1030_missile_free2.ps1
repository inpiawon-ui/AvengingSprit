$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720 x 1280) — **완성 화면 시안** 한 장.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(빛은 도트 모양 + 셰이더 광원 · 파티클, 강약, 연출 원칙).
화풍 · 화면 구성 기준: Projects/AVSR/_exchange/in/mock1029_ult_mg_a.png (방금 통과한 같은 코만도 필살기 시안 — 이 정도의 퀄 · 도트 질감).
화면 글자는 전부 **한국어**. 게임 정보(글자 · 숫자 · 아이콘)를 연출이 가리지 않는다. 그림 옆 여백에 한국어 메모 3줄 이내로 「무엇이 · 어떻게 움직이는지」를 적어도 된다.
[바탕] Projects/AVSR/_exchange/ref/party_check_1029/r2_bubble.png — 지금 인게임 화면(위 HUD · 아래 조작 단추 그대로 둔다).
[주인공] 코만도(미사일) — 원작 그림 Assets/BaseResource/Unit/commando_missile/unit_commando_missile_e.png · _atk1 · _atk2 그대로(새로 디자인 금지). 화면 가운데 아래쪽.
[장면] 필살기 「집중 포격」 — 가장 센 적 하나를 잠그고, 미사일 8발이 몸 둘레 반원에서 솟아 바깥으로 퍼졌다가 그 적 한 점으로 휘어 모여 꽂힌다(연쇄 폭발).
  미사일이 날아가는 **궤적**이 한눈에 읽혀야 한다(지금 게임은 폭발 · 숫자만 보이고 미사일이 안 보인다 — Projects/AVSR/_exchange/mock1030_skill/now_missile_focus.png). 힘주는 순간을 그린다.
[자유] 1안과 다른 방향으로(2안) — 예: 궤적 · 폭발 · 표식 표현을 다르게.
[저장] Projects/AVSR/_exchange/mock1030_skill/mock_missile_free2.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
