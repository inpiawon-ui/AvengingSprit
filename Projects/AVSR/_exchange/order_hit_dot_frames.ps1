$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임에 바로 넣을 **피격 도트 폭발 프레임 시트** — PD 가 고른 시안 「C 도트 폭발」과 **똑같은 모양 · 색 · 크기 비율**로.
정답 시안(앵커): Projects/AVSR/_exchange/ref/hit_c_anchor_zoom.png (보통 18 · 치명 136 의 시작 · 최대 · 끝, 2배 확대)
                원본 줄: Projects/AVSR/_exchange/ref/hit_c_anchor_row.png

시트: 768 x 256, 순수 마젠타(#FF00FF) 배경, 칸 128 x 128, 2줄 x 6칸(왼쪽부터 시간 순).
- 1줄 「보통」 5칸(6번째 칸은 빈 마젠타): 작은 주황-노랑 픽셀 폭발 — 1 점화(작은 흰-노랑 점) → 2 부풀기 → 3 최대(지름 약 44 px, 가운데 흰-노랑 · 바깥 주황 · 가장자리 빨강 픽셀 덩어리) → 4 흩어짐(작은 사각 불티 몇 개) → 5 거의 사라짐
- 2줄 「치명」 6칸: 같은 그림체로 훨씬 크고 세게 — 1 점화 → 2 부풀기 → 3 최대(중심 덩어리 지름 약 80 px + 사각 불티가 바깥으로, 전체 약 120 px) → 4 터져 흩어짐(불티가 더 멀리) → 5 불티만 → 6 거의 사라짐
- 그림체: 앵커처럼 **도트(픽셀아트)**, 노랑 · 주황 · 빨강 사각 픽셀 덩어리, 가운데는 흰-노랑, 연기는 거의 없음. 외곽선 없음.
- 각 칸 가운데에 폭발 중심을 두고, 칸 밖으로 넘치지 않게.
- 금지: 별 · 십자 반짝이 · 표창 모양 · 원형 고리 · 큰 연기 · 마젠타/분홍 색(배경과 섞인다) · 그림자 · 캐릭터.
[저장] Projects/AVSR/_exchange/in/hit_dot_frames.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
