$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — PD 가 고른 시안을 **게임에 넣을 부품 그림**으로 나눠 그린다. 새로 디자인하지 말고 시안 · 앵커의 화풍 · 색 · 도트 크기를 그대로 따른다.
[공통] 크기 1024 x 1024 시트 한 장. 부품끼리 사방 40px 이상 띄운다. **글자 · 숫자를 넣지 마라.** 그림자 판 · 부품 옆 이름 메모 금지.
[정답 시안] Projects/AVSR/_exchange/in/mock1029_host_unlock.png (호스트 해금 순간). 앵커: Projects/AVSR/_exchange/ref/party_ui_1029/anchor/p5_unlock_*.png
시트를 위아래 둘로 나눈다. **위 절반 = 완전한 검정(#000000) 바탕**(빛 — 더하기로 그림), **아래 절반 = 완전한 분홍(#FF00FF) 바탕**(형태 — 뚫어 씀). 경계에 칸 선을 그리지 마라.
[위 절반 · 검정]
1) 금빛 빛살 터짐 — 앵커 p5_unlock_rays 의 가운데서 사방으로 뻗는 금빛 빛살(가운데는 비우고 둘레만). 약 440 x 440
2) 금빛 고리 — 몸 뒤 금빛 원 고리(얇은 빛 고리 + 작은 반짝). 약 300 x 300
[아래 절반 · 분홍]
3) 조각 알갱이 세 개 — 앵커 p5_unlock_shards 의 금빛 · 붉은 · 푸른 작은 결정 조각(도트 네모 결정). 각 약 44 x 44, 가로 한 줄
4) 등급 딱지 판 — 앵커 p5_unlock_badge 의 금 장식 테 + 보라 안 판(글자 「SSR」 빼고 빈 판). 약 200 x 80
5) 이름 판 — 앵커 p5_unlock_name 의 어두운 갈색 안 + 금 장식 테 판(글자 빼고). 약 320 x 80
6) 단추 두 개 — 앵커 p5_unlock_buttons 의 파란 단추 · 금빛 단추(글자 빼고). 각 약 270 x 90
[저장] Projects/AVSR/_exchange/in/parts1029_unlock.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
