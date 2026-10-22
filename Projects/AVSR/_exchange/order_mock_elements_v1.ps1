$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 잡몹 원소판 시안 (같은 몸, 원소만 다르게)

■ 우리 그림체 (반드시 — 먼저 통과한 잡몹과 같게)
  · 통과한 잡몹 라인업 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v2/mock_lineup_v2_labeled.png
  · 지금 게임 캐릭터 4배 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v2/ref_game_sprites_x4.png
  · 실제 화면 크기 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v2/ref_ingame_scale.png
  · 1991 아케이드 16비트 픽셀 아트. **96 x 96 픽셀 캔버스 안에 그린 크기** — 위 참조와 키 · 굵기가 같아야 한다.
  · 1 픽셀 짙은 외곽선, 한 마리 12~16색, 명암 2~3단 뚝뚝 끊기게(그라데이션 · 잔털 · 질감 노이즈 금지).
  · 밝고 채도 높은 색, 실루엣이 한눈에 읽히는 만화 비례. **사람 모양 금지**(사람 = 빼앗을 수 있는 몸이라 헷갈린다).
  · 몸에 빛 · 아우라 · 반짝이를 굽지 않는다(빛은 게임에서 이펙트로 따로 낸다).
  · 그린 뒤 **4배로 키워(가장 가까운 이웃)** 늘어놓는다. 글자 · 번호를 그리지 마라.

■ 하는 일 : 같은 잡몹의 **원소판**. 생김새 · 자세 · 크기는 원래 몸과 같고 **색 · 작은 부속만** 바뀐다(한눈에 「불 붙은 고릴라」로 읽히게).
  원래 몸 (게임 그림, 동쪽) :
  · 돌 고릴라 : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/actor_enforcer/unit_actor_enforcer_e.png
  · 고철 전갈 : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/scrapgunner/unit_scrapgunner_e.png
  · 해골      : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/skeleton/unit_skeleton_e.png

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_elements_v1.png — 가로형(1672 x 941 쯤), 배경 어두운 회청 단색
  · 세 줄. 줄마다 왼쪽에 **원래 몸**(위 그림을 그대로 4배), 오른쪽에 원소판.
    1줄 돌 고릴라 → **불** : 돌 몸 틈이 용암처럼 주황 · 빨강으로 갈라지고 어깨에 도트 불꽃 몇 개.
    2줄 고철 전갈 → **얼음** : 고철 껍데기에 서리 · 고드름, 꼬리 끝 포신이 얼음 결정.
    3줄 해골 → **독** : 뼈가 연두 · 보라로 물들고 갈비뼈 사이에 독 거품, 발밑에 독 방울 두어 개.
  · 원소판은 원래 몸과 **실루엣이 같아야** 한다(같은 몹이라는 게 읽혀야 한다).
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
