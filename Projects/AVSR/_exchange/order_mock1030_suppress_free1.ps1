$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720 x 1280) — **완성 화면 시안** 한 장.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(빛은 도트 모양 + 셰이더 광원 · 파티클, 강약, 연출 원칙).
화풍 · 화면 구성 기준: Projects/AVSR/_exchange/in/mock1029_ult_mg_a.png (방금 통과한 같은 코만도 필살기 시안 — 이 정도의 퀄 · 도트 질감).
화면 글자는 전부 **한국어**. 게임 정보(글자 · 숫자 · 아이콘)를 연출이 가리지 않는다. 그림 옆 여백에 한국어 메모 3줄 이내로 「무엇이 · 어떻게 움직이는지」를 적어도 된다.
[바탕] Projects/AVSR/_exchange/ref/party_check_1029/r2_bubble.png — 지금 인게임 화면(위 HUD · 아래 조작 단추 그대로 둔다).
[주인공] 코만도(기관총) — 원작 그림 Assets/BaseResource/Unit/commando_mg/unit_commando_mg_e.png · _atk1 · _atk2 그대로(새로 디자인 금지). 화면 가운데 아래쪽에 크게, 서서 쏜다.
[장면] 액티브 「제압 사격」 — 앞쪽 좁은 부채꼴로 2초간 연사, 맞은 적은 30% 느려진다.
  ⚠ 같은 캐릭터의 필살기 「탄막 요새」(바닥에 거치 · 다섯 줄이 넓게 동시에 · 굵은 화력, 기준 그림)와 **한눈에 달라 보여야 한다** — 거치하지 않고 서서, 한 줄기로 쏟는 연사. 화력은 필살기보다 한 단계 약하게.
  무엇이 나가는지 · 맞은 적이 느려진 것이 한눈에 읽혀야 한다(지금 게임은 일반 총알뿐이라 안 보인다 — Projects/AVSR/_exchange/mock1030_skill/now_suppress.png).
[자유] 연출 방식은 네가 가장 멋지게 정한다(1안).
[저장] Projects/AVSR/_exchange/mock1030_skill/mock_suppress_free1.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
