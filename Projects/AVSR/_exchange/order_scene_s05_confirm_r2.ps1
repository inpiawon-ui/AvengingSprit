$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 720x1280 비율) — 상점 · 소환 · 패스 **장면 기획 시안**.
[반드시 먼저 읽는다] Projects/AVSR/_exchange/store_scenes.md — 이 장면(S05)의 구성이 정답이다(무엇이 · 어디에 · 어떤 상태로). 0절 판매 구성표도 본다.
[화풍 기준 — 이어서 그린다]
- 소환(보라 제단): Projects/AVSR/_exchange/in/mock_summon_a.png — 하단 바 네 칸 HOST · PLAY · SUMMON · SHOP 이 이 시안 모양.
- 상점(남색 · 하늘 네온): Projects/AVSR/_exchange/in/mock_store_currency.png · 패스: Projects/AVSR/_exchange/in/mock_store_pass.png · 로비: Projects/AVSR/_exchange/in/mock_store_lobby.png
- 유령은 Projects/AVSR/Reference/Hosts/idle/ghost_anchor_user_x8.png 모습 그대로. 호스트 얼굴은 Assets/BaseResource/Growth/Faces/face_{키}.png 를 그대로 쓴다.
[공통] 글자 한국어 · 값 원화(₩) · 젬. 이 게임 도트 화풍. 값 · 수량 · 남은 횟수 · 상태(구매 완료 · 잠김 · 받기)가 또렷하게. 그림 아래 여백에 한국어 메모 3줄 이내.
[장면 S05] 구매 확인 팝업(결제 상품 — 입문 패키지 Ⅱ). 상점 화면 위에 뜬 팝업, 뒤는 어둡게 살짝.
[고칠 것 — 1차 납품 Projects/AVSR/_exchange/in/scene_s05_confirm_r2.png 를 보고 이것만 고쳐 다시 그린다] 뒤 상점의 탭이 넷으로 그려졌다 — **탭은 셋 「오늘 · 상자 · 재화」**. 하단 바는 scene_s01_lobby.png 의 네 칸 모양(SHOP 선택). 팝업은 1차 그대로 좋다.
[저장] Projects/AVSR/_exchange/in/scene_s05_confirm_r2.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
