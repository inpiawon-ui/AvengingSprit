$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 글꼴을 렌더링해 때우지 마라 — 이미지 도구가 막히면 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 탑다운) **피격 데미지 숫자 글꼴** 한 벌. 목표 느낌: 던전앤파이터의 데미지 숫자 — 굵고 납작한 덩어리 숫자가 살짝 오른쪽으로 기울고(이탤릭), 아주 두꺼운 어두운 외곽선(2px) 안에 위가 밝고 아래로 갈수록 살짝 어두워지는 세로 그라데이션 면, 윗부분에 한 줄 하이라이트.
게임이 숫자마다 색을 곱해서 쓴다(흰 · 금 · 주황 · 빨강 · 은). 그래서 면은 **색 없이 흰색 → 밝은 회색 그라데이션**으로 그린다(외곽선만 짙은 남색 · 검정).
[만들 것] 숫자 0 1 2 3 4 5 6 7 8 9 와 기호 + - 를 한 줄로(12칸). 각 칸 32x40, 칸 가운데 정렬, 숫자끼리 굵기 · 높이 · 기울기가 같게, 도트 결이 또렷하게(흐림 · 안티에일리어싱 번짐 금지). 12칸 가로 한 줄(384x40). **투명 바탕(알파)** — 안 되면 분홍 #FF00FF 단색.
[저장] Projects/AVSR/_exchange/in/part_dmg_digits.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
