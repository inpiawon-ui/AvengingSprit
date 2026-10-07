$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 HUD 상성 화살표 2개 — 512 x 256, 순수 마젠타(#FF00FF) 배경, 왼쪽 칸(0~256)에 ▲, 오른쪽 칸(256~512)에 ▼.
기준 그림(모양 · 도트 크기 · 검은 외곽선 · 입체감을 그대로): Projects/AVSR/_exchange/in/_rps_arrows_orange.png
- 왼쪽 ▲ 주황: 기준 그림 그대로 다시 그린다(같은 모양 · 같은 주황).
- 오른쪽 ▼: 밋밋한 회색이 「꺼진 버튼」처럼 보였다(PD). **은빛 강철**로 — 차갑고 살짝 푸른 은색(대략 RGB 170,182,198) 바탕, 밝은 면엔 거의 흰 하이라이트, 어두운 면은 푸른 강철 그림자, 왼쪽 위에 작은 하얀 반짝임 한 점(쇠에 빛이 튀는 느낌). 검은 외곽선 유지.
- 피격 시안 7차(Projects/AVSR/_exchange/in/mock_hitfx_v7.png)의 「불리」 ▼ · 숫자 24 의 은빛과 같은 색이어야 한다.
- 픽셀아트, 두 화살표 크기 · 위치는 기준 그림과 같게. 마젠타 배경에 그림자 · 번짐 금지.
[저장] Projects/AVSR/_exchange/in/_rps_arrows_silver.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
