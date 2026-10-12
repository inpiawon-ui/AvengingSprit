$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 와 Projects/AVSR/_exchange/guide_tiers_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 레벨업 카드 **틀 통일 시안** 한 장.
[바탕 = 지금 카드 4등급] Projects/AVSR/_exchange/ref/tiers_mock/base_lv_tiers.png
지금은 등급마다 카드 틀 구조가 다르다(상자 위치 · 경계선 높이 · 폭이 제각각) — 글자 칸을 등급마다 따로 맞춰야 했고 전설 설명이 잘렸다.
PD 「카드 프레임들이 다 다른데, 통일하는 게 낫지 않아?」
[할 일] 네 등급이 **같은 틀 구조**를 쓰게 다시 그린다:
- 구조(네 장 모두 같은 자리 · 같은 크기): 맨 위 등급 명판 → 아이콘 칸(가장 큼) → 이름 띠 → 설명 상자(세 줄이 넉넉히 들어감). 지금 COMMON 카드 구조를 기준으로.
- 등급 차이는 **색과 장식만**: COMMON = 회색 강철, 빛선 없음 / RARE = 파랑 빛선 / EPIC = 보라 빛선 / LEGENDARY = 금 금속 + 맨 위 명판 위에 작은 장식(날개나 왕관 같은 도트 장식) 하나.
  카드 크기 · 외곽선 모양은 네 장이 같다. 레퍼런스: Projects/AVSR/_exchange/ref/refboard/cap/22_sjm2kb.jpg (보통 카드) · 23_oqiz69.jpg (전설 카드 — 모양은 같고 금색 · 날개 장식만 더함).
- 지금 게임의 화풍(어두운 강철 · 리벳 · 파이프, 도트)은 그대로. 아이콘 · 이름 · 설명 글자는 바탕의 것을 그대로 넣어 보여 준다.
- 크기 1174 x 640, 네 장 나란히, 위에 등급 이름.
[저장] Projects/AVSR/_exchange/in/mock_cardframe_unified.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
