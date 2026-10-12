$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(등급 레벨링 3-1).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 레벨업 카드 4등급 **색 구분 시안** 한 장.
[바탕 = 지금 카드] Projects/AVSR/_exchange/in/mock_cardframe_unified.png (네 등급이 같은 틀 구조 — 이 구조 · 칸 자리 · 아이콘 · 글자는 그대로)
PD 「딱 봤을 때 어떤 게 좋은 거고 안 좋은 건지 구분이 안 된다, 다 좋아 보인다」 — 지금은 네 장 모두 어두운 남색 속 + 회색 강철 틀이라 등급이 얇은 빛선 색으로만 갈린다.
[할 일] 틀 구조는 그대로, **색과 재질로 등급이 한눈에 갈리게**:
- COMMON: 칙칙한 회색 강철 틀 + 회색 속(바탕) — 빛선 없음, 가장 수수하게
- RARE: 푸른 강철 틀 + 짙은 푸른 남색 속 — 파랑 빛선
- EPIC: 보라빛 금속 틀 + 짙은 보라 속 — 보라 빛선이 더 밝게
- LEGENDARY: **금 틀**(강철 아님) + 짙은 붉은 금색 속 + 맨 위 왕관 · 날개 장식 — 가장 화려하게
- 레퍼런스: Projects/AVSR/_exchange/ref/refboard/cap/22_sjm2kb.jpg (보통 카드 — 수수함) · 23_oqiz69.jpg (전설 카드 — 카드 자체가 금색에 장식)
- 속(아이콘 칸 · 이름 띠 · 설명 상자) 색은 글자(흰색 · 밝은 회색)가 잘 읽히게 어둡게 유지한다. 카드 크기 · 외곽선 모양은 네 장이 같다.
- 화풍(도트 · 리벳 · 파이프)은 그대로. 크기 1174 x 640, 네 장 나란히, 위에 등급 이름.
[저장] Projects/AVSR/_exchange/in/mock_cardrarity_color.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
