$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720 x 1280) — **완성 화면 시안** 한 장.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(빛은 도트 모양 + 셰이더 광원 · 파티클, 강약, 연출 원칙).
화면 글자는 전부 **한국어**. 게임 정보(글자 · 숫자 · 아이콘)를 연출이 가리지 않는다. 그림 옆 여백에 한국어 메모 3줄 이내로 「무엇이 · 어떻게 움직이는지」를 적어도 된다.
[바탕] Projects/AVSR/_exchange/ref/party_check_1029/r2_bubble.png — 지금 인게임 화면(위 HUD · 아래 조작 단추 그대로 둔다).
[주인공] 코만도(기관총) — 원작 그림 Assets/BaseResource/Unit/commando_mg/unit_commando_mg_e.png 그대로(새로 디자인 금지).
[장면] 필살기 B안 「기관총 포탑」 — 코만도가 옆에 **삼각대 기관총 포탑**을 내려놓고(도트 군용 장비, 원작 군인 느낌) 자기는 다른 곳으로 움직인다.
  포탑이 8초 동안 가까운 적을 자동으로 돌려 겨눠 쏜다. 포탑 머리 위 남은 시간 고리, 총구 불꽃, 탄피. 코만도와 포탑이 따로 싸우는 그림이 한눈에 읽혀야 한다.
  ⚠ 폭력배 「회전 난사」(제자리 사방 탄막)와 달라 보여야 한다.
[저장] Projects/AVSR/_exchange/in/mock1029_ult_mg_b.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
