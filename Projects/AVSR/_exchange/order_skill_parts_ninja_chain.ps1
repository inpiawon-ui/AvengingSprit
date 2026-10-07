$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임에 바로 넣을 **스킬 연출 부품 시트** — PD 가 통과시킨 시안 Projects/AVSR/_exchange/in/mock_skill_ninja_chain_v1.png 와
**똑같은 모양 · 색 · 도트 그림체**로 그린다(시안 1 발동 · 2 최대 · 3 끝 칸에서 해당 부분을 그대로 따라).

시트: 1024 x 512, 순수 마젠타(#FF00FF) 배경, 칸 256 x 256 이 4열 x 2줄. 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
- 1줄 칸 1: 쇠사슬 한 토막(가로로 이어 붙일 수 있게 좌우 끝이 맞물리는 사슬 고리 6~7개, 칸 가운데 가로 약 220 px · 높이 약 22 px)
- 1줄 칸 2~4: 사슬이 적에게 감기는 순간 작은 쇳빛 섬광(지름 약 50 px) 1 작게 → 2 최대 → 3 사라짐
- 2줄 칸 1~3: 적 몸통에 사슬이 감겨 조이는 모습 — 가로 고리가 2~3바퀴(너비 약 90 px · 높이 약 60 px), 1 느슨 → 2 감김 → 3 조임
- 2줄 칸 4: 묶인 상태 표시 — 사슬 고리 몇 개만 남은 작은 고리(너비 약 70 px)
- 캐릭터 · 적은 그리지 않는다(연출 부품만). 그림자 · 마젠타/분홍 색 금지(배경과 섞인다) · 별 · 표창 · 십자 반짝이 모양 금지.
- 빈 칸은 마젠타로 비워 둔다.
[저장] Projects/AVSR/_exchange/in/skill_parts_ninja_chain.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
