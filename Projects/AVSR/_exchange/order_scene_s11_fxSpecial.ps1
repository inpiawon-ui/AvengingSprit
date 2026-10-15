$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 720x1280 비율) — 상점 · 소환 · 패스 **장면 기획 시안**.
[반드시 먼저 읽는다] Projects/AVSR/_exchange/store_scenes.md — 이 장면(S11)의 구성이 정답이다(무엇이 · 어디에 · 어떤 상태로). 0절 판매 구성표도 본다.
[화풍 기준 — 이어서 그린다]
- 소환(보라 제단): Projects/AVSR/_exchange/in/mock_summon_a.png — 하단 바 네 칸 HOST · PLAY · SUMMON · SHOP 이 이 시안 모양.
- 상점(남색 · 하늘 네온): Projects/AVSR/_exchange/in/mock_store_currency.png · 패스: Projects/AVSR/_exchange/in/mock_store_pass.png · 로비: Projects/AVSR/_exchange/in/mock_store_lobby.png
- 유령은 Projects/AVSR/Reference/Hosts/idle/ghost_anchor_user_x8.png 모습 그대로. 호스트 얼굴은 Assets/BaseResource/Growth/Faces/face_{키}.png 를 그대로 쓴다.
[공통] 글자 한국어 · 값 원화(₩) · 젬. 이 게임 도트 화풍. 값 · 수량 · 남은 횟수 · 상태(구매 완료 · 잠김 · 받기)가 또렷하게. 그림 아래 여백에 한국어 메모 3줄 이내.
[연출 가이드] Projects/AVSR/_exchange/guide_summon_fx.md 를 읽는다 — 박자 · 강약 · 등급 예고 색 · 하지 말 것.
[장면 S11] 소환 특별 연출 콘티 — **한 장에 4컷(2x2)**: ① 대박 x20 — 금 빛살 · 조각 파편이 튀었다 모임 · 「대박!」 ② 픽업 — 「PICK UP」 띠가 꽂히고 그 몸(닌자(사슬)) 전신이 카드 뒤로 비침 ③ 새 몸 — 실루엣에 색이 차오르며 「NEW」 도장 ④ 정수 전환 — 조각이 보라 불꽃으로 녹아 「정수 +5」. 컷 라벨은 guide_summon_fx.md 그대로.
[저장] Projects/AVSR/_exchange/in/scene_s11_fxSpecial.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
