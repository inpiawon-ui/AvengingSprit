$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 UI 리뉴얼(혼합 컨셉 「픽셀 연대기 × 쇼케이스」) — **UI 부품 키트 1: 판 · 버튼 · 탭**.
먼저 읽는다: Projects/AVSR/_exchange/plan_ui_v3.md (혼합 규칙).
앵커(PD 가 고른 시안에서 자른 조각 — 이 모양 · 색 · 질감 그대로, 깨끗하게): Projects/AVSR/_exchange/ref/ui_v5/kit_tabs.png · kit_skill_cards.png · kit_gold_button.png · kit_showcase_panel.png · kit_parchment_card.png · kit_stat_cards.png
캔버스 2048 × 2048, 배경은 순수 마젠타(#FF00FF) 단색. 부품 사이 마젠타 여백 40px 이상. **글자 · 아이콘 · 캐릭터는 넣지 마라(빈 판만)** — 게임이 글자를 얹는다.
그릴 부품(각각 가로로 늘려 쓰는 9분할 판 — 모서리 · 테두리 · 질감이 가장자리에 모이고 가운데는 고르게):
 1행: 양피지 카드 판(스킬 카드 · 능력치 카드 바탕, 600 × 200) · 양피지 큰 판(600 × 400)
 2행: 잉크 남색 판(진열 칸 테두리 · 고동금 1px 테, 600 × 300) · 잉크 남색 작은 판(300 × 120)
 3행: 금색 큰 버튼(600 × 140) · 남색 버튼(400 × 110) · 금색 작은 버튼(260 × 80)
 4행: 탭 켜짐(금 테 · 밝은 남색, 320 × 100) · 탭 꺼짐(어두운 남색, 320 × 100) · 경험치 막대 바탕(500 × 40) · 경험치 막대 채움 청록(500 × 40)
 5행: 호스트 카드 테 R(청록) · SR(보라) · SSR(금) · 고른 카드 테(밝은 금 + 빛) — 각 200 × 280, 가운데는 비운다(투명이 아니라 어두운 남색 바탕)
도트 질감은 시안처럼(픽셀 테두리 · 양피지 결). 그림자 · 빛 번짐은 판 바깥으로 그리지 마라(마젠타가 뚫린다).
[저장] Projects/AVSR/_exchange/in/uikit_v5_1.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
