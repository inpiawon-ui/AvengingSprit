param([string]$Which)
$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$common = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 색만 돌려 때우지 마라. 한 장만 그린다.
[배경] 세로 모바일 액션 게임의 상성 표시. 지금은 유리 = 초록 위 화살표, 불리 = 빨강 아래 화살표다.
PD 지시: **주식처럼** 바꾼다 — 유리(오름) = 빨강 ▲, 불리(내림) = 파랑 ▼.
기존 그림의 모양 · 도트 결 · 외곽선 · 크기 · 구도는 그대로 두고 **색만** 바꿔 다시 그린다.
배경은 **완전히 평평한 순수 마젠타 한 색**(그림자 · 빛 번짐 없이) — 나중에 빼서 투명하게 만든다. 그림 안에 마젠타를 쓰지 않는다. 글자는 넣지 않는다.
'@
switch ($Which) {
  'arrows' { $body = @'
[참고] 지금 그림: Assets/BaseResource/InGameMainUI/rps_up.png (초록 위 화살표) · Assets/BaseResource/InGameMainUI/rps_down.png (빨강 아래 화살표)
[그릴 것] 캔버스 512 x 256 — 256 x 256 칸 2개, 가로 한 줄. 칸 사이에 선을 긋지 않는다. 각 칸의 그림은 칸 가운데, 칸의 80% 크기.
  1) 위 화살표 — 빨강(밝은 면 #FF5A4A, 어두운 면 #C81E1E, 가장 밝은 점 #FFC2B8), 검은 외곽선
  2) 아래 화살표 — 파랑(밝은 면 #4FA8FF, 어두운 면 #1E5FD0, 가장 밝은 점 #C8E4FF), 검은 외곽선
[저장] Projects/AVSR/_exchange/in/_rps_arrows_stock.png
'@ }
  'tri_force' { $body = @'
[참고] 지금 그림: Assets/BaseResource/InGameMainUI/rps_tri_force.png (파워 주먹이 빛나는 삼각 상성판)
지금 화살표 색: 주먹 → 칼/권총(내가 이긴다) = 초록, 마법 구슬 → 주먹(나를 이긴다) = 빨강, 칼/권총 → 마법 구슬(나와 상관없음) = 짙은 남색.
[그릴 것] 같은 그림 · 같은 구도 · 같은 캔버스 비율(480 x 440 의 2배인 960 x 880)로, 화살표 색만 바꾼다:
  - 내가 이기는 화살표(주먹 → 칼/권총) = **빨강**(#FF4A3A 계열, 밝은 면 #FF8A70)
  - 나를 이기는 화살표(마법 구슬 → 주먹) = **파랑**(#3D8BFF 계열, 밝은 면 #8CC4FF)
  - 나와 상관없는 화살표(칼/권총 → 마법 구슬) = **차분한 회색**(#8A8F98, 어두운 면 #5A5F68) — 파랑과 헷갈리지 않게
  - 주먹 · 칼/권총 · 마법 구슬 아이콘과 주먹 뒤 빛살은 그대로
[저장] Projects/AVSR/_exchange/ref/affinity3/rps_tri_force_stock.png
'@ }
  'tri_blade' { $body = @'
[참고] 지금 그림: Assets/BaseResource/InGameMainUI/rps_tri_blade.png (무기 칼/권총이 빛나는 삼각 상성판)
화살표 규칙: 파워(주먹) → 무기(칼/권총) → 마법(구슬) → 파워. 이 그림의 주인공은 무기(칼/권총)다.
[그릴 것] 같은 그림 · 같은 구도 · 같은 캔버스 비율(480 x 440 의 2배인 960 x 880)로, 화살표 색만 바꾼다:
  - 내가 이기는 화살표(칼/권총 → 마법 구슬) = **빨강**(#FF4A3A 계열, 밝은 면 #FF8A70)
  - 나를 이기는 화살표(주먹 → 칼/권총) = **파랑**(#3D8BFF 계열, 밝은 면 #8CC4FF)
  - 나와 상관없는 화살표(마법 구슬 → 주먹) = **차분한 회색**(#8A8F98, 어두운 면 #5A5F68)
  - 아이콘과 칼/권총 뒤 빛살은 그대로
[저장] Projects/AVSR/_exchange/ref/affinity3/rps_tri_blade_stock.png
'@ }
  'tri_magic' { $body = @'
[참고] 지금 그림: Assets/BaseResource/InGameMainUI/rps_tri_magic.png (마법 구슬이 빛나는 삼각 상성판)
화살표 규칙: 파워(주먹) → 무기(칼/권총) → 마법(구슬) → 파워. 이 그림의 주인공은 마법(구슬)이다.
[그릴 것] 같은 그림 · 같은 구도 · 같은 캔버스 비율(480 x 440 의 2배인 960 x 880)로, 화살표 색만 바꾼다:
  - 내가 이기는 화살표(마법 구슬 → 주먹) = **빨강**(#FF4A3A 계열, 밝은 면 #FF8A70)
  - 나를 이기는 화살표(칼/권총 → 마법 구슬) = **파랑**(#3D8BFF 계열, 밝은 면 #8CC4FF)
  - 나와 상관없는 화살표(주먹 → 칼/권총) = **차분한 회색**(#8A8F98, 어두운 면 #5A5F68)
  - 아이콘과 구슬 뒤 빛살은 그대로
[저장] Projects/AVSR/_exchange/ref/affinity3/rps_tri_magic_stock.png
'@ }
}
codex exec --sandbox workspace-write --skip-git-repo-check ($common + "`n" + $body)
