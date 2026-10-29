$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 캐릭터 연출 부품 시트. 그림체는 기존 연출 부품 Projects/AVSR/_exchange/out/fxs/levelup/ 와 같은 도트.
[부품] fx_present_card_absorb — 얻은 카드의 힘이 몸에 깃드는 순간 — 위에서 내려온 흰 · 금빛 빛 알갱이 무리가 몸 가운데로 빨려 들어가고, 둥근 빛 고리가 안쪽으로 좁혀지며 사라진다. 마지막 장에 몸 둘레 얇은 빛 테두리만. 흰색 ~ 밝은 회색(게임에서 카드 등급색을 곱한다). 가운데(몸 자리)는 비움
[시트] 칸 256 x 256 가 4열로 8장, 왼쪽 위부터 시간 순(시트 전체 1024 x 512). 각 그림은 칸 안에, 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF). 마젠타/분홍을 그림에 쓰지 않는다. 글자 · 숫자 금지.
[저장] Projects/AVSR/_exchange/in/fxp_card_absorb.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
