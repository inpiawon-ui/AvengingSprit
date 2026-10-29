$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 인게임 배경 심플화의 **게임에 넣을 부품**이다. 시안이 아니다.
먼저 Projects/AVSR/_exchange/guide_ingame_simple.md 를 읽는다(「새 챕터 · 새 무대 규칙」 다섯 줄).
[보스 방] 킹핀 보스 방 **바닥 한 장**.
[지금 것 — 테마 · 색 · 틀 정답] Assets/BundleResource/RoomFloor/roomfloor_kingpin.png — 이 보스 방의 **대표 색 · 바닥 무늬의 뜻(가운데 원형 경기장 · 표시 등) · 위아래 띠 자리**를 살린다.
[화풍 정답] Projects/AVSR/_exchange/bg_apply/refinery/roomfloor_env_refinery.png (승인된 새 화풍 바닥 — 이것처럼 심플하게) · Projects/AVSR/_exchange/in/mock_simple2_ch6.png
- 크기 정확히 **720 x 1152**. 가운데 720 x 936 이 걸어 다니는 방, 위 108 px · 아래 108 px · 좌우 약 24 px 는 벽 — 지금 파일의 띠 자리와 맞춘다.
- 바닥: 큰 타일 결 하나 · 낮은 대비 · 얼룩 · 금 · 연기 · 그을림 없음. 보스 방다움은 **바닥의 큰 표시 하나(원형 경기장 선 등)와 벽 띠**로만 — 지금 것의 표시를 단순하게 옮긴다.
- 캐릭터 · 보스 · 탄 · 물건 · HUD 금지(빈 바닥).
- 그림에 빛을 굽지 마라: 번지는 광채 · 아우라 · 반짝이 · 렌즈 빛 금지(빛은 게임에서 이펙트로 따로 낸다). 도트로 그린 작은 불빛 점 · 램프 몸통은 괜찮다.
- 글자 · 숫자 · 메모 · 테두리 액자 · 워터마크 금지.
[저장] Projects/AVSR/_exchange/in/bg_boss_kingpin_floor.png (720 x 1152, PNG)
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
