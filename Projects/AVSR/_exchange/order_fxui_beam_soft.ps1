$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 UI 연출용 이펙트 부품 시트 — 게임과 같은 도트(픽셀 아트). 연출 시안 Projects/AVSR/_exchange/in/mock_fxstory_*_v1.png 의 빛 · 원혼 표현과 같은 그림체.
[부품] fx_ui_beam_soft — 가로로 긴 부드러운 흰 빛 띠(가운데 밝고 위아래 · 양끝으로 알파가 빠짐), 시트 가운데 한 줄 가득
[색] 흰색 ~ 밝은 중성 회색만(게임에서 색을 곱한다). 가운데가 하얗게 날아가지 않게 명암 단계를 남긴다.
시트: 1024 x 256 한 장 — 빔 한 줄이 가로 가득(양끝 알파 페이드).
- 바탕은 순수 마젠타(#FF00FF). 마젠타/분홍 색을 그림에 쓰지 않는다. 별 · 표창 · 십자 반짝이 모양 금지. 글자 · 숫자 금지.
[저장] Projects/AVSR/_exchange/in/fxui_beam_soft.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
