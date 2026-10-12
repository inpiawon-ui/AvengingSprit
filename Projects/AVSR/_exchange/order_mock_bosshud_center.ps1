$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일) — 인게임 **보스 체력 게이지** 디자인 시안. 완성된 게임 화면 한 장으로 그린다.
[먼저 볼 것]
- 지금 HUD(승인된 D안): Projects/AVSR/_exchange/ref/ingame_ui/hud_draft_D.png · 게임 실제 화면 Projects/AVSR/_exchange/ref/boss_hud_mock/base_hud_720.png
  — 남색 판 · 하늘색 네온 테두리 · 둥근 알약 · 도트 글꼴. 왼쪽 PLAYER SOUL 칸 · 가운데 CHAPTER/지역/ROOM/진행 막대 · 오른쪽 CURRENT HOST 칸 · 그 아래 골드 · 일시정지.
- 지금 보스 게이지(문제): Projects/AVSR/_exchange/ref/boss_hud_mock/now_pd_boss_panel.png — 큰 금속 판이 HUD 아래 싸움판 한가운데로 내려와 몹을 가린다.
- HUD 부품: Assets/BaseResource/InGameMainUI/hud3/ (hud3_top_band · hud3_ghost_pill · hud3_host_pill · hud3_stage_track · hud3_fill_red · hud3_fill_cyan)
PD 말: 「보스 UI 가 내려와서 너무 거슬린다 · 저 게이지를 우리 UI 에 들어가게 해 줘」 · 앞서 가운데 좁은 칸(폭 100)에 이름 · 숫자 · 막대를 욱여넣은 안은 「누가 저따구로 만드냐」로 반려 — 작고 답답해 보스 게이지 존재감이 없었다.
[조건]
- 보스 게이지가 **지금 HUD 와 한 벌**(같은 판 · 네온 · 글꼴)로 보여야 한다. 싸움판(몹이 서는 곳)을 가리지 않는다 — HUD 띠 안이나 HUD 바로 아래 가장자리에 붙인다.
- 들어갈 것: 보스 이름 + 단계(예: 「로봇 P2」) · 체력 숫자(301/772) · 길고 또렷한 체력 막대(보스답게 존재감 있게). 일본어 · 영어 이름(「ロボット」 「ROBOT」)도 들어가는 폭.
- 왼쪽 PLAYER SOUL · 오른쪽 CURRENT HOST · CHAPTER 정보는 지우지 않는다(보스가 챕터 정보를 밀어내면 안 된다는 PD 지적이 있었다).
- 바탕 화면의 몹 · 바닥은 그대로 두고 HUD 부분만 고친 완성 화면. 세로 9:16, 바탕과 같은 비율.
- 그림 아래 여백에 한국어 메모 3줄 이내(무엇을 어디에 · 왜).
[방향] 가운데 CHAPTER 칸 자리를 보스가 있는 동안 **넓은 보스 칸**으로 키운다 — 양옆 칸은 조금 좁히거나 그대로 두고, 가운데에 보스 이름 · 큰 막대 · 숫자가 HUD 판 안에 또렷하게. CHAPTER 번호는 작게라도 남긴다.
[저장] Projects/AVSR/_exchange/in/mock_bosshud_center.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
