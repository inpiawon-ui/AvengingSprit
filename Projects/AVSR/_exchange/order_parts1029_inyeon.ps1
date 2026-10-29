$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — PD 가 고른 시안을 **게임에 넣을 부품 그림**으로 나눠 그린다. 새로 디자인하지 말고 시안 · 앵커의 화풍 · 색 · 도트 크기를 그대로 따른다.
[공통] 크기 1024 x 1024 시트 한 장. 부품끼리 사방 40px 이상 띄운다. **글자 · 숫자를 넣지 마라.** 그림자 판 · 부품 옆 이름 메모 금지.
[정답 시안] Projects/AVSR/_exchange/in/mock1029_growth_inyeon.png (호스트 성장 화면 오른쪽 인연 판). 앵커: Projects/AVSR/_exchange/ref/party_ui_1029/anchor/p5_iy_*.png
[바탕] 완전한 분홍(#FF00FF). 지금 화면(Projects/AVSR/_exchange/ref/ui_reborn_1023/play/growth_16x9.png)의 남색 판 · 청록 테 화풍 그대로.
위에서 아래, 왼쪽에서 오른쪽 순서로:
1) 인연 큰 판 — 앵커 p5_iy_panel 의 바깥 판(남색 안 · 밝은 청록 두꺼운 테 · 모서리 장식). 9분할로 늘려 쓰게 가운데 고르게. 약 380 x 440
2) 안쪽 칸 — 앵커 p5_iy_pair · p5_iy_effect 의 판 안 작은 칸(더 어두운 남색 · 가는 회청 테). 9분할. 약 330 x 170
3) 단계 딱지 — 앵커 p5_iy_header 오른쪽 금빛 둥근 딱지(글자 「1단」 빼고). 약 70 x 36
4) 사슬 고리 아이콘 — 앵커 p5_iy_header 왼쪽 금빛 사슬 고리. 약 40 x 40
5) 얼굴 칸 두 개 — 앵커 p5_iy_pair 의 왼쪽 청록 테 얼굴 칸 · 오른쪽 금 테 얼굴 칸(얼굴 빼고 칸만). 각 약 100 x 100
6) 잇는 사슬 화살 — 앵커 p5_iy_pair 가운데 청록 사슬 + 오른쪽 화살촉. 약 110 x 40
7) 켜짐 스위치 — 앵커 p5_iy_pair 아래 청록 토글(켜진 모양). 약 50 x 28
8) 작은 화살 — 앵커 p5_iy_effect 의 청록 화살. 약 36 x 24
[저장] Projects/AVSR/_exchange/in/parts1029_inyeon.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
