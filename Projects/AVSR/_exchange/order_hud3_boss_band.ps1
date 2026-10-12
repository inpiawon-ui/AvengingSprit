$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 인게임 HUD **보스 체력 띠** 부품 발주. PD 가 고른 시안: Projects/AVSR/_exchange/in/mock_bosshud_band.png (②안).
지금 HUD 부품과 한 벌이어야 한다: Assets/BaseResource/InGameMainUI/hud3/ (hud3_ghost_pill · hud3_host_pill · hud3_stage_track · hud3_fill_red) — 같은 남색 · 하늘색 네온 · 도트 질감.
[그릴 것] 앵커 Projects/AVSR/_exchange/ref/boss_hud_mock/anchor_band_plate.png 의 **긴 띠 판만** — 남색 바탕 · 하늘색 네온 이중 테두리 · 양 끝 둥근 알약.
- 글자(로봇 P2 · HP · 숫자) · 빨간 막대 · 막대 홈은 **그리지 않는다** — 빈 판. 판 안쪽은 고른 남색(나중에 게임이 글자 · 막대를 올린다).
- 크기 1040 x 76 px 의 가로로 긴 판 하나. 판 바깥은 단색 분홍(#FF00FF) — 나중에 뚫는다. 그림자 · 빛 번짐을 분홍 위로 퍼뜨리지 않는다.
- 판 가운데 부분은 가로로 늘려도 깨지지 않게 무늬 없이 고르게(양 끝 모서리에만 장식).
[저장] Projects/AVSR/_exchange/in/hud3_boss_band.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
