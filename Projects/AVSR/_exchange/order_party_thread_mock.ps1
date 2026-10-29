$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 이 브리프를 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md . 그리고 Projects/AVSR/_exchange/guide_party_possess_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션).
[바탕] Projects/AVSR/_exchange/ref/party_thread/base_thread_now.png — 지금 게임 화면. 분홍 망토 메인(화이트 위저드)과 보랏빛 보조(초록 망토 영매, 발밑 혼고리) 사이에 파란 줄이 있다. PD 「저 실이 너무 안 이쁘다」.
[할 일] 메인과 보조를 잇는 **상시 혼실** 디자인 세 안. 이 실은 플레이 내내 화면에 떠 있으므로 **얇고(심 2~3 px) 전투를 가리지 않아야 한다** — PD 「인게임에서 최대한 지장 없게 얇은 선으로」 · 「너무 과하지 않게」.
[형식] 1440 x 900 한 장. 위에 세 칸(A · B · C) — 칸마다 바탕 화면에서 두 캐릭터 둘레만 잘라 2배로 키운 장면에 그 안의 실을 그린다. 아래 줄에는 세 안의 실 한 토막만 검은 바탕 위에 가로로 크게(실제 크기 x4) 따로 보여 준다. 칸 왼위에 A · B · C 글자만. 설명 문단은 넣지 마라.
[세 안 방향] A = 가늘게 일렁이는 보랏빛 혼실(끝이 흐려짐) · B = 점점이 이어진 작은 혼불 알갱이 줄(도트 점선처럼 흘러감) · C = 네가 아트 디렉터로서 가장 예쁘다고 보는 안.
[색] 혼 보랏빛 #B98CFF 와 흰 심 — 우리 게임 혼의 색. 도트 질감.
[저장] Projects/AVSR/_exchange/in/mock_party_thread.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
