$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720 x 1280) — **완성 화면 시안** 한 장.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(빛은 도트 모양 + 셰이더 광원 · 파티클, 강약, 연출 원칙).
화면 글자는 전부 **한국어**. 게임 정보(글자 · 숫자 · 아이콘)를 연출이 가리지 않는다. 그림 옆 여백에 한국어 메모 3줄 이내로 「무엇이 · 어떻게 움직이는지」를 적어도 된다.
[바탕] Projects/AVSR/_exchange/ref/party_check_1029/base_growth_iy_now.png — 지금 「호스트 성장」 화면(왼쪽 스킬 칸 셋 · 오른쪽 인연 판, 오늘 PD 가 고른 시안 Projects/AVSR/_exchange/in/mock1029_growth_inyeon.png 을 넣은 것).
[주제] 이 화면에 **무리 시너지 설명**을 더한다(PD 「인연이랑 우리가 추가하는 시너지도 넣어 줄 수 있을까? 2마리일 때 3마리일 때 효과 — 인연과 시너지 둘 다 저기서 설명이 필요」).
  이 호스트(갱스터)의 무리 = 「갱단」(뒷골목 갱단 — 급소를 노린다). 2명 = 치명타 +10% · 3명 = 치명타 +15%. 같은 무리 호스트: 갱스터 · 폭력배 · 호퍼 · 호퍼(기관단총).
  알려야 할 것: 무리 문장(육각 아이콘 — Assets/BaseResource/InGameMainUI/syn_icon_gang.png 그대로) · 무리 이름과 설명 · 2명 / 3명 효과 두 줄 · 같은 무리 호스트 얼굴 줄(가진 것 밝게 · 없는 것 어둡게) · 「같은 무리를 메인 · 보조로 함께 데려가면 켜진다」.
  인연 판과 나란히 또는 그 아래에 같은 화풍(남색 판 · 청록 테 · 금빛 강조)으로. 스크롤 아래로 이어져도 된다. 기존 칸들은 그대로 둔다.
[저장] Projects/AVSR/_exchange/in/mock1029_growth_syn.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
