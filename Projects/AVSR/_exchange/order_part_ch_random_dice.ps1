$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우거나 다른 그림을 잘라 붙이지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 로비 — 「랜덤 선택」 주사위 부품.
[기준 그림] Projects/AVSR/_exchange/ref/hostinfo_mock/anchor_dice.png — PD 가 통과시킨 시안에서 잘라 온 것. 이 모양 · 화풍 그대로:
  흰 정육면체 주사위(파란 눈, 비스듬히 세 면이 보임, 굵은 도트 외곽선) + 그 오른쪽에 큰 노란 「?」(굵은 검은 외곽선).
- 배경 **투명**(알파 PNG) — 사각 틀 · 배경판 · 빛 번짐 · 그림자 없음(빛은 게임에서 따로 준다).
- 크기 320x240, 주사위와 「?」 가 꽉 차게(위아래 여백 8px 안팎).
[저장] Projects/AVSR/_exchange/in/ch_random_dice.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
