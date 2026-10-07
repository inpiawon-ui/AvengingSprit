$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.

[할 일] 스킬 이펙트 퀄업 **시안 한 장** — 「코만도(레이저) · 연쇄 방전」 스킬을 퀄업한 모습을 4컷으로.
캔버스 2880 x 1280 — 가로로 720 x 1280 칸 4개(왼쪽부터 ① 예비 ② 발동 ③ 타격 ④ 여운). 각 칸은 완성된 게임 화면 한 장이다.

[지금 게임 화면 — 반드시 열어 본다(배경 · HUD · 캐릭터 크기 · 그림체를 그대로 쓴다)]
- Projects/AVSR/_exchange/ref/skillfx_now/laser_0.3.png · laser_1.2.png · laser_1.45.png · laser_1.7.png · laser_2.2.png (지금의 연쇄 방전 — 밋밋하다)
- Projects/AVSR/_exchange/ref/skillfx_now/07_commando_laser.png (0.25초 간격 연속 장면)
[아트 디렉션 — 반드시 읽고 따른다] Projects/AVSR/_exchange/review_skillfx_now.md 의 「2. 방향」 · 「3. 공통 부품」 · 「4. 첫 시안 추천 — 07 Commando Laser」의 4컷 묘사.

[지킬 것]
- 배경 · 상단 HUD · 하단 조작부 · 캐릭터(원작 도트 — 화면 폭의 약 7% 로 작다)는 지금 화면 그대로. 캐릭터 자세를 새로 그리지 않는다.
- 이펙트만 바꾼다. **크게 키우는 게 아니라** 예비 → 발동 → 타격 → 여운의 대비, 발사점과 착탄점의 연결, 타격 한 순간의 가장 밝은 코어, 층(바닥 반응 · 주 이펙트 · 옅은 광량), 무기 속성의 직선 · 절삭선 모양으로 퀄을 올린다.
- 화면 전체를 어둡게 깔거나 색으로 덮지 않는다(지금 장면에 있는 화면 전체 틴트는 없앤다). 빛은 타격 지점 둘레만.
- 이펙트 그림체: 캐릭터 가까이의 코어 · 스파크 · 파편은 계단형 가장자리(2~4 px 도트 결), 연기 · 빛 · 왜곡은 부드러운 고해상 — 두 결이 섞인 하나의 그림체.
- 각 칸 맨 위 HUD 아래 왼쪽에 작은 흰 글자로 칸 이름: 「① 예비」 「② 발동」 「③ 타격」 「④ 여운」. 그 밖의 글자는 넣지 않는다.
[저장] Projects/AVSR/_exchange/in/skillfx_sample_laser_v1.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
