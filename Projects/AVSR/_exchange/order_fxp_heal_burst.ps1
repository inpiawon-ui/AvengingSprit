$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 캐릭터 연출 부품 시트. 그림체는 기존 연출 부품 Projects/AVSR/_exchange/out/fxs/levelup/ 와 같은 도트.
[부품] fx_present_heal_burst — 캐릭터 몸(칸 가운데 아래쪽 2/3, 비워 둔다)을 둘러싸고 회복되는 순간 — 발밑에서 연한 청록 · 초록 빛 고리가 퍼지고, 몸 둘레에서 작은 초록 「+」 표시 6~10개와 둥근 빛 알갱이가 위로 천천히 떠오르며 사라진다. 몸을 감싸는 은은한 세로 빛 기둥. 1 고리 생김 → 3~5 「+」 최대 → 8 사라짐. 가운데(몸 자리)는 비워 캐릭터가 보이게
[시트] 칸 256 x 256 가 4열로 8장, 왼쪽 위부터 시간 순(시트 전체 1024 x 512). 각 그림은 칸 안에, 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF). 마젠타/분홍을 그림에 쓰지 않는다. 글자 · 숫자 금지.
[저장] Projects/AVSR/_exchange/in/fxp_heal_burst.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
