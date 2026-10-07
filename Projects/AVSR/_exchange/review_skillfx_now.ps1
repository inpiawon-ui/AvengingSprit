$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. 이미지를 열어 보고 글로만 답한다. API 키를 쓰는 CLI 폴백은 쓰지 마라.

[게임] 세로 모바일 액션(720x1280). 원작 「Avenging Spirit」(1991 아케이드)의 리부트 — 유령이 적의 몸에 빙의해 그 몸의 무기 · 스킬로 싸운다.
캐릭터는 원작 도트 그대로(작게 — 화면 폭의 약 7%), 배경은 고해상 일러스트 톤. 스킬을 쓰면 짧은 컷인 띠가 지나간 뒤 이펙트가 나간다.

[보여 줄 것 — 지금 게임에서 스킬을 쓴 장면, 0.25초 간격]
- Projects/AVSR/_exchange/ref/skillfx_now/cast_1.png · cast_2.png — 22명 각각 스킬 직후 2초(한 줄 = 한 몸)
- Projects/AVSR/_exchange/ref/skillfx_now/{01_gangster, 02_thug, 03_amazon, 07_commando_laser, 08_commando_grenade, 10_dragoon,
  13_white_wizard, 17_robot, 19_snowwoman, 22_death}.png — 몇 개는 크게(0.9~3.2초, 0.25초 간격)

[사용자 의견] 「이펙트가 너무 밋밋하다. 그렇다고 이펙트만 크게 한다고 멋진 건 아니다. 전체적인 퀄을 높여 달라.」
[지켜야 할 것] 화면을 어둡게 깔거나 적을 멈춰 세우는 연출로 「멈춘 것처럼」 만들지 않는다 — 연출은 이펙트 자체로.
캐릭터 동작(자세 · 프레임)은 원작 그대로 두고 연출은 이펙트로 화려하게. 상성 3속성(파워 · 무기 · 마법)이 있다.

[답할 것] 한국어로, 아트 디렉터처럼 구체적으로.
1. 진단 — 지금 이펙트가 밋밋해 보이는 이유를 우선순위대로 5~7개(크기 말고: 예비 동작 · 타격감 · 층 · 색 · 잔상 · 빛 · 화면 반응 · 타이밍 등). 장면을 근거로.
2. 방향 — 이 게임에 맞는 이펙트 원칙 5~7개(도트 캐릭터와 일러스트 배경 사이에서 이펙트의 그림체 · 해상도 · 색 규칙 포함).
3. 공통 부품 — 여러 스킬이 같이 쓸 기본 이펙트 세트(예: 타격 섬광 · 충격파 고리 · 파편 · 잔상 · 빛기둥)와 각각의 모양 · 프레임 수.
4. 샘플 하나 추천 — 시안으로 먼저 보여 주기에 가장 좋은 스킬 하나와 이유, 그 스킬의 「퀄업 후」 장면을 4컷(예비 → 발동 → 타격 → 여운)으로 글로 묘사.
[저장] Projects/AVSR/_exchange/review_skillfx_now.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
