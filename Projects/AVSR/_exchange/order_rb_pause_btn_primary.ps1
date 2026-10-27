$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 — 일시정지 창 **버튼 판 부품**.
[정답 시안] Projects/AVSR/_exchange/in/mock_pause_panel_free_peak.png — PD 가 고른 그림. 맨 아래 「설정」 · 「포기하기」 · 「계속하기」 버튼 화풍 그대로.
[앵커] Projects/AVSR/_exchange/ref/pause_panel_mock/crop_btn_primary.png — 시안에서 잘라 낸 그 버튼. 이 모양 · 색 · 테 굵기를 따른다.
[부품] rb_pause_btn_primary — 「계속하기」 큰 금색 버튼 판
- 한 장에 **2칸 세로로**(칸마다 1024 × 160, 전체 1024 × 320). 위 칸 = 평소, 아래 칸 = 눌림(조금 어둡게).
- 판: 밝은 노랑 · 금색 위아래 그러데이션 + 금테 + **양옆 끝의 작은 금속 장식**과 **비스듬히 깎인 모서리**(시안처럼) + 바깥 은은한 금빛. 칸의 94% 안.
- **글자는 그리지 않는다**(게임이 넣는다). 판 바깥은 분홍(#FF00FF) — 나중에 뚫는다. 판 · 테 · 빛에 분홍이 섞이지 않게.
[저장] Projects/AVSR/_exchange/in/rb_pause_btn_primary.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
