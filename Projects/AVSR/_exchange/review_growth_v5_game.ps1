$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
그림을 그리지 마라. 이미지를 열어 보고 글로만 검수한다. API 키를 쓰는 CLI 폴백은 쓰지 마라.

[검수 대상] 「호스트 육성」 화면을 게임에 옮긴 결과.
- 시안(통과): Projects/AVSR/_exchange/in/growth_host_v5.png (화면) · Projects/AVSR/_exchange/in/growth_starup_mock_v4.png 오른쪽 절반(성급 올리기 창)
- 게임: Projects/AVSR/_exchange/ref/growth_v5_game_page.png (화면) · Projects/AVSR/_exchange/ref/growth_v5_game_window.png (창)
- 나란히: Projects/AVSR/_exchange/growth_v5_vs_game.png (왼쪽 시안, 오른쪽 게임)
[알고 있는 다른 점 — 지적하지 않아도 된다] 숫자는 게임 실제 값이다(강화 상한 15 · 골드 960 · 8.7초 등). 다른 몸은 아직 안 가진 몸이라 목록 그림이 어둡다. 하단 바는 게임 공통 바라 위치가 조금 다르다. 능력치는 8종이라 5줄 창에서 넘긴다.
[볼 것] 항목마다 「통과 / 고칠 것(구체적으로, 어느 자리를 몇 px)」로 짧게.
1. 배치 — 시안과 자리 · 크기가 맞는가(카드 · 성급 상자 · 능력치 줄 · 스킬 칸 · 목록 · 창)
2. 글자 — 크기 · 굵기 · 색이 시안과 맞는가, 깨지거나 잘린 곳
3. 그림 — 빠진 부품 · 어긋난 부품 · 얼룩
4. 창 — 성급 · 스킬 · 패시브 · 강화 상한이 함께 오른다는 연결이 읽히는가
마지막 줄 종합 판정: 「그대로 보고」 또는 「고쳐서 다시」.
[저장] Projects/AVSR/_exchange/review_growth_v5_game.md 에 한국어로.
"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt