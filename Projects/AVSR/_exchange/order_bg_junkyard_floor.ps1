$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 인게임 배경 심플화의 **게임에 넣을 부품**이다. 시안이 아니다.
먼저 Projects/AVSR/_exchange/guide_ingame_simple.md 를 읽는다(「새 챕터 · 새 무대 규칙」 다섯 줄).
[무대] 쓰레기 집적장 — 전투 방 **바닥 한 장**.
[완성도 · 화풍 · 색 정답] Projects/AVSR/_exchange/in/mock_simple3_ch1.png (PD 승인 시안) — 플레이 영역만 자른 것: Projects/AVSR/_exchange/ref/bg_order/mock_junkyard_crop.png
  이 시안의 **바닥 타일 · 좌우 가장자리 벽 · 색 · 명암 · 도트 굵기**를 그대로 따른다.
[틀 정답] Assets/BundleResource/RoomFloor/roomfloor_env_junkyard.png (지금 게임 바닥 — 이것을 열어 틀을 잰다. 화풍은 따르지 마라)
  - 크기 정확히 **720 x 1152**. 가운데 720 x 936 이 걸어 다니는 방이고, **위 108 px · 아래 108 px 는 방 밖 벽 띠**, 좌우 끝 약 24 px 는 가장자리 벽이다 — 지금 파일의 띠 자리와 폭을 그대로 맞춘다.
  - 위 · 아래 띠와 좌우 벽은 시안의 가장자리 벽과 같은 재질로, 무대다움은 벽 띠의 조명 한 줄 · 패널 무늬로만 낸다.
[빼야 할 것] 시안의 장애물 블록 · 물건 · 캐릭터 · 적 · 적 탄 · 바닥 빨간 경고 띠 · 그림자 · HUD · 버튼은 **전부 빼고** 빈 바닥만 그린다(블록은 따로 깐다).
- 걸어 다니는 바닥: 큰 타일 결 하나 · 낮은 대비 · 낮은 채도 · 잔무늬 · 얼룩 · 금 · 바닥 그림 없음. 바닥 배수구 같은 고정 무늬도 넣지 마라.
- 그림에 빛을 굽지 마라: 번지는 광채 · 아우라 · 반짝이 · 렌즈 빛 금지(빛은 게임에서 이펙트로 따로 낸다). 도트로 그린 작은 불빛 점 · 램프 몸통은 괜찮다.
- 글자 · 숫자 · 메모 · 테두리 액자 · 워터마크 금지.
[저장] Projects/AVSR/_exchange/in/bg_junkyard_floor.png (720 x 1152, PNG)
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
