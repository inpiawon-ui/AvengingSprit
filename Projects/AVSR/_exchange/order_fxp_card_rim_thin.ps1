$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 캐릭터 연출 부품 시트. 그림체는 기존 연출 부품 Projects/AVSR/_exchange/out/fxs/levelup/ 와 같은 도트.
[부품] fx_present_card_rim_thin — 카드 테두리를 따라 아주 가는(2~3px) 흰 빛줄기 한 토막이 천천히 한 바퀴 도는 것. 테두리 선은 칸 가장자리에서 6px 안쪽. 카드 안쪽은 완전히 비움(마젠타). 흰색 ~ 밝은 회색(게임에서 등급색을 곱한다). 빛 토막 길이는 둘레의 1/6, 4장이 1/4 바퀴씩 이동
[시트] 칸 190 x 350 가 4열로 4장, 왼쪽 위부터 시간 순(시트 전체 760 x 350). 각 그림은 칸 안에, 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF). 마젠타/분홍을 그림에 쓰지 않는다. 글자 · 숫자 금지.
[저장] Projects/AVSR/_exchange/in/fxp_card_rim_thin.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
