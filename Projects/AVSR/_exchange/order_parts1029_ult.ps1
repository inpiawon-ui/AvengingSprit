$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — PD 가 고른 시안을 **게임에 넣을 부품 그림**으로 나눠 그린다. 새로 디자인하지 말고 시안 · 앵커의 화풍 · 색 · 도트 크기를 그대로 따른다.
[공통] 크기 1024 x 1024 시트 한 장. 부품끼리 사방 40px 이상 띄운다. **글자 · 숫자를 넣지 마라.** 그림자 판 · 부품 옆 이름 메모 금지.
[정답 시안] Projects/AVSR/_exchange/in/mock1029_ult_mg_a.png (코만도 기관총 필살기 「거치 기관포」). 앵커: Projects/AVSR/_exchange/ref/party_ui_1029/anchor/p5_ult_*.png
[바탕] **완전한 검정(#000000)** — 빛이라 더하기로 그린다(분홍 바탕 금지). 단 6) 탄피만 분홍(#FF00FF) 바탕 칸을 따로 둔다(시트 오른쪽 아래 200x200 분홍 칸 안에).
위에서 아래, 왼쪽에서 오른쪽 순서로:
1) 굵은 탄줄 한 줄 — 앵커 p5_ult_tracer 의 주황 · 노랑 빛줄기 하나(가운데 흰 심, 끝이 가늘게). 세로로 길게 약 48 x 300
2) 총구 불꽃 4장 — 앵커 p5_ult_muzzle, 위를 향해 터지는 큰 불꽃의 4단계(작게 → 크게 → 갈라짐 → 사그라짐). 각 약 150 x 150, 가로 한 줄
3) 맞은 자리 푸른 반짝 4장 — 앵커 p5_ult_hit 의 푸른 · 흰 빛과 작은 네모 알갱이(둔해진 적), 4단계. 각 약 100 x 100, 가로 한 줄
4) 바닥 흙먼지 2장 — 앵커 p5_ult_ground 의 발밑 흙먼지 덩이(밝은 갈색), 약 220 x 90
6) 탄피 한 개 — 분홍 칸 안에 놋쇠 탄피 하나(약 14 x 24)
[저장] Projects/AVSR/_exchange/in/parts1029_ult.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
