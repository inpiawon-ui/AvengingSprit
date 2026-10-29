$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라. 코드 · 에셋은 고치지 마라 — 답 파일 하나만 쓴다. 한 번에 끝낸다.
Projects/AVSR/_exchange/consult_result_ghostlight.md 를 읽고, 거기 적힌 파일(녹화 GIF · 프레임 · 그림 · 셰이더 · 파티클 코드)을 직접 열어 본 뒤
「묻는 것」 1~4 에 답하라. 답은 Projects/AVSR/_exchange/consult_result_ghostlight_answer.md 에 한국어로, 「답 형식」대로 쓴다.
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
