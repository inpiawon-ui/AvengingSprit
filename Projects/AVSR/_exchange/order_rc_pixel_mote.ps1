$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 결과창 부품. 기준 그림: Assets/BaseResource/ParticleFx/ghostmote.png (이 파일을 열어 그림체 · 색 · 크기를 맞춘다).
[부품] pixel_mote — 파티클로 띄울 아주 작은 **도트 빛 알갱이** 4종(한 칸에 하나, 칸 가운데). 2x2 픽셀을 한 도트로 그린다(실제 16x16 도트 그림을 2배로 키운 모양). 1 = 지름 5도트 둥근 점(가운데 1도트 가장 밝고 바깥 1도트 테두리는 옅게), 2 = 지름 3도트 작은 둥근 점, 3 = 5도트 마름모(가운데 밝음), 4 = 7도트 마름모 테두리가 옅은 것. 흰색 ~ 밝은 회색만(게임에서 색을 곱한다). **십자별 · 4갈래 별 · 표창 모양 금지** — 점과 마름모만. 번짐 · 안티앨리어싱 금지, 딱 떨어지는 도트
[시트] 칸 32 x 32 가 4열로 4장, 왼쪽 위부터 시간 순(시트 전체 128 x 32). 각 그림은 칸 안에, 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF). 마젠타/분홍을 그림에 쓰지 않는다. 글자 · 숫자 금지.
[저장] Projects/AVSR/_exchange/in/rc_pixel_mote.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
