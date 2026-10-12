$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(4절 부품 규칙).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) UI 부품 — **레벨업 카드판 4등급(등급 색 구분판)**을 한 장에.
[앵커 — 이 시안의 색 · 재질 그대로] Projects/AVSR/_exchange/in/mock_cardrarity_color.png (PD 「딱 봤을 때 좋은 거 안 좋은 거 구분이 안 된다」로 만든 시안)
[구조 앵커 — 칸 자리는 이 납품과 똑같이] Projects/AVSR/_exchange/in/tiers_part_cardpanels.png (지난번 통일 카드판 — 이 그림과 상자 자리 · 크기 · 외곽선이 픽셀까지 같아야 한다)
- 시안의 카드 틀만 그린다 — **아이콘 · 이름 · 설명 글자 · 등급 글자는 빼고 빈 칸으로**. 등급 명판도 빈 판.
- COMMON 회색 강철 + 회색 속(빛선 없음) / RARE 푸른 강철 + 짙은 푸른 남색 속 + 파랑 빛선 / EPIC 보라 금속 + 짙은 보라 속 + 보라 빛선 /
  LEGENDARY 금 틀 + 짙은 붉은 금색 속 + 위 테두리에 겹쳐 앉은 작은 왕관 · 날개 장식(칸 380 x 700 안에, 지난번과 같은 자리)
- 속(아이콘 칸 · 이름 띠 · 설명 상자)은 흰 글자가 잘 읽히게 어둡게.
- 각 카드 380 x 700 칸, 상자 자리는 지난번과 같다: 등급 명판 안쪽 x 60~320 · y 70~120 / 아이콘 칸 x 40~340 · y 136~396 / 이름 띠 x 40~340 · y 410~470 / 설명 상자 x 40~340 · y 484~632 / 아래 테두리 y 640~700
- 바탕 **분홍(#FF00FF)** — 게임에서 뚫는다. 카드 안쪽 판은 불투명.
- 그림 전체 1600 x 760: 왼쪽부터 COMMON · RARE · EPIC · LEGENDARY, 각 카드 왼쪽 위 = (10 + 400 × 순번, 30). 등급 이름 글자는 쓰지 마라.
[저장] Projects/AVSR/_exchange/in/tiers_part_cardpanels_color.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
