$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 인게임 배경의 **게임에 넣을 부품**이다. 시안이 아니다.
먼저 Projects/AVSR/_exchange/guide_ingame_simple.md 를 읽는다(「새 챕터 · 새 무대 규칙」 다섯 줄).
[무대] **포로 수용실** — 챕터마다 중간 보스 방(10번 · 20번 방)이 쓰는 방 **바닥 한 장**. 원작 5스테이지 「포로 수용실」이 뿌리다.
[테마 · 색 · 틀 정답] Assets/BundleResource/RoomFloor/roomfloor_env_holding.png (지금 것 — **붉은 · 자줏빛 수용 시설**이라는 정체 · 대표 색 · 위아래 벽 띠 자리 · 바닥 가운데 큰 팔각 구획을 살린다. 흐릿한 대리석 얼룩 · 뿌연 붉은 안개 화풍은 버린다)
[화풍 정답 — 새로 바뀐 무대 바닥] Projects/AVSR/_exchange/bg_apply/junkyard/roomfloor_env_junkyard.png · Projects/AVSR/_exchange/bg_apply/lab/roomfloor_env_lab.png · Projects/AVSR/_exchange/bg_apply/refinery/roomfloor_env_refinery.png
  이 세 장처럼: 또렷한 금속 바닥 판 · 이음선 · 낮은 대비 · 위아래 벽 띠는 난간 · 패널 · 작은 램프 도트. 같은 도트 굵기 · 같은 단순함.
- 크기 정확히 **720 x 1152**. 가운데 720 x 936 이 걸어 다니는 방이고, **위 108 px · 아래 108 px 는 방 밖 벽 띠**, 좌우 끝 약 24 px 는 가장자리 벽이다 — 화풍 정답 바닥들의 띠 자리 · 폭과 똑같이.
- 수용실다움은 벽 띠의 붉은 경고등 · 철창 패널 무늬, 바닥 가운데 큰 팔각 구획선 하나로만 낸다. 바닥은 큰 타일 결 하나 · 얼룩 · 금 · 핏자국 · 안개 없음.
- 대표 색은 지금 것처럼 어두운 자줏빛 · 적갈. 다른 무대(남색 · 청록 · 주황 · 회녹)와 한눈에 갈려야 한다.
- 빈 바닥: 캐릭터 · 물건 · 탄 · HUD 금지.
- 그림에 빛을 굽지 마라: 번지는 광채 · 아우라 · 반짝이 · 렌즈 빛 금지(빛은 게임에서 이펙트로 따로 낸다). 도트로 그린 작은 불빛 점 · 램프 몸통은 괜찮다.
- 글자 · 숫자 · 메모 · 테두리 액자 · 워터마크 금지.
[저장] Projects/AVSR/_exchange/in/bgfix_holding_floor.png (720 x 1152, PNG)
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
