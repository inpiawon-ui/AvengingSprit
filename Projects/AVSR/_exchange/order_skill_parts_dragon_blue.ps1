$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임에 바로 넣을 **스킬 연출 부품 시트** — PD 가 통과시킨 시안 Projects/AVSR/_exchange/in/mock_skill_dragon_blue_v1.png 와
**똑같은 모양 · 색 · 도트 그림체**로 그린다(시안 1 발동 · 2 최대 · 3 끝 칸에서 해당 부분을 그대로 따라).

시트: 1024 x 512, 순수 마젠타(#FF00FF) 배경, 칸 256 x 256 이 4열 x 2줄. 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
- 1줄 칸 1~3: 적에서 적으로 튕기는 번개 한 줄기 — 가로로 뻗은 지그재그 도트 번개(청백 · 연보라, 칸 가운데 가로 약 230 px · 굵기 약 16 px) 3장(모양만 다르게, 번갈아)
- 2줄 칸 1~2: 청룡 몸에 감기는 청백 전기(지름 약 110 px, 몸 둘레 전기 줄기) 2장 번갈아
- 2줄 칸 3~4: 맞은 적 몸의 작은 전기 잔광(약 50 px) 1 → 2 사라짐
- 캐릭터 · 적은 그리지 않는다(연출 부품만). 그림자 · 마젠타/분홍 색 금지(배경과 섞인다) · 별 · 표창 · 십자 반짝이 모양 금지.
- 빈 칸은 마젠타로 비워 둔다.
[저장] Projects/AVSR/_exchange/in/skill_parts_dragon_blue.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
