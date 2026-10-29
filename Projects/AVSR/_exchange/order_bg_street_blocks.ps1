$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 인게임 배경 심플화의 **게임에 넣을 부품**이다. 시안이 아니다.
먼저 Projects/AVSR/_exchange/guide_ingame_simple.md 를 읽는다(「새 챕터 · 새 무대 규칙」 다섯 줄).
[무대] 밤 도시 거리 — 전투 방 **장애물 블록 3종**을 한 장에.
[완성도 · 화풍 · 색 정답] Projects/AVSR/_exchange/in/mock_simple2_ch3.png (PD 승인 시안) — 플레이 영역 크롭: Projects/AVSR/_exchange/ref/bg_order/mock_street_crop.png
  시안 속 물건의 **모양 · 색 · 도트 굵기 · 명암**을 그대로 옮긴다. 시안에서 그 물건을 찾아 같은 것으로 그린다.
[그릴 것 3종 — 왼쪽부터 이 순서] 파란 신문 자판함 · 빨간 음료 자판기 · 빨간 소화전
[틀 정답] Assets/BaseResource/InGameMainUI/obj_street_block_1.png (지금 게임 블록 — 칸 크기 · 위에서 비스듬히 내려다본 각도 · 바닥에 선 자리만 본다. 화풍은 따르지 마라)
- 블록 하나 = 칸 하나를 꽉 채우는 **덩어리 하나**: 가로 1칸, 세로는 칸 가로의 0.8~1.4배. **윗면 밝게 + 앞면 어둡게** 두 면으로 높이가 보인다. 잔 부품을 흩뿌리지 마라.
- 바닥에 닿는 아래 변은 평평하게, 물건 밑에 그림자 판 · 바닥 조각을 붙이지 마라.
- 바탕은 **완전한 분홍(#FF00FF)** 한 색. 블록끼리 사이를 넉넉히(블록 폭의 절반 이상) 띄우고 **한 줄로** 늘어놓는다. 블록 안에 분홍을 쓰지 마라.
- 블록 하나를 대략 가로 200 px 크기로 크게 그린다(게임에서 72 px 로 줄인다 — 줄여도 읽히게 굵고 단순한 도트로).
- 그림에 빛을 굽지 마라: 번지는 광채 · 아우라 · 반짝이 · 렌즈 빛 금지(빛은 게임에서 이펙트로 따로 낸다). 도트로 그린 작은 불빛 점 · 램프 몸통은 괜찮다.
- 글자 · 숫자 · 메모 · 테두리 액자 · 워터마크 금지.
[저장] Projects/AVSR/_exchange/in/bg_street_blocks.png (PNG)
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
