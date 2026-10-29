$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 인게임 배경 심플화의 **게임에 넣을 부품**이다. 시안이 아니다.
먼저 Projects/AVSR/_exchange/guide_ingame_simple.md 를 읽는다(「새 챕터 · 새 무대 규칙」 다섯 줄).
[무대] 미사일 기지 — 넓은 화면(태블릿)에서 방 **왼쪽 바깥**을 채우는 **세로 벽 띠 한 장**.
[화풍 · 색 정답] Projects/AVSR/_exchange/in/mock_simple2_ch2.png — 시안의 좌우 가장자리 벽(크롭: Projects/AVSR/_exchange/ref/bg_order/mock_missile_crop.png) 재질 · 색 · 도트 굵기를 그대로 잇는다.
[틀 정답] Assets/BundleResource/RoomFloor/roomside_missile.png (지금 것 — 크기 · 쓰임만 본다, 화풍은 따르지 마라)
- 크기 정확히 **160 x 320**. 게임이 이 그림을 **세로로 끝없이 이어 붙인다** — 위 끝과 아래 끝이 이음매 없이 이어져야 한다(위아래 반복 무늬).
- **오른쪽 변이 방 바닥에 맞닿는 앞면**이다(오른쪽 게임 벽은 이 그림을 좌우로 뒤집어 쓴다). 오른쪽 10~20 px 에 벽 앞 모서리(밝은 윗선 + 어두운 앞면)를 두고 왼쪽으로 갈수록 벽 위 · 바깥 구조물로 이어진다.
- 단순하게: 큰 패널 · 기둥 결 하나와 시안의 벽 조명 한 줄 정도. 잔 부품 · 흩어진 물건 금지.
- 그림에 빛을 굽지 마라: 번지는 광채 · 아우라 · 반짝이 · 렌즈 빛 금지(빛은 게임에서 이펙트로 따로 낸다). 도트로 그린 작은 불빛 점 · 램프 몸통은 괜찮다.
- 글자 · 숫자 · 메모 · 테두리 액자 · 워터마크 금지.
[저장] Projects/AVSR/_exchange/in/bg_missile_side.png (160 x 320, PNG)
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
