$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임에 바로 넣을 **스킬 연출 부품 시트** — PD 가 통과시킨 시안 Projects/AVSR/_exchange/in/mock_skill_guru_v1.png 와
**똑같은 모양 · 색 · 도트 그림체**로 그린다(시안 1 발동 · 2 최대 · 3 끝 칸에서 해당 부분을 그대로 따라).

시트: 1024 x 512, 순수 마젠타(#FF00FF) 배경, 칸 256 x 256 이 4열 x 2줄. 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
- 1줄 칸 1~4: 구루 발밑에서 퍼지는 황금빛 원(탑뷰 바닥 타원, 지름 약 120→200 px, 얇은 금빛 테 + 작은 불티)
- 2줄 칸 1~2: 구루 몸을 감싸는 반투명 황금 결계 막(얇은 육각 무늬, 지름 약 150 px, 가운데는 비침) — 2장이 은은히 번갈아
- 2줄 칸 3~4: 결계 지속 표시 — 얇은 황금 테두리 원만(지름 약 130 px, 아주 얇게) — 2장 번갈아
- 캐릭터 · 적은 그리지 않는다(연출 부품만). 그림자 · 마젠타/분홍 색 금지(배경과 섞인다) · 별 · 표창 · 십자 반짝이 모양 금지.
- 빈 칸은 마젠타로 비워 둔다.
[저장] Projects/AVSR/_exchange/in/skill_parts_guru.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
