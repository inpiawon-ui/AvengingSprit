$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 UI 연출용 이펙트 부품 시트 — 게임과 같은 도트(픽셀 아트). 연출 시안 Projects/AVSR/_exchange/in/mock_fxstory_*_v1.png 의 빛 · 원혼 표현과 같은 그림체.
[부품] fx_ui_burst_rays — 가운데서 굵고 짧은 불규칙 빛줄기 8~10개가 터져 나와 퍼지며 사라진다 — 별 모양 중심 · 십자 끝 금지, 줄기 길이 제각각
[색] 흰색 ~ 밝은 중성 회색만(게임에서 색을 곱한다). 가운데가 하얗게 날아가지 않게 명암 단계를 남긴다.
시트: 1024 x 512, 칸 256 x 256 이 4열 x 2줄 — 왼쪽 위부터 1 → 8 시간 순. 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF). 마젠타/분홍 색을 그림에 쓰지 않는다. 별 · 표창 · 십자 반짝이 모양 금지. 글자 · 숫자 금지.
[저장] Projects/AVSR/_exchange/in/fxui_burst_rays.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
