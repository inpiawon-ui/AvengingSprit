$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 캐릭터 연출 부품 시트. 그림체는 기존 연출 부품 Projects/AVSR/_exchange/out/fxs/levelup/ 와 같은 도트.
[부품] fx_present_heal_orb — 제단에서 고스트 몸으로 날아가는 작은 회복 빛 구슬 — 동그란 청록 · 초록 구슬(지름 칸의 35%) + 왼쪽으로 짧은 꼬리. 4장 반복(꼬리 일렁임)
[시트] 칸 96 x 96 가 4열로 4장, 왼쪽 위부터 시간 순(시트 전체 384 x 96). 각 그림은 칸 안에, 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF). 마젠타/분홍을 그림에 쓰지 않는다. 글자 · 숫자 금지.
[저장] Projects/AVSR/_exchange/in/fxp_heal_orb.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
