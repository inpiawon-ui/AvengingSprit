$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 — 일시정지 창 「호스트」 칸의 **얼굴 테두리 부품**.
[정답 시안] Projects/AVSR/_exchange/in/mock_pause_panel_free_peak.png — PD 가 고른 그림. 아래 가운데 「호스트」 칸 왼쪽의 큰 얼굴 테두리 화풍 그대로.
[앵커] Projects/AVSR/_exchange/ref/pause_panel_mock/crop_host_frame.png — 시안에서 잘라 낸 그 테두리. 이 굵기 · 빛 · 모서리 보석 모양을 따른다.
[부품] rb_pause_host_frame — 정사각 얼굴 테두리 3가지(등급마다 색만 다르다, 모양은 같다)
- 한 장에 **3칸 가로로**(칸마다 256 × 256, 전체 768 × 256). 왼쪽부터 R = 하늘색(시안 그대로) · SR = 보라 · SSR = 금색.
- 칸마다: 굵기 칸 폭의 4% 쯤인 밝은 테 + 바깥쪽 은은한 빛 + **네 모서리에 작은 보석 장식**(시안처럼). 테 안쪽 창은 칸의 88% 크기 정사각.
- 테 **안쪽 창과 바깥**은 전부 분홍(#FF00FF)으로 비운다 — 얼굴 · 배경 · 등급 글자 「R」은 게임이 따로 넣는다. 테 · 보석 · 빛에 분홍이 섞이지 않게.
- 얼굴 · 글자 · 별 반짝이는 그리지 않는다(테두리만).
[저장] Projects/AVSR/_exchange/in/rb_pause_host_frame.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
