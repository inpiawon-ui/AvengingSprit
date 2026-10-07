$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임에 바로 넣을 **스킬 연출 부품 시트** — PD 가 통과시킨 시안 Projects/AVSR/_exchange/in/mock_skill_salamander_v1.png 와
**똑같은 모양 · 색 · 도트 그림체**로 그린다(시안 1 발동 · 2 최대 · 3 끝 칸에서 해당 부분을 그대로 따라).

시트: 1024 x 512, 순수 마젠타(#FF00FF) 배경, 칸 256 x 256 이 4열 x 2줄. 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
- 1줄 칸 1~4: 샐러맨더 둘레로 퍼지는 독 안개(탑뷰, 바닥에 낮게 깔린 초록-보라 도트 구름 고리, 지름 약 160→300 px) 1 → 4 옅게 걷힘
- 2줄 칸 1~2: 입 앞에 모이는 초록-보라 빛(지름 약 40 px) 1 → 2 수축
- 2줄 칸 3~4: 중독된 적 머리 위 작은 독 방울 표시(초록-보라 방울 2~3개, 약 30 px) 2장 번갈아
- 캐릭터 · 적은 그리지 않는다(연출 부품만). 그림자 · 마젠타/분홍 색 금지(배경과 섞인다) · 별 · 표창 · 십자 반짝이 모양 금지.
- 빈 칸은 마젠타로 비워 둔다.
[저장] Projects/AVSR/_exchange/in/skill_parts_salamander.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
