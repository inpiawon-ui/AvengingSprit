$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(4절 부품 규칙).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) UI 부품 — **레벨업 카드판 4등급**을 한 장에.
[앵커 — 이 시안 그대로] Projects/AVSR/_exchange/in/mock_cardframe_unified.png (PD 「카드 프레임 통일하는 게 낫지 않아?」로 만든 통일 시안)
- 시안의 카드 틀만 그린다 — **아이콘 · 이름 · 설명 글자 · 등급 글자는 빼고 빈 칸으로**(게임이 글자 · 아이콘을 따로 얹는다). 등급 명판도 빈 판.
- 네 장 모두 **완전히 같은 구조 · 같은 자리**. 각 카드 380 x 700 칸 기준으로 상자 자리를 이대로 맞춘다(픽셀):
  · 등급 명판 안쪽 x 60~320, y 70~120
  · 아이콘 칸 안쪽 x 40~340, y 136~396
  · 이름 띠 안쪽 x 40~340, y 410~470
  · 설명 상자 안쪽 x 40~340, y 484~632
  · 바깥 테두리는 칸 끝까지, 아래 테두리 y 640~700
- 등급 차이는 색 · 장식만: COMMON 회색 강철(빛선 없음) / RARE 파랑 빛선 / EPIC 보라 빛선 / LEGENDARY 금 금속 + 위 테두리에 겹쳐 앉은 작은 왕관 · 날개 장식(칸 380 x 700 안에 들어오게).
- 바탕 **분홍(#FF00FF)** — 게임에서 뚫는다. 카드 안쪽 어두운 판은 불투명하게.
- 그림 전체 1600 x 760: 카드 네 장을 왼쪽부터 COMMON · RARE · EPIC · LEGENDARY 순서로, 각 카드 왼쪽 위 = (10 + 400 × 순번, 30), 카드 크기 380 x 700. 등급 이름 글자는 쓰지 마라.
[저장] Projects/AVSR/_exchange/in/tiers_part_cardpanels.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
