$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 캐릭터 연출 부품 시트. 그림체는 기존 연출 부품 Projects/AVSR/_exchange/out/fxs/levelup/ 와 같은 도트.
[부품] fx_present_vital_up — 체력 상한이 늘어나는 순간 — 몸 뒤에 반투명 초록 하트 모양 빛이 커졌다 몸에 스며들고, 둘레에 작은 하트 알갱이 몇 개가 떠오른다. 가운데(몸 자리)는 비움. 1 → 8 시간 순
[시트] 칸 256 x 256 가 4열로 8장, 왼쪽 위부터 시간 순(시트 전체 1024 x 512). 각 그림은 칸 안에, 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF). 마젠타/분홍을 그림에 쓰지 않는다. 글자 · 숫자 금지.
[저장] Projects/AVSR/_exchange/in/fxp_vital_up.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
