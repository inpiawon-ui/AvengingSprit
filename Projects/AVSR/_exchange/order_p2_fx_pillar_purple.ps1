$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라(사각형 · 원을 코드로 찍은 것은 반려). 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 로비 상점 · 소환 · 패스 · 출전 **부품 시트**. 게임에 그대로 잘라 넣을 부품이다.
[먼저 볼 것]
- 정답 장면(PD 확정 시안): Projects/AVSR/_exchange/in/scene_s09_fx1.png · Projects/AVSR/_exchange/in/scene_s10_fx10.png · Projects/AVSR/_exchange/in/scene_s11_fxSpecial.png
- 부품 목록 · 원칙: Projects/AVSR/_exchange/order_store_parts.md · 장면 구성: Projects/AVSR/_exchange/store_scenes.md
- 지금 로비 부품(같은 결 · 같은 두께로): Assets/BaseResource/ChapterHost/ 의 ch_card_frame.png · ch_start_button.png · ch_pill.png · ch_panel.png, 1차 견본 Projects/AVSR/_exchange/in/parts1_cardkit.png
- 유령은 Projects/AVSR/Reference/Hosts/idle/ghost_anchor_user_x8.png 모습 그대로.
[그리는 법] **검은(#000000) 바탕 한 장**에 프레임을 **왼쪽에서 오른쪽으로 한 줄**(넘치면 두 줄)로, 프레임마다 같은 크기 칸(약 200x320)에 그린다. 빛 · 불꽃 · 반짝임만(가산으로 얹을 것이라 바탕은 순수 검정). 도트 빛 결 — 매끈한 셰이더 번짐 금지. 글자 없음. 박자 · 강약은 Projects/AVSR/_exchange/guide_summon_fx.md.
[그릴 것] 불꽃 기둥 — 보라(A 예고), 6장
[저장] Projects/AVSR/_exchange/in/p2_fx_pillar_purple.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
