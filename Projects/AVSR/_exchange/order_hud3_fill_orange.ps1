$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 인게임 HUD **보스 체력 띠** 부품 발주. PD 가 고른 시안: Projects/AVSR/_exchange/in/mock_bosshud_band.png (②안).
지금 HUD 부품과 한 벌이어야 한다: Assets/BaseResource/InGameMainUI/hud3/ (hud3_ghost_pill · hud3_host_pill · hud3_stage_track · hud3_fill_red) — 같은 남색 · 하늘색 네온 · 도트 질감.
[그릴 것] Assets/BaseResource/InGameMainUI/hud3/hud3_fill_red.png 와 **모양 · 크기 · 명암 구조가 똑같은 주황판**. 크기 546 x 19 px.
- 용도: 보스가 맞으면 막 깎인 만큼이 주황으로 잠깐 남았다가 줄어드는 「잔상」 막대(시안 Projects/AVSR/_exchange/ref/boss_hud_mock/anchor_trail.png 의 주황 부분).
- 색: 밝은 주황(빨강 막대 옆에서 또렷이 구분되게, 노랑으로 가지 않게). 위쪽 밝은 줄 · 아래 어두운 줄 같은 명암은 빨강판을 그대로 따른다.
- 바탕 없이 꽉 찬 막대 한 장(빨강판처럼). 글자 · 테두리 없음.
[저장] Projects/AVSR/_exchange/in/hud3_fill_orange.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
