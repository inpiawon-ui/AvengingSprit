$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 인게임 배경 심플화의 **게임에 넣을 부품**이다. 시안이 아니다.
먼저 Projects/AVSR/_exchange/guide_ingame_simple.md 를 읽는다(「새 챕터 · 새 무대 규칙」 다섯 줄).
[무대] 옥상 — 전투 방 **아래쪽 바깥**(입구 밑) 그림 한 장. 방에 들어서기 전 화면 아래에 보이는 자리다.
[완성도 · 화풍 · 색 정답] Projects/AVSR/_exchange/bg_apply/rooftop/roomfloor_env_rooftop.png (이 무대의 새 방 바닥 — 이 그림의 **아래 끝 벽 띠 바로 밑에 붙는다**) · Projects/AVSR/_exchange/in/mock_simple2_ch4.png
[틀 정답] Assets/BundleResource/RoomFloor/roomapron_rooftop.png (지금 것 — 크기 · 자리만 본다. 고철 더미 · 연기 같은 복잡한 화풍은 버린다)
- 크기 정확히 **720 x 540**. 위 끝은 새 방 바닥의 아래 벽 띠와 같은 재질 · 색으로 이어지게 시작하고, 아래로 갈수록 방 밖(복도 · 계단 · 바깥 바닥)으로 이어지며 **점점 어두워진다**.
- 심플하게: 큰 패널 · 바닥 결 하나와 이 무대 물건 한두 개 정도. 잔 부품 · 흩어진 쓰레기 · 연기 금지. 가운데는 입구 길이라 비워 둔다.
- 그림에 빛을 굽지 마라: 번지는 광채 · 아우라 · 반짝이 · 렌즈 빛 금지(빛은 게임에서 이펙트로 따로 낸다). 도트로 그린 작은 불빛 점 · 램프 몸통은 괜찮다.
- 글자 · 숫자 · 메모 · 테두리 액자 · 워터마크 금지.
[저장] Projects/AVSR/_exchange/in/bg_rooftop_apron.png (720 x 540, PNG)
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
