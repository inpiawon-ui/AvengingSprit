$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 — 일시정지 창 「호스트」 칸의 **체력 줄 부품**.
[정답 시안] Projects/AVSR/_exchange/in/mock_pause_panel_free_peak.png — 「호스트」 칸 이름 아래 하트 옆의 「182 / 207」 체력 줄.
[앵커] Projects/AVSR/_exchange/ref/pause_panel_mock/crop_hp_bar.png — 시안에서 잘라 2배로 키운 그 줄. 이 두께 · 색 · 둥근 모서리를 따른다.
[부품] rb_pause_hp_bar — 한 장에 **2칸 세로로**(칸마다 512 × 64, 전체 512 × 128).
- 위 칸 = **빈 칸(바탕)**: 어두운 남색 둥근 막대 + 아주 가는 밝은 테. 칸의 96% 안.
- 아래 칸 = **채움**: 두툼한 빨강 둥근 막대(시안처럼 위쪽이 살짝 밝은 평면 빨강 · 빛 번짐 없음). 바탕 칸 안쪽에 딱 들어가는 크기(위 칸 막대보다 위아래 · 양옆 4 px 작게).
- 글자 · 하트는 그리지 않는다(게임이 넣는다). 바깥은 분홍(#FF00FF) — 나중에 뚫는다. 막대에 분홍이 섞이지 않게.
[저장] Projects/AVSR/_exchange/in/rb_pause_hp_bar.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
