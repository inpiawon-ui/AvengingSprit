$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720 x 1280) — **완성 화면 시안** 한 장.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(빛은 도트 모양 + 셰이더 광원 · 파티클, 강약, 연출 원칙).
화면 글자는 전부 **한국어**. 게임 정보(글자 · 숫자 · 아이콘)를 연출이 가리지 않는다. 그림 옆 여백에 한국어 메모 3줄 이내로 「무엇이 · 어떻게 움직이는지」를 적어도 된다.
[바탕] Projects/AVSR/_exchange/ref/party_check_1029/r2_bubble.png — 지금 인게임 화면(위 HUD · 아래 조작 단추 그대로 둔다).
[주인공] 코만도(기관총) — 원작 그림 Assets/BaseResource/Unit/commando_mg/unit_commando_mg_e.png · _atk1 · _atk2 그대로(새로 디자인 금지). 화면 가운데 아래쪽에 크게.
[장면] 필살기 A안 「거치 기관포」 — 힘주는 순간. 기관총을 바닥에 거치하고 **앞쪽 60° 부채꼴만** 좌우로 훑으며 굵은 관통 탄줄을 3초 쏟는다.
  탄줄이 적 여럿을 꿰뚫고, 맞은 적은 파랗게 둔해진다. 총구 불꽃 · 튀는 탄피 · 바닥 흙먼지. 화면 위쪽(앞) 방향으로만 쏜다.
  ⚠ 폭력배 필살기 「회전 난사」(제자리에서 사방으로 도는 탄막)와 **한눈에 달라 보여야 한다** — 사방 · 원형 탄막 금지. 「앞을 쓸어 버리는 군인의 제압 사격」.
[저장] Projects/AVSR/_exchange/in/mock1029_ult_mg_a.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
