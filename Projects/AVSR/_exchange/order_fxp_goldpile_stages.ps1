$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 캐릭터 연출 부품 시트. 그림체는 기존 연출 부품 Projects/AVSR/_exchange/out/fxs/levelup/ 와 같은 도트.
[부품] fx_present_goldpile_stages — 기준 그림 Assets/BaseResource/ChapterScreens/reward_goldpile.png (200x140 금화 더미) 와 **똑같은 그림체 · 색 · 금화 모양**으로 더미가 쌓여 가는 4단계. 1 = 바닥에 금화 3~4개, 2 = 작은 더미(기준의 1/3), 3 = 중간 더미(2/3), 4 = 기준 그림과 같은 크기의 가득 찬 더미. 네 장 모두 더미 바닥 위치 · 가로 가운데가 같게(칸 아래 가운데에 놓인다). 반짝이 별 장식은 4단계에만 1~2개
[시트] 칸 200 x 140 가 4열로 4장, 왼쪽 위부터 시간 순(시트 전체 800 x 140). 각 그림은 칸 안에, 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF). 마젠타/분홍을 그림에 쓰지 않는다. 글자 · 숫자 금지.
[저장] Projects/AVSR/_exchange/in/fxp_goldpile_stages.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
