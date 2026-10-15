$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라(사각형 · 원을 코드로 찍은 것은 반려). 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 로비 상점 · 소환 · 패스 · 출전 **부품 시트**. 게임에 그대로 잘라 넣을 부품이다.
[먼저 볼 것]
- 정답 장면(PD 확정 시안): Projects/AVSR/_exchange/in/scene_s04_currency.png · Projects/AVSR/_exchange/in/scene_s07_reward.png
- 부품 목록 · 원칙: Projects/AVSR/_exchange/order_store_parts.md · 장면 구성: Projects/AVSR/_exchange/store_scenes.md
- 지금 로비 부품(같은 결 · 같은 두께로): Assets/BaseResource/ChapterHost/ 의 ch_card_frame.png · ch_start_button.png · ch_pill.png · ch_panel.png, 1차 견본 Projects/AVSR/_exchange/in/parts1_cardkit.png
- 유령은 Projects/AVSR/Reference/Hosts/idle/ghost_anchor_user_x8.png 모습 그대로.
[그리는 법] **분홍(#FF00FF) 바탕 한 장**에 아래 부품을 **위에서 아래 · 왼쪽에서 오른쪽 순서로** 줄지어, 부품끼리 40px 이상 띄워 그린다. **글자 · 이름표 · 숫자 없이**(목록에서 「이 글자만 그림에」라고 한 것만 예외). 판 · 틀은 남색 바탕 + 하늘 네온 결이 기본, 빛 번짐은 테두리 옆 2~3px 까지만(분홍 위로 넓게 퍼뜨리지 않는다). 물건 그림에는 빛 · 아우라를 굽지 않는다.
[부품 — 이 순서대로]
1. 골드 더미 작게 — 약 150x110
2. 골드 더미 중간 — 약 150x110
3. 골드 더미 크게 — 약 150x110
4. 영혼 계약 그림 — 기준 유령 + 젬 + 달력 — 약 180x130
5. 망령 군주의 계약 그림 — 왕관 쓴 유령 + 젬 더미 — 약 180x130
6. 입문 Ⅰ 묶음 그림(젬 · 소환권 · 정수 불꽃) — 약 150x110
7. 입문 Ⅱ 묶음 그림(젬 · 소환권 · 백금 상자) — 약 150x110
8. 입문 Ⅲ 묶음 그림(젬 더미 · 소환권 다발 · 백금 상자 둘) — 약 150x110
9. 챕터 돌파 기념 그림(젬 · 골드 · 소환권 · 상자 쌓임) — 약 220x120
10. 소환권 아이콘 — 보라 영혼 불꽃 문양 티켓 — 약 64x64
11. 행동력 묶음 그림 — 영혼 불꽃 번개 셋 — 약 120x110

[저장] Projects/AVSR/_exchange/in/p2_products_misc.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
