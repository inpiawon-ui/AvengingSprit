$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 인게임 배경 심플화의 **게임에 넣을 부품**이다. 시안이 아니다.
먼저 Projects/AVSR/_exchange/guide_ingame_simple.md 를 읽는다(「새 챕터 · 새 무대 규칙」 다섯 줄).
[보스 방] 슬러지 보스 방 **아래쪽 바깥**(입구 밑) 그림 한 장.
[지금 것 — 테마 정답] Assets/BundleResource/RoomFloor/roomapron_boss_sludge.png — 이 보스의 **테마 물건 · 색**을 살린다(무엇이 있는 곳인지).
[이어 붙는 것] Projects/AVSR/_exchange/in/bg_boss_sludge_floor.png 가 있으면 그 아래 벽 띠, 없으면 Projects/AVSR/_exchange/bg_apply/rooftop/roomfloor_env_rooftop.png 의 아래 벽 띠 — 위 끝이 그 재질 · 색으로 이어지게.
[화풍 정답] Projects/AVSR/_exchange/bg_apply/rooftop/roomfloor_env_rooftop.png · Projects/AVSR/_exchange/in/mock_simple2_ch4.png — 심플하게.
- 크기 정확히 **720 x 540**. 아래로 갈수록 어두워진다. 테마 물건은 크고 단순한 덩어리 한두 개로, 잔 부품 · 연기 · 흩어진 잔해 금지. 가운데는 입구 길이라 비워 둔다.
- 그림에 빛을 굽지 마라: 번지는 광채 · 아우라 · 반짝이 · 렌즈 빛 금지(빛은 게임에서 이펙트로 따로 낸다). 도트로 그린 작은 불빛 점 · 램프 몸통은 괜찮다.
- 글자 · 숫자 · 메모 · 테두리 액자 · 워터마크 금지.
[저장] Projects/AVSR/_exchange/in/bg_boss_sludge_apron.png (720 x 540, PNG)
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
