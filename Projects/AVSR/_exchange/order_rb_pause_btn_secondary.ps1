$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 — 일시정지 창 **버튼 판 부품**.
[정답 시안] Projects/AVSR/_exchange/in/mock_pause_panel_free_peak.png — PD 가 고른 그림. 맨 아래 「설정」 · 「포기하기」 · 「계속하기」 버튼 화풍 그대로.
[앵커] Projects/AVSR/_exchange/ref/pause_panel_mock/crop_btn_secondary.png — 시안에서 잘라 낸 그 버튼. 이 모양 · 색 · 테 굵기를 따른다.
[부품] rb_pause_btn_secondary — 「설정」 · 「포기하기」 버튼 판(둘이 같은 판 — 글자 색만 게임이 바꾼다)
- 한 장에 **2칸 세로로**(칸마다 512 × 128, 전체 512 × 256). 위 칸 = 평소, 아래 칸 = 눌림(조금 어둡고 테 빛이 약하게).
- 판: 어두운 남색 속 + 밝은 하늘색 테 2~3 px + 바깥 은은한 빛. **네 모서리는 비스듬히 깎인 모양**(시안처럼). 칸의 94% 안.
- **글자는 그리지 않는다**(게임이 넣는다). 판 바깥은 분홍(#FF00FF) — 나중에 뚫는다. 판 · 테 · 빛에 분홍이 섞이지 않게.
[저장] Projects/AVSR/_exchange/in/rb_pause_btn_secondary.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
