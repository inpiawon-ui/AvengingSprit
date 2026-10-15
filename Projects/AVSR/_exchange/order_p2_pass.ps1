$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라(사각형 · 원을 코드로 찍은 것은 반려). 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 로비 상점 · 소환 · 패스 · 출전 **부품 시트**. 게임에 그대로 잘라 넣을 부품이다.
[먼저 볼 것]
- 정답 장면(PD 확정 시안): Projects/AVSR/_exchange/in/scene_s16_pass.png · Projects/AVSR/_exchange/in/scene_s17_mission.png · Projects/AVSR/_exchange/in/scene_s18_premium.png
- 부품 목록 · 원칙: Projects/AVSR/_exchange/order_store_parts.md · 장면 구성: Projects/AVSR/_exchange/store_scenes.md
- 지금 로비 부품(같은 결 · 같은 두께로): Assets/BaseResource/ChapterHost/ 의 ch_card_frame.png · ch_start_button.png · ch_pill.png · ch_panel.png, 1차 견본 Projects/AVSR/_exchange/in/parts1_cardkit.png
- 유령은 Projects/AVSR/Reference/Hosts/idle/ghost_anchor_user_x8.png 모습 그대로.
[그리는 법] **분홍(#FF00FF) 바탕 한 장**에 아래 부품을 **위에서 아래 · 왼쪽에서 오른쪽 순서로** 줄지어, 부품끼리 40px 이상 띄워 그린다. **글자 · 이름표 · 숫자 없이**(목록에서 「이 글자만 그림에」라고 한 것만 예외). 판 · 틀은 남색 바탕 + 하늘 네온 결이 기본, 빛 번짐은 테두리 옆 2~3px 까지만(분홍 위로 넓게 퍼뜨리지 않는다). 물건 그림에는 빛 · 아우라를 굽지 않는다.
[부품 — 이 순서대로]
1. 시즌 1 띠 그림 — 「복수의 유령」(기준 유령 · 붉은 눈 · 보라 불꽃) + 밤 도시, 오른쪽에 글자 자리 비움(글자 없음) — 약 680x260
2. 단계 줄 무료 칸 판 — 약 300x80
3. 단계 줄 프리미엄 칸 판(금 · 보라) — 약 300x80
4. 단계 번호 판 — 약 70x80
5. 받음 체크(초록) — 약 44x44
6. 받기 버튼 판(노랑 반짝) — 약 120x44
7. 미션 한 줄 판 — 약 660x56
8. 미션 막대 바탕 — 약 200x14
9. 미션 막대 채움 — 약 200x14
10. 프리미엄 상품 비교 칸 판(금) — 약 300x360
11. 프리미엄+15단계 칸 판(보라 · 「추천」 띠 자리) — 약 300x360

[저장] Projects/AVSR/_exchange/in/p2_pass.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
