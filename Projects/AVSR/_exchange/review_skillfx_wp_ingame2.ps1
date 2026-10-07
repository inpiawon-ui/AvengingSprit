$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. 이미지를 열어 보고 글로만 검수한다. API 키를 쓰는 CLI 폴백은 쓰지 마라.

[배경] 네 1차 검수(Projects/AVSR/_exchange/review_skillfx_wp_ingame1.md)대로 고쳐서 4종을 다시 찍었다.
  - 아마존: 발밑 펄스 168x56 타원 · 진하기 55% · 0.33초, 바닥 갈라짐 128x64 · 50%, 파편은 착지 1회 · 35%
  - 난사: 세 갈래 조준선을 가운데 → 좌 → 우 0.03초 간격 · 45% · 0.10초, 맞은 자리 코어 0.12초에 한 번 · 38px · 85%
  - 방벽: 방벽이 서기 전 예전 쉴드 거품을 뺐다
  - 분신: 방(세로 1152)이 화면(1050)보다 길어 방 한가운데가 HUD 밑에 숨었다 → 방 한가운데에서 가장 가까운, 화면 안 자리로 당겼다
[알아 둘 것 — 내 스킬 연출이 아닌 것]
  - 적 몸에서 터지는 파란 얼음 조각 덩어리 = 상성 유리 타격 이펙트(모든 몸 공통)
  - 노란 별 = 평타 공통 타격 이펙트, 흰 수리검 = 닌자 평타, 몸 둘레 큰 흰 원 = 사거리 표시(PD 지시)
  - 분신은 닌자 몸 그림을 반투명 · 푸르게 복제한 것이다(그림 새로 안 그림)

[게임 결과 — 0.05초 간격, 캐릭터 주변을 잘라 크게] Projects/AVSR/_exchange/ref/skillfx_wp_ingame2/
  03_amazon.png · 02_thug.png · 06_commando_mg.png · 15_ninja.png · 15_ninja_full.png(전체 화면 0.2초 간격)
  시간 0.9~1.0초는 컷인 — 그 뒤가 스킬이다.

[답할 것] 한국어. 4종 각각: 판정(통과 / 손볼 것 있음) · 손볼 것(숫자로, 코드로 되는 것만) · 한 줄 평.
마지막 줄에 「전체 판정: 통과 / 한 번 더」.
[저장] Projects/AVSR/_exchange/review_skillfx_wp_ingame2.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
