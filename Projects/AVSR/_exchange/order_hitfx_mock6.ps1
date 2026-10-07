$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
PD에게 보여 줄 **피격 이펙트 시안 6차**(완성된 게임 화면 한 장, 720 x 1280 세로).
설계서: Projects/AVSR/_exchange/spec_hitfx_v3.md — **「4. 시안 발주문」 절을 그대로 따른다**(화면 구성 · 네 적에 그릴 것 · 유리/불리 필수 비교 구도 · 절대 금지 체크).
지금 게임 화면(구도 · HUD · 캐릭터 크기 · 배경을 이것에 맞춘다): Projects/AVSR/_exchange/affinity_stock_ingame.png
반려된 지난 시안(이렇게 하지 말 것): Projects/AVSR/_exchange/in/mock_hitfx_v5.png — 유리 · 불리가 둘 다 「흰 원 + 큰 호」라 구별이 안 됐다.

꼭 지킬 것(요약):
- 의미 = 색: 보통 흰색 · 치명 금빛 노랑 · 유리 **주황**(+ 주황 ▲ · 주황 숫자) · 불리 **회색**(+ 회색 ▼ · 작은 회색 숫자). 속성색(빨강 · 청록 · 보라) 금지.
- 유리: 적 앞(플레이어 쪽)에 작은 입구 → 가는 주황 빛이 몸을 관통 → **적 등 뒤(화면 위쪽)로 넓어지는 주황 원뿔 폭발**, 주황 파편 전부 위쪽으로. 적은 살짝 위로 밀림.
- 불리: **적 앞면(화면 아래쪽 면)에 밝은 회색 사다리꼴 빛면이 번쩍**, 몸 안 · 등 뒤에는 빛 없음, 회색 알갱이 · 짧은 빛꼬리가 **플레이어 쪽(화면 아래)으로 되튕김**. 적 자세 · 위치 그대로.
- 보통: 작은 흰 코어 · 얇은 고리 · 미세 파편. 치명: 금빛 충격 고리 2겹 · 짧은 추상 광선 · 파편, 가장 크고 밝음.
- 무기 모양 금지: 표창 · 별 · 칼 자국 · X 베기 · 총구 화염 · 탄피 · 탄두 · 방패 그림.
[저장] Projects/AVSR/_exchange/in/mock_hitfx_v6.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
