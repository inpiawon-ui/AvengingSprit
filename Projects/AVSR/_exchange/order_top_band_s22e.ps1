$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 로비 **위 고정 띠** 새 판.
[바탕 — 그대로 쓴다] Projects/AVSR/_exchange/ref/store_mock/base_fixed_top_band.png (1080 x 180) — 밤하늘 · 별 · 달 배경, 칸 결(남색 + 하늘 네온 테두리 · 오른쪽 + 버튼), 금화 · 젬 · 편지(빨간 ! 점) · 톱니 아이콘. 결 · 색 · 아이콘 그림을 똑같이 유지한다.
[정답 배치] Projects/AVSR/_exchange/ref/store_mock/anchor_s22e_topbar.png (PD 확정 시안 S22e 의 위 띠를 1080 폭으로 키운 것) — 칸 셋(행동력 · 금화 · 젬) + 편지 + 톱니.
[칸 — 판 좌표(1080 x 180), 테두리 바깥선 기준, 세로는 셋 다 y 24~92]
1. 행동력 칸: x 292~494 — 왼쪽에 하늘빛 번개 아이콘(Projects/AVSR/_exchange/out/parts2/topbar/icon_stamina.png 와 같은 그림), 오른쪽 끝에 + 버튼(금화 칸의 + 와 같은 것)
2. 금화 칸: x 505~712 — 왼쪽 금화 아이콘 · 오른쪽 + 버튼(지금 그림의 금화 칸을 이 폭으로)
3. 젬 칸: x 723~918 — 왼쪽 젬 아이콘 · 오른쪽 + 버튼
4. 편지: x 930~995 (지금 그림의 편지 칸 그대로 줄여서, 빨간 ! 점 유지)
5. 톱니: x 1000~1068
- 왼쪽 x 0~285 는 **배경만**(게임이 유령 프로필 판을 얹는다). 칸 속은 비운다 — 숫자 · 글자 없음.
- 크기 1080 x 180 그대로.
[저장] Projects/AVSR/_exchange/in/fixed_top_band_s22e.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
