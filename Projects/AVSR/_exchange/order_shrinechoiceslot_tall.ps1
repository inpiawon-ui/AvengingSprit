$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 회복의 제단 창의 선택 칸 그림 — 기존 칸 Projects/AVSR/_exchange/in/anchor_fx/shrinechoiceslot_ref.png (440 x 62) 과 **똑같은 그림체 · 색 · 재질 · 장식**으로, 칸만 세로로 길게 다시 그린다.
[크기] 440 x 134 (가로는 같고 세로만 길다).
[바꾸는 것] 칸 안쪽(어두운 남색 판)의 세로 높이만 늘린다 — 글자 두 줄 반이 들어갈 자리.
[바꾸지 않는 것] 바깥 금속 테두리 두께 · 네 모서리 장식 · 위아래 청록 장식 줄의 크기와 모양은 원본 픽셀 크기 그대로.
  왼쪽 청록 아이콘 틀은 원본과 **같은 폭**, 세로만 칸 안쪽에 맞춰 길게(모서리 깎인 모양 유지).
  오른쪽 청록 물방울 보석은 원본과 **같은 크기(늘리지 말 것)** 로 세로 가운데에 하나.
- 바탕은 순수 마젠타(#FF00FF) — 칸 바깥은 전부 마젠타. 글자 · 아이콘 그림 넣지 않는다.
[저장] Projects/AVSR/_exchange/in/setc_shrinechoiceslot_tall.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
